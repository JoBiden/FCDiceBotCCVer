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

                if (IsSatisfiedBy(interactionType, parentIdentifier, vice.Vice))
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
        /// Returns true when the parent interaction inherently delivers the named vice and
        /// should therefore quiet the craving instead of triggering one. The three matches:
        /// <list type="bullet">
        /// <item><description><c>!feed</c> where the substance identifier == vice name.</description></item>
        /// <item><description><c>!odorize</c> where the scent identifier == vice name.</description></item>
        /// <item><description><c>!dose</c> where the vice identifier == vice name (the escalating dose itself).</description></item>
        /// </list>
        /// All comparisons are case-insensitive. Climax interactions intentionally don't
        /// match — they dose the partner instead of satisfying anyone.
        /// </summary>
        public static bool IsSatisfiedBy(string parentInteractionType, string parentIdentifier, string viceName)
        {
            if (string.IsNullOrEmpty(parentIdentifier) || string.IsNullOrEmpty(viceName)) return false;
            if (!string.Equals(parentIdentifier, viceName, StringComparison.OrdinalIgnoreCase)) return false;

            if (string.Equals(parentInteractionType, "feed", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(parentInteractionType, "odorize", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(parentInteractionType, DoseProcessor.DoseType, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
