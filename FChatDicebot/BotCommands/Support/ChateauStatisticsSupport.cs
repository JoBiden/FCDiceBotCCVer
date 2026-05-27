using FChatDicebot.BotCommands.Base;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.BotCommands.Support
{
    /// <summary>
    /// Aggregation helpers shared by the chateau-wide readout commands
    /// (!statistics, !populations, !flora, !birthrates, !parasites, !payroll, !economics).
    /// Each helper either scans all profiles, scans the interaction log, or reads from the
    /// MonsterStats aggregate. None of them mutate state; all are safe to call from any
    /// information command.
    /// </summary>
    public static class ChateauStatisticsSupport
    {
        /// <summary>
        /// Format a comma-separated list of the top N entries from a label→count map,
        /// rendered as "{count} {label-pluralized}". Returns an empty string if every
        /// entry has count 0. Ties at the cut-off are included.
        /// </summary>
        public static string FormatTopJobs(Dictionary<string, int> jobCounts, int topN)
        {
            if (jobCounts == null || jobCounts.Count == 0) return string.Empty;
            var ordered = jobCounts
                .Where(kv => kv.Value > 0)
                .OrderByDescending(kv => kv.Value)
                .ToList();
            if (ordered.Count == 0) return string.Empty;

            int cutoff = ordered.Count <= topN ? ordered.Count : topN;
            int cutoffValue = ordered[cutoff - 1].Value;
            // Include ties at the cut-off so we don't arbitrarily drop one of N at the same count.
            var top = ordered.Where(kv => kv.Value >= cutoffValue).Take(topN).ToList();

            var parts = new List<string>();
            foreach (var entry in top)
            {
                string label = entry.Value == 1 ? Utils.JobToText(entry.Key) : Utils.JobToPlural(entry.Key);
                parts.Add(entry.Value + " " + label);
            }
            return string.Join(", ", parts);
        }

        /// <summary>
        /// Render the "most ___" descriptor for the top entry of a label→count map.
        /// Ties surface all tied labels. Returns an empty string if every entry has
        /// count 0 (so the caller can suppress the parenthetical).
        /// </summary>
        public static string MostFromMap(Dictionary<string, int> counts, Func<string, string> labelFormatter = null)
        {
            if (counts == null || counts.Count == 0) return string.Empty;
            int max = counts.Values.DefaultIfEmpty(0).Max();
            if (max <= 0) return string.Empty;
            if (labelFormatter == null) labelFormatter = (k => k);
            var tied = counts.Where(kv => kv.Value == max).Select(kv => labelFormatter(kv.Key));
            return string.Join(", ", tied);
        }

        /// <summary>
        /// Count distinct profiles in <paramref name="profiles"/> grouped by their
        /// <c>characteristics["monster"]</c> value (lowercased, skipping unset entries).
        /// </summary>
        public static Dictionary<string, int> CountMonsterizedByType(IEnumerable<Profile> profiles)
        {
            var result = new Dictionary<string, int>();
            foreach (var profile in profiles)
            {
                if (profile?.characteristics == null) continue;
                if (!profile.characteristics.ContainsKey("monster")) continue;
                string type = profile.characteristics["monster"];
                if (string.IsNullOrEmpty(type)) continue;
                type = type.ToLowerInvariant();
                if (!result.ContainsKey(type)) result[type] = 0;
                result[type]++;
            }
            return result;
        }

        /// <summary>
        /// Count distinct profiles in <paramref name="profiles"/> grouped by their
        /// <c>characteristics["job"]</c> value (lowercased, skipping unset entries).
        /// </summary>
        public static Dictionary<string, int> CountEmployedByJob(IEnumerable<Profile> profiles)
        {
            var result = new Dictionary<string, int>();
            foreach (var profile in profiles)
            {
                if (profile?.characteristics == null) continue;
                if (!profile.characteristics.ContainsKey("job")) continue;
                string job = profile.characteristics["job"];
                if (string.IsNullOrEmpty(job)) continue;
                job = job.ToLowerInvariant();
                if (!result.ContainsKey(job)) result[job] = 0;
                result[job]++;
            }
            return result;
        }

        /// <summary>
        /// Sum <c>profile.jobExperience</c> values across all profiles, grouped by job.
        /// Each !work completion increments one entry by 1 (see <see cref="ChateauWork"/>),
        /// so this is the lifetime duties-completed tally per job.
        /// </summary>
        public static Dictionary<string, int> SumDutiesByJob(IEnumerable<Profile> profiles)
        {
            var result = new Dictionary<string, int>();
            foreach (var profile in profiles)
            {
                if (profile?.jobExperience == null) continue;
                foreach (var entry in profile.jobExperience)
                {
                    string job = (entry.Key ?? string.Empty).ToLowerInvariant();
                    if (string.IsNullOrEmpty(job)) continue;
                    if (!result.ContainsKey(job)) result[job] = 0;
                    result[job] += entry.Value;
                }
            }
            return result;
        }

        /// <summary>
        /// Sum <c>profile.currencies</c> values across all profiles, grouped by currency name.
        /// Currency names are kept as-stored (case preserved) so the display matches !bank.
        /// </summary>
        public static Dictionary<string, int> SumCurrenciesAcrossProfiles(IEnumerable<Profile> profiles)
        {
            var result = new Dictionary<string, int>();
            foreach (var profile in profiles)
            {
                if (profile?.currencies == null) continue;
                foreach (var entry in profile.currencies)
                {
                    if (string.IsNullOrEmpty(entry.Key)) continue;
                    if (!result.ContainsKey(entry.Key)) result[entry.Key] = 0;
                    result[entry.Key] += entry.Value;
                }
            }
            return result;
        }

        /// <summary>
        /// Per-parasite snapshot: distinct hosts currently carrying that parasite.
        /// </summary>
        public static Dictionary<string, int> CountCurrentParasiteHosts(IEnumerable<Profile> profiles)
        {
            var result = new Dictionary<string, int>();
            foreach (var profile in profiles)
            {
                var parasites = ParasiteInstance.LoadAll(profile);
                if (parasites.Count == 0) continue;
                var distinctOnThisHost = new HashSet<string>();
                foreach (var p in parasites)
                {
                    if (string.IsNullOrEmpty(p?.Parasite)) continue;
                    distinctOnThisHost.Add(p.Parasite.ToLowerInvariant());
                }
                foreach (var name in distinctOnThisHost)
                {
                    if (!result.ContainsKey(name)) result[name] = 0;
                    result[name]++;
                }
            }
            return result;
        }

        /// <summary>
        /// Per-parasite lifetime "ever spread" = direct !infest interactions plus a count of
        /// every <c>SpreadFromContact</c> parasite instance ever observed on a profile.
        /// (The spread instances are still on the profile, but they get cleared on !purge,
        /// so the count is necessarily snapshot+lifetime hybrid — we sum direct infests with
        /// currently-active spread instances.)
        /// </summary>
        public static Dictionary<string, int> CountLifetimeParasiteSpread(IEnumerable<Profile> profiles, List<Interaction> infestInteractions)
        {
            var result = new Dictionary<string, int>();
            if (infestInteractions != null)
            {
                foreach (var i in infestInteractions)
                {
                    string name = (i.identifier ?? string.Empty).ToLowerInvariant();
                    if (string.IsNullOrEmpty(name)) continue;
                    if (!result.ContainsKey(name)) result[name] = 0;
                    result[name]++;
                }
            }
            foreach (var profile in profiles)
            {
                var parasites = ParasiteInstance.LoadAll(profile);
                foreach (var p in parasites)
                {
                    if (!p.SpreadFromContact) continue;
                    string name = (p.Parasite ?? string.Empty).ToLowerInvariant();
                    if (string.IsNullOrEmpty(name)) continue;
                    if (!result.ContainsKey(name)) result[name] = 0;
                    result[name]++;
                }
            }
            return result;
        }

        /// <summary>
        /// Per-parasite lifetime purge count = !purge interactions logged on or after the
        /// purge-logging change shipped (see <see cref="ChateauPurge.PurgeType"/>).
        /// </summary>
        public static Dictionary<string, int> CountLifetimePurges(List<Interaction> purgeInteractions)
        {
            var result = new Dictionary<string, int>();
            if (purgeInteractions == null) return result;
            foreach (var i in purgeInteractions)
            {
                string name = (i.identifier ?? string.Empty).ToLowerInvariant();
                if (string.IsNullOrEmpty(name)) continue;
                if (!result.ContainsKey(name)) result[name] = 0;
                result[name]++;
            }
            return result;
        }

        /// <summary>
        /// Sum the magnitudes of every <c>corrupt</c> (resp. <c>purify</c>) interaction.
        /// The CorruptionProcessor stamps "{verb}|{magnitude}" onto Interaction.identifier
        /// (see <see cref="InteractionProcessors.IdentifierPayload"/>); we unpack the tail.
        /// </summary>
        public static int SumCorruptionVolume(List<Interaction> interactions, string verb)
        {
            if (interactions == null) return 0;
            int sum = 0;
            foreach (var i in interactions)
            {
                if (!string.Equals(i.type, verb, StringComparison.OrdinalIgnoreCase)) continue;
                sum += Math.Abs(IdentifierPayload.ExtractTailOr(i.identifier, 1));
            }
            return sum;
        }

        /// <summary>
        /// Plant identifier → lifetime count, from the interaction log.
        /// </summary>
        public static Dictionary<string, int> CountByIdentifier(List<Interaction> interactions)
        {
            var result = new Dictionary<string, int>();
            if (interactions == null) return result;
            foreach (var i in interactions)
            {
                string key = (i.identifier ?? string.Empty).ToLowerInvariant();
                if (string.IsNullOrEmpty(key)) continue;
                if (!result.ContainsKey(key)) result[key] = 0;
                result[key]++;
            }
            return result;
        }

        /// <summary>
        /// Filter the MonsterStats collection down to the per-monster-type entries (rows
        /// with id prefixed "monster:"). The "category:" rows aggregate across types and
        /// would double-count if mixed in.
        /// </summary>
        public static Dictionary<string, int> OffspringByMonsterType(IEnumerable<MonsterStats> allStats)
        {
            var result = new Dictionary<string, int>();
            if (allStats == null) return result;
            foreach (var s in allStats)
            {
                if (s == null || string.IsNullOrEmpty(s.Id)) continue;
                if (!s.Id.StartsWith("monster:", StringComparison.OrdinalIgnoreCase)) continue;
                string monster = s.Id.Substring("monster:".Length);
                if (string.IsNullOrEmpty(monster)) continue;
                result[monster] = s.OffspringCount;
            }
            return result;
        }

        /// <summary>
        /// Aggregated per-resident "Sired" tally for a given user, parsed from every other
        /// resident's <c>lists["offspring"]</c> entries. Each entry has the shape
        /// <c>"yyyy-MM-dd: monsterType brood of N (parent: SireName)"</c>; this method picks
        /// the entries where the sire matches and accumulates brood sizes per monster type.
        /// </summary>
        public static Dictionary<string, int> SiredByMonsterType(IEnumerable<Profile> profiles, string sireUserName)
        {
            var result = new Dictionary<string, int>();
            if (profiles == null || string.IsNullOrEmpty(sireUserName)) return result;
            string marker = "(parent: " + sireUserName + ")";
            foreach (var profile in profiles)
            {
                if (profile?.lists == null) continue;
                if (!profile.lists.ContainsKey("offspring")) continue;
                foreach (var raw in profile.lists["offspring"])
                {
                    if (string.IsNullOrEmpty(raw)) continue;
                    if (raw.IndexOf(marker, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (TryParseOffspringEntry(raw, out string monsterType, out int broodSize))
                    {
                        if (!result.ContainsKey(monsterType)) result[monsterType] = 0;
                        result[monsterType] += broodSize;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Aggregated per-resident "Birthed" tally for a given user, parsed from their own
        /// <c>lists["offspring"]</c>. Sums brood sizes per monster type.
        /// </summary>
        public static Dictionary<string, int> BirthedByMonsterType(Profile carrier)
        {
            var result = new Dictionary<string, int>();
            if (carrier?.lists == null) return result;
            if (!carrier.lists.ContainsKey("offspring")) return result;
            foreach (var raw in carrier.lists["offspring"])
            {
                if (string.IsNullOrEmpty(raw)) continue;
                if (TryParseOffspringEntry(raw, out string monsterType, out int broodSize))
                {
                    if (!result.ContainsKey(monsterType)) result[monsterType] = 0;
                    result[monsterType] += broodSize;
                }
            }
            return result;
        }

        /// <summary>
        /// Parse an offspring-list entry, expected shape
        /// <c>"yyyy-MM-dd: monsterType brood of N (parent: ...)"</c>. Returns false if the
        /// row doesn't conform (corrupted blob, future format change, etc.).
        /// </summary>
        public static bool TryParseOffspringEntry(string raw, out string monsterType, out int broodSize)
        {
            monsterType = null;
            broodSize = 0;
            if (string.IsNullOrEmpty(raw)) return false;
            int colon = raw.IndexOf(": ", StringComparison.Ordinal);
            if (colon < 0) return false;
            int broodMarker = raw.IndexOf(" brood of ", colon, StringComparison.OrdinalIgnoreCase);
            if (broodMarker < 0) return false;
            monsterType = raw.Substring(colon + 2, broodMarker - colon - 2).Trim().ToLowerInvariant();
            int broodStart = broodMarker + " brood of ".Length;
            int broodEnd = raw.IndexOf(' ', broodStart);
            if (broodEnd < 0) broodEnd = raw.Length;
            string broodText = raw.Substring(broodStart, broodEnd - broodStart);
            return int.TryParse(broodText, out broodSize);
        }
    }
}
