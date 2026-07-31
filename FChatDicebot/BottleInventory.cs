using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot
{
    /// <summary>
    /// Shared selection and grouping over a Profile's <see cref="Profile.milkInventory"/>.
    ///
    /// <c>!sell</c>, <c>!drink</c>, <c>!bottles</c>, and bottle transfer through <c>!pay</c> all
    /// need identical answers to "which bottles does this filter mean, and in what order", so
    /// the rules live here once. The ordering contract is <b>newest first</b>: what the top of
    /// <c>!bottles</c> shows is what an unfiltered <c>!sell</c> or <c>!drink</c> acts on next.
    ///
    /// Empties are excluded from every implicit selection. They are still in the collection and
    /// still carry their serial, but they cannot be sold, cannot be drunk again, and only move
    /// between residents when named by serial.
    /// </summary>
    public static class BottleInventory
    {
        /// <summary>
        /// Full bottles matching the optional filters, newest first. Null/empty filters mean
        /// "any". Never returns empties. Returns an empty list for a null profile or inventory
        /// so callers can enumerate without null checks.
        /// </summary>
        public static List<MilkBottle> SelectFull(Profile profile, string substanceFilter, string sourceFilter)
        {
            if (profile?.milkInventory == null) return new List<MilkBottle>();
            return profile.milkInventory
                .Where(b => b != null && !b.IsEmpty && MatchesFilters(b, substanceFilter, sourceFilter))
                .OrderByDescending(b => b.milkedAt)
                .ThenByDescending(b => b.serial)
                .ToList();
        }

        /// <summary>Emptied bottles matching the optional filters, newest-emptied first.</summary>
        public static List<MilkBottle> SelectEmpty(Profile profile, string substanceFilter, string sourceFilter)
        {
            if (profile?.milkInventory == null) return new List<MilkBottle>();
            return profile.milkInventory
                .Where(b => b != null && b.IsEmpty && MatchesFilters(b, substanceFilter, sourceFilter))
                .OrderByDescending(b => b.emptiedAt ?? b.milkedAt)
                .ThenByDescending(b => b.serial)
                .ToList();
        }

        /// <summary>
        /// The bottle carrying this serial, full or empty, or null if the resident doesn't hold
        /// it. Serial 0 never matches — it is the "predates the backfill" sentinel, not a
        /// referenceable number.
        /// </summary>
        public static MilkBottle FindBySerial(Profile profile, int serial)
        {
            if (profile?.milkInventory == null || serial <= 0) return null;
            return profile.milkInventory.FirstOrDefault(b => b != null && b.serial == serial);
        }

        /// <summary>True when the resident holds no bottles at all, full or empty.</summary>
        public static bool IsCollectionEmpty(Profile profile)
        {
            return profile?.milkInventory == null || profile.milkInventory.Count == 0;
        }

        public static bool MatchesFilters(MilkBottle bottle, string substanceFilter, string sourceFilter)
        {
            if (bottle == null) return false;
            if (!string.IsNullOrEmpty(substanceFilter)
                && !string.Equals(bottle.substance, substanceFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(sourceFilter)
                && !string.Equals(bottle.sourceName, sourceFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// One display row: bottles sharing a substance, a donor, and a corruption tag, collapsed
        /// together with their serials listed. Separate milkings of the same kind read as one
        /// line, which is what keeps a large collection legible.
        /// </summary>
        public class BottleGroup
        {
            public string Substance;
            public string SourceName;
            public string CorruptionTag;
            public List<int> Serials = new List<int>();
            /// <summary>Newest milkedAt in the group, carrying the group's sort position.</summary>
            public DateTime NewestAt;
            public int Count => Serials.Count;
        }

        /// <summary>
        /// Collapse an already-ordered bottle list into display groups, preserving the order the
        /// bottles arrived in (so a newest-first input yields newest-first groups). Serials within
        /// a group keep that same order.
        /// </summary>
        public static List<BottleGroup> Group(IEnumerable<MilkBottle> bottles)
        {
            var groups = new List<BottleGroup>();
            var index = new Dictionary<string, BottleGroup>(StringComparer.Ordinal);
            if (bottles == null) return groups;

            foreach (var bottle in bottles)
            {
                if (bottle == null) continue;
                string key = (bottle.substance ?? "") + "|" + (bottle.sourceName ?? "") + "|" + (bottle.corruptionTag ?? "");
                if (!index.TryGetValue(key, out var group))
                {
                    group = new BottleGroup
                    {
                        Substance = bottle.substance,
                        SourceName = bottle.sourceName,
                        CorruptionTag = bottle.corruptionTag,
                        NewestAt = bottle.milkedAt,
                    };
                    index[key] = group;
                    groups.Add(group);
                }
                group.Serials.Add(bottle.serial);
                if (bottle.milkedAt > group.NewestAt) group.NewestAt = bottle.milkedAt;
            }
            return groups;
        }

        /// <summary>
        /// Render a group's serials as "#12, #40, #41", capped at
        /// <see cref="ChateauCurrency.BottleSerialDisplayCap"/> with an "and N more" tail so one
        /// prolific donor can't push the rest of the collection out of the message.
        /// Serial 0 (pre-backfill) renders as "#?" rather than a misleading "#0".
        /// </summary>
        public static string FormatSerials(IEnumerable<int> serials, int cap)
        {
            if (serials == null) return string.Empty;
            var all = serials.ToList();
            if (all.Count == 0) return string.Empty;

            var shown = all.Take(cap).Select(s => s > 0 ? "#" + s : "#?");
            string text = string.Join(", ", shown);
            int remaining = all.Count - cap;
            if (remaining > 0)
            {
                text += ", and " + remaining + " more";
            }
            return text;
        }

        /// <summary>
        /// Per-substance totals for the public dossier line, highest count first. Deliberately
        /// drops donor names: the dossier is read by everyone, and who a resident has milked is
        /// the donor's business.
        /// </summary>
        public static List<KeyValuePair<string, int>> CountsBySubstance(IEnumerable<MilkBottle> bottles)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (bottles != null)
            {
                foreach (var bottle in bottles)
                {
                    if (bottle == null || string.IsNullOrEmpty(bottle.substance)) continue;
                    counts.TryGetValue(bottle.substance, out int current);
                    counts[bottle.substance] = current + 1;
                }
            }
            return counts
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
