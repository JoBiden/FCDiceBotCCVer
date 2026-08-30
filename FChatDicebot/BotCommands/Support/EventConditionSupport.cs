using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.BotCommands.Support
{
    /// <summary>
    /// Evaluates the authored <see cref="EventCondition"/> gates that make a random event's
    /// outcomes sensitive to the winner who is about to receive them ("the corrupted are flipped,
    /// the pure are blessed, everyone else gets pocket change").
    ///
    /// The whole system is one idea: <b>every stat projects a profile down to a single integer,
    /// and a condition is an inclusive range on that integer.</b> That removes any operator
    /// vocabulary (atLeast / atMost / has / lacks) and — the reason this exists rather than
    /// reusing the duty <see cref="Conditional"/> — it expresses the negative half of a signed
    /// axis. Corruption is stored signed (negative = corrupt, positive = pure), so "the corrupted"
    /// is <c>max: -10</c>, "the pure" is <c>min: 10</c>, and "the untouched middle" is
    /// <c>min: -9, max: 9</c>. The duty conditional's three-letter-prefix plus single "at least"
    /// comparison cannot say any of that.
    ///
    /// <b>Key semantics are per stat, and consistently shaped:</b> for the stats backed by a list
    /// (title / curse / parasite / vice / pregnancy / collectible) a blank key projects "how many
    /// do they have at all", and a named key narrows to that one. So a titles-held count needs no
    /// separate stat — it is <c>{stat:"title"}</c> with no key. The dictionary-backed stats
    /// (currency / training / job / count) have no meaningful total, so they require a key.
    ///
    /// <b>Failure direction is deliberate.</b> An unknown stat, or a blank key where one is
    /// required, makes the condition <i>fail</i> rather than pass. A typo therefore kills the
    /// branch it was written on and the winner falls through to an unconditional outcome; the
    /// alternative (treating malformed as satisfied) would silently hand out the wrong branch.
    /// This mirrors the <see cref="DutyConditionalSupport"/> rule that a malformed conditional
    /// must never crash the roll — here it also must never widen it.
    ///
    /// Every projection is a pure read. Nothing in this class mutates a profile.
    /// </summary>
    public static class EventConditionSupport
    {
        // Signed corruption axis; key is ignored. Negative = corrupt, positive = pure.
        public const string StatCorruption = "corruption";
        // Dictionary-backed stats: a key is required (there is no meaningful "total").
        public const string StatCurrency = "currency";
        public const string StatTraining = "training";
        public const string StatJob = "job";
        public const string StatCount = "count";
        // List-backed stats: blank key = how many they hold, named key = narrowed to that one.
        public const string StatTitle = "title";
        public const string StatCurse = "curse";
        public const string StatParasite = "parasite";
        public const string StatVice = "vice";
        public const string StatPregnancy = "pregnancy";
        public const string StatCollectible = "collectible";

        /// <summary>
        /// The authorable stat vocabulary, in the order the builder UI lists them. Adding a stat
        /// means adding a const, an entry here, and a case in <see cref="TryProject"/>.
        /// </summary>
        public static readonly string[] Stats = new string[]
        {
            StatCorruption, StatCurrency, StatTraining, StatJob, StatCount,
            StatTitle, StatCurse, StatParasite, StatVice, StatPregnancy, StatCollectible,
        };

        /// <summary>Stats whose projection is meaningless without a key.</summary>
        public static bool RequiresKey(string stat)
        {
            string s = Normalize(stat);
            return s == StatCurrency || s == StatTraining || s == StatJob || s == StatCount;
        }

        /// <summary>
        /// Project a profile to the single integer this stat measures. Returns false (with
        /// <paramref name="value"/> zeroed) for an unknown stat or a missing required key, which
        /// callers treat as "the condition does not match".
        /// </summary>
        public static bool TryProject(Profile profile, string stat, string key, out int value)
        {
            value = 0;
            if (profile == null) return false;

            string s = Normalize(stat);
            string k = (key ?? "").Trim();
            if (RequiresKey(s) && k.Length == 0) return false;

            switch (s)
            {
                case StatCorruption:
                    value = CorruptionProcessor.ReadCorruption(profile);
                    return true;

                case StatCurrency:
                    value = LookupInt(profile.currencies, k);
                    return true;

                case StatTraining:
                    value = LookupInt(profile.trainings, k);
                    return true;

                case StatJob:
                    value = LookupInt(profile.jobExperience, k);
                    return true;

                case StatCount:
                    value = LookupInt(profile.counts, k);
                    return true;

                case StatTitle:
                    if (profile.titles == null) return true;
                    value = k.Length == 0
                        ? profile.titles.Count
                        : profile.titles.Count(t => t != null && Same(t.titleText, k));
                    return true;

                case StatCurse:
                    value = CountInstances(CurseInstance.LoadAll(profile).Select(c => c.Curse), k);
                    return true;

                case StatParasite:
                    value = CountInstances(ParasiteInstance.LoadAll(profile).Select(p => p.Parasite), k);
                    return true;

                case StatVice:
                    // A named vice projects its ADDICTION LEVEL (1-10, 0 when absent) rather than
                    // a plain 0/1, because that is the number an author actually wants to gate on
                    // ("the deeply hooked"). A min of 1 still reads as "has this vice" either way.
                    // A blank key falls back to the list-count shape the other list stats use.
                    {
                        List<ViceInstance> vices = ViceInstance.LoadAll(profile);
                        if (k.Length == 0) { value = vices.Count; return true; }
                        ViceInstance match = vices.FirstOrDefault(v => v != null && Same(v.Vice, k));
                        value = match != null ? match.AddictionLevel : 0;
                        return true;
                    }

                case StatPregnancy:
                    // Only active pregnancies live in the list — !birth removes them — so the
                    // count is "currently carrying", not "ever conceived".
                    if (profile.pregnancies == null) return true;
                    value = k.Length == 0
                        ? profile.pregnancies.Count
                        : profile.pregnancies.Count(p => p != null && Same(p.MonsterType, k));
                    return true;

                case StatCollectible:
                    if (profile.collectibles == null) return true;
                    value = k.Length == 0
                        ? profile.collectibles.Count
                        : profile.collectibles.Count(c => c != null && Same(c.TypeLabel, k));
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Does this profile satisfy one condition? A null condition is no constraint at all
        /// (true); an unprojectable one is false. Bounds are inclusive and independently optional.
        /// </summary>
        public static bool Matches(Profile profile, EventCondition condition)
        {
            if (condition == null) return true;

            int value;
            if (!TryProject(profile, condition.stat, condition.key, out value)) return false;
            if (condition.min.HasValue && value < condition.min.Value) return false;
            if (condition.max.HasValue && value > condition.max.Value) return false;
            return true;
        }

        /// <summary>All conditions must hold (an empty or null list is unconditional).</summary>
        public static bool MatchesAll(Profile profile, List<EventCondition> conditions)
        {
            if (conditions == null || conditions.Count == 0) return true;
            return conditions.All(c => Matches(profile, c));
        }

        /// <summary>
        /// Does this event author ANY outcome condition? This is the switch between the original
        /// "roll one outcome and share it across every winner" behavior and the per-winner roll —
        /// an event with no conditions anywhere behaves exactly as it did before conditions
        /// existed, so no authored event changes meaning by being loaded into the new engine.
        /// </summary>
        public static bool HasAnyConditions(RandomEvent ev)
        {
            if (ev == null || ev.outcomes == null) return false;
            return ev.outcomes.Any(o => o != null && o.conditions != null && o.conditions.Count > 0);
        }

        /// <summary>The subset of an event's outcomes this winner is eligible for (never null).</summary>
        public static List<EventOutcome> EligibleOutcomes(RandomEvent ev, Profile profile)
        {
            if (ev == null || ev.outcomes == null) return new List<EventOutcome>();
            return ev.outcomes.Where(o => o != null && MatchesAll(profile, o.conditions)).ToList();
        }

        // ============================ Small private helpers ============================

        private static string Normalize(string stat)
        {
            return (stat ?? "").Trim().ToLowerInvariant();
        }

        private static bool Same(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static int LookupInt(Dictionary<string, int> store, string key)
        {
            if (store == null) return 0;
            int value;
            if (store.TryGetValue(key, out value)) return value;
            // These dictionaries are authored by several different code paths, so fall back to a
            // case-insensitive scan rather than reporting zero for a key that differs only in
            // casing from what the event author typed.
            foreach (KeyValuePair<string, int> entry in store)
            {
                if (Same(entry.Key, key)) return entry.Value;
            }
            return 0;
        }

        private static int CountInstances(IEnumerable<string> names, string key)
        {
            List<string> all = names.Where(n => !string.IsNullOrEmpty(n)).ToList();
            return key.Length == 0 ? all.Count : all.Count(n => Same(n, key));
        }
    }
}
