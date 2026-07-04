using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FChatDicebot.Model
{
    /// <summary>
    /// One curse currently active on a profile, applied via <c>!curse</c> and removed via
    /// <c>!cleanse</c> at a per-curse cost. Stored JSON-encoded inside
    /// <c>Profile.lists["curses"]</c>, mirroring the <see cref="ParasiteInstance"/> and
    /// <see cref="ViceInstance"/> patterns so the existing
    /// <c>Dictionary&lt;string, List&lt;string&gt;&gt;</c> shape doesn't grow a new typed field.
    ///
    /// Curses are looked up against the static
    /// <c>InteractionProcessors.Consequence.CurseProcessor.CatalogMap</c> for bucket, blocked
    /// interactions, modifier template, and cleanse cost; that map is the source of truth for
    /// curse behaviour. See
    /// <see cref="InteractionProcessors.StatusEffectContributors.CurseStatusContributor"/> for
    /// the validation/completion contributions and <see cref="BotCommands.ChateauCleanse"/>
    /// for the reversal.
    /// </summary>
    public class CurseInstance
    {
        public string Curse { get; set; }
        public string AppliedBy { get; set; }
        public DateTime AppliedAt { get; set; }

        public const string CursesListKey = "curses";

        /// <summary>
        /// Read all CurseInstance entries off a profile, deserializing each JSON string in
        /// <c>profile.lists["curses"]</c>. Returns an empty list (never null) and silently
        /// drops malformed entries so a single corrupt blob can't break the whole status
        /// surface.
        /// </summary>
        public static List<CurseInstance> LoadAll(Profile profile)
        {
            return ProfileListStore<CurseInstance>.LoadAll(profile, CursesListKey,
                entry => !string.IsNullOrEmpty(entry.Curse));
        }

        /// <summary>
        /// Persist a full CurseInstance list back into <c>profile.lists["curses"]</c>,
        /// replacing prior contents. Removes the key entirely when the list is empty so a
        /// freshly-cleansed profile doesn't carry an empty array forever.
        /// </summary>
        public static void SaveAll(Profile profile, List<CurseInstance> entries)
        {
            ProfileListStore<CurseInstance>.SaveAll(profile, CursesListKey, entries);
        }
    }
}
