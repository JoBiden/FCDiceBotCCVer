using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the !infest interaction. Plants a parasite on the recipient that then
    /// has a chance to spread to subsequent non-casual partners via
    /// <see cref="StatusEffectContributors.ParasiteSpreadEffect"/>. The recipient (or any
    /// spread-to victim) clears the parasite with <c>!purge</c> at a parasite-specific cost
    /// — see <see cref="PurgeCostFor"/>.
    ///
    /// Cooldown is per-(initiator → recipient → parasite) tuple on the initiator's timers,
    /// mirroring <see cref="OdorizeProcessor"/> and <see cref="DoseProcessor"/>: an initiator
    /// can spread different parasites to different victims freely, but can't infest the same
    /// (target, parasite) combo twice in a week.
    /// </summary>
    public class InfestProcessor : InteractionProcessorBase
    {
        public const string InfestType = "infest";
        public const string ParasiteCategory = "parasite";

        /// <summary>Default cost used when a parasite identifier has no explicit mapping
        /// in <see cref="CostMap"/>. Bites at productivity; aligns with the spec's
        /// MissedWork default.</summary>
        public const PurgeCostType DefaultPurgeCost = PurgeCostType.MissedWork;

        /// <summary>
        /// How long a spread-acquired infestation can be purged for free. Direct-infest
        /// instances purge at full cost immediately and do not consult this window.
        /// </summary>
        public static readonly TimeSpan SpreadGracePeriod = TimeSpan.FromHours(24);

        /// <summary>
        /// Cooldown the initiator's (recipient, parasite) pair sits under after a successful
        /// infest, matching the consequence-tier default used by odorize and dose.
        /// </summary>
        public static readonly TimeSpan InfestCooldown = TimeSpan.FromDays(7);

        /// <summary>
        /// Per-parasite purge cost. Keys are identifier names (lowercase). Unmapped parasites
        /// fall back to <see cref="DefaultPurgeCost"/>. Mapping reflects the existing
        /// identifier catalog's flavor — see the !infest design notes for rationale.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, PurgeCostType> CostMap =
            new Dictionary<string, PurgeCostType>(StringComparer.OrdinalIgnoreCase)
            {
                { "paraslime", PurgeCostType.MissedWork },
                { "bimboslime", PurgeCostType.LostTrainingPoint },
                { "lustleeches", PurgeCostType.RandomCurse },
                { "love", PurgeCostType.MissedWork },
                { "tentacles", PurgeCostType.RandomBreak },
                { "nymites", PurgeCostType.MissedWork },
            };

        public override string InteractionType => InfestType;
        public override string InvestmentLevel => "consequence";

        public InfestProcessor(IChateauDatabase database) : base(database) { }
        public InfestProcessor() : base() { }

        public static string CooldownTimerKey(string parasite, string recipientUserName)
        {
            return "infest_" + parasite + "_" + recipientUserName;
        }

        /// <summary>
        /// Resolve a parasite identifier's purge cost. Unmapped parasites fall back to
        /// <see cref="DefaultPurgeCost"/> so user-added identifiers in the parasite category
        /// still cost something.
        /// </summary>
        public static PurgeCostType PurgeCostFor(string parasiteName)
        {
            if (string.IsNullOrEmpty(parasiteName)) return DefaultPurgeCost;
            return CostMap.TryGetValue(parasiteName, out var mapped) ? mapped : DefaultPurgeCost;
        }

        /// <summary>
        /// Human-readable phrase for the cost shown in the !infest consent warning and the
        /// !purge confirmation. Kept in sync with PurgeCostApplier's after-the-fact
        /// descriptions but written in forward-looking voice for the consent prompt.
        /// </summary>
        public static string CostPhrase(PurgeCostType cost)
        {
            switch (cost)
            {
                case PurgeCostType.MissedWork:
                    return "missing work to recover";
                case PurgeCostType.RandomBreak:
                    return "a broken body part for one to three days";
                case PurgeCostType.LostTrainingPoint:
                    return "a point of training in a random skill";
                case PurgeCostType.RandomCurse:
                    return "a curse from a witch's help";
                default:
                    return "an unknown cost";
            }
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid) return baseValidation;

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText(ParasiteCategory));
            }

            Identifier parasiteIdentifier = Database.GetIdentifier(identifier);
            if (parasiteIdentifier == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(identifier));
            }
            if (parasiteIdentifier.categories == null
                || !parasiteIdentifier.categories.Contains(ParasiteCategory, StringComparer.OrdinalIgnoreCase))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText(ParasiteCategory));
            }

            Profile recipientProfile = Database.GetProfile(recipient);
            var existing = ParasiteInstance.LoadAll(recipientProfile);
            if (existing.Any(p => string.Equals(p.Parasite, identifier, StringComparison.OrdinalIgnoreCase)))
            {
                string recipientName = string.IsNullOrEmpty(recipientProfile?.displayName)
                    ? recipient
                    : recipientProfile.displayName;
                return ValidationResult.Failure(recipientName + " is already a willing host of "
                    + ParasiteText.ParasiteName(identifier)
                    + "! No need to infest them again, but feel free to try with a different parasite.");
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string parasite = command.pendingInteraction.identifier;

            Database.AddInteraction(command.pendingInteraction);

            bool isSelfInfest = string.Equals(initiator, recipient, StringComparison.Ordinal);
            Profile recipientProfile = Database.GetProfile(recipient);
            Profile initiatorProfile = isSelfInfest ? recipientProfile : Database.GetProfile(initiator);

            string infesterName = string.IsNullOrEmpty(initiatorProfile?.displayName)
                ? initiator
                : initiatorProfile.displayName;
            ApplyInfestation(recipientProfile, parasite, infesterName,
                spreadFromContact: false, gracePeriod: TimeSpan.Zero);

            DateTime now = DateTime.UtcNow;
            if (initiatorProfile.timers == null)
            {
                initiatorProfile.timers = new Dictionary<string, CoolDown>();
            }
            initiatorProfile.timers[CooldownTimerKey(parasite, recipient)] = new CoolDown
            {
                timerStart = now,
                timerEnd = now.Add(InfestCooldown),
            };

            Database.SetProfile(recipient, recipientProfile);
            if (!isSelfInfest)
            {
                Database.SetProfile(initiator, initiatorProfile);
            }

            Database.DeletePendingCommand(command.Id);

            return InfestType;
        }

        /// <summary>
        /// Add a parasite to a profile. New parasite → fresh ParasiteInstance.
        /// Same parasite already present → no-op (the validator already rejected on direct
        /// infest; spread also checks before calling). Mutates the profile but does not
        /// save — callers own persistence.
        /// </summary>
        /// <param name="spreadFromContact">True when this came from spread, false for direct
        /// !infest. Only spread instances honor <paramref name="gracePeriod"/>.</param>
        /// <param name="gracePeriod">How long the recipient has to !purge for free. Ignored
        /// for direct infestations.</param>
        public static void ApplyInfestation(
            Profile recipientProfile,
            string parasite,
            string infesterName,
            bool spreadFromContact,
            TimeSpan gracePeriod)
        {
            if (recipientProfile == null || string.IsNullOrEmpty(parasite)) return;

            var entries = ParasiteInstance.LoadAll(recipientProfile);
            if (entries.Any(p => string.Equals(p.Parasite, parasite, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            entries.Add(new ParasiteInstance
            {
                Parasite = parasite.ToLowerInvariant(),
                InfestedBy = infesterName,
                InfestedAt = now,
                SpreadFromContact = spreadFromContact,
                GraceUntil = spreadFromContact ? now.Add(gracePeriod) : now,
            });

            ParasiteInstance.SaveAll(recipientProfile, entries);
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            // Two flavors of completion: when the initiator was already carrying the
            // parasite, the message frames the infest as a direct spread (rather than a
            // fresh introduction) so the player sees the continuity with their own
            // infestation.
            bool initiatorAlreadyCarried = false;
            if (initiatorProfile != null)
            {
                var initiatorParasites = ParasiteInstance.LoadAll(initiatorProfile);
                initiatorAlreadyCarried = initiatorParasites.Any(p =>
                    string.Equals(p.Parasite, identifier, StringComparison.OrdinalIgnoreCase));
            }

            string parasitePhrase = ParasiteText.ParasiteName(identifier);
            if (initiatorAlreadyCarried)
            {
                return initiatorProfile.displayName + " spreads their " + parasitePhrase + " directly to "
                    + recipientProfile.displayName + "! "
                    + "Enjoy your future as a host, and see if you can help it spread, just like "
                    + initiatorProfile.displayName + " has~";
            }

            string parasiteWithArticle = ParasiteText.ParasiteNameWithArticle(identifier);
            return initiatorProfile.displayName + " has infested " + recipientProfile.displayName
                + " with " + parasiteWithArticle + "! "
                + "Enjoy your future as a host, and see if you can help it spread~";
        }

        public override CooldownSpec CooldownRule => Cooldown;

        public static readonly CooldownSpec Cooldown = new CooldownSpec
        {
            Kind = CooldownKind.Cooldown,
            Binds = CooldownBinds.Initiator,
            PeriodDays = 7,
            Scope = "parasite"
        };

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string costPhrase = CostPhrase(PurgeCostFor(identifier));
            int graceHours = (int)SpreadGracePeriod.TotalHours;
            string parasiteWithArticle = ParasiteText.ParasiteNameWithArticle(identifier);

            string seriousness = ConsentWarningText.Block(
                ConsentWarningText.FrequencyPerAxis(initiatorProfile.displayName, "infest you with a given parasite", Cooldown.PeriodDays),
                "This parasite has a chance to spread to anyone you have a non-casual interaction with, "
                    + "and signs of your infection might be noticeable in any interaction. "
                    + "You can !purge at the cost of " + costPhrase + ", "
                    + "with a " + graceHours + "-hour grace period during which !purge has no cost.");
            string baseWarning = initiatorProfile.displayName + " wants to infest "
                + recipientProfile.displayName + " with " + parasiteWithArticle + "! " + seriousness + " "
                + "Do you !consent to being made into a new host?";
            var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Consent, identifier, isInitiator: false);
            return AppendStatusFragments(baseWarning, effects.ConsentWarnings);
        }
    }
}
