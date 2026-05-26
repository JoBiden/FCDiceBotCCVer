using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

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
                    // Curse-and-Cleanse hasn't shipped — fall back to a random break so the
                    // cost is never silently skipped. When the curse system lands, replace
                    // this branch with the real curse application; until then we wrap the
                    // break-cost noun phrase in the "witch" framing the user picked.
                    var pick = PickAndApplyRandomBreak(database, callerProfile, rng);
                    if (pick == null) return ApplyMissedWork(database, callerProfile);
                    string daysWordW = pick.Days == 1 ? "day" : "days";
                    return new PurgeCostResult
                    {
                        Applied = true,
                        Description = "They had to ask a witch for help though, and the cost was a broken "
                            + pick.Part + " for " + pick.Days + " " + daysWordW + ".",
                    };
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
                Description = "Withdrawals leave them too unwell to !work for the rest of the day.",
            };
        }

        private static PurgeCostResult ApplyRandomBreak(IChateauDatabase database, Profile callerProfile, Random rng)
        {
            var pick = PickAndApplyRandomBreak(database, callerProfile, rng);
            if (pick == null)
            {
                // No break catalog — degrade to MissedWork so the cost still bites.
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
        /// </summary>
        private static BreakPick PickAndApplyRandomBreak(IChateauDatabase database, Profile callerProfile, Random rng)
        {
            List<Identifier> breakables = database?.GetIdentifiersByCategory(BreakProcessor.BreakCategory)
                ?? new List<Identifier>();
            if (breakables.Count == 0) return null;

            Identifier picked = breakables[rng.Next(breakables.Count)];
            int days = rng.Next(RandomBreakMinDays, RandomBreakMaxDays + 1);
            BreakProcessor.ApplyBreak(callerProfile, picked.type, days, "Withdrawal");
            if (database != null && !string.IsNullOrEmpty(callerProfile.userName))
            {
                database.SetProfile(callerProfile.userName, callerProfile);
            }
            return new BreakPick { Part = picked.type, Days = days };
        }

        private class BreakPick
        {
            public string Part;
            public int Days;
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
