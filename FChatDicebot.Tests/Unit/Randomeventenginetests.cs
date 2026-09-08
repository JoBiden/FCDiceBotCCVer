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
        private int _changeCurrencyCalls;

        private RandomEventEngine NewEngine(int seed = 12345, Action<string> log = null)
        {
            return new RandomEventEngine(
                userName => _profiles.TryGetValue(userName, out var p) ? p : null,
                (userName, p) => { _profiles[userName] = p; _setProfileCalls++; },
                // In-memory stand-in for the atomic ChangeCurrency $inc (B12-2): mutates the
                // stored profile's currency dict directly, bypassing the whole-profile save.
                (userName, key, amount) =>
                {
                    _changeCurrencyCalls++;
                    if (!_profiles.TryGetValue(userName, out var p)) return;
                    if (p.currencies == null) p.currencies = new Dictionary<string, int>();
                    if (p.currencies.ContainsKey(key)) p.currencies[key] += amount;
                    else p.currencies[key] = amount;
                },
                new Random(seed),
                log);
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
        public void OpenEventLocked_FlavorTextCoincidentallyContainsKeyword_StillShowsInstruction()
        {
            // Regression test for B12-4: the old check re-scanned the *substituted output*
            // for the keyword string, so flavor text that happened to mention the chosen
            // keyword word (unrelated to any {keyword} placeholder) suppressed the "Quick,
            // !random X" instruction players need to see to participate. The fix checks
            // whether the *template* actually used the {keyword} placeholder instead.
            var engine = NewEngine();
            var ev = new RandomEvent
            {
                label = "test",
                weight = 1,
                // Contains every possible KeywordPool word, so whichever one gets rolled is
                // "coincidentally" present here even though there's no {keyword} placeholder.
                announceText = "The air smells of rose, velvet, candle, satin, amber, ivory, ribbon, lace, ember, petal, feather, crimson, violet, honey, pearl, and thorn.",
                responseType = RandomEventEngine.ResponseTypeKeyword,
                responseWindowSeconds = 60,
                winnerRule = RandomEventEngine.WinnerRuleFirstValid,
                outcomes = new List<EventOutcome>
                {
                    new EventOutcome { weight = 1, resultText = "Resolved!", rewards = new List<EventReward>() }
                }
            };

            var ae = engine.ForceOpen(Channel, ev, DateTime.UtcNow);

            Assert.Contains("!random " + ae.Keyword, ae.AnnounceText);
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
        public void Reward_Currency_WithSink_DefersInsteadOfMutatingProfile()
        {
            // B12-2: when the caller supplies a currency sink, the rolled amount must go to
            // the sink (for an atomic $inc) and must NOT be written onto the profile object
            // that will later be saved whole.
            var p = new Profile { userName = "Alice" };
            var granted = new List<KeyValuePair<string, int>>();

            string frag = RandomEventEngine.ApplyEventReward(p, Reward("currency", "rosequartz", 5, 5),
                new Random(1), (key, amount) => granted.Add(new KeyValuePair<string, int>(key, amount)));

            Assert.Contains("5 rosequartz", frag);
            Assert.Single(granted);
            Assert.Equal("rosequartz", granted[0].Key);
            Assert.Equal(5, granted[0].Value);
            Assert.Empty(p.currencies);
        }

        [Fact]
        public void Resolve_CurrencyOnlyOutcome_CreditsAtomically_WithoutWholeProfileSave()
        {
            // B12-2 regression, resolution-level: a pure-currency outcome must be granted
            // entirely through the changeCurrency delegate, never through setProfile — the
            // whole-profile ReplaceOne is what silently reverted concurrent balance changes.
            var engine = NewEngine();
            AddProfile("Alice");
            engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleFirstValid,
                rewards: Reward("currency", "rosequartz", 5, 5)), DateTime.UtcNow);

            engine.HandleRandom(Channel, "Alice", "", DateTime.UtcNow);

            Assert.Equal(5, _profiles["Alice"].currencies["rosequartz"]);
            Assert.Equal(1, _changeCurrencyCalls);
            Assert.Equal(0, _setProfileCalls);
        }

        [Fact]
        public void Resolve_MixedOutcome_SavesProfileForTitle_ButStillCreditsCurrencyAtomically()
        {
            // An outcome that grants both a document-field reward (title) and a currency
            // reward saves the profile for the former but the currency still arrives via
            // the atomic delegate — the saved document must not carry the credit.
            var engine = NewEngine();
            AddProfile("Alice");
            engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleFirstValid,
                rewards: new[] { Reward("title", "Lucky", 0, 0), Reward("currency", "rosequartz", 3, 3) }),
                DateTime.UtcNow);

            engine.HandleRandom(Channel, "Alice", "", DateTime.UtcNow);

            Assert.Single(_profiles["Alice"].titles);
            Assert.Equal("Lucky", _profiles["Alice"].titles[0].titleText);
            Assert.Equal(3, _profiles["Alice"].currencies["rosequartz"]);
            Assert.Equal(1, _changeCurrencyCalls);
            Assert.Equal(1, _setProfileCalls);
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

        // ------------------------------------------------------------------
        // "invert": mirror the whole signed corruption axis. Always the full flip - min/max/key
        // are unused - which is why it dwarfs the per-day magnitude quota that gates the
        // player-driven !corrupt / !purify.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(-27, 27)]
        [InlineData(27, -27)]
        [InlineData(-1, 1)]
        [InlineData(250, -250)]
        public void Reward_Invert_MirrorsTheSignedAxis(int before, int after)
        {
            var p = new Profile { userName = "Alice" };
            p.characteristics[CorruptionProcessor.CorruptionCharacteristicKey] = before.ToString();

            RandomEventEngine.ApplyEventReward(p, Reward("invert", null, 0, 0), new Random(1));

            Assert.Equal(after.ToString(), p.characteristics[CorruptionProcessor.CorruptionCharacteristicKey]);
        }

        // Unlike every other reward, invert's fragment is a whole PREDICATE carrying its own
        // verb - an inversion is not something you "receive", it happens to what you already had.
        [Fact]
        public void Reward_Invert_FragmentIsASelfVerbedPredicateNamingBothSidesOfTheFlip()
        {
            var corrupted = new Profile { userName = "Alice" };
            corrupted.characteristics[CorruptionProcessor.CorruptionCharacteristicKey] = "-27";
            Assert.Equal("now {has|have} [b]27 purity[/b], inverted from [b]27 corruption[/b]",
                RandomEventEngine.ApplyEventReward(corrupted, Reward("invert", null, 0, 0), new Random(1)));

            var pure = new Profile { userName = "Bob" };
            pure.characteristics[CorruptionProcessor.CorruptionCharacteristicKey] = "27";
            Assert.Equal("now {has|have} [b]27 corruption[/b], inverted from [b]27 purity[/b]",
                RandomEventEngine.ApplyEventReward(pure, Reward("invert", null, 0, 0), new Random(1)));
        }

        [Fact]
        public void Reward_Invert_IsTheOnlySelfVerbedRewardType()
        {
            Assert.True(RandomEventEngine.IsSelfVerbedReward("invert"));
            Assert.True(RandomEventEngine.IsSelfVerbedReward("  Invert "));
            foreach (string other in new[] { "currency", "title", "training", "corruption", "purity", "curse", "none", null })
                Assert.False(RandomEventEngine.IsSelfVerbedReward(other));
        }

        // Dead neutral has nothing to mirror: no write and no fragment, which drops the winner
        // out of the reward lines entirely and leaves the outcome text to cover them.
        [Fact]
        public void Reward_Invert_AtZero_IsANoOpWithNoFragment()
        {
            var p = new Profile { userName = "Alice" };
            Assert.Equal("", RandomEventEngine.ApplyEventReward(p, Reward("invert", null, 0, 0), new Random(1)));
            Assert.False(p.characteristics.ContainsKey(CorruptionProcessor.CorruptionCharacteristicKey));

            var explicitZero = new Profile { userName = "Bob" };
            explicitZero.characteristics[CorruptionProcessor.CorruptionCharacteristicKey] = "0";
            Assert.Equal("", RandomEventEngine.ApplyEventReward(explicitZero, Reward("invert", null, 0, 0), new Random(1)));
            Assert.Equal("0", explicitZero.characteristics[CorruptionProcessor.CorruptionCharacteristicKey]);
        }

        // Only reachable from a hand-edited characteristic, but Math.Abs would throw on it.
        [Fact]
        public void Reward_Invert_IntMinValue_IsRefusedRatherThanThrowing()
        {
            var p = new Profile { userName = "Alice" };
            p.characteristics[CorruptionProcessor.CorruptionCharacteristicKey] = int.MinValue.ToString();
            Assert.Equal("", RandomEventEngine.ApplyEventReward(p, Reward("invert", null, 0, 0), new Random(1)));
            Assert.Equal(int.MinValue.ToString(), p.characteristics[CorruptionProcessor.CorruptionCharacteristicKey]);
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
            // Two document-field rewards still produce exactly ONE whole-profile save per
            // winner (not one per reward). Currency rewards no longer count toward this at
            // all — they bypass the profile save entirely via the atomic delegate (B12-2),
            // see Resolve_CurrencyOnlyOutcome_CreditsAtomically_WithoutWholeProfileSave.
            var engine = NewEngine();
            AddProfile("Alice");
            _setProfileCalls = 0;
            engine.ForceOpen(Channel, EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleFirstValid,
                rewards: new[] { Reward("title", "Lucky", 0, 0), Reward("training", "magic", 5, 5) }),
                DateTime.UtcNow);

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

        // ------------------------------------------------------------------
        // {singular|plural} count agreement in resultText. allInWindow is the only rule whose
        // winner count varies, so an outcome authored for a crowd used to read "Fia are all
        // officially cuties" when one person won (feedback 6a6fb15d).
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(1, "The winner is crowned.")]
        [InlineData(2, "The winner are crowned.")]
        [InlineData(3, "The winner are crowned.")]
        [InlineData(0, "The winner are crowned.")]
        public void CountAgreement_PicksBranchByWinnerCount(int winners, string expected)
        {
            Assert.Equal(expected, RandomEventEngine.ResolveCountAgreement("The winner {is|are} crowned.", winners));
        }

        [Fact]
        public void CountAgreement_ResolvesEveryAlternationIndependently()
        {
            const string template = "{winners} {is|are} now {a cutie|cuties}, and {its|their} day is made.";

            Assert.Equal("{winners} is now a cutie, and its day is made.",
                RandomEventEngine.ResolveCountAgreement(template, 1));
            Assert.Equal("{winners} are now cuties, and their day is made.",
                RandomEventEngine.ResolveCountAgreement(template, 4));
        }

        [Theory]
        [InlineData("{winners}")]
        [InlineData("{keyword}")]
        [InlineData("{challenge}")]
        [InlineData("{window}")]
        [InlineData("{seconds}")]
        public void CountAgreement_LeavesPipelessPlaceholdersAlone(string placeholder)
        {
            // Load-bearing: this is what makes every string authored before the syntax existed
            // safe, and eating one of these would be the worst failure this change could cause.
            Assert.Equal(placeholder, RandomEventEngine.ResolveCountAgreement(placeholder, 1));
            Assert.Equal(placeholder, RandomEventEngine.ResolveCountAgreement(placeholder, 5));
        }

        [Fact]
        public void CountAgreement_LeavesTextWithoutBracesUntouched()
        {
            const string plain = "Everyone follows her lead onto the dance floor. [b]Nice[/b] moves!";
            Assert.Equal(plain, RandomEventEngine.ResolveCountAgreement(plain, 1));
        }

        [Fact]
        public void CountAgreement_UnclosedBraceKeepsTheRestOfTheLine()
        {
            // An author's typo should cost a stray character, not the tail of their sentence.
            Assert.Equal("All done {is|are the winner.",
                RandomEventEngine.ResolveCountAgreement("All done {is|are the winner.", 1));
        }

        [Fact]
        public void CountAgreement_SecondPipeBelongsToThePluralBranch()
        {
            Assert.Equal("a", RandomEventEngine.ResolveCountAgreement("{a|b|c}", 1));
            Assert.Equal("b|c", RandomEventEngine.ResolveCountAgreement("{a|b|c}", 2));
        }

        [Fact]
        public void CountAgreement_HandlesNullAndEmpty()
        {
            Assert.Equal("", RandomEventEngine.ResolveCountAgreement(null, 1));
            Assert.Equal("", RandomEventEngine.ResolveCountAgreement("", 3));
        }

        [Fact]
        public void Resolution_PluralAuthoredText_ReadsCorrectlyForASingleWinner()
        {
            var engine = NewEngine();
            AddProfile("Fia");
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            engine.ForceOpen(Channel, PluralOutcomeEvent(), t0);
            engine.HandleRandom(Channel, "Fia", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            // The reported bug, inverted: one winner, singular grammar.
            Assert.Equal(new List<string> { "[user]Fia[/user] happens to get a taste of corruption." }, output);
        }

        [Fact]
        public void Resolution_PluralAuthoredText_StillReadsCorrectlyForSeveralWinners()
        {
            var engine = NewEngine();
            AddProfile("Fia");
            AddProfile("Celeste");
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            engine.ForceOpen(Channel, PluralOutcomeEvent(), t0);
            engine.HandleRandom(Channel, "Fia", "", t0);
            engine.HandleRandom(Channel, "Celeste", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Equal(
                new List<string> { "[user]Fia[/user] and [user]Celeste[/user] happen to get a taste of corruption." },
                output);
        }

        private static RandomEvent PluralOutcomeEvent()
        {
            return new RandomEvent
            {
                label = "test", weight = 1, announceText = "An event happens.",
                responseType = RandomEventEngine.ResponseTypeNone, responseWindowSeconds = 60,
                winnerRule = RandomEventEngine.WinnerRuleAllInWindow,
                outcomes = new List<EventOutcome>
                {
                    new EventOutcome
                    {
                        weight = 1,
                        resultText = "{winners} {happens|happen} to get a taste of corruption.",
                        rewards = new List<EventReward> { Reward("none", null, 0, 0) }
                    }
                }
            };
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
        public void Scheduler_DueFireIntoQuietChannel_ArmsWithoutPosting()
        {
            var engine = NewEngine();
            var events = new List<RandomEvent> { EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleAllInWindow) };
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            engine.Tick(Channel, t0, () => events); // seed
            // 9h later with NO recent activity → due but quiet → arm, no post, no active event.
            var output = engine.Tick(Channel, t0.AddHours(9), () => events);
            Assert.Empty(output);
            Assert.False(engine.HasActiveEvent(Channel));

            // Still quiet many hours later → the slot is held, not lost: still nothing posted and
            // still no active event (the pre-fix behavior would have discarded and rescheduled).
            var later = engine.Tick(Channel, t0.AddHours(20), () => events);
            Assert.Empty(later);
            Assert.False(engine.HasActiveEvent(Channel));
        }

        [Fact]
        public void Scheduler_QuietThenWakes_FiresAfterWakeDelay()
        {
            var engine = NewEngine();
            AddProfile("Alice");
            var events = new List<RandomEvent> { EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleAllInWindow) };
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            engine.Tick(Channel, t0, () => events);          // seed 6-8h out
            engine.Tick(Channel, t0.AddHours(9), () => events); // due + quiet → armed

            // Someone finally posts. The wake schedules the fire a short (< activity window)
            // delay out, so the immediate next tick must NOT fire on the exact first message.
            DateTime tWake = t0.AddHours(9);
            engine.RecordActivity(Channel, tWake);
            Assert.Empty(engine.Tick(Channel, tWake, () => events));
            Assert.False(engine.HasActiveEvent(Channel));

            // Once the wake delay (<= WakeDelayMaxMinutes) has elapsed with the room still active
            // (that max is below ActivityWindowMinutes, so the lone wake message still counts),
            // the fire lands.
            DateTime tAfterDelay = tWake.AddMinutes(RandomEventEngine.WakeDelayMaxMinutes + 0.001);
            var fired = engine.Tick(Channel, tAfterDelay, () => events);
            Assert.Single(fired);
            Assert.True(engine.HasActiveEvent(Channel));
        }

        [Fact]
        public void Scheduler_QuietArming_IsLoggedOnce()
        {
            var logs = new List<string>();
            var engine = NewEngine(log: logs.Add);
            var events = new List<RandomEvent> { EventWith(RandomEventEngine.ResponseTypeNone, RandomEventEngine.WinnerRuleAllInWindow) };
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            engine.Tick(Channel, t0, () => events); // seed
            engine.Tick(Channel, t0.AddHours(9), () => events);            // due + quiet → arm + log
            engine.Tick(Channel, t0.AddHours(9).AddMinutes(1), () => events); // still armed → no 2nd log

            Assert.Single(logs.Where(l => l.Contains("armed")));
        }

        [Fact]
        public void Scheduler_DueFireWithNoAuthoredEvents_LogsAndPostsNothing()
        {
            var logs = new List<string>();
            var engine = NewEngine(log: logs.Add);
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            engine.Tick(Channel, t0, () => new List<RandomEvent>()); // seed
            DateTime tDue = t0.AddHours(9);
            engine.RecordActivity(Channel, tDue);                    // active room, so we try to fire
            var output = engine.Tick(Channel, tDue, () => new List<RandomEvent>());

            Assert.Empty(output);
            Assert.False(engine.HasActiveEvent(Channel));
            Assert.Contains(logs, l => l.Contains("no events are authored"));
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

        // ==================== Winner-conditional outcomes (per-winner roll) ====================
        //
        // An event that authors ANY outcome condition switches from one shared outcome roll to a
        // per-winner roll among the outcomes that winner qualifies for. Winners are then grouped
        // by the outcome they landed on, and each group gets its own header block.

        private static EventCondition Cond(string stat, string key = null, int? min = null, int? max = null)
        {
            return new EventCondition { stat = stat, key = key, min = min, max = max };
        }

        private static EventOutcome Outcome(string resultText, List<EventCondition> conditions, params EventReward[] rewards)
        {
            return new EventOutcome
            {
                weight = 1,
                resultText = resultText,
                conditions = conditions,
                rewards = rewards.ToList(),
            };
        }

        private static RandomEvent AllInWindowEvent(params EventOutcome[] outcomes)
        {
            return new RandomEvent
            {
                label = "mirror",
                weight = 1,
                announceText = "The mirror turns.",
                responseType = RandomEventEngine.ResponseTypeNone,
                responseWindowSeconds = 60,
                winnerRule = RandomEventEngine.WinnerRuleAllInWindow,
                outcomes = outcomes.ToList(),
            };
        }

        private Profile AddProfileWithCorruption(string userName, int corruption)
        {
            Profile p = AddProfile(userName);
            p.characteristics[CorruptionProcessor.CorruptionCharacteristicKey] = corruption.ToString();
            return p;
        }

        // The whole point of the feature: one event, three states, three different announcements.
        [Fact]
        public void Conditional_SplitsWinnersIntoOneBlockPerOutcomeTheyQualifyFor()
        {
            var engine = NewEngine();
            AddProfileWithCorruption("Alice", -30);  // corrupted
            AddProfileWithCorruption("Bob", 40);     // pure
            AddProfileWithCorruption("Cass", 0);     // neutral

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = AllInWindowEvent(
                Outcome("{winners} {sees|see} the mirror turn.",
                    new List<EventCondition> { Cond("corruption", max: -10) },
                    Reward("invert", null, 0, 0)),
                Outcome("{winners} {is|are} pulled into the dark.",
                    new List<EventCondition> { Cond("corruption", min: 10) },
                    Reward("invert", null, 0, 0)),
                Outcome("{winners} {finds|find} the glass blank.",
                    new List<EventCondition> { Cond("corruption", min: -9, max: 9) },
                    Reward("currency", "rosequartz", 5, 5)));

            engine.ForceOpen(Channel, ev, t0);
            engine.HandleRandom(Channel, "Alice", "", t0);
            engine.HandleRandom(Channel, "Bob", "", t0);
            engine.HandleRandom(Channel, "Cass", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Single(output);
            string expected = string.Join("\n", new[]
            {
                "[user]Alice[/user] sees the mirror turn.",
                "[user]Alice[/user] now has [b]30 purity[/b], inverted from [b]30 corruption[/b]!",
                "[user]Bob[/user] is pulled into the dark.",
                "[user]Bob[/user] now has [b]40 corruption[/b], inverted from [b]40 purity[/b]!",
                "[user]Cass[/user] finds the glass blank.",
                "[user]Cass[/user] receives [b]5 rosequartz[/b]!",
            });
            Assert.Equal(expected, output[0]);

            // And the grants actually landed, mirrored.
            Assert.Equal("30", _profiles["Alice"].characteristics[CorruptionProcessor.CorruptionCharacteristicKey]);
            Assert.Equal("-40", _profiles["Bob"].characteristics[CorruptionProcessor.CorruptionCharacteristicKey]);
            Assert.Equal(5, _profiles["Cass"].currencies["rosequartz"]);
        }

        // Count agreement has to resolve against the BLOCK size, not the event total - otherwise a
        // 3-winner event with one corrupted winner would read "Alice see the mirror turn".
        [Fact]
        public void Conditional_CountAgreementFollowsTheBlockNotTheEventTotal()
        {
            var engine = NewEngine();
            AddProfileWithCorruption("Alice", -30);
            AddProfileWithCorruption("Bob", -30);
            AddProfileWithCorruption("Cass", 0);

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = AllInWindowEvent(
                Outcome("{winners} {sees|see} the mirror turn.",
                    new List<EventCondition> { Cond("corruption", max: -10) }),
                Outcome("{winners} {finds|find} the glass blank.",
                    new List<EventCondition> { Cond("corruption", min: -9, max: 9) }));

            engine.ForceOpen(Channel, ev, t0);
            engine.HandleRandom(Channel, "Alice", "", t0);
            engine.HandleRandom(Channel, "Bob", "", t0);
            engine.HandleRandom(Channel, "Cass", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Single(output);
            Assert.Equal(
                "[user]Alice[/user] and [user]Bob[/user] see the mirror turn.\n"
                + "[user]Cass[/user] finds the glass blank.",
                output[0]);
        }

        // Identical grants still collapse onto one line - but only within their own block, so
        // winners under different headers can never be merged into the same sentence.
        [Fact]
        public void Conditional_RewardLineGroupingIsScopedToItsOwnBlock()
        {
            var engine = NewEngine();
            AddProfileWithCorruption("Alice", -30);
            AddProfileWithCorruption("Bob", -30);
            AddProfileWithCorruption("Cass", 0);
            AddProfileWithCorruption("Dee", 0);

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = AllInWindowEvent(
                Outcome("The corrupted are called.",
                    new List<EventCondition> { Cond("corruption", max: -10) },
                    Reward("currency", "rosequartz", 3, 3)),
                Outcome("The rest look on.",
                    new List<EventCondition> { Cond("corruption", min: -9, max: 9) },
                    Reward("currency", "rosequartz", 3, 3)));

            engine.ForceOpen(Channel, ev, t0);
            foreach (string name in new[] { "Alice", "Bob", "Cass", "Dee" })
                engine.HandleRandom(Channel, name, "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Single(output);
            Assert.Equal(
                "The corrupted are called.\n"
                + "[user]Alice[/user] and [user]Bob[/user] receive [b]3 rosequartz[/b]!\n"
                + "The rest look on.\n"
                + "[user]Cass[/user] and [user]Dee[/user] receive [b]3 rosequartz[/b]!",
                output[0]);
        }

        // A winner who qualifies for no outcome is dropped from the announcement rather than
        // showing up under someone else's header. Authors are told to keep a catch-all.
        [Fact]
        public void Conditional_WinnerWhoQualifiesForNothingIsOmitted()
        {
            var engine = NewEngine();
            AddProfileWithCorruption("Alice", -30);
            AddProfileWithCorruption("Cass", 0);

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = AllInWindowEvent(
                Outcome("{winners} {sees|see} the mirror turn.",
                    new List<EventCondition> { Cond("corruption", max: -10) }));

            engine.ForceOpen(Channel, ev, t0);
            engine.HandleRandom(Channel, "Alice", "", t0);
            engine.HandleRandom(Channel, "Cass", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Single(output);
            Assert.Equal("[user]Alice[/user] sees the mirror turn.", output[0]);
            Assert.DoesNotContain("Cass", output[0]);
        }

        // Silence after people responded reads as the bot having broken, so a fully gated-out
        // resolution closes with the existing no-winner line and logs why.
        [Fact]
        public void Conditional_AllWinnersGatedOut_ClosesWithNoWinnerLineAndLogs()
        {
            var logged = new List<string>();
            var engine = NewEngine(log: logged.Add);
            AddProfileWithCorruption("Cass", 0);

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = AllInWindowEvent(
                Outcome("Only the corrupted are called.",
                    new List<EventCondition> { Cond("corruption", max: -10) }));

            engine.ForceOpen(Channel, ev, t0);
            engine.HandleRandom(Channel, "Cass", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Single(output);
            Assert.Equal(RandomEventEngine.NoWinnerMessage(new ActiveRandomEvent { Event = ev }), output[0]);
            Assert.Contains(logged, m => m.Contains("no outcome applied"));
            Assert.False(engine.HasActiveEvent(Channel));
        }

        // Conditions FILTER the outcome table; they do not replace the weighted roll. A winner who
        // qualifies for both a gated outcome and an unconditional catch-all can land on either -
        // which is what lets an author mix "rare thing that can happen to anyone" with
        // state-specific branches. Deterministic branching is authored by gating EVERY outcome so
        // the conditions partition the space (see the tests above, where the catch-all is itself
        // pinned to the neutral band).
        [Fact]
        public void Conditional_UnconditionalOutcomeStaysInTheRollForAQualifyingWinner()
        {
            var seen = new HashSet<string>();
            for (int seed = 1; seed <= 40 && seen.Count < 2; seed++)
            {
                var run = new RandomEventEngineTests();
                var engine = run.NewEngine(seed);
                run.AddProfileWithCorruption("Alice", -30);

                DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var ev = AllInWindowEvent(
                    Outcome("Gated.", new List<EventCondition> { Cond("corruption", max: -10) }),
                    Outcome("Catch-all.", null));

                engine.ForceOpen(Channel, ev, t0);
                engine.HandleRandom(Channel, "Alice", "", t0);
                seen.Add(engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>())[0]);
            }

            Assert.Equal(new HashSet<string> { "Gated.", "Catch-all." }, seen);
        }

        // The compatibility guarantee: with no conditions anywhere, every winner still shares ONE
        // rolled outcome. Two heavily-weighted mutually-exclusive outcomes would split under a
        // per-winner roll; here all four winners must land in the same block.
        [Fact]
        public void NoConditions_StillRollsASingleSharedOutcomeForEveryWinner()
        {
            var engine = NewEngine();
            foreach (string name in new[] { "Alice", "Bob", "Cass", "Dee" })
                AddProfile(name);

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = AllInWindowEvent(
                Outcome("Heads.", null),
                Outcome("Tails.", null));

            engine.ForceOpen(Channel, ev, t0);
            foreach (string name in new[] { "Alice", "Bob", "Cass", "Dee" })
                engine.HandleRandom(Channel, name, "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Single(output);
            // Exactly one header, so exactly one outcome was rolled for the whole event.
            Assert.True(output[0] == "Heads." || output[0] == "Tails.", "unexpected resolution: " + output[0]);
        }

        // Conditions read the winner's state BEFORE this event grants anything - which is what
        // lets the invert outcome gate on the very stat it is about to flip.
        [Fact]
        public void Conditional_EvaluatesStateBeforeThisEventGrantsAnything()
        {
            var engine = NewEngine();
            AddProfileWithCorruption("Alice", -30);

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = AllInWindowEvent(
                Outcome("{winners} {is|are} mirrored.",
                    new List<EventCondition> { Cond("corruption", max: -10) },
                    Reward("invert", null, 0, 0)),
                Outcome("Nothing happens.",
                    new List<EventCondition> { Cond("corruption", min: -9) }));

            engine.ForceOpen(Channel, ev, t0);
            engine.HandleRandom(Channel, "Alice", "", t0);
            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Contains("mirrored", output[0]);
            // Post-flip she is +30, which would no longer satisfy the gate she came in through.
            Assert.Equal("30", _profiles["Alice"].characteristics[CorruptionProcessor.CorruptionCharacteristicKey]);
        }

        // Two winners who happened to hold the same magnitude group onto one line, and the
        // predicate's {has|have} has to follow the GROUP size, not stay stuck on the singular.
        [Fact]
        public void Invert_GroupedWinners_AgreeInThePlural()
        {
            var engine = NewEngine();
            AddProfileWithCorruption("Alice", -30);
            AddProfileWithCorruption("Bob", -30);

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = AllInWindowEvent(Outcome("The mirror turns.", null, Reward("invert", null, 0, 0)));

            engine.ForceOpen(Channel, ev, t0);
            engine.HandleRandom(Channel, "Alice", "", t0);
            engine.HandleRandom(Channel, "Bob", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Equal(
                "The mirror turns.\n"
                + "[user]Alice[/user] and [user]Bob[/user] now have [b]30 purity[/b], inverted from [b]30 corruption[/b]!",
                output[0]);
        }

        // Winners whose corruption differs end up with different predicates, so they do NOT group
        // - each gets their own truthful line.
        [Fact]
        public void Invert_WinnersWithDifferentMagnitudes_GetSeparateLines()
        {
            var engine = NewEngine();
            AddProfileWithCorruption("Alice", -30);
            AddProfileWithCorruption("Bob", 12);

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = AllInWindowEvent(Outcome("The mirror turns.", null, Reward("invert", null, 0, 0)));

            engine.ForceOpen(Channel, ev, t0);
            engine.HandleRandom(Channel, "Alice", "", t0);
            engine.HandleRandom(Channel, "Bob", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Equal(
                "The mirror turns.\n"
                + "[user]Alice[/user] now has [b]30 purity[/b], inverted from [b]30 corruption[/b]!\n"
                + "[user]Bob[/user] now has [b]12 corruption[/b], inverted from [b]12 purity[/b]!",
                output[0]);
        }

        // An outcome mixing a received reward with an inversion keeps them on separate lines -
        // joining them would put two verbs in one clause ("receives 5 rosequartz and now has...").
        [Fact]
        public void Invert_AlongsideAReceivedReward_KeepsTheTwoLineShapesApart()
        {
            var engine = NewEngine();
            AddProfileWithCorruption("Alice", -30);

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = AllInWindowEvent(Outcome("The mirror turns.", null,
                Reward("currency", "rosequartz", 5, 5),
                Reward("invert", null, 0, 0)));

            engine.ForceOpen(Channel, ev, t0);
            engine.HandleRandom(Channel, "Alice", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Equal(
                "The mirror turns.\n"
                + "[user]Alice[/user] receives [b]5 rosequartz[/b]!\n"
                + "[user]Alice[/user] now has [b]30 purity[/b], inverted from [b]30 corruption[/b]!",
                output[0]);
        }

        // A winner at dead neutral has nothing to mirror, so they contribute no line at all and
        // the outcome's own text is left to cover them.
        [Fact]
        public void Invert_AtZero_ProducesNoLineForThatWinner()
        {
            var engine = NewEngine();
            AddProfileWithCorruption("Alice", -30);
            AddProfileWithCorruption("Cass", 0);

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = AllInWindowEvent(Outcome("{winners} {steps|step} up to the mirror.", null,
                Reward("invert", null, 0, 0)));

            engine.ForceOpen(Channel, ev, t0);
            engine.HandleRandom(Channel, "Alice", "", t0);
            engine.HandleRandom(Channel, "Cass", "", t0);

            var output = engine.Tick(Channel, t0.AddSeconds(61), () => new List<RandomEvent>());

            Assert.Equal(
                "[user]Alice[/user] and [user]Cass[/user] step up to the mirror.\n"
                + "[user]Alice[/user] now has [b]30 purity[/b], inverted from [b]30 corruption[/b]!",
                output[0]);
            Assert.Equal("0", _profiles["Cass"].characteristics[CorruptionProcessor.CorruptionCharacteristicKey]);
        }

        [Fact]
        public void Conditional_SingleWinnerRules_PickTheBranchThatWinnerQualifiesFor()
        {
            var engine = NewEngine();
            AddProfileWithCorruption("Alice", 55);

            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ev = new RandomEvent
            {
                label = "mirror", weight = 1, announceText = "The mirror turns.",
                responseType = RandomEventEngine.ResponseTypeNone, responseWindowSeconds = 60,
                winnerRule = RandomEventEngine.WinnerRuleFirstValid,
                outcomes = new List<EventOutcome>
                {
                    Outcome("The dark takes {winners}.", new List<EventCondition> { Cond("corruption", min: 10) },
                        Reward("invert", null, 0, 0)),
                    Outcome("Nothing stirs.", new List<EventCondition> { Cond("corruption", max: 9) }),
                }
            };

            engine.ForceOpen(Channel, ev, t0);
            var result = engine.HandleRandom(Channel, "Alice", "", t0);

            Assert.Equal(
                "The dark takes [user]Alice[/user].\n"
                + "[user]Alice[/user] now has [b]55 corruption[/b], inverted from [b]55 purity[/b]!",
                result.ChannelAnnouncement);
        }
    }
}
