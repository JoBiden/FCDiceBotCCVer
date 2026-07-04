using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FChatDicebot.Model
{
    /// <summary>
    /// Generic JSON-in-<c>Profile.lists[listKey]</c> load/save, shared by BreakInstance,
    /// CurseInstance, ParasiteInstance, ViceInstance, and ScentLayer — each stores its
    /// entries as JSON-encoded strings in one <c>profile.lists</c> key so the existing
    /// <c>Dictionary&lt;string, List&lt;string&gt;&gt;</c> Profile shape doesn't need a new
    /// typed field per status effect.
    /// </summary>
    public static class ProfileListStore<T> where T : class
    {
        /// <summary>
        /// Read all entries off a profile, deserializing each JSON string in
        /// <c>profile.lists[listKey]</c>. Returns an empty list (never null) and silently
        /// drops malformed entries so a single corrupt blob can't break the whole status
        /// surface. <paramref name="isValid"/> lets callers additionally reject a
        /// successfully-deserialized entry (e.g. missing its identifying field).
        /// </summary>
        public static List<T> LoadAll(Profile profile, string listKey, Func<T, bool> isValid = null)
        {
            var result = new List<T>();
            if (profile?.lists == null) return result;
            if (!profile.lists.ContainsKey(listKey)) return result;

            foreach (string raw in profile.lists[listKey])
            {
                if (string.IsNullOrEmpty(raw)) continue;
                T entry;
                try
                {
                    entry = JsonConvert.DeserializeObject<T>(raw);
                }
                catch (JsonException)
                {
                    continue;
                }
                if (entry == null) continue;
                if (isValid != null && !isValid(entry)) continue;
                result.Add(entry);
            }
            return result;
        }

        /// <summary>
        /// Persist a full entry list back into <c>profile.lists[listKey]</c>, replacing any
        /// prior contents. Removes the key entirely when the list is empty so a cleared
        /// profile doesn't carry an empty array forever.
        /// </summary>
        public static void SaveAll(Profile profile, string listKey, List<T> entries)
        {
            if (profile == null) return;
            if (profile.lists == null) profile.lists = new Dictionary<string, List<string>>();

            if (entries == null || entries.Count == 0)
            {
                profile.lists.Remove(listKey);
                return;
            }

            var encoded = new List<string>();
            foreach (var entry in entries)
            {
                encoded.Add(JsonConvert.SerializeObject(entry));
            }
            profile.lists[listKey] = encoded;
        }
    }
}
