using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Commitment
{
    /// <summary>
    /// Processor for the breed interaction - implants the recipient with a new pregnancy
    /// of the specified monster type. Pregnancies gestate for a monster-defined number of
    /// days, then can be released via !birth (a self-action). Multiple concurrent
    /// pregnancies are supported.
    /// </summary>
    public class BreedProcessor : InteractionProcessorBase
    {
        public const int MaxGestationDays = 7;
        public const int DefaultGestationDays = 1;
        public const int DefaultBroodSize = 1;
        public const int RareTwinChanceDenominator = 100; // 1-in-100 → 1% chance

        // Single mutable RNG used for brood-size rolls. Tests can swap this out for a
        // seeded Random to make twin-chance and brood-range outcomes deterministic.
        // (Random isn't thread-safe; the bot processes one command at a time so that's
        // fine here.)
        internal static Random Rng = new Random();

        // Key-prefix helpers for MonsterStats so the "monster type" and "category"
        // namespaces can't collide.
        public static string MonsterStatsKey(string monsterType) => "monster:" + monsterType.ToLowerInvariant();
        public static string CategoryStatsKey(string category) => "category:" + category.ToLowerInvariant();

        internal struct CategoryDefault
        {
            public string Category;
            public int Priority;
            public int GestationDays;
            public int BroodSizeMin;
            public int BroodSizeMax;
        }

        // Priority-ordered lookup. Lower priority number wins when a monster has multiple
        // matching categories (e.g. Lamia: [monster, snake, beast, mount, carapace, poison]
        // resolves to snake). Categories not in this table contribute nothing.
        internal static readonly CategoryDefault[] CategoryDefaults = new[]
        {
            new CategoryDefault { Category = "insect",     Priority = 1, GestationDays = 2, BroodSizeMin = 5, BroodSizeMax = 15 },
            new CategoryDefault { Category = "worm",       Priority = 1, GestationDays = 2, BroodSizeMin = 6, BroodSizeMax = 20 },
            new CategoryDefault { Category = "arachne",    Priority = 1, GestationDays = 3, BroodSizeMin = 4, BroodSizeMax = 12 },
            new CategoryDefault { Category = "dragon",     Priority = 2, GestationDays = 7, BroodSizeMin = 1, BroodSizeMax = 1 },
            new CategoryDefault { Category = "giant",      Priority = 2, GestationDays = 7, BroodSizeMin = 1, BroodSizeMax = 1 },
            new CategoryDefault { Category = "slime",      Priority = 3, GestationDays = 2, BroodSizeMin = 2, BroodSizeMax = 5 },
            new CategoryDefault { Category = "alraune",    Priority = 3, GestationDays = 4, BroodSizeMin = 3, BroodSizeMax = 8 },
            new CategoryDefault { Category = "undead",     Priority = 4, GestationDays = 4, BroodSizeMin = 1, BroodSizeMax = 2 },
            new CategoryDefault { Category = "construct",  Priority = 4, GestationDays = 5, BroodSizeMin = 1, BroodSizeMax = 1 },
            new CategoryDefault { Category = "intangible", Priority = 4, GestationDays = 5, BroodSizeMin = 1, BroodSizeMax = 1 },
            new CategoryDefault { Category = "elemental",  Priority = 4, GestationDays = 3, BroodSizeMin = 1, BroodSizeMax = 2 },
            new CategoryDefault { Category = "fiery",      Priority = 4, GestationDays = 4, BroodSizeMin = 1, BroodSizeMax = 2 },
            new CategoryDefault { Category = "electric",   Priority = 4, GestationDays = 3, BroodSizeMin = 1, BroodSizeMax = 2 },
            new CategoryDefault { Category = "mimic",      Priority = 4, GestationDays = 3, BroodSizeMin = 1, BroodSizeMax = 3 },
            new CategoryDefault { Category = "angel",      Priority = 5, GestationDays = 5, BroodSizeMin = 1, BroodSizeMax = 1 },
            new CategoryDefault { Category = "holy",       Priority = 5, GestationDays = 5, BroodSizeMin = 1, BroodSizeMax = 1 },
            new CategoryDefault { Category = "corrupt",    Priority = 5, GestationDays = 4, BroodSizeMin = 1, BroodSizeMax = 3 },
            new CategoryDefault { Category = "infernal",   Priority = 5, GestationDays = 4, BroodSizeMin = 1, BroodSizeMax = 3 },
            new CategoryDefault { Category = "aquatic",    Priority = 6, GestationDays = 3, BroodSizeMin = 3, BroodSizeMax = 8 },
            new CategoryDefault { Category = "snake",      Priority = 6, GestationDays = 3, BroodSizeMin = 4, BroodSizeMax = 8 },
            new CategoryDefault { Category = "bird",       Priority = 6, GestationDays = 3, BroodSizeMin = 2, BroodSizeMax = 5 },
            new CategoryDefault { Category = "feathered",  Priority = 6, GestationDays = 3, BroodSizeMin = 2, BroodSizeMax = 5 },
            new CategoryDefault { Category = "flight",     Priority = 6, GestationDays = 3, BroodSizeMin = 2, BroodSizeMax = 5 },
            new CategoryDefault { Category = "nocturnal",  Priority = 6, GestationDays = 4, BroodSizeMin = 1, BroodSizeMax = 3 },
            new CategoryDefault { Category = "cow",        Priority = 7, GestationDays = 5, BroodSizeMin = 1, BroodSizeMax = 1 },
            new CategoryDefault { Category = "mount",      Priority = 7, GestationDays = 5, BroodSizeMin = 1, BroodSizeMax = 1 },
            new CategoryDefault { Category = "horned",     Priority = 8, GestationDays = 4, BroodSizeMin = 1, BroodSizeMax = 2 },
            new CategoryDefault { Category = "beast",      Priority = 9, GestationDays = 3, BroodSizeMin = 2, BroodSizeMax = 5 },
            new CategoryDefault { Category = "cat",        Priority = 9, GestationDays = 3, BroodSizeMin = 2, BroodSizeMax = 5 },
            new CategoryDefault { Category = "dog",        Priority = 9, GestationDays = 3, BroodSizeMin = 2, BroodSizeMax = 5 },
            new CategoryDefault { Category = "fox",        Priority = 9, GestationDays = 3, BroodSizeMin = 2, BroodSizeMax = 5 },
        };

        public override string InteractionType => "breed";
        public override string InvestmentLevel => "commitment";

        public BreedProcessor(IChateauDatabase database) : base(database)
        {
        }

        public BreedProcessor() : base()
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:
                    return "bred";
                case VerbTense.Present:
                    return "breeds";
                case VerbTense.Future:
                    return "will breed";
                default:
                    return "breed";
            }
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("monster"));
            }

            Identifier monsterIdentifier = Database.GetIdentifier(identifier);
            if (monsterIdentifier == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(identifier));
            }
            if (monsterIdentifier.categories == null
                || !monsterIdentifier.categories.Contains("monster", StringComparer.OrdinalIgnoreCase))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("monster"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string monsterType = command.pendingInteraction.identifier;

            Database.AddInteraction(command.pendingInteraction);

            Profile initiatorProfile = Database.GetProfile(initiator);
            Profile recipientProfile = Database.GetProfile(recipient);

            Identifier monsterIdentifier = Database.GetIdentifier(monsterType);
            ResolveGestationAndBrood(monsterIdentifier, Rng, out int gestationDays, out int broodSize, out bool isRareTwins);

            // Snapshot the monster's categories on the pregnancy so birth-time category
            // counters don't need to re-fetch the Identifier (which could have changed).
            List<string> categoriesSnapshot = monsterIdentifier?.categories != null
                ? new List<string>(monsterIdentifier.categories)
                : new List<string>();

            DateTime now = DateTime.UtcNow;
            Pregnancy pregnancy = new Pregnancy
            {
                Id = Guid.NewGuid().ToString("N"),
                Initiator = initiator,
                MonsterType = monsterType,
                ConceivedAt = now,
                ReadyAt = now.AddDays(gestationDays),
                BroodSize = broodSize,
                Categories = categoriesSnapshot,
                IsRareTwins = isRareTwins
            };

            if (recipientProfile.pregnancies == null)
            {
                recipientProfile.pregnancies = new List<Pregnancy>();
            }
            recipientProfile.pregnancies.Add(pregnancy);

            CoolDown pairTimer = new CoolDown { timerEnd = now.AddDays(1) };
            initiatorProfile.timers[PairTimerKey(recipient)] = pairTimer;
            recipientProfile.timers[PairTimerKey(initiator)] = pairTimer;

            Database.SetProfile(initiator, initiatorProfile);
            Database.SetProfile(recipient, recipientProfile);

            // Bump the global pregnancy counter for this monster type and each of its
            // categories. Offspring counters are bumped at birth time, not here.
            IncrementGlobalPregnancyCounts(Database, monsterType, categoriesSnapshot);

            Database.DeletePendingCommand(command.Id);

            return "breed";
        }

        /// <summary>
        /// Increment the global pregnancy counter by 1 for the monster type and for each
        /// of its categories. Used at breed time. (Offspring counters live separately and
        /// are bumped at birth time by ChateauBirth.)
        /// </summary>
        public static void IncrementGlobalPregnancyCounts(IChateauDatabase database, string monsterType, IEnumerable<string> categories)
        {
            if (string.IsNullOrEmpty(monsterType)) return;
            database.IncrementMonsterStats(MonsterStatsKey(monsterType), pregnancyDelta: 1, offspringDelta: 0);
            if (categories == null) return;
            foreach (var category in categories)
            {
                if (string.IsNullOrEmpty(category)) continue;
                database.IncrementMonsterStats(CategoryStatsKey(category), pregnancyDelta: 1, offspringDelta: 0);
            }
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " has bred " + recipientProfile.displayName
                + " with new " + identifier + " life! "
                + recipientProfile.displayName + " will carry the pregnancy until they're ready to !birth their young.";
        }

        public override CooldownSpec CooldownRule => Cooldown;

        public static readonly CooldownSpec Cooldown = new CooldownSpec
        {
            Kind = CooldownKind.Cooldown,
            Binds = CooldownBinds.Both,
            PeriodDays = 1
        };

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            int existingPregnancies = recipientProfile.pregnancies != null ? recipientProfile.pregnancies.Count : 0;
            string pregnancyCountText = existingPregnancies > 0
                ? " (" + recipientProfile.displayName + " is already carrying " + existingPregnancies + " other "
                    + (existingPregnancies == 1 ? "pregnancy" : "pregnancies") + ".)"
                : string.Empty;

            string seriousness = ConsentWarningText.Block(
                ConsentWarningText.FrequencyBoth("breed", Cooldown.PeriodDays));

            return initiatorProfile.displayName + " wants to breed " + recipientProfile.displayName
                + " with new " + identifier + " life! " + seriousness
                + pregnancyCountText
                + " Do you !consent to being bred?";
        }

        public static string PairTimerKey(string otherUser)
        {
            return "breed_pair_" + otherUser;
        }

        /// <summary>
        /// Computes the gestation duration and brood size for a monster, with three
        /// layers of fallback: explicit per-Identifier fields (when &gt; 0) override
        /// everything, otherwise the highest-priority matching category default applies,
        /// otherwise the absolute fallback (1 day, brood 1) is used. Each field is
        /// resolved independently — a monster can override only gestation while
        /// inheriting brood size from its category, or vice versa.
        ///
        /// When the resolved brood range is exactly [1, 1], a 1% twin chance applies:
        /// the roll yields 2 instead of 1 and <paramref name="isRareTwins"/> is set so
        /// the birth message can emit special flavor.
        /// </summary>
        internal static void ResolveGestationAndBrood(Identifier monsterIdentifier, Random random, out int gestationDays, out int broodSize, out bool isRareTwins)
        {
            CategoryDefault? categoryDefault = ResolveCategoryDefault(monsterIdentifier);

            int resolvedGestation = DefaultGestationDays;
            int resolvedBroodMin = DefaultBroodSize;
            int resolvedBroodMax = DefaultBroodSize;

            if (categoryDefault.HasValue)
            {
                resolvedGestation = categoryDefault.Value.GestationDays;
                resolvedBroodMin = categoryDefault.Value.BroodSizeMin;
                resolvedBroodMax = categoryDefault.Value.BroodSizeMax;
            }

            if (monsterIdentifier != null)
            {
                if (monsterIdentifier.gestationDays > 0)
                {
                    resolvedGestation = monsterIdentifier.gestationDays;
                }
                if (monsterIdentifier.broodSizeMin > 0)
                {
                    resolvedBroodMin = monsterIdentifier.broodSizeMin;
                }
                if (monsterIdentifier.broodSizeMax > 0)
                {
                    resolvedBroodMax = monsterIdentifier.broodSizeMax;
                }
            }

            gestationDays = ClampGestation(resolvedGestation);
            broodSize = RollBroodSize(resolvedBroodMin, resolvedBroodMax, random, out isRareTwins);
        }

        internal static CategoryDefault? ResolveCategoryDefault(Identifier monsterIdentifier)
        {
            if (monsterIdentifier == null || monsterIdentifier.categories == null) return null;

            CategoryDefault? best = null;
            foreach (var category in monsterIdentifier.categories)
            {
                if (string.IsNullOrEmpty(category)) continue;
                for (int i = 0; i < CategoryDefaults.Length; i++)
                {
                    if (string.Equals(CategoryDefaults[i].Category, category, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!best.HasValue || CategoryDefaults[i].Priority < best.Value.Priority)
                        {
                            best = CategoryDefaults[i];
                        }
                        break;
                    }
                }
            }
            return best;
        }

        private static int ClampGestation(int rawDays)
        {
            if (rawDays < 1) return DefaultGestationDays;
            if (rawDays > MaxGestationDays) return MaxGestationDays;
            return rawDays;
        }

        internal static int RollBroodSize(int rawMin, int rawMax, Random random, out bool isRareTwins)
        {
            isRareTwins = false;
            int min = rawMin > 0 ? rawMin : DefaultBroodSize;
            int max = rawMax >= min ? rawMax : min;
            if (min == max)
            {
                // Rare twin chance only fires when both bounds are exactly 1 (the
                // monster would otherwise always birth a single offspring).
                if (min == 1 && random.Next(RareTwinChanceDenominator) == 0)
                {
                    isRareTwins = true;
                    return 2;
                }
                return min;
            }
            return random.Next(min, max + 1);
        }
    }
}
