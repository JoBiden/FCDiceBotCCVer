using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot
{
    /// <summary>
    /// Bottle-specific selection and grouping over a Profile's collection.
    ///
    /// <c>!sell</c>, <c>!drink</c>, <c>!bottles</c>, and bottle transfer through <c>!pay</c> all
    /// need identical answers to "which bottles does this filter mean, and in what order", so
    /// the rules live here once. The ordering contract is <b>newest first</b>: what the top of
    /// <c>!bottles</c> shows is what an unfiltered <c>!sell</c> or <c>!drink</c> acts on next.
    ///
    /// Empties are excluded from every implicit selection. They are still in the collection and
    /// still carry their serial, but they cannot be sold, cannot be drunk again, and only move
    /// between residents when named by serial.
    ///
    /// <para>
    /// Everything here reads <see cref="MilkBottle"/>'s own fields — <c>substance</c>,
    /// <c>corruptionTag</c>, the full/empty split — which is exactly why it did not generalize
    /// when bottles became <see cref="Collectible"/>s. The type-agnostic half lives in
    /// <see cref="CollectionInventory"/>.
    /// </para>
    /// </summary>
    public static class BottleInventory
    {
        /// <summary>
        /// Full bottles matching the optional filters, newest first. Null/empty filters mean
        /// "any". Never returns empties. Returns an empty list for a null profile or collection
        /// so callers can enumerate without null checks.
        /// </summary>
        public static List<MilkBottle> SelectFull(Profile profile, string substanceFilter, string sourceFilter)
        {
            return CollectionInventory.OfType<MilkBottle>(profile)
                .Where(b => b != null && !b.IsEmpty && MatchesFilters(b, substanceFilter, sourceFilter))
                .OrderByDescending(b => b.acquiredAt)
                .ThenByDescending(b => b.serial)
                .ToList();
        }

        /// <summary>Emptied bottles matching the optional filters, newest-emptied first.</summary>
        public static List<MilkBottle> SelectEmpty(Profile profile, string substanceFilter, string sourceFilter)
        {
            return CollectionInventory.OfType<MilkBottle>(profile)
                .Where(b => b != null && b.IsEmpty && MatchesFilters(b, substanceFilter, sourceFilter))
                .OrderByDescending(b => b.emptiedAt ?? b.acquiredAt)
                .ThenByDescending(b => b.serial)
                .ToList();
        }

        /// <summary>
        /// The bottle carrying this serial, full or empty, or null if the resident doesn't hold
        /// it. Serial 0 never matches — it is the "predates the backfill" sentinel, not a
        /// referenceable number.
        ///
        /// Serials are shared across every collectible type, so a number that belongs to
        /// something that isn't a bottle returns null here. That's the right answer for
        /// <c>!drink #42</c> and <c>!pay ... bottle #42</c>: the resident does not have
        /// <i>bottle</i> #42.
        /// </summary>
        public static MilkBottle FindBySerial(Profile profile, int serial)
        {
            return CollectionInventory.FindBySerial(profile, serial) as MilkBottle;
        }

        /// <summary>True when the resident holds no bottles at all, full or empty. Other kinds
        /// of collectible don't count — this is what <c>!bottles</c> and <c>!drink</c> ask.</summary>
        public static bool IsCollectionEmpty(Profile profile)
        {
            return CollectionInventory.HasNone<MilkBottle>(profile);
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
                && !string.Equals(bottle.subjectName, sourceFilter, StringComparison.OrdinalIgnoreCase))
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
            /// <summary>Newest acquiredAt in the group, carrying the group's sort position.</summary>
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
                string key = (bottle.substance ?? "") + "|" + (bottle.subjectName ?? "") + "|" + (bottle.corruptionTag ?? "");
                if (!index.TryGetValue(key, out var group))
                {
                    group = new BottleGroup
                    {
                        Substance = bottle.substance,
                        SourceName = bottle.subjectName,
                        CorruptionTag = bottle.corruptionTag,
                        NewestAt = bottle.acquiredAt,
                    };
                    index[key] = group;
                    groups.Add(group);
                }
                group.Serials.Add(bottle.serial);
                if (bottle.acquiredAt > group.NewestAt) group.NewestAt = bottle.acquiredAt;
            }
            return groups;
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
