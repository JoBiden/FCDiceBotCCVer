using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FChatDicebot.Model
{
    /// <summary>
    /// One parasite currently active on a profile, applied via <c>!infest</c> (or spread from
    /// a partner who already had it) and removed via <c>!purge</c>. Stored JSON-encoded inside
    /// <c>Profile.lists["parasites"]</c>, mirroring the <see cref="ViceInstance"/> pattern so
    /// the existing <c>Dictionary&lt;string, List&lt;string&gt;&gt;</c> shape doesn't grow a
    /// new typed field.
    ///
    /// <see cref="SpreadFromContact"/> distinguishes direct infestations (which purge at the
    /// parasite's full cost immediately) from spread infestations (which purge for free
    /// during the <see cref="GraceUntil"/> window — 24 hours by default). See
    /// <see cref="InteractionProcessors.StatusEffectContributors.ParasiteSpreadEffect"/> for
    /// the spread logic and <see cref="BotCommands.ChateauPurge"/> for the reversal.
    /// </summary>
    public class ParasiteInstance
    {
        public string Parasite { get; set; }
        public string InfestedBy { get; set; }
        public DateTime InfestedAt { get; set; }
        public bool SpreadFromContact { get; set; }
        public DateTime GraceUntil { get; set; }

        public const string ParasitesListKey = "parasites";

        /// <summary>
        /// Read all ParasiteInstance entries off a profile, deserializing each JSON string in
        /// <c>profile.lists["parasites"]</c>. Returns an empty list (never null) and silently
        /// drops malformed entries so a single corrupt blob can't break the whole status
        /// surface.
        /// </summary>
        public static List<ParasiteInstance> LoadAll(Profile profile)
        {
            var result = new List<ParasiteInstance>();
            if (profile?.lists == null) return result;
            if (!profile.lists.ContainsKey(ParasitesListKey)) return result;

            foreach (string raw in profile.lists[ParasitesListKey])
            {
                if (string.IsNullOrEmpty(raw)) continue;
                ParasiteInstance entry;
                try
                {
                    entry = JsonConvert.DeserializeObject<ParasiteInstance>(raw);
                }
                catch (JsonException)
                {
                    continue;
                }
                if (entry == null || string.IsNullOrEmpty(entry.Parasite)) continue;
                result.Add(entry);
            }
            return result;
        }

        /// <summary>
        /// Persist a full ParasiteInstance list back into <c>profile.lists["parasites"]</c>,
        /// replacing prior contents. Removes the key entirely when the list is empty so a
        /// freshly-purged profile doesn't carry an empty array forever.
        /// </summary>
        public static void SaveAll(Profile profile, List<ParasiteInstance> entries)
        {
            if (profile == null) return;
            if (profile.lists == null) profile.lists = new Dictionary<string, List<string>>();

            if (entries == null || entries.Count == 0)
            {
                profile.lists.Remove(ParasitesListKey);
                return;
            }

            var encoded = new List<string>();
            foreach (var entry in entries)
            {
                encoded.Add(JsonConvert.SerializeObject(entry));
            }
            profile.lists[ParasitesListKey] = encoded;
        }
    }
}
