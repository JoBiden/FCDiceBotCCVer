using FChatDicebot.Model;
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
        /// different questions, and <c>!bottles</c> wants the first one.
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
