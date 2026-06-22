using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the <c>!curse</c> interaction. Applies one of the curses from
    /// <see cref="CatalogMap"/> to the recipient; the curse then surfaces through
    /// <see cref="StatusEffectContributors.CurseStatusContributor"/> on subsequent
    /// interactions (disablers emit validation blockers; modifiers emit completion
    /// fragments). The recipient removes a curse with <c>!cleanse</c> at the per-curse
    /// cost in <see cref="CatalogMap"/>.
    ///
    /// Curses live as Identifiers tagged <c>categories: ["curse"]</c> — the Identifier
    /// catalog is the source of truth for "what curses exist," while the static
    /// <see cref="CatalogMap"/> is the source of truth for "what each curse does." A
    /// curse identifier with no entry in the catalog is treated as inert and rejected
    /// by validation.
    ///
    /// Cooldown is per-(initiator, recipient, curseName) on the initiator's timers,
    /// mirroring <see cref="InfestProcessor"/>: a caster can spread different curses
    /// to different victims freely, but can't re-apply the same (target, curse) tuple
    /// for a week.
    /// </summary>
    public class CurseProcessor : InteractionProcessorBase
    {
        public const string CurseType = "curse";
        public const string CurseCategory = "curse";

        /// <summary>
        /// Cooldown the initiator's (recipient, curse) pair sits under after a successful
        /// curse, matching the consequence-tier default used by dose / infest / odorize.
        /// </summary>
        public static readonly TimeSpan CurseCooldown = TimeSpan.FromDays(7);

        public enum CurseBucket
        {
            Disabler,
            Modifier,
        }

        /// <summary>
        /// Which party of a parent interaction is blocked when the cursed profile is on
        /// that side. <see cref="Both"/> means whichever side carries the curse blocks.
        /// </summary>
        public enum BlockSide
        {
            Initiator,
            Recipient,
            Both,
        }

        /// <summary>
        /// Per-curse spec. Disablers populate <see cref="BlockedInteractions"/>;
        /// modifiers populate <see cref="ModifierTemplate"/>. <see cref="CleanseCost"/>
        /// drives <c>!cleanse</c>; per the design decision, no curse cleanses to
        /// <see cref="PurgeCostType.RandomCurse"/> (avoiding a cleanse-spiral).
        /// </summary>
        public class CurseSpec
        {
            public CurseBucket Bucket;
            // For disablers: each entry maps an interaction type to which side(s) of that
            // interaction the cursed party is blocked from filling. Empty for modifiers.
            public Dictionary<string, BlockSide> BlockedInteractions = new Dictionary<string, BlockSide>(StringComparer.OrdinalIgnoreCase);
            // For modifiers: the completion-fragment template. {subject} is replaced by
            // the cursed party's display name. Empty for disablers.
            public string ModifierTemplate = string.Empty;
            // Cost charged by !cleanse. Never RandomCurse — that would let cleansing
            // recursively apply another curse.
            public PurgeCostType CleanseCost;
        }

        /// <summary>
        /// Source of truth for every curse's bucket, blocked interactions, modifier
        /// template, and cleanse cost. Keyed by curse identifier name (lowercase).
        /// Adding a new curse means an entry here AND a row in
        /// <c>IdentifiersSnapshot.json</c> with <c>categories: ["curse"]</c>.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, CurseSpec> CatalogMap =
            new Dictionary<string, CurseSpec>(StringComparer.OrdinalIgnoreCase)
            {
                // ----- Disablers -----
                ["meekness"] = new CurseSpec
                {
                    Bucket = CurseBucket.Disabler,
                    BlockedInteractions = new Dictionary<string, BlockSide>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bully"] = BlockSide.Initiator,
                    },
                    CleanseCost = PurgeCostType.MissedWork,
                },
                ["chastity"] = new CurseSpec
                {
                    Bucket = CurseBucket.Disabler,
                    BlockedInteractions = new Dictionary<string, BlockSide>(StringComparer.OrdinalIgnoreCase)
                    {
                        // !climax is initiator-climaxer; !climaxfor is recipient-climaxer.
                        // Either way the climaxer slot is blocked.
                        ["climax"] = BlockSide.Initiator,
                        ["climaxfor"] = BlockSide.Recipient,
                    },
                    CleanseCost = PurgeCostType.MissedWork,
                },
                ["cooties"] = new CurseSpec
                {
                    Bucket = CurseBucket.Disabler,
                    BlockedInteractions = new Dictionary<string, BlockSide>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["kiss"] = BlockSide.Both,
                        ["cuddle"] = BlockSide.Both,
                        ["handhold"] = BlockSide.Both,
                    },
                    CleanseCost = PurgeCostType.MissedWork,
                },
                ["costume"] = new CurseSpec
                {
                    Bucket = CurseBucket.Disabler,
                    BlockedInteractions = new Dictionary<string, BlockSide>(StringComparer.OrdinalIgnoreCase)
                    {
                        // Both sides — neither dressing nor being dressed touches the curse.
                        ["dressup"] = BlockSide.Both,
                    },
                    CleanseCost = PurgeCostType.MissedWork,
                },
                ["poverty"] = new CurseSpec
                {
                    // Poverty's effect lives in ChateauWork/ChateauVolunteer (zeros the
                    // currency reward); no ValidateInteraction blockers from the contributor.
                    Bucket = CurseBucket.Disabler,
                    CleanseCost = PurgeCostType.MissedWork,
                },
                ["laziness"] = new CurseSpec
                {
                    Bucket = CurseBucket.Disabler,
                    BlockedInteractions = new Dictionary<string, BlockSide>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["train"] = BlockSide.Recipient,
                    },
                    CleanseCost = PurgeCostType.LostTrainingPoint,
                },
                ["hunger"] = new CurseSpec
                {
                    Bucket = CurseBucket.Disabler,
                    BlockedInteractions = new Dictionary<string, BlockSide>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["feed"] = BlockSide.Recipient,
                    },
                    CleanseCost = PurgeCostType.RandomBreak,
                },
                ["greed"] = new CurseSpec
                {
                    Bucket = CurseBucket.Disabler,
                    BlockedInteractions = new Dictionary<string, BlockSide>(StringComparer.OrdinalIgnoreCase)
                    {
                        // !pay maps to paymentGive when the amount is positive; greed gates
                        // *giving away* currency, so we hook the give side only.
                        ["paymentGive"] = BlockSide.Initiator,
                    },
                    CleanseCost = PurgeCostType.LostTrainingPoint,
                },
                ["antisocial"] = new CurseSpec
                {
                    Bucket = CurseBucket.Disabler,
                    BlockedInteractions = new Dictionary<string, BlockSide>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bond"] = BlockSide.Both,
                    },
                    CleanseCost = PurgeCostType.MissedWork,
                },

                // ----- Modifiers -----
                ["mooing"] = new CurseSpec
                {
                    Bucket = CurseBucket.Modifier,
                    ModifierTemplate = "Moooo says {subject}.",
                    CleanseCost = PurgeCostType.MissedWork,
                },
                ["tsundere"] = new CurseSpec
                {
                    Bucket = CurseBucket.Modifier,
                    ModifierTemplate = "N-not like {subject} likes you or anything, baka!!",
                    CleanseCost = PurgeCostType.MissedWork,
                },
                ["blushing"] = new CurseSpec
                {
                    Bucket = CurseBucket.Modifier,
                    ModifierTemplate = "{subject} blushes brighter than the sun.",
                    CleanseCost = PurgeCostType.MissedWork,
                },
                ["horny"] = new CurseSpec
                {
                    Bucket = CurseBucket.Modifier,
                    ModifierTemplate = "This only makes {subject} even more aroused.",
                    CleanseCost = PurgeCostType.RandomBreak,
                },
                ["bimbo"] = new CurseSpec
                {
                    Bucket = CurseBucket.Modifier,
                    ModifierTemplate = "Liiike, totally, riiiight {subject}?",
                    CleanseCost = PurgeCostType.LostTrainingPoint,
                },
                ["vibrating"] = new CurseSpec
                {
                    Bucket = CurseBucket.Modifier,
                    ModifierTemplate = "{subject} is literally buzzing as they vibrate excitedly.",
                    CleanseCost = PurgeCostType.RandomBreak,
                },
            };

        public override string InteractionType => CurseType;
        public override string InvestmentLevel => "consequence";

        public CurseProcessor(IChateauDatabase database) : base(database) { }
        public CurseProcessor() : base() { }

        public static string CooldownTimerKey(string curseName, string recipientUserName)
        {
            return "curse_" + curseName + "_" + recipientUserName;
        }

        /// <summary>
        /// Forward-looking phrase for the cleanse cost. Mirrors
        /// <see cref="InfestProcessor.CostPhrase"/>'s tone, used in the !curse consent
        /// warning so the recipient sees what !cleanse will cost before agreeing.
        /// </summary>
        public static string CleanseCostPhrase(PurgeCostType cost)
        {
            switch (cost)
            {
                case PurgeCostType.MissedWork:
                    return "missing a day of work";
                case PurgeCostType.RandomBreak:
                    return "a broken body part for one to three days";
                case PurgeCostType.LostTrainingPoint:
                    return "a point of training in a random skill";
                default:
                    return "an unknown cost";
            }
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            // Skip the base class's status-effect block check for !curse itself — being
            // already-cursed shouldn't prevent a NEW curse. (Disablers don't block !curse
            // in CatalogMap, but skipping the gate also keeps modifier flavor noise out of
            // the consent-warning route for the parent !curse interaction.)
            Profile initiatorProfile = Database.GetProfile(initiator);
            Profile recipientProfile = Database.GetProfile(recipient);

            if (initiatorProfile == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(initiator));
            }
            if (recipientProfile == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(recipient));
            }

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText(CurseCategory));
            }

            Identifier curseIdentifier = Database.GetIdentifier(identifier);
            if (curseIdentifier == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(identifier));
            }
            if (curseIdentifier.categories == null
                || !curseIdentifier.categories.Contains(CurseCategory, StringComparer.OrdinalIgnoreCase))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText(CurseCategory));
            }
            if (!CatalogMap.ContainsKey(identifier))
            {
                // Identifier exists but the catalog has no spec for it — the curse can't
                // actually do anything, so we refuse rather than silently apply an inert
                // entry. (Should only fire if someone seeds an identifier without a
                // matching CatalogMap entry.)
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(identifier));
            }

            var existing = CurseInstance.LoadAll(recipientProfile);
            if (existing.Any(c => string.Equals(c.Curse, identifier, StringComparison.OrdinalIgnoreCase)))
            {
                string recipientName = string.IsNullOrEmpty(recipientProfile.displayName)
                    ? recipient
                    : recipientProfile.displayName;
                return ValidationResult.Failure(recipientName + " is already burdened by the [b]"
                    + identifier + "[/b] curse. Try a different one, or wait for them to !cleanse.");
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string curseName = command.pendingInteraction.identifier;

            Database.AddInteraction(command.pendingInteraction);

            bool isSelfCurse = string.Equals(initiator, recipient, StringComparison.Ordinal);
            Profile recipientProfile = Database.GetProfile(recipient);
            Profile initiatorProfile = isSelfCurse ? recipientProfile : Database.GetProfile(initiator);

            string casterName = string.IsNullOrEmpty(initiatorProfile?.displayName)
                ? initiator
                : initiatorProfile.displayName;
            ApplyCurse(recipientProfile, curseName, casterName);

            // Costume passive: if the recipient is undressed at apply time, lock them in
            // a random outfit. Done here in the processor (not the contributor) because
            // it's a one-time mutation at apply time, not a per-interaction surface.
            if (string.Equals(curseName, "costume", StringComparison.OrdinalIgnoreCase))
            {
                ApplyCostumePassive(recipientProfile);
            }

            DateTime now = DateTime.UtcNow;
            if (initiatorProfile.timers == null)
            {
                initiatorProfile.timers = new Dictionary<string, CoolDown>();
            }
            initiatorProfile.timers[CooldownTimerKey(curseName, recipient)] = new CoolDown
            {
                timerStart = now,
                timerEnd = now.Add(CurseCooldown),
            };

            Database.SetProfile(recipient, recipientProfile);
            if (!isSelfCurse)
            {
                Database.SetProfile(initiator, initiatorProfile);
            }

            Database.DeletePendingCommand(command.Id);

            return CurseType;
        }

        /// <summary>
        /// Add a curse to a profile. New curse → fresh CurseInstance. Same curse already
        /// present → no-op (the validator already rejected on direct curse; the random-
        /// curse cost applier also checks before calling). Mutates the profile but does
        /// not save — callers own persistence.
        /// </summary>
        public static void ApplyCurse(Profile recipientProfile, string curseName, string casterName)
        {
            if (recipientProfile == null || string.IsNullOrEmpty(curseName)) return;

            var entries = CurseInstance.LoadAll(recipientProfile);
            if (entries.Any(c => string.Equals(c.Curse, curseName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            entries.Add(new CurseInstance
            {
                Curse = curseName.ToLowerInvariant(),
                AppliedBy = casterName,
                AppliedAt = DateTime.UtcNow,
            });

            CurseInstance.SaveAll(recipientProfile, entries);
        }

        /// <summary>
        /// Costume passive: if the recipient has no attire characteristic (or an empty
        /// one), pick a random non-"nude" attire identifier from the catalog and lock
        /// it in. Idempotent — calling on an already-dressed recipient is a no-op. The
        /// pick excludes "nude" because the curse is "stuck in a costume," not "stuck
        /// without one"; if no non-nude attire is available, the passive is skipped.
        /// </summary>
        internal void ApplyCostumePassive(Profile recipientProfile)
        {
            ApplyCostumePassive(recipientProfile, new Random());
        }

        /// <summary>Test seam: caller supplies the rng used to pick the random outfit.</summary>
        internal void ApplyCostumePassive(Profile recipientProfile, Random rng)
        {
            if (recipientProfile == null) return;
            if (recipientProfile.characteristics == null)
            {
                recipientProfile.characteristics = new Dictionary<string, string>();
            }
            if (recipientProfile.characteristics.TryGetValue("attire", out var existing)
                && !string.IsNullOrEmpty(existing))
            {
                return;
            }

            List<Identifier> attires = Database?.GetIdentifiersByCategory("attire") ?? new List<Identifier>();
            var pickable = attires
                .Where(a => !string.Equals(a.type, "nude", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (pickable.Count == 0) return;

            var picked = pickable[rng.Next(pickable.Count)];
            recipientProfile.characteristics["attire"] = picked.type;
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string subjectName = string.IsNullOrEmpty(recipientProfile?.displayName)
                ? recipientProfile?.userName
                : recipientProfile.displayName;
            string casterName = string.IsNullOrEmpty(initiatorProfile?.displayName)
                ? initiatorProfile?.userName
                : initiatorProfile.displayName;

            return casterName + " has cursed " + subjectName + " with [b]" + identifier + "[/b]! "
                + "Surely nothing ill will come of this.";
        }

        public override CooldownSpec CooldownRule => Cooldown;

        public static readonly CooldownSpec Cooldown = new CooldownSpec
        {
            Kind = CooldownKind.Cooldown,
            Binds = CooldownBinds.Initiator,
            PeriodDays = 7,
            Scope = "curse"
        };

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string casterName = string.IsNullOrEmpty(initiatorProfile?.displayName)
                ? initiatorProfile?.userName
                : initiatorProfile.displayName;
            string subjectName = string.IsNullOrEmpty(recipientProfile?.displayName)
                ? recipientProfile?.userName
                : recipientProfile.displayName;

            // Identifier description carries the "what you'll miss out on" phrasing — no
            // bucket vocabulary leaks out to players; the disabler/modifier distinction is
            // an implementation detail.
            string effectDescription = "an unknown effect.";
            Identifier curseIdentifier = Database?.GetIdentifier(identifier);
            if (curseIdentifier != null && !string.IsNullOrEmpty(curseIdentifier.description))
            {
                effectDescription = curseIdentifier.description;
            }

            string cleanseCostPhrase = "an unknown cost";
            if (CatalogMap.TryGetValue(identifier ?? string.Empty, out var spec))
            {
                cleanseCostPhrase = CleanseCostPhrase(spec.CleanseCost);
            }

            string seriousness = ConsentWarningText.Block(
                ConsentWarningText.FrequencyPerAxis(casterName, "afflict you with a given curse", Cooldown.PeriodDays),
                "To remove this curse, you'll need to !cleanse at the cost of " + cleanseCostPhrase + ".");
            string baseWarning = casterName + " wants to curse " + subjectName
                + " with [b]" + identifier + "[/b]! "
                + effectDescription + " " + seriousness + " "
                + "Do you !consent to this curse?";

            var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Consent, identifier, isInitiator: false);
            return AppendStatusFragments(baseWarning, effects.ConsentWarnings);
        }
    }
}
