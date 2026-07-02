using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FChatDicebot.BotCommands.Support
{
    /// <summary>
    /// The whole engine behind the B12 ambient "random events" system. Holds the per-channel
    /// in-memory scheduler state, fires a seeded <see cref="RandomEvent"/> into an opted-in
    /// channel every several hours, validates <c>!random</c> responses (defeating snipers per
    /// the event's anti-snipe rule), resolves the winner(s), and grants the rolled outcome's
    /// rewards to existing profile stores (currency / title / training / corruption / purity /
    /// curse).
    ///
    /// Deliberately lives outside <c>FChatDicebot.BotCommands</c> so the reflection-based command
    /// loader in <c>BotCommandController</c> doesn't try to <c>Activator.CreateInstance</c> it.
    ///
    /// Profile reads/writes go through injected delegates (in production, MonDB; in tests, an
    /// in-memory dictionary) so the entire fire → respond → resolve → grant path is unit-testable
    /// away from BotMain and Mongo. All in-memory state (active events, per-channel next-fire and
    /// last-activity timestamps, collected responders) is intentionally non-persistent and resets
    /// on restart — only the event definitions and granted rewards are durable.
    ///
    /// Thread-safety: the F-Chat receive thread (<c>!random</c>, activity) and the heartbeat thread
    /// (the scheduler tick) both touch this state, so every public entry point takes the single
    /// instance lock.
    /// </summary>
    public class RandomEventEngine
    {
        // ---- Tuning (owner-review defaults; see Random-Events.md "Open items") ----
        // Cadence: about one event per channel every 6-8 hours, with jitter.
        public const double IntervalMinHours = 6.0;
        public const double IntervalMaxHours = 8.0;
        // Activity gate: only fire into a channel that saw a human message this recently.
        public const int ActivityWindowMinutes = 15;
        // Fallback response window when an event leaves responseWindowSeconds at 0.
        public const int DefaultResponseWindowSeconds = 60;

        public const string ResponseTypeNone = "none";
        public const string ResponseTypeKeyword = "keyword";
        public const string ResponseTypeChallenge = "challenge";

        public const string WinnerRuleFirstValid = "firstValid";
        public const string WinnerRuleAllInWindow = "allInWindow";
        public const string WinnerRuleNth = "nth";
        public const string WinnerRuleRandom = "random";

        // Pool of simple tokens used to randomize the "keyword" anti-snipe arg per fire.
        public static readonly string[] KeywordPool = new string[]
        {
            "rose", "velvet", "candle", "satin", "amber", "ivory", "ribbon", "lace",
            "ember", "petal", "feather", "crimson", "violet", "honey", "pearl", "thorn",
        };

        private readonly object _lock = new object();
        private readonly Random _rng;
        private readonly Func<string, Profile> _getProfile;
        private readonly Action<string, Profile> _setProfile;

        // Per-channel state (keys are lowercased channel ids).
        private readonly Dictionary<string, ActiveRandomEvent> _active = new Dictionary<string, ActiveRandomEvent>();
        private readonly Dictionary<string, DateTime> _nextFire = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, DateTime> _lastActivity = new Dictionary<string, DateTime>();

        public RandomEventEngine(Func<string, Profile> getProfile, Action<string, Profile> setProfile, Random rng = null)
        {
            _getProfile = getProfile;
            _setProfile = setProfile;
            _rng = rng ?? new Random();
        }

        // ============================ Public entry points ============================

        /// <summary>Record that a human posted in a channel (drives the activity gate).</summary>
        public void RecordActivity(string channel, DateTime utcNow)
        {
            if (string.IsNullOrEmpty(channel)) return;
            lock (_lock)
            {
                _lastActivity[Key(channel)] = utcNow;
            }
        }

        /// <summary>
        /// Handle one <c>!random</c> from a resident. Returns what (if anything) to PM the
        /// responder and what (if anything) to announce publicly. An invalid response is nudged
        /// but does NOT consume the resident's shot (owner-chosen default) — the anti-snipe
        /// protection lives entirely in needing the right keyword/answer; a genuinely fast,
        /// correct response is never penalized just for being quick.
        /// </summary>
        public RandomResponseResult HandleRandom(string channel, string userName, string arg, DateTime utcNow)
        {
            lock (_lock)
            {
                string key = Key(channel);
                ActiveRandomEvent ae;
                if (!_active.TryGetValue(key, out ae) || ae.Resolved)
                    return RandomResponseResult.Reply(NoEventMessage);

                // keyword / challenge validation; a wrong arg is nudged without consuming the shot.
                if (!IsValidArg(ae, arg))
                    return RandomResponseResult.Reply(WrongArgMessage(ae));

                // One shot per resident per event.
                if (ae.Responders.Any(r => string.Equals(r, userName, StringComparison.OrdinalIgnoreCase)))
                    return RandomResponseResult.Reply(AlreadyRespondedMessage);

                ae.Responders.Add(userName);

                // firstValid resolves immediately on the first valid responder.
                if (IsRule(ae, WinnerRuleFirstValid))
                    return RandomResponseResult.Announce(ResolveLocked(key, ae, new List<string> { userName }));

                // nth resolves as soon as the N-th valid responder arrives.
                if (IsRule(ae, WinnerRuleNth) && ae.Responders.Count >= NthTarget(ae))
                    return RandomResponseResult.Announce(ResolveLocked(key, ae, null));

                // allInWindow / random / nth-not-yet: accept and wait for the window to close.
                return RandomResponseResult.Reply(AcceptedWaitMessage);
            }
        }

        /// <summary>
        /// One scheduler heartbeat for a single eligible channel. Seeds the first fire time,
        /// resolves an active event whose window has elapsed, and fires a new event when one is
        /// due and the channel is active. Returns the channel messages to post (0-2). The event
        /// list is loaded lazily so Mongo is only hit when an event is actually about to fire.
        /// </summary>
        public List<string> Tick(string channel, DateTime utcNow, Func<List<RandomEvent>> getEvents)
        {
            var output = new List<string>();
            lock (_lock)
            {
                string key = Key(channel);
                EnsureScheduledLocked(key, utcNow);

                // Resolve / time out any active event whose window has elapsed.
                ActiveRandomEvent ae;
                if (_active.TryGetValue(key, out ae) && !ae.Resolved && utcNow >= ae.WindowEndUtc)
                {
                    if (IsRule(ae, WinnerRuleFirstValid) && ae.Responders.Count == 0)
                    {
                        // firstValid that nobody won in time: close it out (next fire was already
                        // scheduled when this event fired).
                        _active.Remove(key);
                        output.Add(NoWinnerMessage(ae));
                    }
                    else
                    {
                        output.Add(ResolveLocked(key, ae, null));
                    }
                }

                // Fire a new event if one is due, none is active, and the room is awake. The next
                // fire is scheduled at fire time, so events stay 6-8h apart regardless of how long
                // resolution takes.
                if (!_active.ContainsKey(key) && IsFireDueLocked(key, utcNow))
                {
                    if (IsChannelActiveLocked(key, utcNow))
                    {
                        List<RandomEvent> events = getEvents != null ? getEvents() : null;
                        ActiveRandomEvent fired = OpenEventLocked(key, channel, events, utcNow);
                        RescheduleLocked(key, utcNow);
                        if (fired != null)
                            output.Add(fired.AnnounceText);
                    }
                    else
                    {
                        // Due, but the room is quiet: skip and reschedule (don't spam a dead room).
                        RescheduleLocked(key, utcNow);
                    }
                }
            }
            return output;
        }

        // ============================ Test / inspection helpers ============================

        public bool HasActiveEvent(string channel)
        {
            lock (_lock) { return _active.ContainsKey(Key(channel)); }
        }

        public ActiveRandomEvent PeekActiveEvent(string channel)
        {
            lock (_lock)
            {
                ActiveRandomEvent ae;
                return _active.TryGetValue(Key(channel), out ae) ? ae : null;
            }
        }

        /// <summary>
        /// Test seam: open a specific event immediately, bypassing the weighted scheduler so a
        /// test can exercise validation / winner / resolution paths deterministically. Not used in
        /// production (the scheduler fires events via <see cref="Tick"/>).
        /// </summary>
        public ActiveRandomEvent ForceOpen(string channel, RandomEvent ev, DateTime utcNow)
        {
            lock (_lock)
            {
                return OpenEventLocked(Key(channel), channel, new List<RandomEvent> { ev }, utcNow);
            }
        }

        // ============================ Resolution ============================

        // Assumes the instance lock is held. Marks the event resolved, removes it from the active
        // map, grants the rolled outcome's rewards to each winner, and returns the public
        // announcement. The next fire was already scheduled when this event fired.
        //
        // Multi-winner support: the outcome's own resultText is expected to carry whatever
        // flavor applies to every winner together (via the {winners} placeholder — e.g. "{winners}
        // are now glowing purple."), rather than the engine bolting on a generic "joins in" line.
        // Reward grants are still per-winner (magnitudes are re-rolled per person), but winners
        // who end up with identical reward fragments are combined onto one line so identical
        // grants read as one clean sentence instead of a repeated line per person.
        private string ResolveLocked(string key, ActiveRandomEvent ae, List<string> winnersOverride)
        {
            ae.Resolved = true;
            _active.Remove(key);

            List<string> winners = winnersOverride ?? ResolveWinners(ae.Event.winnerRule, NthTarget(ae), ae.Responders, _rng);
            if (winners == null || winners.Count == 0)
                return NoWinnerMessage(ae);

            EventOutcome outcome = SelectOutcome(ae.Event, _rng);

            // Apply rewards per winner (each profile saved once) and collect their fragments.
            var fragmentsByWinner = new Dictionary<string, List<string>>();
            foreach (string winner in winners)
            {
                Profile p = _getProfile != null ? _getProfile(winner) : null;
                var fragments = new List<string>();
                if (p != null && outcome != null && outcome.rewards != null)
                {
                    foreach (EventReward reward in outcome.rewards)
                    {
                        string frag = ApplyEventReward(p, reward, _rng);
                        if (!string.IsNullOrEmpty(frag))
                            fragments.Add(frag);
                    }
                    if (_setProfile != null)
                        _setProfile(winner, p);
                }
                fragmentsByWinner[winner] = fragments;
            }

            StringBuilder sb = new StringBuilder();
            string header = outcome != null ? SubstituteWinners(outcome.resultText, winners) : null;
            if (!string.IsNullOrEmpty(header))
                sb.Append(header);

            // Group winners with identical reward fragments (in first-seen order) into one line
            // each. A winner with no reward at all gets no line — the header already covers the
            // flavor for everyone via {winners}.
            var groups = new List<WinnerGroup>();
            foreach (string winner in winners)
            {
                List<string> fragments = fragmentsByWinner[winner];
                if (fragments.Count == 0) continue;

                WinnerGroup existing = groups.FirstOrDefault(g => g.Fragments.SequenceEqual(fragments));
                if (existing != null)
                    existing.Names.Add(winner);
                else
                    groups.Add(new WinnerGroup { Fragments = fragments, Names = new List<string> { winner } });
            }

            foreach (WinnerGroup group in groups)
            {
                List<string> tags = group.Names.Select(n => "[user]" + n + "[/user]").ToList();
                string verb = tags.Count > 1 ? " receive " : " receives ";
                if (sb.Length > 0) sb.Append("\n");
                sb.Append(JoinWithAnd(tags) + verb + JoinWithAnd(group.Fragments) + "!");
            }

            return sb.ToString();
        }

        private class WinnerGroup
        {
            public List<string> Fragments;
            public List<string> Names;
        }

        // ============================ Firing / materialization ============================

        // Assumes the instance lock is held. Picks an event by weight, materializes the
        // per-fire anti-snipe state, stores it active, and returns it (null if no events).
        private ActiveRandomEvent OpenEventLocked(string key, string channel, List<RandomEvent> events, DateTime utcNow)
        {
            RandomEvent chosen = SelectByWeight(events, _rng);
            if (chosen == null) return null;

            int windowSeconds = chosen.responseWindowSeconds > 0 ? chosen.responseWindowSeconds : DefaultResponseWindowSeconds;
            var ae = new ActiveRandomEvent
            {
                Event = chosen,
                Channel = channel,
                FiredAtUtc = utcNow,
                WindowEndUtc = utcNow.AddSeconds(windowSeconds),
            };

            string announce = chosen.announceText ?? "";
            string type = (chosen.responseType ?? ResponseTypeNone).ToLowerInvariant();
            switch (type)
            {
                case ResponseTypeKeyword:
                    ae.Keyword = KeywordPool[_rng.Next(KeywordPool.Length)];
                    announce = Substitute(announce, ae.Keyword, "", windowSeconds);
                    if (!announce.Contains(ae.Keyword))
                        announce += "\n[sub]Quick, [b]!random " + ae.Keyword + "[/b] to take part![/sub]";
                    break;
                case ResponseTypeChallenge:
                    string problem;
                    GenerateChallenge(_rng, out problem, out string answer);
                    ae.ChallengeAnswer = answer;
                    announce = Substitute(announce, "", problem, windowSeconds);
                    if (!announce.Contains(problem))
                        announce += "\n[sub]Quick, [b]!random " + problem + " = ?[/b] to take part![/sub]";
                    break;
                default:
                    announce = Substitute(announce, "", "", windowSeconds);
                    break;
            }

            ae.AnnounceText = announce;
            _active[key] = ae;
            return ae;
        }

        // ============================ Scheduling (lock-held) ============================

        private void EnsureScheduledLocked(string key, DateTime utcNow)
        {
            if (!_nextFire.ContainsKey(key))
                _nextFire[key] = utcNow.Add(RandomInterval());
        }

        private void RescheduleLocked(string key, DateTime utcNow)
        {
            _nextFire[key] = utcNow.Add(RandomInterval());
        }

        private bool IsFireDueLocked(string key, DateTime utcNow)
        {
            DateTime when;
            return _nextFire.TryGetValue(key, out when) && utcNow >= when;
        }

        private bool IsChannelActiveLocked(string key, DateTime utcNow)
        {
            DateTime when;
            return _lastActivity.TryGetValue(key, out when)
                && (utcNow - when) <= TimeSpan.FromMinutes(ActivityWindowMinutes);
        }

        private TimeSpan RandomInterval()
        {
            double hours = IntervalMinHours + _rng.NextDouble() * (IntervalMaxHours - IntervalMinHours);
            return TimeSpan.FromHours(hours);
        }

        // ============================ Pure helpers (unit-tested directly) ============================

        /// <summary>Weighted pick over candidate events; null if the list is empty.</summary>
        public static RandomEvent SelectByWeight(List<RandomEvent> events, Random rng)
        {
            if (events == null || events.Count == 0) return null;
            if (events.Count == 1) return events[0];

            int total = events.Sum(e => Math.Max(0, e.weight));
            if (total <= 0) return events[rng.Next(events.Count)]; // all weightless => uniform

            int pick = rng.Next(0, total);
            foreach (RandomEvent e in events)
            {
                pick -= Math.Max(0, e.weight);
                if (pick < 0) return e;
            }
            return events[events.Count - 1];
        }

        /// <summary>Weighted pick over an event's outcomes; null if it has none.</summary>
        public static EventOutcome SelectOutcome(RandomEvent ev, Random rng)
        {
            if (ev == null || ev.outcomes == null || ev.outcomes.Count == 0) return null;
            if (ev.outcomes.Count == 1) return ev.outcomes[0];

            int total = ev.outcomes.Sum(o => Math.Max(0, o.weight));
            if (total <= 0) return ev.outcomes[rng.Next(ev.outcomes.Count)];

            int pick = rng.Next(0, total);
            foreach (EventOutcome o in ev.outcomes)
            {
                pick -= Math.Max(0, o.weight);
                if (pick < 0) return o;
            }
            return ev.outcomes[ev.outcomes.Count - 1];
        }

        /// <summary>
        /// Resolve the winner list from the collected (ordered, deduped) responders per the rule.
        /// Pure — used by both the immediate firstValid path and the window-close path.
        /// </summary>
        public static List<string> ResolveWinners(string winnerRule, int nthTarget, List<string> responders, Random rng)
        {
            var empty = new List<string>();
            if (responders == null || responders.Count == 0) return empty;

            string rule = (winnerRule ?? WinnerRuleFirstValid).ToLowerInvariant();
            switch (rule)
            {
                case "allinwindow":
                    return new List<string>(responders);
                case "nth":
                    int n = Math.Max(1, nthTarget);
                    return responders.Count >= n ? new List<string> { responders[n - 1] } : empty;
                case "random":
                    return new List<string> { responders[rng.Next(responders.Count)] };
                case "firstvalid":
                default:
                    return new List<string> { responders[0] };
            }
        }

        /// <summary>Inclusive roll in [min, max]; tolerant of an inverted or degenerate range.</summary>
        public static int RollAmount(int min, int max, Random rng)
        {
            if (max < min) { int t = min; min = max; max = t; }
            if (max == min) return min;
            return rng.Next(min, max + 1); // +1: Random.Next upper bound is exclusive
        }

        /// <summary>
        /// Apply one reward to a profile in place and return a short human fragment describing it
        /// (or "" when nothing was granted — e.g. a maxed training, an unknown curse, or "none").
        /// Each branch reuses the same store the corresponding interaction writes to; corruption
        /// and curse are system-granted here, intentionally bypassing the consent flow (the player
        /// opted in by responding). Pure save-free: the caller persists the profile once after all
        /// rewards apply.
        /// </summary>
        public static string ApplyEventReward(Profile profile, EventReward reward, Random rng)
        {
            if (profile == null || reward == null) return "";
            string type = (reward.type ?? ResponseTypeNone).ToLowerInvariant();
            int amount = RollAmount(reward.min, reward.max, rng);

            switch (type)
            {
                case "currency":
                    if (string.IsNullOrEmpty(reward.key) || amount <= 0) return "";
                    if (profile.currencies == null) profile.currencies = new Dictionary<string, int>();
                    if (profile.currencies.ContainsKey(reward.key)) profile.currencies[reward.key] += amount;
                    else profile.currencies[reward.key] = amount;
                    return "[b]" + amount + " " + reward.key + "[/b]";

                case "title":
                    if (string.IsNullOrEmpty(reward.key)) return "";
                    if (profile.titles == null) profile.titles = new List<Title>();
                    bool alreadyHas = profile.titles.Any(t =>
                        t.IsSystemTitle && string.Equals(t.titleText, reward.key, StringComparison.OrdinalIgnoreCase));
                    if (alreadyHas) return "";
                    profile.titles.Add(new Title { titleText = reward.key, givenBy = "Chateau", grantedTime = DateTime.UtcNow });
                    return "the title ·" + reward.key + "·";

                case "training":
                    if (string.IsNullOrEmpty(reward.key) || amount <= 0) return "";
                    if (profile.trainings == null) profile.trainings = new Dictionary<string, int>();
                    int current = profile.trainings.ContainsKey(reward.key) ? profile.trainings[reward.key] : 0;
                    int capped = Math.Min(TrainProcessor.LevelCap, Math.Max(0, current + amount));
                    int gained = capped - current;
                    profile.trainings[reward.key] = capped;
                    if (gained <= 0) return "";
                    return "[b]" + gained + "[/b] " + reward.key + " training";

                case "corruption":
                    // Negative on the signed axis = corruption (see CorruptionProcessor).
                    if (amount <= 0) return "";
                    WriteCorruption(profile, CorruptionProcessor.ReadCorruption(profile) - amount);
                    return "[b]" + amount + "[/b] corruption";

                case "purity":
                    // Positive on the signed axis = purity.
                    if (amount <= 0) return "";
                    WriteCorruption(profile, CorruptionProcessor.ReadCorruption(profile) + amount);
                    return "[b]" + amount + "[/b] purity";

                case "curse":
                    if (string.IsNullOrEmpty(reward.key) || !CurseProcessor.CatalogMap.ContainsKey(reward.key)) return "";
                    List<CurseInstance> curses = CurseInstance.LoadAll(profile);
                    if (curses.Any(c => string.Equals(c.Curse, reward.key, StringComparison.OrdinalIgnoreCase))) return "";
                    curses.Add(new CurseInstance { Curse = reward.key, AppliedBy = "Chateau", AppliedAt = DateTime.UtcNow });
                    CurseInstance.SaveAll(profile, curses);
                    return "the [b]" + reward.key + "[/b] curse";

                default: // "none" and anything unrecognized: flavor only, no write.
                    return "";
            }
        }

        public bool IsValidArg(ActiveRandomEvent ae, string arg)
        {
            if (ae == null) return false;
            string type = (ae.Event.responseType ?? ResponseTypeNone).ToLowerInvariant();
            string trimmed = (arg ?? "").Trim();
            switch (type)
            {
                case ResponseTypeKeyword:
                    return string.Equals(trimmed, ae.Keyword, StringComparison.OrdinalIgnoreCase);
                case ResponseTypeChallenge:
                    return string.Equals(trimmed, ae.ChallengeAnswer, StringComparison.OrdinalIgnoreCase);
                default: // "none" ignores the arg entirely
                    return true;
            }
        }

        // ============================ Small private helpers ============================

        private static void WriteCorruption(Profile profile, int value)
        {
            if (profile.characteristics == null) profile.characteristics = new Dictionary<string, string>();
            profile.characteristics[CorruptionProcessor.CorruptionCharacteristicKey] = value.ToString();
        }

        private static void GenerateChallenge(Random rng, out string problem, out string answer)
        {
            int a = rng.Next(2, 10);
            int b = rng.Next(2, 10);
            problem = a + " + " + b;
            answer = (a + b).ToString();
        }

        private static string Substitute(string template, string keyword, string challenge, int windowSeconds)
        {
            if (string.IsNullOrEmpty(template)) return template ?? "";
            return template
                .Replace("{keyword}", keyword ?? "")
                .Replace("{challenge}", challenge ?? "")
                .Replace("{window}", windowSeconds.ToString())
                .Replace("{seconds}", windowSeconds.ToString());
        }

        private static string JoinWithAnd(List<string> parts)
        {
            if (parts == null || parts.Count == 0) return "";
            if (parts.Count == 1) return parts[0];
            if (parts.Count == 2) return parts[0] + " and " + parts[1];
            return string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[parts.Count - 1];
        }

        // Lets an outcome's resultText address every winner by name via a {winners} placeholder
        // (e.g. "{winners} are now glowing purple.") so multi-person flavor is authored per-event
        // rather than invented generically by the engine. A no-op when the placeholder is absent.
        private static string SubstituteWinners(string template, List<string> winners)
        {
            if (string.IsNullOrEmpty(template)) return template ?? "";
            if (!template.Contains("{winners}")) return template;
            List<string> tags = winners.Select(w => "[user]" + w + "[/user]").ToList();
            return template.Replace("{winners}", JoinWithAnd(tags));
        }

        private static bool IsRule(ActiveRandomEvent ae, string rule)
        {
            return string.Equals(ae.Event.winnerRule, rule, StringComparison.OrdinalIgnoreCase);
        }

        private static int NthTarget(ActiveRandomEvent ae)
        {
            return Math.Max(1, ae.Event.winnerN);
        }

        private static string Key(string channel)
        {
            return (channel ?? "").ToLowerInvariant();
        }

        // ============================ Framework user-facing strings (owner review) ============================

        public const string NoEventMessage =
            "There's no random event for you to respond to right now. When there is, everyone will be explicitly prompted.";
        public const string AlreadyRespondedMessage =
            "You can't participate twice! Wait for next event.";
        public const string AcceptedWaitMessage =
            "You're in! We'll announce the results once everyone has had some time to join.";

        public static string WrongArgMessage(ActiveRandomEvent ae)
        {
            string type = (ae?.Event?.responseType ?? "").ToLowerInvariant();
            if (type == ResponseTypeChallenge)
                return "That's not quite the answer this one's looking for. Give it another read and try again!";
            return "That's not the word this one's looking for. Try again!";
        }

        public static string NoWinnerMessage(ActiveRandomEvent ae)
        {
            return "Time's up for now. Until next time~";
        }
    }

    /// <summary>One in-flight event in a channel. In-memory only; never persisted.</summary>
    public class ActiveRandomEvent
    {
        public RandomEvent Event;
        public string Channel;
        public DateTime FiredAtUtc;
        public DateTime WindowEndUtc;
        public string AnnounceText;          // fully materialized announce string (placeholders resolved)
        public string Keyword;               // required token for the "keyword" type
        public string ChallengeAnswer;       // expected answer for the "challenge" type
        public List<string> Responders = new List<string>(); // valid responders, in order, deduped
        public bool Resolved;
    }

    /// <summary>
    /// The result of one <c>!random</c>: an optional private reply to the responder and an optional
    /// public channel announcement. The command sends whichever are non-empty.
    /// </summary>
    public class RandomResponseResult
    {
        public string ReplyToUser;
        public string ChannelAnnouncement;

        public static RandomResponseResult Reply(string message)
        {
            return new RandomResponseResult { ReplyToUser = message };
        }

        public static RandomResponseResult Announce(string message)
        {
            return new RandomResponseResult { ChannelAnnouncement = message };
        }
    }
}
