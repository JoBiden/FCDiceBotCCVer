using FChatDicebot.BotCommands.Support;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Pure unit tests for the B12 random-events engine — selection, response validation, winner
    /// rules, reward application, and the in-memory scheduler. Profiles live in an in-memory
    /// dictionary (no Mongo) via the engine's injected accessors.
    /// </summary>
    public class RandomEventEngineTests
    {
        private const string Channel = "ADH-testchannel";

        // ---- in-memory profile store wired into the engine ----
        private readonly Dictionary<string, Profile> _profiles = new Dictionary<string, Profile>();
        private int _setProfileCalls;

        private RandomEventEngine NewEngine(int seed = 12345)
        {
            return new RandomEventEngine(
                userName => _profiles.TryGetValue(userName, out var p) ? p : null,
                (userName, p) => { _profiles[userName] = p; _setProfileCalls++; },
                new Random(seed));
        }

        private Profile AddProfile(string userName, string displayName = null)
        {
            var p = new Profile { userName = userName, displayName = displayName ?? userName };
            _profiles[userName] = p;
            return p;
        }

        private static EventReward Reward(string type, string key, int min, int max)
        {
            return new EventReward { type = type, key = key, min = min, max = max };
        }

        private static RandomEvent EventWith(string responseType, string winnerRule, int winnerN = 0,
            int windowSeconds = 60, params EventReward[] rewards)
        {
            return new RandomEvent
            {
                label = "test",
                weight = 1,
                announceText = "An event happens.",
                responseType = responseType,
                responseWindowSeconds = windowSeconds,
                winnerRule = winnerRule,
                winnerN = winnerN,
                outcomes = new List<EventOutcome>
                {
                    new EventOutcome { weight = 1, resultText = "Resolved!", rewards = rewards.ToList() }
                }
            };
        }

        // =========================== Selection ===========================

        [Fact]
        public void SelectByWeight_SkipsZeroWeightEntries()
        {
            var a = new RandomEvent { label = "a", weight = 0 };
            var b = new RandomEvent { label = "b", weight = 5 };
            var rng = new Random(1);

            for (int i = 0; i < 50; i++)
                Assert.Equal("b", RandomEventEngine.SelectByWeight(new List<RandomEvent> { a, b }, rng).label);
        }

        [Fact]
        public void SelectByWeight_EmptyOrSingle()
        {
            Assert.Null(RandomEventEngine.SelectByWeight(new List<RandomEvent>(), new Random(1)));
            Assert.Null(RandomEventEngine.SelectByWeight(null, new Random(1)));

            var only = new RandomEvent { label = "only", weight = 0 };
            Assert.Same(only, RandomEventEngine.SelectByWeight(new List<RandomEvent> { only }, new Random(1)));
        }

        [Fact]
        public void SelectByWeight_AllZero_FallsBackToUniform()
        {
            var a = new RandomEvent { label = "a", weight = 0 };
            var b = new RandomEvent { label = "b", weight = 0 };
            Assert.NotNull(RandomEventEngine.SelectByWeight(new List<RandomEvent> { a, b }, new Random(1)));
        }

        // =========================== Response validation ===========================

        [Fact]
        public void Validation_Keyword_CaseInsensitive_RejectsOthers()
        {
            var engine = NewEngine();
            var ae = engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeKeyword, RandomEventEngine.WinnerRuleFirstValid), DateTime.UtcNow);

            Assert.True(engine.IsValidArg(ae, ae.Keyword.ToUpperInvariant()));
            Assert.True(engine.IsValidArg(ae, ae.Keyword));
            Assert.False(engine.IsValidArg(ae, "definitely-not-the-token"));
        }

        [Fact]
        public void Validation_Challenge_AcceptsOnlyCorrectAnswer()
        {
            var engine = NewEngine();
            var ae = engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeChallenge, RandomEventEngine.WinnerRuleFirstValid), DateTime.UtcNow);

            Assert.True(engine.IsValidArg(ae, ae.ChallengeAnswer));
            Assert.False(engine.IsValidArg(ae, "999999"));
        }

        [Fact]
        public void Validation_None_AcceptsBareAndAnything()
        {
            var engine = NewEngine();
            var ae = engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleFirstValid), DateTime.UtcNow);

            Assert.True(engine.IsValidArg(ae, ""));
            Assert.True(engine.IsValidArg(ae, "whatever"));
        }

        [Fact]
        public void HandleRandom_NoActiveEvent_RepliesNoEvent()
        {
            var engine = NewEngine();
            var result = engine.HandleRandom(Channel, "Alice", "", DateTime.UtcNow);
            Assert.Equal(RandomEventEngine.NoEventMessage, result.ReplyToUser);
        }

        [Fact]
        public void HandleRandom_WrongKeyword_DoesNotConsumeShot()
        {
            var engine = NewEngine();
            AddProfile("Alice");
            var ae = engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeKeyword, RandomEventEngine.WinnerRuleFirstValid), DateTime.UtcNow);

            var wrong = engine.HandleRandom(Channel, "Alice", "wrong-token", DateTime.UtcNow);
            Assert.NotNull(wrong.ReplyToUser);
            Assert.Null(wrong.ChannelAnnouncement);
            Assert.Empty(ae.Responders);
            Assert.True(engine.HasActiveEvent(Channel)); // still open — they can retry

            var right = engine.HandleRandom(Channel, "Alice", ae.Keyword, DateTime.UtcNow);
            Assert.NotNull(right.ChannelAnnouncement);
        }

        // =========================== Winner rules ===========================

        [Fact]
        public void Winner_FirstValid_ResolvesOnFirstResponder()
        {
            var engine = NewEngine();
            AddProfile("Alice");
            AddProfile("Bob");
            engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleFirstValid,
                rewards: Reward("currency", "rosequartz", 5, 5)), DateTime.UtcNow);

            var first = engine.HandleRandom(Channel, "Alice", "", DateTime.UtcNow);
            Assert.Contains("[user]Alice[/user]", first.ChannelAnnouncement);
            Assert.False(engine.HasActiveEvent(Channel));

            // Bob is too late — event already resolved.
            var late = engine.HandleRandom(Channel, "Bob", "", DateTime.UtcNow);
            Assert.Equal(RandomEventEngine.NoEventMessage, late.ReplyToUser);
        }

        [Fact]
        public void Winner_Nth_SelectsExactlyTheNthResponder()
        {
            var engine = NewEngine();
            AddProfile("Alice");
            AddProfile("Bob");
            AddProfile("Carol");
            engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleNth, winnerN: 2,
                rewards: Reward("currency", "rosequartz", 3, 3)), DateTime.UtcNow);

            Assert.Null(engine.HandleRandom(Channel, "Alice", "", DateTime.UtcNow).ChannelAnnouncement); // pending
            var second = engine.HandleRandom(Channel, "Bob", "", DateTime.UtcNow);

            Assert.NotNull(second.ChannelAnnouncement);
            Assert.Contains("[user]Bob[/user]", second.ChannelAnnouncement);
            Assert.DoesNotContain("[user]Alice[/user]", second.ChannelAnnouncement);
            Assert.False(engine.HasActiveEvent(Channel));
        }

        [Fact]
        public void Winner_AllInWindow_GrantsEveryValidResponder_DuplicatesIgnored()
        {
            var engine = NewEngine();
            AddProfile("Alice");
            AddProfile("Bob");
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleAllInWindow,
                windowSeconds: 60, rewards: Reward("currency", "rosequartz", 2, 2)), t0);

            engine.HandleRandom(Channel, "Alice", "", t0);
            engine.HandleRandom(Channel, "Alice", "", t0); // duplicate ignored
            engine.HandleRandom(Channel, "Bob", "", t0);

            // Window elapses → resolved by the scheduler tick.
            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());
            string announcement = string.Join("\n", output);

            Assert.Contains("[user]Alice[/user]", announcement);
            Assert.Contains("[user]Bob[/user]", announcement);
            Assert.False(engine.HasActiveEvent(Channel));
            Assert.Equal(2, _profiles["Alice"].currencies["rosequartz"]); // granted once despite the dup
            Assert.Equal(2, _profiles["Bob"].currencies["rosequartz"]);
        }

        [Fact]
        public void Winner_Random_PicksExactlyOneResponder()
        {
            var engine = NewEngine();
            AddProfile("Alice");
            AddProfile("Bob");
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleRandom,
                windowSeconds: 60, rewards: Reward("currency", "rosequartz", 4, 4)), t0);

            engine.HandleRandom(Channel, "Alice", "", t0);
            engine.HandleRandom(Channel, "Bob", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());
            string announcement = string.Join("\n", output);

            int winners = (announcement.Contains("[user]Alice[/user]") ? 1 : 0) + (announcement.Contains("[user]Bob[/user]") ? 1 : 0);
            Assert.Equal(1, winners);
        }

        // =========================== Reward application ===========================

        [Fact]
        public void Reward_Currency_AddsToWalletCreatingKey()
        {
            var p = new Profile { userName = "Alice" };
            string frag = RandomEventEngine.ApplyEventReward(p, Reward("currency", "rosequartz", 5, 5), new Random(1));
            Assert.Equal(5, p.currencies["rosequartz"]);
            Assert.Contains("5 rosequartz", frag);

            RandomEventEngine.ApplyEventReward(p, Reward("currency", "rosequartz", 3, 3), new Random(1));
            Assert.Equal(8, p.currencies["rosequartz"]);
        }

        [Fact]
        public void Reward_Title_AddedOnce_AsSystemTitle()
        {
            var p = new Profile { userName = "Alice" };
            string frag = RandomEventEngine.ApplyEventReward(p, Reward("title", "Lucky", 0, 0), new Random(1));
            Assert.Single(p.titles);
            Assert.True(p.titles[0].IsSystemTitle);
            Assert.Equal("Lucky", p.titles[0].titleText);
            Assert.Contains("Lucky", frag);

            // Idempotent: granting again does nothing and yields no fragment.
            string again = RandomEventEngine.ApplyEventReward(p, Reward("title", "Lucky", 0, 0), new Random(1));
            Assert.Single(p.titles);
            Assert.Equal("", again);
        }

        [Fact]
        public void Reward_Training_ClampedToHundred()
        {
            var p = new Profile { userName = "Alice" };
            p.trainings["magic"] = 95;
            string frag = RandomEventEngine.ApplyEventReward(p, Reward("training", "magic", 10, 10), new Random(1));
            Assert.Equal(TrainProcessor.LevelCap, p.trainings["magic"]); // 100, not 105
            Assert.Contains("5", frag); // only +5 actually gained

            // Already maxed → no gain, no fragment.
            string maxed = RandomEventEngine.ApplyEventReward(p, Reward("training", "magic", 10, 10), new Random(1));
            Assert.Equal("", maxed);
        }

        [Fact]
        public void Reward_Corruption_DecreasesSignedAxis_PurityIncreases()
        {
            var p = new Profile { userName = "Alice" };
            RandomEventEngine.ApplyEventReward(p, Reward("corruption", null, 5, 5), new Random(1));
            Assert.Equal("-5", p.characteristics[CorruptionProcessor.CorruptionCharacteristicKey]);

            RandomEventEngine.ApplyEventReward(p, Reward("purity", null, 7, 7), new Random(1));
            Assert.Equal("2", p.characteristics[CorruptionProcessor.CorruptionCharacteristicKey]); // -5 + 7
        }

        [Fact]
        public void Reward_Curse_AddsKnownCurseOnce_RejectsUnknown()
        {
            var p = new Profile { userName = "Alice" };
            string frag = RandomEventEngine.ApplyEventReward(p, Reward("curse", "poverty", 0, 0), new Random(1));
            Assert.Single(CurseInstance.LoadAll(p));
            Assert.Equal("poverty", CurseInstance.LoadAll(p)[0].Curse);
            Assert.Contains("poverty", frag);

            // Duplicate curse → no-op, no fragment.
            Assert.Equal("", RandomEventEngine.ApplyEventReward(p, Reward("curse", "poverty", 0, 0), new Random(1)));

            // Unknown curse id → nothing applied.
            Assert.Equal("", RandomEventEngine.ApplyEventReward(p, Reward("curse", "not-a-real-curse", 0, 0), new Random(1)));
            Assert.Single(CurseInstance.LoadAll(p));
        }

        [Fact]
        public void Reward_None_NoWriteNoFragment()
        {
            var p = new Profile { userName = "Alice" };
            string frag = RandomEventEngine.ApplyEventReward(p, Reward("none", null, 0, 0), new Random(1));
            Assert.Equal("", frag);
            Assert.Empty(p.currencies);
            Assert.Empty(p.titles);
        }

        [Fact]
        public void Resolution_SavesEachWinnerProfileOnce()
        {
            var engine = NewEngine();
            AddProfile("Alice");
            _setProfileCalls = 0;
            engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleFirstValid,
                rewards: Reward("currency", "rosequartz", 5, 5)), DateTime.UtcNow);

            engine.HandleRandom(Channel, "Alice", "", DateTime.UtcNow);
            Assert.Equal(1, _setProfileCalls);
        }

        [Fact]
        public void Resolution_WinnersPlaceholder_CarriesMultiPersonFlavor_NoRewardLineWhenNoneGranted()
        {
            var engine = NewEngine();
            AddProfile("Alice");
            AddProfile("Bob");
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = new RandomEvent
            {
                label = "test", weight = 1, announceText = "An event happens.",
                responseType = RandomEventEngine.ResponseTypeNone, responseWindowSeconds = 60,
                winnerRule = RandomEventEngine.WinnerRuleAllInWindow,
                outcomes = new List<EventOutcome>
                {
                    new EventOutcome
                    {
                        weight = 1,
                        resultText = "{winners} are now glowing purple.",
                        rewards = new List<EventReward> { Reward("none", null, 0, 0) }
                    }
                }
            };
            engine.ForceOpen(Channel, ev, t0);
            engine.HandleRandom(Channel, "Alice", "", t0);
            engine.HandleRandom(Channel, "Bob", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Equal(new List<string> { "[user]Alice[/user] and [user]Bob[/user] are now glowing purple." }, output);
        }

        [Fact]
        public void Resolution_GroupsIdenticalRewardsOntoOneLine_WithPluralVerb()
        {
            var engine = NewEngine();
            AddProfile("Alice");
            AddProfile("Bob");
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = new RandomEvent
            {
                label = "test", weight = 1, announceText = "An event happens.",
                responseType = RandomEventEngine.ResponseTypeNone, responseWindowSeconds = 60,
                winnerRule = RandomEventEngine.WinnerRuleAllInWindow,
                outcomes = new List<EventOutcome>
                {
                    new EventOutcome
                    {
                        weight = 1,
                        resultText = "Cutie giggles with delight.",
                        rewards = new List<EventReward> { Reward("currency", "rosequartz", 3, 3) }
                    }
                }
            };
            engine.ForceOpen(Channel, ev, t0);
            engine.HandleRandom(Channel, "Alice", "", t0);
            engine.HandleRandom(Channel, "Bob", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Equal("Cutie giggles with delight.\n[user]Alice[/user] and [user]Bob[/user] receive [b]3 rosequartz[/b]!", output[0]);
        }

        // =========================== Scheduler ===========================

        [Fact]
        public void Scheduler_DueFireIntoActiveChannel_OpensEvent_ThenOneAtATime()
        {
            var engine = NewEngine();
            AddProfile("Alice");
            var events = new List<RandomEvent> { EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleAllInWindow) };
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // First tick seeds the next-fire (6-8h out): nothing fires yet.
            Assert.Empty(engine.Tick(Channel, t0, () => events));
            Assert.False(engine.HasActiveEvent(Channel));

            // 9h later, with fresh activity, the due fire opens an event.
            DateTime t1 = t0.AddHours(9);
            engine.RecordActivity(Channel, t1);
            var fired = engine.Tick(Channel, t1, () => events);
            Assert.Single(fired);
            Assert.True(engine.HasActiveEvent(Channel));

            // One event at a time: an immediate second tick fires nothing more.
            Assert.Empty(engine.Tick(Channel, t1, () => events));
            Assert.True(engine.HasActiveEvent(Channel));
        }

        [Fact]
        public void Scheduler_DueFireIntoQuietChannel_SkipsAndDoesNotPost()
        {
            var engine = NewEngine();
            var events = new List<RandomEvent> { EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleAllInWindow) };
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            engine.Tick(Channel, t0, () => events); // seed
            // 9h later with NO recent activity → due but quiet → skip, no post, no active event.
            var output = engine.Tick(Channel, t0.AddHours(9), () => events);
            Assert.Empty(output);
            Assert.False(engine.HasActiveEvent(Channel));
        }

        [Fact]
        public void Scheduler_WindowElapse_ResolvesNonFirstValid()
        {
            var engine = NewEngine();
            AddProfile("Alice");
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleAllInWindow,
                windowSeconds: 30, rewards: Reward("currency", "rosequartz", 1, 1)), t0);
            engine.HandleRandom(Channel, "Alice", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(31), () => new List<RandomEvent>());
            Assert.Single(output);
            Assert.Contains("[user]Alice[/user]", output[0]);
            Assert.False(engine.HasActiveEvent(Channel));
        }

        [Fact]
        public void Scheduler_FirstValidWindowElapsesWithNoResponder_ClosesWithNoWinner()
        {
            var engine = NewEngine();
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleFirstValid, windowSeconds: 30), t0);

            var output = engine.Tick(Channel, t0.AddSeconds(31), () => new List<RandomEvent>());
            Assert.Single(output);
            Assert.False(engine.HasActiveEvent(Channel));
        }
    }
}
