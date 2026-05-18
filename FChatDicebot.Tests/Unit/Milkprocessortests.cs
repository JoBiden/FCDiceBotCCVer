using FChatDicebot;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    [Collection("Database")]
    public class MilkProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly MilkProcessor _processor;
        private readonly Random _originalRng;

        public MilkProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new MilkProcessor(_database);
            // Replace the static RNG with a fixed-sample one so quantity rolls are
            // deterministic across tests. 0.5 → middle of the [min, max] range → 2 bottles
            // for the default 1–3 spread. Tests that need a specific value swap it.
            _originalRng = MilkProcessor.Rng;
            MilkProcessor.Rng = new FixedSampleRandom(0.5);

            // Common substance identifier used by validation. Tests that need a vice or
            // an unknown substance seed their own.
            _fixture.SeedIdentifier(new Identifier
            {
                type = "cum",
                description = "cum",
                categories = new[] { "substance" }
            });
        }

        public void Dispose()
        {
            MilkProcessor.Rng = _originalRng;
        }

        // -------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------

        [Fact]
        public void InteractionType_ReturnsMilk()
        {
            Assert.Equal("milk", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsInvolved()
        {
            Assert.Equal("involved", _processor.InvestmentLevel);
        }

        // -------------------------------------------------------------------
        // Validation
        // -------------------------------------------------------------------

        [Fact]
        public void ValidateInteraction_SelfTarget_ProcessorRejectsAsDefensiveGuard()
        {
            // Self-target is allowed *at the command layer* via the self-sale shortcut,
            // which never enters the processor. The processor's validation still rejects
            // it so a stray self-target Interaction can't sneak through downstream code.
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Alice", "cum");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_NoSubstance_Fails()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_UnknownSubstance_Fails()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            // "ectoplasm" was never seeded as a substance — should be rejected.
            var result = _processor.ValidateInteraction("Alice", "Bob", "ectoplasm");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_SubstanceWrongCategory_Fails()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);
            // Exists, but in the wrong category — should still be rejected.
            _fixture.SeedIdentifier(new Identifier
            {
                type = "musk",
                description = "musk",
                categories = new[] { "scent" }
            });

            var result = _processor.ValidateInteraction("Alice", "Bob", "musk");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_ViceCategory_Allowed()
        {
            // The spec accepts both "substance" and "vice" as milkable categories.
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "lustessence",
                description = "lust essence",
                categories = new[] { "vice" }
            });

            var result = _processor.ValidateInteraction("Alice", "Bob", "lustessence");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_NoMilkInventoryPrecondition()
        {
            // The Chateau provides empty bottles — fresh profile (no milkInventory yet)
            // should still pass validation.
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "cum");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_PairLockActive_Fails()
        {
            // Alice already milked Bob today — the timer on her side should block.
            new ProfileBuilder()
                .WithUserName("Alice")
                .WithTimer(MilkProcessor.PairTimerKey("Bob"), DateTime.UtcNow.AddHours(2))
                .BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "cum");

            Assert.False(result.IsValid);
            Assert.Contains("already milked", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidateInteraction_ExpiredPairLock_Allowed()
        {
            // Timer is past — should be ignored.
            new ProfileBuilder()
                .WithUserName("Alice")
                .WithTimer(MilkProcessor.PairTimerKey("Bob"), DateTime.UtcNow.AddHours(-2))
                .BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "cum");

            Assert.True(result.IsValid);
        }

        // -------------------------------------------------------------------
        // ProcessInteraction
        // -------------------------------------------------------------------

        [Fact]
        public void ProcessInteraction_AppendsBottleToInitiator()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pending = BuildPendingCommand("Alice", "Bob", "cum");
            _database.AddPendingCommand(pending);

            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            Assert.Single(alice.milkInventory);
            var bottle = alice.milkInventory[0];
            Assert.Equal("cum", bottle.substance);
            Assert.Equal("Bob", bottle.sourceName);
            // 0.5 sample over [1, 4) → 2 bottles
            Assert.Equal(2, bottle.quantity);
            Assert.Null(bottle.corruptionTag); // Bob has neutral corruption
        }

        [Fact]
        public void ProcessInteraction_NoInventoryDeductionFromInitiator()
        {
            // The Chateau provides bottles; nothing on the initiator's profile should
            // be debited beyond appending the inventory entry.
            new ProfileBuilder()
                .WithUserName("Alice")
                .WithCurrency("copper", 50)
                .BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "cum")));

            var alice = _database.GetProfile("Alice");
            // Copper balance untouched — no charge for the empty.
            Assert.Equal(50, alice.currencies["copper"]);
        }

        [Fact]
        public void ProcessInteraction_SetsSymmetricPairLock()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "cum")));

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            Assert.True(alice.timers.ContainsKey(MilkProcessor.PairTimerKey("Bob")));
            Assert.True(bob.timers.ContainsKey(MilkProcessor.PairTimerKey("Alice")));
            // Locks until the next day boundary.
            DateTime expected = DateTime.UtcNow.Date.AddDays(1);
            Assert.Equal(expected, alice.timers[MilkProcessor.PairTimerKey("Bob")].timerEnd);
            Assert.Equal(expected, bob.timers[MilkProcessor.PairTimerKey("Alice")].timerEnd);
        }

        [Fact]
        public void ProcessInteraction_IncrementsCounts()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "cum")));

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            Assert.Equal(1, alice.counts["milkgive"]);
            Assert.Equal(1, bob.counts["milktake"]);
        }

        [Fact]
        public void ProcessInteraction_SavesInteractionToHistory()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "cum")));

            var interactions = _database.GetInteractionsByInitiator("Alice");
            Assert.Contains(interactions, i => i.type == "milk");
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = SaveAndReturn(BuildPendingCommand("Alice", "Bob", "cum"));
            _processor.ProcessInteraction(pending);

            Assert.Null(_database.GetPendingCommand(pending.Id));
        }

        [Fact]
        public void ProcessInteraction_CorruptedRecipient_TagsBottleCorrupt()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder()
                .WithUserName("Bob")
                .WithCharacteristic(CorruptionProcessor.CorruptionCharacteristicKey, "-15")
                .BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "cum")));

            var alice = _database.GetProfile("Alice");
            Assert.Equal(ChateauCurrency.CorruptTag, alice.milkInventory[0].corruptionTag);
        }

        [Fact]
        public void ProcessInteraction_PurifiedRecipient_TagsBottlePurified()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder()
                .WithUserName("Bob")
                .WithCharacteristic(CorruptionProcessor.CorruptionCharacteristicKey, "20")
                .BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "cum")));

            var alice = _database.GetProfile("Alice");
            Assert.Equal(ChateauCurrency.PurifiedTag, alice.milkInventory[0].corruptionTag);
        }

        [Fact]
        public void ProcessInteraction_NeutralRecipient_NoTag()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder()
                .WithUserName("Bob")
                .WithCharacteristic(CorruptionProcessor.CorruptionCharacteristicKey, "5")
                .BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "cum")));

            var alice = _database.GetProfile("Alice");
            Assert.Null(alice.milkInventory[0].corruptionTag);
        }

        // -------------------------------------------------------------------
        // Identifier round-trip
        // -------------------------------------------------------------------

        [Fact]
        public void Identifier_RoundTrip_PreservesSubstanceAndQuantity()
        {
            string id = MilkProcessor.ComposeIdentifier("cum", 3);
            Assert.Equal("cum", MilkProcessor.ParseSubstanceFromIdentifier(id));
            Assert.Equal(3, MilkProcessor.ParseQuantityFromIdentifier(id));
        }

        [Fact]
        public void ParseQuantity_NoPipe_ReturnsSentinelMissing()
        {
            // Pre-process command-time shape: identifier is just the substance.
            Assert.Equal(-1, MilkProcessor.ParseQuantityFromIdentifier("cum"));
        }

        [Fact]
        public void ParseSubstance_NoPipe_ReturnsIdentifierAsIs()
        {
            Assert.Equal("cum", MilkProcessor.ParseSubstanceFromIdentifier("cum"));
        }

        // -------------------------------------------------------------------
        // Completion / consent text
        // -------------------------------------------------------------------

        [Fact]
        public void GetCompletionMessage_AfterProcess_IncludesQuantityAndSubstance()
        {
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pending = SaveAndReturn(BuildPendingCommand("Alice", "Bob", "cum"));
            _processor.ProcessInteraction(pending);

            string message = _processor.GetCompletionMessage(
                _database.GetProfile("Alice"),
                _database.GetProfile("Bob"),
                pending.pendingInteraction.identifier);

            Assert.Contains("Alice milks Bob for 2 bottles of cum", message);
            Assert.Contains("Bottled, sealed, and tagged.", message);
        }

        [Fact]
        public void GetCompletionMessage_CorruptedRecipient_AppendsDarkSheenFlavor_WithSubstanceName()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithCharacteristic(CorruptionProcessor.CorruptionCharacteristicKey, "-50")
                .BuildAndSave(_database);

            var pending = SaveAndReturn(BuildPendingCommand("Alice", "Bob", "cum"));
            _processor.ProcessInteraction(pending);

            string message = _processor.GetCompletionMessage(
                _database.GetProfile("Alice"),
                _database.GetProfile("Bob"),
                pending.pendingInteraction.identifier);

            // Should reference the substance by name, not the generic word "substance".
            Assert.Contains("The cum has a faint dark sheen", message);
        }

        [Fact]
        public void GetCompletionMessage_PurifiedRecipient_AppendsGlowFlavor_WithSubstanceName()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithCharacteristic(CorruptionProcessor.CorruptionCharacteristicKey, "50")
                .BuildAndSave(_database);

            var pending = SaveAndReturn(BuildPendingCommand("Alice", "Bob", "cum"));
            _processor.ProcessInteraction(pending);

            string message = _processor.GetCompletionMessage(
                _database.GetProfile("Alice"),
                _database.GetProfile("Bob"),
                pending.pendingInteraction.identifier);

            Assert.Contains("The cum practically glows", message);
        }

        [Fact]
        public void GetConsentWarning_NeutralRecipient_BasicTemplate()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            string warning = _processor.GetConsentWarning(
                _database.GetProfile("Alice"),
                _database.GetProfile("Bob"),
                "cum");

            Assert.Contains("Alice wants to milk Bob for some cum.", warning);
            Assert.Contains("Do you !consent to being milked?", warning);
        }

        [Fact]
        public void GetConsentWarning_CorruptedRecipient_FlagsCorruptTag_WithSubstance()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithCharacteristic(CorruptionProcessor.CorruptionCharacteristicKey, "-30")
                .BuildAndSave(_database);

            string warning = _processor.GetConsentWarning(
                _database.GetProfile("Alice"),
                _database.GetProfile("Bob"),
                "cum");

            // Should reference recipient and substance by name in the flavor append.
            Assert.Contains("Bob's cum is going to be quite [b]corrupt[/b].", warning);
        }

        [Fact]
        public void GetConsentWarning_PurifiedRecipient_FlagsPureTag_WithSubstance()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithCharacteristic(CorruptionProcessor.CorruptionCharacteristicKey, "30")
                .BuildAndSave(_database);

            string warning = _processor.GetConsentWarning(
                _database.GetProfile("Alice"),
                _database.GetProfile("Bob"),
                "cum");

            Assert.Contains("Bob's cum is going to be quite [b]pure[/b].", warning);
        }

        [Fact]
        public void PairLockMessage_IncludesTimeUntilNextDay()
        {
            // Sanity check that we mention "in <time>" wording and avoid "UTC".
            string message = MilkProcessor.PairLockMessage("Bob");
            Assert.Contains("already milked Bob today", message);
            Assert.Contains("milk them again in", message);
            Assert.DoesNotContain("UTC", message);
        }

        [Fact]
        public void PairLockMessage_SelfVariant_UsesYourselfWording()
        {
            // Self-milk shortcut shouldn't read "You've already milked <your name>" — it
            // should say "yourself" both as the target and in the retry hint.
            string message = MilkProcessor.PairLockMessage("Alice", isSelf: true);
            Assert.Contains("already milked yourself today", message);
            Assert.Contains("milk yourself again in", message);
            Assert.DoesNotContain("Alice", message);
            Assert.DoesNotContain("UTC", message);
        }

        // -------------------------------------------------------------------
        // Test helpers
        // -------------------------------------------------------------------

        private PendingCommand BuildPendingCommand(string initiator, string recipient, string substance)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = "milk",
                    identifier = substance,
                    investmentLevel = "involved",
                }
            };
        }

        private PendingCommand SaveAndReturn(PendingCommand pendingCommand)
        {
            _database.AddPendingCommand(pendingCommand);
            return pendingCommand;
        }

        /// <summary>
        /// A Random whose Sample() always returns the same fixed value. Same trick the
        /// BreedProcessor tests use — overrides the base hook that Next(int, int) ultimately
        /// reduces to, so quantity rolls are deterministic without monkeypatching Next itself.
        /// </summary>
        private class FixedSampleRandom : Random
        {
            private readonly double _sample;
            public FixedSampleRandom(double sample) { _sample = sample; }
            protected override double Sample() => _sample;
        }
    }
}
