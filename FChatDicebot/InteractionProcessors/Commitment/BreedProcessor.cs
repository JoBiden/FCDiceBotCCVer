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

        /// <summary>
        /// True when the typed !breed identifier is one of the mystery keywords
        /// ("random": one rolled species for the whole brood; "mixed": every child rolls
        /// its own) rather than a real monster Identifier. The keywords are reserved —
        /// GetIdentifierFromCommandTerms can't match them, so a monster could never be
        /// registered under these names anyway.
        /// </summary>
        public static bool IsMysteryKeyword(string identifier)
        {
            return string.Equals(identifier, Pregnancy.MysteryRandom, StringComparison.OrdinalIgnoreCase)
                || string.Equals(identifier, Pregnancy.MysteryMixed, StringComparison.OrdinalIgnoreCase);
        }

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

            if (IsMysteryKeyword(identifier))
            {
                // The species roll needs a pool to draw from; with no registered monsters
                // (only realistic in a fresh install) the keyword can't work.
                var pool = Database.GetIdentifiersByCategory("monster");
                if (pool == null || pool.Count == 0)
                {
                    return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("monster"));
                }
            }
            else
            {
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
            }

            // Direction lock recheck (H4): without this, nothing stopped a second !breed
            // pending against the same recipient from landing the same day (e.g. via
            // !consent all resolving two queued breed pendings sequentially).
            Profile initiatorProfile = Database.GetProfile(initiator);
            if (HasActiveDirectionLock(initiatorProfile, recipient))
            {
                Profile recipientProfile = Database.GetProfile(recipient);
                string recipientDisplay = recipientProfile?.displayName ?? recipient;
                return ValidationResult.Failure(DirectionLockMessage(recipientDisplay));
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// True when <paramref name="profile"/> (the prospective breeder) has already bred
        /// <paramref name="recipientUser"/> today. Mirrors <see cref="Involved.MilkProcessor.HasActiveDirectionLock"/>
        /// — used by both ValidateInteraction (the consent-time gate) and ProcessInteraction
        /// (a TOCTOU belt-and-suspenders recheck) so breed is safe regardless of call path.
        /// </summary>
        public static bool HasActiveDirectionLock(Profile profile, string recipientUser)
        {
            if (profile?.timers == null) return false;
            string key = DirectionTimerKey(recipientUser);
            if (!profile.timers.TryGetValue(key, out var timer)) return false;
            return DateTime.UtcNow < timer.timerEnd;
        }

        /// <summary>Channel-facing wording for the once-per-day per-direction breed lock.</summary>
        public static string DirectionLockMessage(string recipientDisplayOrName)
        {
            DateTime now = DateTime.UtcNow;
            string untilReset = Utils.GetTimeSpanPrint(now.Date.AddDays(1) - now);
            return "You've already bred " + recipientDisplayOrName
                + " today. You can breed them again in " + untilReset + ".";
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string monsterType = command.pendingInteraction.identifier;

            Profile initiatorProfile = Database.GetProfile(initiator);

            // TOCTOU belt-and-suspenders (H4): ValidateInteraction already gates this at
            // consent time, but recheck here too so ProcessInteraction is safe even if a
            // future caller reaches it directly without going through the consent spine —
            // mirrors MilkProcessor's own recheck.
            if (HasActiveDirectionLock(initiatorProfile, recipient))
            {
                Database.DeletePendingCommand(command.Id);
                return "NoInteraction";
            }

            Database.AddInteraction(command.pendingInteraction);

            Profile recipientProfile = Database.GetProfile(recipient);

            // Mystery keywords (feedback 6a51d2fa): roll the species here, at breed time, so
            // gestation and brood size resolve from a real monster — but keep the result off
            // every pre-birth display via Pregnancy.MysteryKind. For "random" the rolled
            // monster IS the brood; for "mixed" it is only the womb-math host and each child
            // rolls its own species below.
            string mysteryKind = null;
            List<BroodChild> children = null;
            List<Identifier> mysteryPool = null;
            Identifier monsterIdentifier;
            if (IsMysteryKeyword(monsterType))
            {
                mysteryPool = Database.GetIdentifiersByCategory("monster");
                if (mysteryPool == null || mysteryPool.Count == 0)
                {
                    // ValidateInteraction gates this; belt-and-suspenders for direct callers.
                    Database.DeletePendingCommand(command.Id);
                    return "NoInteraction";
                }
                mysteryKind = string.Equals(monsterType, Pregnancy.MysteryMixed, StringComparison.OrdinalIgnoreCase)
                    ? Pregnancy.MysteryMixed
                    : Pregnancy.MysteryRandom;
                monsterIdentifier = mysteryPool[Rng.Next(mysteryPool.Count)];
                monsterType = mysteryKind == Pregnancy.MysteryRandom ? monsterIdentifier.type : Pregnancy.MysteryMixed;
            }
            else
            {
                monsterIdentifier = Database.GetIdentifier(monsterType);
            }

            ResolveGestationAndBrood(monsterIdentifier, Rng, out int gestationDays, out int broodSize, out bool isRareTwins);

            // Snapshot the monster's categories on the pregnancy so birth-time category
            // counters don't need to re-fetch the Identifier (which could have changed).
            // A mixed brood snapshots per child instead — the host only donated the numbers.
            List<string> categoriesSnapshot;
            if (mysteryKind == Pregnancy.MysteryMixed)
            {
                children = new List<BroodChild>();
                for (int i = 0; i < broodSize; i++)
                {
                    Identifier childSpecies = mysteryPool[Rng.Next(mysteryPool.Count)];
                    children.Add(new BroodChild
                    {
                        Species = childSpecies.type,
                        Categories = childSpecies.categories != null
                            ? new List<string>(childSpecies.categories)
                            : new List<string>()
                    });
                }
                categoriesSnapshot = new List<string>();
            }
            else
            {
                categoriesSnapshot = monsterIdentifier?.categories != null
                    ? new List<string>(monsterIdentifier.categories)
                    : new List<string>();
            }

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
                IsRareTwins = isRareTwins,
                MysteryKind = mysteryKind,
                Children = children
            };

            if (recipientProfile.pregnancies == null)
            {
                recipientProfile.pregnancies = new List<Pregnancy>();
            }
            recipientProfile.pregnancies.Add(pregnancy);

            // Per-direction daily lock — stamped on the breeder only and scoped to the
            // resident they bred, so being bred by someone (including breeding them back)
            // is unaffected.
            CoolDown directionTimer = new CoolDown { timerEnd = now.AddDays(1) };
            initiatorProfile.timers[DirectionTimerKey(recipient)] = directionTimer;

            Database.SetProfile(initiator, initiatorProfile);
            Database.SetProfile(recipient, recipientProfile);

            // Bump the global pregnancy counter for this monster type and each of its
            // categories. Offspring counters are bumped at birth time, not here. A mixed
            // brood counts once per distinct species/category it carries — "mixed" itself
            // is not a monster and gets no counter.
            if (mysteryKind == Pregnancy.MysteryMixed)
            {
                IncrementGlobalMixedPregnancyCounts(Database, children);
            }
            else
            {
                IncrementGlobalPregnancyCounts(Database, monsterType, categoriesSnapshot);
            }

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

        /// <summary>
        /// Bump the global pregnancy counter for a mixed brood: +1 per distinct species and
        /// +1 per distinct category across the litter (it is one pregnancy carrying each of
        /// them, not one pregnancy per child).
        /// </summary>
        public static void IncrementGlobalMixedPregnancyCounts(IChateauDatabase database, IEnumerable<BroodChild> children)
        {
            if (children == null) return;
            var species = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in children)
            {
                if (child == null || string.IsNullOrEmpty(child.Species)) continue;
                species.Add(child.Species);
                if (child.Categories == null) continue;
                foreach (var category in child.Categories)
                {
                    if (!string.IsNullOrEmpty(category)) categories.Add(category);
                }
            }
            foreach (var s in species)
            {
                database.IncrementMonsterStats(MonsterStatsKey(s), pregnancyDelta: 1, offspringDelta: 0);
            }
            foreach (var c in categories)
            {
                database.IncrementMonsterStats(CategoryStatsKey(c), pregnancyDelta: 1, offspringDelta: 0);
            }
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            if (string.Equals(identifier, Pregnancy.MysteryRandom, StringComparison.OrdinalIgnoreCase))
            {
                return initiatorProfile.displayName + " has bred " + recipientProfile.displayName
                    + " with new life of a mystery species! "
                    + recipientProfile.displayName + " will carry the pregnancy until they're ready to !birth their young — and only then will anyone learn what's inside.";
            }
            if (string.Equals(identifier, Pregnancy.MysteryMixed, StringComparison.OrdinalIgnoreCase))
            {
                return initiatorProfile.displayName + " has bred " + recipientProfile.displayName
                    + " with a mixed brood of mystery life! "
                    + recipientProfile.displayName + " will carry the pregnancy until they're ready to !birth their young — and only then will anyone learn what's inside.";
            }
            return initiatorProfile.displayName + " has bred " + recipientProfile.displayName
                + " with new " + identifier + " life! "
                + recipientProfile.displayName + " will carry the pregnancy until they're ready to !birth their young.";
        }

        public override CooldownSpec CooldownRule => Cooldown;

        public static readonly CooldownSpec Cooldown = new CooldownSpec
        {
            Kind = CooldownKind.Cooldown,
            // Per-direction: the breeder is locked against the specific resident they bred,
            // so they can still be bred by others or breed that resident back the same day.
            Binds = CooldownBinds.Initiator,
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
                ConsentWarningText.FrequencyPerAxis(initiatorProfile.displayName, "breed you", Cooldown.PeriodDays));

            string lifePhrase = " with new " + identifier + " life!";
            if (string.Equals(identifier, Pregnancy.MysteryRandom, StringComparison.OrdinalIgnoreCase))
            {
                lifePhrase = " with new life of a mystery species — nobody will know what it is until the !birth!";
            }
            else if (string.Equals(identifier, Pregnancy.MysteryMixed, StringComparison.OrdinalIgnoreCase))
            {
                lifePhrase = " with a mixed brood — every child a mystery species until the !birth!";
            }

            return initiatorProfile.displayName + " wants to breed " + recipientProfile.displayName
                + lifePhrase + " " + seriousness
                + pregnancyCountText
                + " Do you !consent to being bred?";
        }

        /// <summary>
        /// Key for the per-direction daily lock, stamped on the breeder only and scoped to
        /// the resident they bred: alice has <c>breed_give_Bob</c> after breeding Bob.
        /// Living only on the breeder's side means being bred back the same day is allowed.
        /// </summary>
        public static string DirectionTimerKey(string recipientUser)
        {
            return "breed_give_" + recipientUser;
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
            ResolveGestationAndBroodRange(monsterIdentifier, out gestationDays, out int broodMin, out int broodMax);
            broodSize = RollBroodSize(broodMin, broodMax, random, out isRareTwins);
        }

        /// <summary>
        /// Pure (no RNG) resolution of a monster's gestation days and brood size range —
        /// the same category-default-then-override precedence as
        /// <see cref="ResolveGestationAndBrood"/>, minus the brood-size roll. Used by
        /// !whatis to display gestation info without actually rolling a pregnancy.
        /// </summary>
        internal static void ResolveGestationAndBroodRange(Identifier monsterIdentifier, out int gestationDays, out int broodSizeMin, out int broodSizeMax)
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
            broodSizeMin = resolvedBroodMin > 0 ? resolvedBroodMin : DefaultBroodSize;
            broodSizeMax = resolvedBroodMax >= broodSizeMin ? resolvedBroodMax : broodSizeMin;
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
