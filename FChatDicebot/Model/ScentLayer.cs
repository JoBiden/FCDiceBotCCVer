using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FChatDicebot.Model
{
    /// <summary>
    /// One stack of a single scent saturating a profile, applied via !odorize and reduced
    /// via !wash or by being mentioned in subsequent interactions. Stored JSON-encoded
    /// inside <c>Profile.lists["scents"]</c> so the existing <c>Dictionary&lt;string, List&lt;string&gt;&gt;</c>
    /// shape doesn't need to grow a new typed field.
    ///
    /// Fade semantics: each layer is worth 3 "mentions". RemainingMentions ticks down by
    /// one per interaction that surfaces the scent; when it crosses a 3-mention boundary
    /// the layer descriptor steps down (5→4→3→2→1), and when it hits zero the entry is
    /// removed entirely. See OdorizeStatusContributor for the mutation logic.
    /// </summary>
    public class ScentLayer
    {
        public string Scent { get; set; }
        public int Layers { get; set; }
        public int RemainingMentions { get; set; }
        public string AppliedBy { get; set; }
        public DateTime LastAppliedAt { get; set; }

        public const string ScentsListKey = "scents";

        /// <summary>
        /// Read all ScentLayer entries off a profile, deserializing each JSON string in
        /// <c>profile.lists["scents"]</c>. Returns an empty list (never null) and silently
        /// drops malformed entries so a single corrupt blob can't break the whole status
        /// surface.
        /// </summary>
        public static List<ScentLayer> LoadAll(Profile profile)
        {
            var result = new List<ScentLayer>();
            if (profile?.lists == null) return result;
            if (!profile.lists.ContainsKey(ScentsListKey)) return result;

            foreach (string raw in profile.lists[ScentsListKey])
            {
                if (string.IsNullOrEmpty(raw)) continue;
                ScentLayer layer;
                try
                {
                    layer = JsonConvert.DeserializeObject<ScentLayer>(raw);
                }
                catch (JsonException)
                {
                    continue;
                }
                if (layer == null) continue;
                result.Add(layer);
            }
            return result;
        }

        /// <summary>
        /// Persist a full ScentLayer list back into <c>profile.lists["scents"]</c>, replacing
        /// any prior contents. Removes the key entirely when the list is empty so an
        /// exhausted profile doesn't carry an empty array forever.
        /// </summary>
        public static void SaveAll(Profile profile, List<ScentLayer> layers)
        {
            if (profile == null) return;
            if (profile.lists == null) profile.lists = new Dictionary<string, List<string>>();

            if (layers == null || layers.Count == 0)
            {
                profile.lists.Remove(ScentsListKey);
                return;
            }

            var encoded = new List<string>();
            foreach (var layer in layers)
            {
                encoded.Add(JsonConvert.SerializeObject(layer));
            }
            profile.lists[ScentsListKey] = encoded;
        }
    }
}
