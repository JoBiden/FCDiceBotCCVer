using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

// CurseProcessor catalog is referenced for the RandomCurse application path.

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Applies a <see cref="PurgeCostType"/> to a caller's profile and returns a user-facing
    /// description of what was suffered. Mutates the profile in-place (and writes
    /// <c>SetProfile</c> for the work timer / training decrement paths) — callers that have
    /// already mutated the same profile elsewhere should pass it through here and then save
    /// once at the end.
    ///
    /// All cost types are designed to bite. The contract is "you do not get out of this for
    /// free": <see cref="PurgeCostType.RandomCurse"/> degrades to
    /// <see cref="PurgeCostType.RandomBreak"/> until <c>!curse</c>/<c>!cleanse</c> ships, and
    /// <see cref="PurgeCostType.LostTrainingPoint"/> falls back to MissedWork when the caller
    /// has no trained skills at all (so a fresh resident can't escape via the training path).
    /// </summary>
    public static class PurgeCostApplier
    {
        /// <summary>Inclusive bounds for the duration of a RandomBreak cost.</summary>
        public const int RandomBreakMinDays = 1;
        public const int RandomBreakMaxDays = 3;

        public static PurgeCostResult Apply(
            IChateauDatabase database,
            Profile callerProfile,
            PurgeCostType type)
        {
            return Apply(database, callerProfile, type, new Random());
        }

        /// <summary>
        /// Test seam: lets a deterministic Random instance drive the random selections
        /// (which break part is picked, which training is decremented).
        /// </summary>
        public static PurgeCostResult Apply(
            IChateauDatabase database,
            Profile callerProfile,
            PurgeCostType type,
            Random rng)
        {
            if (callerProfile == null)
            {
                return new PurgeCostResult { Applied = false, Description = string.Empty };
            }
            rng = rng ?? new Random();

            switch (type)
            {
                case PurgeCostType.MissedWork:
                    return ApplyMissedWork(database, callerProfile);
                case PurgeCostType.RandomBreak:
                    return ApplyRandomBreak(database, callerProfile, rng);
                case PurgeCostType.LostTrainingPoint:
                    return ApplyLostTrainingPoint(database, callerProfile, rng);
                case PurgeCostType.RandomCurse:
                    return ApplyRandomCurse(database, callerProfile, rng);
                default:
                    return new PurgeCostResult { Applied = false, Description = string.Empty };
            }
        }

        private static PurgeCostResult ApplyMissedWork(IChateauDatabase database, Profile callerProfile)
        {
            if (callerProfile.timers == null) callerProfile.timers = new Dictionary<string, CoolDown>();
            DateTime nextMidnight = DateTime.UtcNow.Date.AddDays(1);
            callerProfile.timers["work"] = new CoolDown { timerStart = DateTime.UtcNow, timerEnd = nextMidnight };
            if (database != null && !string.IsNullOrEmpty(callerProfile.userName))
            {
                database.SetProfile(callerProfile.userName, callerProfile);
            }
            return new PurgeCostResult
            {
                Applied = true,
                Description = "Side effects leave them too unwell to !work for the rest of the day.",
            };
        }

        private static PurgeCostResult ApplyRandomBreak(IChateauDatabase database, Profile callerProfile, Random rng)
        {
            var pick = PickAndApplyRandomBreak(database, callerProfile, rng);
            if (pick == null)
            {
                // No break catalog, or every breakable part is already broken at least as
                // severely as any roll could produce — degrade to MissedWork so the cost
                // still bites rather than silently doing nothing (L8).
                return ApplyMissedWork(database, callerProfile);
            }
            string daysWord = pick.Days == 1 ? "day" : "days";
            return new PurgeCostResult
            {
                Applied = true,
                Description = "It looks like in the process, they abused their " + pick.Part
                    + " to the point of being exhausted for " + pick.Days + " " + daysWord + ".",
            };
        }

        /// <summary>
        /// Pick a random breakable part from the catalog, roll a 1–3 day duration, and apply
        /// the break to the caller's profile. Returns the (part, days) tuple for the caller
        /// to use in its description, or null if no break catalog exists. Mutates and
        /// persists the profile.
        ///
        /// BreakProcessor.ApplyBreak only raises severity to the max of the roll and any
        /// existing break on that part — a roll for an already-more-severely-broken part is
        /// a silent no-op (L8), letting the cost land for free. Retries with a fresh part
        /// (excluding ones already tried) until a roll actually raises severity, or gives up
        /// after trying every breakable part (falls back to MissedWork via the null return).
        /// </summary>
        private static BreakPick PickAndApplyRandomBreak(IChateauDatabase database, Profile callerProfile, Random rng)
        {
            List<Identifier> breakables = database?.GetIdentifiersByCategory(BreakProcessor.BreakCategory)
                ?? new List<Identifier>();
            if (breakables.Count == 0) return null;

            var existingSeverityByPart = BreakInstance.LoadAllWithTick(callerProfile)
                .ToDictionary(b => b.Part, b => b.Severity, StringComparer.OrdinalIgnoreCase);

            var remaining = new List<Identifier>(breakables);
            while (remaining.Count > 0)
            {
                int index = rng.Next(remaining.Count);
                Identifier picked = remaining[index];
                remaining.RemoveAt(index);

                int days = rng.Next(RandomBreakMinDays, RandomBreakMaxDays + 1);
                int existingSeverity = existingSeverityByPart.TryGetValue(picked.type, out var sev) ? sev : 0;
                if (days <= existingSeverity)
                {
                    continue; // would be a no-op; try a different part
                }

                BreakProcessor.ApplyBreak(callerProfile, picked.type, days, "Withdrawal");
                if (database != null && !string.IsNullOrEmpty(callerProfile.userName))
                {
                    database.SetProfile(callerProfile.userName, callerProfile);
                }
                return new BreakPick { Part = picked.type, Days = days };
            }

            return null; // every breakable part was already broken at least as severely
        }

        private class BreakPick
        {
            public string Part;
            public int Days;
        }

        /// <summary>
        /// Pick a random curse from <see cref="CurseProcessor.CatalogMap"/> that the
        /// caller doesn't already carry, apply it, and persist. If every curse is already
        /// present (or the catalog is empty), fall back to MissedWork so the cost still
        /// bites.
        /// </summary>
        private static PurgeCostResult ApplyRandomCurse(IChateauDatabase database, Profile callerProfile, Random rng)
        {
            var alreadyCarried = new HashSet<string>(
                CurseInstance.LoadAll(callerProfile).Select(c => c.Curse ?? string.Empty),
                StringComparer.OrdinalIgnoreCase);

            var candidates = CurseProcessor.CatalogMap.Keys
                .Where(name => !alreadyCarried.Contains(name))
                .ToList();

            if (candidates.Count == 0)
            {
                return ApplyMissedWork(database, callerProfile);
            }

            string picked = candidates[rng.Next(candidates.Count)];
            CurseProcessor.ApplyCurse(callerProfile, picked, "a passing witch");

            if (database != null && !string.IsNullOrEmpty(callerProfile.userName))
            {
                database.SetProfile(callerProfile.userName, callerProfile);
            }

            return new PurgeCostResult
            {
                Applied = true,
                Description = "They had to ask a witch for help though, and were left with a new [b]"
                    + picked + "[/b] curse for their trouble.",
            };
        }

        private static PurgeCostResult ApplyLostTrainingPoint(IChateauDatabase database, Profile callerProfile, Random rng)
        {
            if (callerProfile.trainings == null || callerProfile.trainings.Count == 0)
            {
                // No skills to lose — degrade to MissedWork.
                return ApplyMissedWork(database, callerProfile);
            }

            var candidates = callerProfile.trainings.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
            if (candidates.Count == 0)
            {
                return ApplyMissedWork(database, callerProfile);
            }

            string pick = candidates[rng.Next(candidates.Count)];
            callerProfile.trainings[pick] = callerProfile.trainings[pick] - 1;

            if (database != null && !string.IsNullOrEmpty(callerProfile.userName))
            {
                database.SetProfile(callerProfile.userName, callerProfile);
            }

            return new PurgeCostResult
            {
                Applied = true,
                Description = "They weren't able to do their usual " + pick
                    + " practice though... it feels like they've gotten worse at it as a result.",
            };
        }
    }

    /// <summary>
    /// Outcome of a single <see cref="PurgeCostApplier.Apply"/> call. <see cref="Applied"/>
    /// is false only when no profile was supplied or the cost type wasn't recognized; every
    /// real cost type ends up either applied or degraded to a fallback (still Applied = true).
    /// </summary>
    public class PurgeCostResult
    {
        public bool Applied { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
