using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FChatDicebot.Model
{
    /// <summary>
    /// One broken bodypart on a profile. Stored JSON-encoded inside
    /// <c>Profile.lists["breaks"]</c> alongside the existing <c>scents</c> shape so the
    /// dictionary-of-string-lists Profile model doesn't need a new typed field.
    ///
    /// Heal semantics: Severity starts at the number of days specified by !break and
    /// decrements once per UTC day via the lazy tick in <c>LoadAllWithTick</c>. !rest
    /// decrements an additional 1 in exchange for blocking !work that day. When Severity
    /// hits 0 the entry is removed.
    /// </summary>
    public class BreakInstance
    {
        public string Part { get; set; }
        public int Severity { get; set; }
        public string BrokenBy { get; set; }
        public DateTime BrokenAt { get; set; }
        public DateTime LastTickedAt { get; set; }

        public const string BreaksListKey = "breaks";

        /// <summary>
        /// Read all BreakInstance entries off a profile without ticking. Useful for tests
        /// and read-only inspection; runtime callers should use
        /// <see cref="LoadAllWithTick"/> so stale severities heal first.
        /// </summary>
        public static List<BreakInstance> LoadAll(Profile profile)
        {
            return ProfileListStore<BreakInstance>.LoadAll(profile, BreaksListKey);
        }

        /// <summary>
        /// Read + lazy-tick. For each entry, if <c>LastTickedAt.Date &lt; UtcNow.Date</c>,
        /// decrements <c>Severity</c> by the elapsed whole-day count and advances
        /// <c>LastTickedAt</c> to today's date. Entries that hit zero are dropped. If any
        /// state changed, the result is written back to the profile in-place via
        /// <see cref="SaveAll"/>; callers still own database persistence.
        /// </summary>
        public static List<BreakInstance> LoadAllWithTick(Profile profile)
        {
            var entries = LoadAll(profile);
            if (entries.Count == 0) return entries;

            DateTime today = DateTime.UtcNow.Date;
            bool mutated = false;
            var kept = new List<BreakInstance>();
            foreach (var entry in entries)
            {
                int daysElapsed = (today - entry.LastTickedAt.Date).Days;
                if (daysElapsed > 0)
                {
                    entry.Severity -= daysElapsed;
                    entry.LastTickedAt = today;
                    mutated = true;
                }
                if (entry.Severity > 0)
                {
                    kept.Add(entry);
                }
                else
                {
                    mutated = true;
                }
            }
            if (mutated) SaveAll(profile, kept);
            return kept;
        }

        /// <summary>
        /// Persist the full list back into <c>profile.lists["breaks"]</c>. Removes the key
        /// entirely when the list is empty so an exhausted profile doesn't carry an empty
        /// array.
        /// </summary>
        public static void SaveAll(Profile profile, List<BreakInstance> entries)
        {
            ProfileListStore<BreakInstance>.SaveAll(profile, BreaksListKey, entries);
        }
    }
}
