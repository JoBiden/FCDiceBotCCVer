using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.StatusEffectContributors
{
    /// <summary>
    /// Surfaces a profile's active vice addictions in subsequent interactions: rolls cravings
    /// scaled by AddictionLevel, and emits satisfaction fragments when the parent interaction
    /// happens to deliver the matching vice (<c>!feed</c> of the matching substance,
    /// <c>!odorize</c> of the matching scent, or the escalating <c>!dose</c> itself).
    ///
    /// SymmetricInvocation is true — cravings are not attached to a single party. Both the
    /// initiator's and recipient's vice rolls surface in every consent-driven interaction,
    /// because the recipient of one kiss is the initiator of the next.
    ///
    /// Cravings DO NOT mutate AddictionLevel — only <c>!dose</c> escalates, and only
    /// <c>!detox</c> removes. The contributor is a pure read of the profile's
    /// <see cref="ViceInstance"/> list plus a probability roll.
    ///
    /// <c>!climaxfor</c> / <c>!climax</c> do not trigger satisfaction here — they auto-dose
    /// the partner via <see cref="DoseProcessor.IntensifyExistingVices"/> instead, which is a
    /// separate effect handled by the climax processor.
    /// </summary>
    public class DoseStatusContributor : IStatusEffectContributor
    {
        /// <summary>Per-level chance of a craving fragment firing on a single call. Linear:
        /// AddictionLevel 1 → 10%, AddictionLevel 10 → 100%.</summary>
        public const double CravingProbabilityPerLevel = 0.10;

        private static readonly string[] CravingTemplates = new[]
        {
            "{subject} has a sudden craving for {vice}.",
            "{subject} looks like they're distracted, daydreaming about {vice}.",
            "Some {vice} would really hit the spot about now, right {subject}?",
        };

        private const string SatisfactionTemplate = "{subject} gets the {vice} they've been craving.";

        private readonly Random _rng;
        private readonly IChateauDatabase _database;

        public DoseStatusContributor() : this(null, new Random()) { }
        public DoseStatusContributor(IChateauDatabase database) : this(database, new Random()) { }
        public DoseStatusContributor(Random rng) : this(null, rng) { }
        public DoseStatusContributor(IChateauDatabase database, Random rng)
        {
            _database = database;
            _rng = rng ?? new Random();
        }

        public bool SymmetricInvocation => true;

        public StatusEffectFragments Contribute(
            Profile profile,
            StatusEffectCallSite callSite,
            string interactionType,
            string parentIdentifier,
            bool isInitiator)
        {
            var result = new StatusEffectFragments();
            if (profile == null) return result;

            // Consent-phase contributions could spoil the consent prompt; per spec the
            // craving / satisfaction text shows up in completion only.
            if (callSite != StatusEffectCallSite.Completion) return result;

            var vices = ViceInstance.LoadAll(profile);
            if (vices.Count == 0) return result;

            string subjectName = string.IsNullOrEmpty(profile.displayName) ? profile.userName : profile.displayName;

            foreach (var vice in vices)
            {
                if (vice.AddictionLevel <= 0) continue;

                string vicePhrase = RenderVicePhrase(vice);

                if (IsSatisfiedBy(interactionType, parentIdentifier, vice.Vice, isInitiator))
                {
                    result.CompletionAppendix.Add(SatisfactionTemplate
                        .Replace("{subject}", subjectName)
                        .Replace("{vice}", vicePhrase));
                    continue;
                }

                double chance = vice.AddictionLevel * CravingProbabilityPerLevel;
                if (chance > 1.0) chance = 1.0;
                if (_rng.NextDouble() >= chance) continue;

                string template = CravingTemplates[_rng.Next(CravingTemplates.Length)];
                string fragment = template
                    .Replace("{subject}", subjectName)
                    .Replace("{vice}", vicePhrase);
                result.CompletionAppendix.Add(fragment);
            }

            return result;
        }

        private string RenderVicePhrase(ViceInstance vice)
        {
            Identifier identifier = _database?.GetIdentifier(vice.Vice);
            return ViceText.ViceName(identifier, vice.Vice, vice.DosedBy);
        }

        /// <summary>
        /// Returns true when the parent interaction inherently delivers the named vice to
        /// <b>this</b> party, and should therefore quiet their craving instead of triggering one.
        ///
        /// <list type="bullet">
        /// <item><description><c>!feed</c> — the fed party only. The one doing the feeding is
        /// handling the substance, not swallowing it.</description></item>
        /// <item><description><c>!drinkfrom</c> — the initiator, who asked to drink.</description></item>
        /// <item><description><c>!forcedrink</c> — the recipient, who was offered the drink.</description></item>
        /// <item><description><c>!drink</c> — the drinker. A self-command with one party, so it
        /// arrives as the initiator; the only satisfaction route needing no second
        /// resident.</description></item>
        /// <item><description><c>!odorize</c> / <c>!dose</c> — either party. These apply the
        /// substance to the air and the body rather than feeding it to one mouth, so there is no
        /// single consumer to single out.</description></item>
        /// </list>
        ///
        /// All comparisons are case-insensitive. Climax interactions intentionally don't
        /// match — they dose the partner instead of satisfying anyone.
        ///
        /// <para>
        /// <b>Why the consumer side matters.</b> This contributor is
        /// <see cref="SymmetricInvocation"/>, so on an interaction it runs against both parties.
        /// Without the role check, feeding Bob a musk he is hooked on would report the feeder as
        /// satisfied too, having eaten nothing. The non-consuming party still runs the ordinary
        /// craving roll below, which is the point: watching someone else get what you crave is
        /// exactly when the craving line should fire.
        /// </para>
        ///
        /// <para>
        /// <paramref name="parentInteractionType"/> is the <b>typed verb</b>, not the processor's
        /// registered type — <see cref="Involved.SourceDrinkProcessor"/> overrides
        /// <c>ContributorInteractionType</c> so <c>forcedrink</c> arrives distinct from
        /// <c>drinkfrom</c>. Their consumer sits on opposite sides.
        /// </para>
        /// </summary>
        public static bool IsSatisfiedBy(
            string parentInteractionType, string parentIdentifier, string viceName, bool isInitiator)
        {
            if (string.IsNullOrEmpty(parentIdentifier) || string.IsNullOrEmpty(viceName)) return false;
            if (!string.Equals(parentIdentifier, viceName, StringComparison.OrdinalIgnoreCase)) return false;

            // One party consumes; the other is present for it.
            if (string.Equals(parentInteractionType, "feed", StringComparison.OrdinalIgnoreCase))
            {
                return !isInitiator;
            }
            if (string.Equals(parentInteractionType, DrinkFromType, StringComparison.OrdinalIgnoreCase))
            {
                return isInitiator;
            }
            if (string.Equals(parentInteractionType, ForceDrinkType, StringComparison.OrdinalIgnoreCase))
            {
                return !isInitiator;
            }
            if (string.Equals(parentInteractionType, DrinkType, StringComparison.OrdinalIgnoreCase))
            {
                return isInitiator;
            }

            // Applied rather than fed: no single consumer to single out.
            if (string.Equals(parentInteractionType, "odorize", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(parentInteractionType, DoseProcessor.DoseType, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Interaction-type key <c>!drink</c> passes when it invokes the contributors through
        /// <see cref="SelfCommandStatusEffects"/>. Lives here rather than on the command so the
        /// satisfaction rule and its trigger can't drift apart.
        /// </summary>
        public const string DrinkType = "drink";

        /// <summary>
        /// The two source-drink verbs, kept distinct here even though they are one act
        /// everywhere else: the party who swallows is the initiator on one and the recipient on
        /// the other, and this is the rule that has to tell them apart.
        /// </summary>
        public const string DrinkFromType = "drinkfrom";
        public const string ForceDrinkType = "forcedrink";
    }
}
