using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FChatDicebot.Model
{
    /// <summary>
    /// One addictive vice currently active on a profile, applied via <c>!dose</c> and removed
    /// via <c>!detox</c>. Stored JSON-encoded inside <c>Profile.lists["vices"]</c> so the
    /// existing <c>Dictionary&lt;string, List&lt;string&gt;&gt;</c> shape doesn't need to grow
    /// a new typed field. Mirrors the <see cref="ScentLayer"/> pattern.
    ///
    /// Severity ranges 1..N (cap defined on <see cref="InteractionProcessors.Consequence.DoseProcessor"/>).
    /// Re-dosing the same vice increments <see cref="AddictionLevel"/>; <c>!feed</c> of the
    /// matching substance or <c>!odorize</c> of the matching scent satisfies a craving
    /// without raising severity; <c>!detox</c> clears the instance entirely. See
    /// <see cref="InteractionProcessors.StatusEffectContributors.DoseStatusContributor"/>
    /// for the craving / satisfaction logic that reads these entries.
    /// </summary>
    public class ViceInstance
    {
        public string Vice { get; set; }
        public int AddictionLevel { get; set; }
        public string DosedBy { get; set; }
        public DateTime FirstDosedAt { get; set; }
        public DateTime LastEscalatedAt { get; set; }

        public const string VicesListKey = "vices";

        /// <summary>
        /// Read all ViceInstance entries off a profile, deserializing each JSON string in
        /// <c>profile.lists["vices"]</c>. Returns an empty list (never null) and silently
        /// drops malformed entries so a single corrupt blob can't break the whole status
        /// surface.
        /// </summary>
        public static List<ViceInstance> LoadAll(Profile profile)
        {
            var result = new List<ViceInstance>();
            if (profile?.lists == null) return result;
            if (!profile.lists.ContainsKey(VicesListKey)) return result;

            foreach (string raw in profile.lists[VicesListKey])
            {
                if (string.IsNullOrEmpty(raw)) continue;
                ViceInstance entry;
                try
                {
                    entry = JsonConvert.DeserializeObject<ViceInstance>(raw);
                }
                catch (JsonException)
                {
                    continue;
                }
                if (entry == null || string.IsNullOrEmpty(entry.Vice)) continue;
                result.Add(entry);
            }
            return result;
        }

        /// <summary>
        /// Persist a full ViceInstance list back into <c>profile.lists["vices"]</c>, replacing
        /// any prior contents. Removes the key entirely when the list is empty so a freshly-
        /// detoxed profile doesn't carry an empty array forever.
        /// </summary>
        public static void SaveAll(Profile profile, List<ViceInstance> entries)
        {
            if (profile == null) return;
            if (profile.lists == null) profile.lists = new Dictionary<string, List<string>>();

            if (entries == null || entries.Count == 0)
            {
                profile.lists.Remove(VicesListKey);
                return;
            }

            var encoded = new List<string>();
            foreach (var entry in entries)
            {
                encoded.Add(JsonConvert.SerializeObject(entry));
            }
            profile.lists[VicesListKey] = encoded;
        }
    }
}
