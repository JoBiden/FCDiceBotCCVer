using FChatDicebot;
using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Tests for SourceDrinkProcessor — the processor behind !drinkfrom and !forcedrink.
    ///
    /// The two behaviours worth pinning hardest: roles follow the typed verb rather than who
    /// typed it (so !forcedrink credits its initiator as the source), and a source drink leaves
    /// nothing behind — no bottle, no serial — which is the trade-off it is paid for in strength.
    /// </summary>
    [Collection("Database")]
    public class SourceDrinkProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly SourceDrinkProcessor _processor;
        private readonly Random _originalRng;

        public SourceDrinkProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new SourceDrinkProcessor(_database);

            _originalRng = SourceDrinkProcessor.Rng;
            SourceDrinkProcessor.Rng = NeverRolls();

            _fixture.SeedIdentifier(new Identifier
            {
                type = "cum",
                description = "cum",
                categories = new[] { "substance", "vice" }
            });
        }

        public void Dispose()
        {
            SourceDrinkProcessor.Rng = _originalRng;
        }

        // -------------------------------------------------------------------
        // Identity and roles
        // -------------------------------------------------------------------

        [Fact]
        public void InteractionType_IsDrinkfromForBothVerbs()
        {
            // One type reported to contributors and history, so the satisfaction rule and the
            // statistics don't have to know which direction was typed.
            Assert.Equal("drinkfrom", _processor.InteractionType);
            Assert.Equal("involved", _processor.InvestmentLevel);
        }

        [Fact]
        public void ResolveDrinker_FollowsTheTypedVerb()
        {
            Assert.Equal("Alice", SourceDrinkProcessor.ResolveDrinker(
                SourceDrinkProcessor.DrinkFromType, "Alice", "Bob"));
            Assert.Equal("Bob", SourceDrinkProcessor.ResolveDrinker(
                SourceDrinkProcessor.ForceDrinkType, "Alice", "Bob"));
        }

        [Fact]
        public void ResolveSource_IsTheMirrorOfResolveDrinker()
        {
            Assert.Equal("Bob", SourceDrinkProcessor.ResolveSource(
                SourceDrinkProcessor.DrinkFromType, "Alice", "Bob"));
            Assert.Equal("Alice", SourceDrinkProcessor.ResolveSource(
                SourceDrinkProcessor.ForceDrinkType, "Alice", "Bob"));
        }

        [Fact]
        public void ProcessInteraction_Drinkfrom_CreditsInitiatorAsTheDrinker()
        {
            SeedPair();

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType));

            Assert.Equal(1, CountOf("Alice", "drinkfromgive"));
            Assert.Equal(1, CountOf("Bob", "drinkfromtake"));
        }

        [Fact]
        public void ProcessInteraction_Forcedrink_CreditsInitiatorAsTheSource()
        {
            // The trap this whole feature sets: !forcedrink's initiator is the one being drunk
            // from, so they take the *take* count even though they typed the command.
            SeedPair();

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.ForceDrinkType));

            Assert.Equal(1, CountOf("Alice", "drinkfromtake"));
            Assert.Equal(1, CountOf("Bob", "drinkfromgive"));
        }

        [Fact]
        public void ProcessInteraction_Forcedrink_AppliesEffectsToTheRecipient()
        {
            // Corruption lands on whoever drank, not whoever typed. On !forcedrink the
            // initiator (Alice) is the source and the recipient (Bob) is the drinker, so it is
            // Alice's corruption that flavors the drink and Bob's that moves.
            SeedPair();
            SetCorruption("Alice", -15);

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.ForceDrinkType));

            Assert.Equal(-15, CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice")));
            Assert.Equal(-ChateauCurrency.SourceDrinkCorruptionShift,
                CorruptionProcessor.ReadCorruption(_database.GetProfile("Bob")));
        }

        // -------------------------------------------------------------------
        // Corruption
        // -------------------------------------------------------------------

        [Fact]
        public void ProcessInteraction_CorruptSource_ShiftsDrinkerByTheSourceAmount()
        {
            // Double a bottle's, and the whole point of the feature.
            SeedPair(sourceCorruption: -15);

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType));

            Assert.Equal(-ChateauCurrency.SourceDrinkCorruptionShift,
                CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice")));
            Assert.True(ChateauCurrency.SourceDrinkCorruptionShift
                > ChateauCurrency.DrinkCorruptionShiftPerBottle);
        }

        [Fact]
        public void ProcessInteraction_PurifiedSource_ShiftsDrinkerTowardPurity()
        {
            SeedPair(sourceCorruption: 15);

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType));

            Assert.Equal(ChateauCurrency.SourceDrinkCorruptionShift,
                CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice")));
        }

        [Fact]
        public void ProcessInteraction_NeutralSource_MovesNothing()
        {
            // Fresher is a bigger push, not a wider net: the tag still comes from the source's
            // live corruption at the usual threshold, so a neutral resident pours nothing.
            SeedPair(sourceCorruption: -(ChateauCurrency.CorruptionTagThreshold - 1));

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType));

            Assert.Equal(0, CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice")));
            var magnitudes = _database.GetProfile("Alice").dailyMagnitudes;
            Assert.True(magnitudes == null || !magnitudes.Any(kv => kv.Key.StartsWith("drinkshift_")));
        }

        // -------------------------------------------------------------------
        // The shared daily budget
        // -------------------------------------------------------------------

        [Fact]
        public void ProcessInteraction_SpendsFromTheSameBudgetAsDrink()
        {
            // A source drink is exactly two bottles' worth of the day's allowance. A separate
            // budget would let a resident take three bottles *and* three source drinks.
            SeedPair(sourceCorruption: -15);

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType));

            int used = ChateauDrink.GetUsedDrinkShift(_database.GetProfile("Alice"), DateTime.UtcNow.Date);
            Assert.Equal(ChateauCurrency.SourceDrinkCorruptionShift, used);
        }

        [Fact]
        public void ProcessInteraction_BottleThenSourceDrink_ShareOneBudget()
        {
            SeedPair(sourceCorruption: -15);
            var alice = _database.GetProfile("Alice");
            alice.milkInventory.Add(BottleInventoryTests.NewBottle(
                1, "cum", "Bob", hour: 1, tag: ChateauCurrency.CorruptTag));
            _database.SetProfile("Alice", alice);

            ChateauDrink.Execute(_database, "Alice", null, 0, NeverRolls());   // spends 1
            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType));

            Assert.Equal(1 + ChateauCurrency.SourceDrinkCorruptionShift,
                ChateauDrink.GetUsedDrinkShift(_database.GetProfile("Alice"), DateTime.UtcNow.Date));
            Assert.Equal(-(1 + ChateauCurrency.SourceDrinkCorruptionShift),
                CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice")));
        }

        [Fact]
        public void ProcessInteraction_WithBudgetPartlyLeft_LandsWhole()
        {
            // The gate is "any budget left", not "enough budget left": a drink that starts
            // inside the limit lands at full strength even when it carries the day past it,
            // rather than degrading into an arbitrary-looking partial.
            SeedPair(sourceCorruption: -15);
            var alice = _database.GetProfile("Alice");
            ChateauDrink.RecordUsedDrinkShift(
                alice, DateTime.UtcNow.Date, ChateauCurrency.DrinkCorruptionDailyLimit - 1);
            _database.SetProfile("Alice", alice);

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType));

            Assert.Equal(-ChateauCurrency.SourceDrinkCorruptionShift,
                CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice")));
            // The day's ceiling is therefore limit - 1 + shift, not the limit itself.
            Assert.Equal(ChateauCurrency.DrinkCorruptionDailyLimit - 1 + ChateauCurrency.SourceDrinkCorruptionShift,
                ChateauDrink.GetUsedDrinkShift(_database.GetProfile("Alice"), DateTime.UtcNow.Date));
        }

        [Fact]
        public void ProcessInteraction_WithBudgetGone_MovesNothingAndSaysSo()
        {
            SeedPair(sourceCorruption: -15);
            var alice = _database.GetProfile("Alice");
            ChateauDrink.RecordUsedDrinkShift(
                alice, DateTime.UtcNow.Date, ChateauCurrency.DrinkCorruptionDailyLimit);
            _database.SetProfile("Alice", alice);

            var pending = Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType);
            _processor.ProcessInteraction(pending);
            string message = CompletionOf(pending);

            Assert.Equal(0, CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice")));
            Assert.Contains("metabolize", message);
        }

        // -------------------------------------------------------------------
        // Addiction
        // -------------------------------------------------------------------

        [Fact]
        public void ProcessInteraction_IntensifiesAViceTheDrinkerAlreadyCarries()
        {
            SeedPair();
            GiveVice("Alice", "cum", level: 2);
            SourceDrinkProcessor.Rng = AlwaysRolls();

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType));

            var vice = ViceInstance.LoadAll(_database.GetProfile("Alice")).Single();
            Assert.Equal(3, vice.AddictionLevel);
        }

        [Fact]
        public void ProcessInteraction_NeverHooksACleanDrinker()
        {
            // Intensify-only on both drink paths. Creating an addiction stays !dose's job,
            // behind its own consent framing — no drink strength routes around that.
            SeedPair();
            SourceDrinkProcessor.Rng = AlwaysRolls();

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType));

            Assert.Empty(ViceInstance.LoadAll(_database.GetProfile("Alice")));
        }

        [Fact]
        public void ProcessInteraction_BelowTheRollThreshold_LeavesTheViceAlone()
        {
            SeedPair();
            GiveVice("Alice", "cum", level: 2);
            SourceDrinkProcessor.Rng = NeverRolls();

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType));

            Assert.Equal(2, ViceInstance.LoadAll(_database.GetProfile("Alice")).Single().AddictionLevel);
        }

        // -------------------------------------------------------------------
        // No keepsake
        // -------------------------------------------------------------------

        [Fact]
        public void ProcessInteraction_CreatesNoBottleAndClaimsNoSerial()
        {
            // The trade-off, stated as an assertion: strength instead of a numbered empty.
            // Checking the counter is what would actually catch a stray ClaimBottleSerials.
            SeedPair(sourceCorruption: -15);
            int serialBefore = _database.ClaimBottleSerials(1);

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType));

            Assert.Empty(_database.GetProfile("Alice").milkInventory);
            Assert.Empty(_database.GetProfile("Bob").milkInventory);
            Assert.Equal(serialBefore + 1, _database.ClaimBottleSerials(1));
        }

        // -------------------------------------------------------------------
        // The shared draw lock
        // -------------------------------------------------------------------

        [Fact]
        public void Validate_AfterMilkingTheSameSource_IsRefusedAndNamesMilking()
        {
            SeedPair();
            StampLock("Alice", DrawVerb.Milk, "Bob");

            var result = _processor.ValidateInteraction(
                "Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType);

            Assert.False(result.IsValid);
            Assert.Contains("already milked", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Validate_MilkAfterDrinkingFromTheSameSource_IsRefusedAndNamesDrinking()
        {
            // The half the shared key would otherwise get wrong: !milk must not claim a
            // milking that never happened.
            SeedPair();
            StampLock("Alice", DrawVerb.DrinkFrom, "Bob");

            var result = new MilkProcessor(_database).ValidateInteraction("Alice", "Bob", "cum");

            Assert.False(result.IsValid);
            Assert.Contains("already drunk from", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("milked", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Validate_DrinkingFromTheSameSourceTwice_IsRefused()
        {
            SeedPair();
            StampLock("Alice", DrawVerb.DrinkFrom, "Bob");

            var result = _processor.ValidateInteraction(
                "Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType);

            Assert.False(result.IsValid);
            Assert.Contains("already drunk from", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Validate_ReverseDirection_IsAllowedTheSameDay()
        {
            // The lock is directional: being drunk from never stops you drinking back.
            SeedPair();
            StampLock("Alice", DrawVerb.DrinkFrom, "Bob");

            var result = _processor.ValidateInteraction(
                "Bob", "Alice", "cum", SourceDrinkProcessor.DrinkFromType);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_AThirdResidentDrawingFromTheSameSource_IsAllowed()
        {
            SeedPair();
            new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").BuildAndSave(_database);
            StampLock("Alice", DrawVerb.DrinkFrom, "Bob");

            var result = _processor.ValidateInteraction(
                "Carol", "Bob", "cum", SourceDrinkProcessor.DrinkFromType);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_Forcedrink_ChecksTheLockAgainstTheDrinkerNotTheTyper()
        {
            // Alice offers Bob a drink. It's Bob's draw allowance against Alice that matters.
            SeedPair();
            StampLock("Bob", DrawVerb.DrinkFrom, "Alice");

            var refused = _processor.ValidateInteraction(
                "Alice", "Bob", "cum", SourceDrinkProcessor.ForceDrinkType);
            Assert.False(refused.IsValid);

            // Alice's own allowance against Bob is untouched and irrelevant here.
            var allowed = _processor.ValidateInteraction(
                "Bob", "Alice", "cum", SourceDrinkProcessor.ForceDrinkType);
            Assert.True(allowed.IsValid);
        }

        [Fact]
        public void ProcessInteraction_StampsTheDrinkVerbsOwnKey()
        {
            SeedPair();

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType));

            var alice = _database.GetProfile("Alice");
            Assert.True(alice.timers.ContainsKey(SourceDrawLock.TimerKey(DrawVerb.DrinkFrom, "Bob")));
            Assert.False(alice.timers.ContainsKey(SourceDrawLock.TimerKey(DrawVerb.Milk, "Bob")));
            // Still blocks a milking, through the shared lookup rather than a shared key.
            Assert.True(MilkProcessor.HasActiveDirectionLock(alice, "Bob"));
            // And expires at the next day-boundary, like every other daily lock.
            Assert.Equal(DateTime.UtcNow.Date.AddDays(1),
                alice.timers[SourceDrawLock.TimerKey(DrawVerb.DrinkFrom, "Bob")].timerEnd);
        }

        [Fact]
        public void ProcessInteraction_Forcedrink_StampsTheLockOnTheDrinker()
        {
            SeedPair();

            _processor.ProcessInteraction(Pending("Alice", "Bob", "cum", SourceDrinkProcessor.ForceDrinkType));

            var bob = _database.GetProfile("Bob");
            Assert.True(bob.timers.ContainsKey(SourceDrawLock.TimerKey(DrawVerb.DrinkFrom, "Alice")));
            var alice = _database.GetProfile("Alice");
            Assert.False(alice.timers != null
                && alice.timers.ContainsKey(SourceDrawLock.TimerKey(DrawVerb.DrinkFrom, "Bob")));
        }

        [Fact]
        public void ProcessInteraction_LockLandingBeforeConsent_DoesNothingAndSaysNothing()
        {
            // TOCTOU: ChateauConsent re-validates, but only through the role-agnostic overload,
            // so the lock recheck has to happen here.
            SeedPair(sourceCorruption: -15);
            var pending = Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType);
            StampLock("Alice", DrawVerb.Milk, "Bob");

            _processor.ProcessInteraction(pending);

            Assert.Equal(string.Empty, CompletionOf(pending));
            Assert.Equal(0, CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice")));
            Assert.Equal(0, CountOf("Alice", "drinkfromgive"));
            Assert.Contains("already milked", _processor.GetAndClearInitiatorPrivateMessage(),
                StringComparison.OrdinalIgnoreCase);
        }

        // -------------------------------------------------------------------
        // Validation
        // -------------------------------------------------------------------

        [Fact]
        public void Validate_MissingSubstance_Fails()
        {
            SeedPair();

            var result = _processor.ValidateInteraction(
                "Alice", "Bob", null, SourceDrinkProcessor.DrinkFromType);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_NonSubstanceIdentifier_Fails()
        {
            SeedPair();
            _fixture.SeedIdentifier(new Identifier
            {
                type = "hat",
                description = "hat",
                categories = new[] { "clothing" }
            });

            var result = _processor.ValidateInteraction(
                "Alice", "Bob", "hat", SourceDrinkProcessor.DrinkFromType);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_SelfTarget_FailsAndPointsAtDrink()
        {
            SeedPair();

            var result = _processor.ValidateInteraction(
                "Alice", "Alice", "cum", SourceDrinkProcessor.DrinkFromType);

            Assert.False(result.IsValid);
            Assert.Contains("!drink", result.ErrorMessage);
        }

        [Fact]
        public void Validate_BrokenSourcePart_FailsAndReadsAsDrinking()
        {
            SeedPair();
            var bob = _database.GetProfile("Bob");
            BreakInstance.SaveAll(bob, new List<BreakInstance>
            {
                new BreakInstance
                {
                    Part = "dick",
                    Severity = 5,
                    BrokenBy = "Carol",
                    BrokenAt = DateTime.UtcNow,
                    // Without this the lazy tick reads an unset LastTickedAt as year 0001 and
                    // heals the break away before anything can be gated on it.
                    LastTickedAt = DateTime.UtcNow.Date,
                },
            });
            _database.SetProfile("Bob", bob);

            var result = _processor.ValidateInteraction(
                "Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType);

            Assert.False(result.IsValid);
            Assert.Contains("drink from", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("milking", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        // -------------------------------------------------------------------
        // Consent prompts
        // -------------------------------------------------------------------

        [Fact]
        public void ConsentWarning_BothVerbs_EndWithTheDeclineHint()
        {
            SeedPair();
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            foreach (var verb in new[] { SourceDrinkProcessor.DrinkFromType, SourceDrinkProcessor.ForceDrinkType })
            {
                string warning = _processor.GetConsentWarning(alice, bob, "cum", verb);
                Assert.Contains("!no", warning);
                Assert.Contains("!consent", warning);
            }
        }

        [Fact]
        public void ConsentWarning_Drinkfrom_AsksTheSourceToBeDrunkFrom()
        {
            SeedPair();

            string warning = _processor.GetConsentWarning(
                _database.GetProfile("Alice"), _database.GetProfile("Bob"),
                "cum", SourceDrinkProcessor.DrinkFromType);

            Assert.Contains("being slurped", warning);
        }

        [Fact]
        public void ConsentWarning_Forcedrink_AsksTheDrinkerToDrink()
        {
            SeedPair();

            string warning = _processor.GetConsentWarning(
                _database.GetProfile("Alice"), _database.GetProfile("Bob"),
                "cum", SourceDrinkProcessor.ForceDrinkType);

            // Names, not pronouns: both parties appear in every sentence of these prompts.
            Assert.Contains("drinking from Alice", warning);
        }

        [Fact]
        public void ConsentWarning_ProjectsTheSourcesCorruptionOntoTheDrink()
        {
            // What the drinker is signing up for is the source's corruption, so the prompt
            // surfaces it the way !milk's does for its bottles.
            SeedPair(sourceCorruption: -15);

            string warning = _processor.GetConsentWarning(
                _database.GetProfile("Alice"), _database.GetProfile("Bob"),
                "cum", SourceDrinkProcessor.DrinkFromType);

            Assert.Contains("corrupt", warning, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CompletionMessage_NamesTheDrinkerAndTheSource()
        {
            SeedPair();
            var pending = Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType);

            _processor.ProcessInteraction(pending);
            string message = CompletionOf(pending);

            Assert.Contains("Alice drinks their fill", message);
            Assert.Contains("straight from Bob", message);
        }

        [Fact]
        public void CompletionMessage_LeavesNoUnsubstitutedFlourishPlaceholder()
        {
            // The flourish pool addresses the parties by name through {source} / {drinker}.
            // A pool entry added without a matching Replace would ship the raw token to the
            // channel, so walk the whole pool rather than whichever one today's RNG picks.
            SeedPair();

            for (int i = 0; i < 20; i++)
            {
                SourceDrinkProcessor.Rng = new FixedSampleRandom(i / 20.0);
                var pending = Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType);
                _processor.ProcessInteraction(pending);
                string message = CompletionOf(pending);

                Assert.DoesNotContain("{", message);
                // Reset the draw lock so the next iteration isn't refused.
                var alice = _database.GetProfile("Alice");
                alice.timers.Clear();
                _database.SetProfile("Alice", alice);
            }
        }

        [Fact]
        public void CompletionWithStatusEffects_SatisfiesTheDrinkerOnly_Drinkfrom()
        {
            // Both are hooked on the substance. Only the drinker swallowed it, so only their
            // craving is quieted; the source, who provided it, is left wanting.
            SeedPair();
            GiveVice("Alice", "cum", level: CertainCravingLevel);
            GiveVice("Bob", "cum", level: CertainCravingLevel);

            string message = ProcessAndRenderFully("Alice", "Bob", SourceDrinkProcessor.DrinkFromType);

            Assert.Contains("Alice gets the", message);
            Assert.DoesNotContain("Bob gets the", message);
        }

        [Fact]
        public void CompletionWithStatusEffects_SatisfiesTheDrinkerOnly_Forcedrink()
        {
            // The mirror, and the reason the contributor has to see the typed verb: the drinker
            // is the recipient here, and one shared interaction type could not tell the two
            // directions apart.
            SeedPair();
            GiveVice("Alice", "cum", level: CertainCravingLevel);
            GiveVice("Bob", "cum", level: CertainCravingLevel);

            string message = ProcessAndRenderFully("Alice", "Bob", SourceDrinkProcessor.ForceDrinkType);

            Assert.Contains("Bob gets the", message);
            Assert.DoesNotContain("Alice gets the", message);
        }

        [Fact]
        public void CompletionWithStatusEffects_TheNonDrinkerCanStillCrave()
        {
            // Watching someone else get what you crave is exactly when the craving line should
            // fire. At this addiction level the roll is certain, so this is deterministic.
            SeedPair();
            GiveVice("Bob", "cum", level: CertainCravingLevel);

            string message = ProcessAndRenderFully("Alice", "Bob", SourceDrinkProcessor.DrinkFromType);

            Assert.DoesNotContain("Bob gets the", message);
            Assert.Contains("Bob", message);
            // One of the three craving templates, all of which name the substance.
            Assert.True(
                message.Contains("craving for") || message.Contains("daydreaming about")
                    || message.Contains("hit the spot"),
                "expected a craving fragment for the source, got: " + message);
        }

        [Fact]
        public void CompletionMessage_IsDrainedAfterOneRead()
        {
            SeedPair();
            var pending = Pending("Alice", "Bob", "cum", SourceDrinkProcessor.DrinkFromType);
            _processor.ProcessInteraction(pending);

            Assert.NotEqual(string.Empty, CompletionOf(pending));
            Assert.Equal(string.Empty, CompletionOf(pending));
        }

        // -------------------------------------------------------------------
        // Test helpers
        // -------------------------------------------------------------------

        private void SeedPair(int sourceCorruption = 0)
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob");
            if (sourceCorruption != 0)
            {
                bob.WithCharacteristic(
                    CorruptionProcessor.CorruptionCharacteristicKey, sourceCorruption.ToString());
            }
            bob.BuildAndSave(_database);
        }

        private void SetCorruption(string userName, int value)
        {
            var profile = _database.GetProfile(userName);
            profile.characteristics[CorruptionProcessor.CorruptionCharacteristicKey] = value.ToString();
            _database.SetProfile(userName, profile);
        }

        private void GiveVice(string userName, string vice, int level)
        {
            var profile = _database.GetProfile(userName);
            ViceInstance.SaveAll(profile, new List<ViceInstance>
            {
                new ViceInstance { Vice = vice, AddictionLevel = level, DosedBy = "Bob", FirstDosedAt = DateTime.UtcNow },
            });
            _database.SetProfile(userName, profile);
        }

        private void StampLock(string taker, DrawVerb verb, string source)
        {
            var profile = _database.GetProfile(taker);
            SourceDrawLock.Stamp(profile, verb, source);
            _database.SetProfile(taker, profile);
        }

        private int CountOf(string userName, string label)
        {
            var counts = _database.GetProfile(userName)?.counts;
            if (counts == null) return 0;
            return counts.TryGetValue(label, out int value) ? value : 0;
        }

        /// <summary>
        /// Addiction level at which the craving roll is certain (level × 10%), so tests that
        /// care about which party got which fragment don't depend on the contributor's own RNG.
        /// </summary>
        private const int CertainCravingLevel = 10;

        /// <summary>
        /// Run the interaction and render the channel message the way `!consent` does, with the
        /// status-effect contributors applied.
        /// </summary>
        private string ProcessAndRenderFully(string initiator, string recipient, string typeKey)
        {
            var pending = Pending(initiator, recipient, "cum", typeKey);
            _processor.ProcessInteraction(pending);
            return _processor.GetCompletionMessageWithStatusEffects(
                _database.GetProfile(initiator), _database.GetProfile(recipient), "cum", typeKey);
        }

        /// <summary>The processor's own completion body, without contributor fragments.</summary>
        private string CompletionOf(PendingCommand pending)
        {
            return _processor.GetCompletionMessage(
                _database.GetProfile(pending.pendingInteraction.initiator),
                _database.GetProfile(pending.pendingInteraction.recipient),
                pending.pendingInteraction.identifier);
        }

        private PendingCommand Pending(string initiator, string recipient, string substance, string typeKey)
        {
            var pending = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = typeKey,
                    identifier = substance,
                    investmentLevel = "involved",
                }
            };
            _database.AddPendingCommand(pending);
            return pending;
        }

        private static Random NeverRolls() => new FixedSampleRandom(0.999);
        private static Random AlwaysRolls() => new FixedSampleRandom(0.0);

        private class FixedSampleRandom : Random
        {
            private readonly double _sample;
            public FixedSampleRandom(double sample) { _sample = sample; }
            protected override double Sample() => _sample;
        }
    }
}
