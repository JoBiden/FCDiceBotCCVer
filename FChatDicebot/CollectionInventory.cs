using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot
{
    /// <summary>
    /// The parts of collection handling that are genuinely the same for every
    /// <see cref="Collectible"/> type: pulling one type out of the mixed list, looking an item
    /// up by its number, and rendering a run of serials.
    ///
    /// <para>
    /// Everything that reads a <i>type's own</i> fields stays with that type —
    /// <see cref="BottleInventory"/> keeps the substance/donor filtering and grouping, because
    /// <c>substance</c> and <c>corruptionTag</c> don't exist on the base and a "general" helper
    /// that reached for them would only be pretending. The boundary is: if it compiles against
    /// <see cref="Collectible"/>, it belongs here.
    /// </para>
    ///
    /// <para>
    /// <b>Serials are shared across types.</b> <see cref="FindBySerial"/> answers "what is item
    /// #42", which may be a bottle, a pair of panties, or nothing. Callers that want a specific
    /// type ask for it (see <see cref="BottleInventory.FindBySerial"/>), and get null when #42
    /// turns out to be something else — which is the right answer for <c>!drink #42</c>.
    /// </para>
    /// </summary>
    public static class CollectionInventory
    {
        /// <summary>
        /// Every collectible of one type this resident holds, in stored order. Returns an empty
        /// list for a null profile or collection so callers can enumerate without null checks.
        /// </summary>
        public static List<T> OfType<T>(Profile profile) where T : Collectible
        {
            if (profile?.collectibles == null) return new List<T>();
            return profile.collectibles.OfType<T>().ToList();
        }

        /// <summary>
        /// True when the resident holds none of this type. Deliberately per-type: once there is
        /// more than one kind of collectible, "holds no bottles" and "holds nothing at all" are
        /// different questions, and <c>!collection</c> wants the first one.
        /// </summary>
        public static bool HasNone<T>(Profile profile) where T : Collectible
        {
            if (profile?.collectibles == null) return true;
            return !profile.collectibles.Any(c => c is T);
        }

        /// <summary>
        /// The item carrying this serial, whatever type it is, or null if the resident doesn't
        /// hold it. Serial 0 never matches — it is the "predates the backfill" sentinel, not a
        /// referenceable number.
        /// </summary>
        public static Collectible FindBySerial(Profile profile, int serial)
        {
            if (profile?.collectibles == null || serial <= 0) return null;
            return profile.collectibles.FirstOrDefault(c => c != null && c.serial == serial);
        }

        /// <summary>True when the resident holds nothing of any type.</summary>
        public static bool IsEmpty(Profile profile)
        {
            return profile?.collectibles == null || profile.collectibles.Count == 0;
        }

        /// <summary>
        /// One type's items, optionally narrowed to a single subject, newest first. The ordering
        /// is the collection-wide contract <see cref="BottleInventory.SelectFull"/> already
        /// follows, stated once here so a type that needs no filtering of its own inherits it
        /// rather than re-deriving it.
        /// </summary>
        public static List<T> Select<T>(Profile profile, string subjectFilter) where T : Collectible
        {
            return OfType<T>(profile)
                .Where(c => c != null && MatchesSubject(c, subjectFilter))
                .OrderByDescending(c => c.acquiredAt)
                .ThenByDescending(c => c.serial)
                .ToList();
        }

        /// <summary>
        /// Whether an item came from this subject. A null or empty filter means "any", which is
        /// what lets every caller pass its optional filter straight through.
        /// </summary>
        public static bool MatchesSubject(Collectible item, string subjectFilter)
        {
            if (item == null) return false;
            if (string.IsNullOrEmpty(subjectFilter)) return true;
            return string.Equals(item.subjectName, subjectFilter, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// One display row's worth of items: everything sharing a subject, with their serials in
        /// the order they arrived. The counterpart of <see cref="BottleInventory.BottleGroup"/>
        /// for types that have nothing to group on but whose they are.
        /// </summary>
        public class SubjectGroup
        {
            public string SubjectName;
            public List<int> Serials = new List<int>();
            public int Count => Serials.Count;
        }

        /// <summary>
        /// Collapse an already-ordered list into one group per subject, preserving the order the
        /// items arrived in (so a newest-first input yields newest-first groups).
        /// </summary>
        public static List<SubjectGroup> GroupBySubject(IEnumerable<Collectible> items)
        {
            var groups = new List<SubjectGroup>();
            var index = new Dictionary<string, SubjectGroup>(StringComparer.Ordinal);
            if (items == null) return groups;

            foreach (var item in items)
            {
                if (item == null) continue;
                string key = item.subjectName ?? "";
                if (!index.TryGetValue(key, out var group))
                {
                    group = new SubjectGroup { SubjectName = item.subjectName };
                    index[key] = group;
                    groups.Add(group);
                }
                group.Serials.Add(item.serial);
            }
            return groups;
        }

        /// <summary>
        /// Resolve a stored <see cref="Collectible.subjectName"/> for display.
        ///
        /// <para>
        /// <c>subjectName</c> is a frozen userName and does not follow renames, so it must never
        /// reach a resident unresolved. Falls back to the stored name only if that person has
        /// vanished from the roster entirely. This is the one display helper that is genuinely
        /// shared by every type, which is why it lives here rather than on the bottle view that
        /// used to own it.
        /// </para>
        /// </summary>
        public static string SubjectText(IChateauDatabase database, string subjectName)
        {
            if (string.IsNullOrEmpty(subjectName)) return "an unknown donor";
            string displayName = database?.GetDisplayName(subjectName);
            return string.IsNullOrEmpty(displayName) ? subjectName : displayName;
        }

        /// <summary>
        /// Render a run of serials as "#12, #40, #41", capped at
        /// <paramref name="cap"/> with an "and N more" tail so one prolific donor can't push the
        /// rest of the collection out of the message. Serial 0 (pre-backfill) renders as "#?"
        /// rather than a misleading "#0".
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
    }
}
