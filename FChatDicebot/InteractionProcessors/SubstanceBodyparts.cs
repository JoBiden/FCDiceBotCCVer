using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Maps a substance to the bodyparts that produce it, and gates a draw on whether those
    /// parts are currently broken.
    ///
    /// Shared by every verb that takes fluid out of a resident's body (<c>!milk</c>,
    /// <c>!drinkfrom</c> / <c>!forcedrink</c>). It lives here rather than on one processor
    /// because the substance→part map is the thing that must not diverge: a substance added to
    /// one copy and not the other silently ungates the other verb.
    ///
    /// The break status contributor can't do this itself — it never sees which substance was
    /// asked for. (See the Break-and-Rest spec, section B.)
    /// </summary>
    public static class SubstanceBodyparts
    {
        /// <summary>
        /// The parts that produce <paramref name="substance"/>, or null when the substance
        /// isn't mapped — an unmapped substance is ungated rather than blocked, so adding a new
        /// identifier never accidentally makes it undrawable.
        /// </summary>
        public static HashSet<string> ForSubstance(string substance)
        {
            if (string.IsNullOrEmpty(substance)) return null;
            switch (substance.ToLowerInvariant())
            {
                case "milk":
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "breast" };
                case "saliva":
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mouth", "tongue" };
                case "golden":
                case "pre":
                case "cum":
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "dick", "ball", "pussy" };
                default:
                    return null;
            }
        }

        /// <summary>
        /// The refusal when the source's producing part is broken, or null when the draw may
        /// proceed (no break, part not relevant, substance unmapped, or no such profile).
        ///
        /// <paramref name="blockedTail"/> is the verb-specific end of the sentence — milking and
        /// drinking read differently — and is the only part of this message that varies.
        /// </summary>
        public static string CheckBreakBlock(
            IChateauDatabase database, string sourceUser, string substance, string blockedTail)
        {
            HashSet<string> relevantParts = ForSubstance(substance);
            if (relevantParts == null) return null;

            Profile sourceProfile = database?.GetProfile(sourceUser);
            if (sourceProfile == null) return null;

            var breaks = BreakInstance.LoadAllWithTick(sourceProfile);
            var brokenAndRelevant = breaks.Where(b => relevantParts.Contains(b.Part))
                                          .Select(b => b.Part)
                                          .ToList();
            if (brokenAndRelevant.Count == 0) return null;

            string sourceDisplay = string.IsNullOrEmpty(sourceProfile.displayName)
                ? sourceUser
                : sourceProfile.displayName;
            string verbToBe = brokenAndRelevant.Count == 1 ? "is" : "are";
            return sourceDisplay + "'s " + JoinParts(brokenAndRelevant) + " " + verbToBe + " " + blockedTail;
        }

        private static string JoinParts(List<string> parts)
        {
            if (parts.Count == 1) return parts[0];
            if (parts.Count == 2) return parts[0] + " and " + parts[1];
            return string.Join(", ", parts.GetRange(0, parts.Count - 1)) + ", and " + parts[parts.Count - 1];
        }
    }
}
