using System;
using System.Collections.Generic;

namespace FChatDicebot
{
    /// <summary>
    /// Short-lived cache of every registered resident's userName/displayName, backing bare-name
    /// targeting (<see cref="RecipientResolver"/>).
    ///
    /// Bare-name resolution runs on the command hot path and needs the whole name set to detect
    /// ambiguity, so it can't be a per-command query. The projection is names-only and residents
    /// are counted in the hundreds, so the cached list is cheap to hold; the TTL exists to pick
    /// up registrations and renames that happened on another process, while
    /// <see cref="Invalidate"/> makes this one's own writes visible immediately.
    /// </summary>
    internal static class ProfileNameCache
    {
        private const double TimeToLiveSeconds = 60;

        private static readonly object CacheLock = new object();
        private static List<ProfileName> _names;
        private static DateTime _loadedUtc = DateTime.MinValue;

        internal static List<ProfileName> GetNames()
        {
            lock (CacheLock)
            {
                if (_names != null && (DateTime.UtcNow - _loadedUtc).TotalSeconds < TimeToLiveSeconds)
                    return _names;

                _names = MonDB.getProfileNames() ?? new List<ProfileName>();
                _loadedUtc = DateTime.UtcNow;
                return _names;
            }
        }

        /// <summary>
        /// Drop the cached list so the next lookup re-reads. Call after anything that adds a
        /// resident or changes what they're called — otherwise a resident who just registered
        /// can't be targeted by name until the TTL lapses.
        /// </summary>
        internal static void Invalidate()
        {
            lock (CacheLock)
            {
                _names = null;
                _loadedUtc = DateTime.MinValue;
            }
        }
    }
}
