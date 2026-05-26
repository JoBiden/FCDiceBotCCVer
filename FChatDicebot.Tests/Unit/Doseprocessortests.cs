using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
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
    [Collection("Database")]
    public class DoseProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly DoseProcessor _processor;

        public DoseProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new DoseProcessor(_database);

            // Seed vice identifiers: "musk" (scent + vice), "drug" (substance + vice), and
            // a control identifier "fire" (substance only, no vice tag) for the
            // validate-rejects-non-vice test.
            _fixture.SeedIdentifier(new Identifier
            {
                type = "musk",
                description = "musk",
                categories = new[] { "scent", "vice" },
            });
            _fixture.SeedIdentifier(new Identifier
            {
                type = "drug",
                description = "drug",
                categories = new[] { "substance", "vice" },
            });
            _fixture.SeedIdentifier(new Identifier
            {
                type = "fire",
                description = "fire",
                categories = new[] { "substance" },
            });
        }

        public void Dispose() { }

        [Fact]
        public void InteractionType_ReturnsDose()
        {
            Assert.Equal(DoseProcessor.DoseType, _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsConsequence()
        {
            Assert.Equal("consequence", _processor.InvestmentLevel);
        }

        [Fact]
        public void ValidateInteraction_EmptyVice_ReturnsFailure()
        {
            SeedPair();
            var result = _processor.ValidateInteraction("Alice", "Bob", "");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_UnknownVice_ReturnsFailure()
        {
            SeedPair();
            var result = _processor.ValidateInteraction("Alice", "Bob", "antimatter");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_NonViceCategory_ReturnsFailure()
        {
            // "fire" is a substance but not tagged vice; reject so non-addictive identifiers
            // can't sneak through.
            SeedPair();
            var result = _processor.ValidateInteraction("Alice", "Bob", "fire");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_KnownVice_Succeeds()
        {
            SeedPair();
            var result = _processor.ValidateInteraction("Alice", "Bob", "musk");
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ProcessInteraction_FirstDose_CreatesViceAtLevel1()
        {
            SeedPair();
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "musk"));

            var bob = _database.GetProfile("Bob");
            var vices = ViceInstance.LoadAll(bob);
            Assert.Single(vices);
            Assert.Equal("musk", vices[0].Vice);
            Assert.Equal(1, vices[0].AddictionLevel);
            // DosedBy stores the initiator's display name for natural read-back ("Alice's
            // dose of musk").
            Assert.Equal("Alice", vices[0].DosedBy);
        }

        [Fact]
        public void ProcessInteraction_SecondDoseSameVice_EscalatesLevel()
        {
            SeedPair();
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "musk"));
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "musk"));

            var bob = _database.GetProfile("Bob");
            var vices = ViceInstance.LoadAll(bob);
            Assert.Single(vices);
            Assert.Equal(2, vices[0].AddictionLevel);
        }

        [Fact]
        public void ProcessInteraction_CapsAtMaxAddictionLevel()
        {
            SeedPair();
            for (int i = 0; i < DoseProcessor.MaxAddictionLevel + 3; i++)
            {
                _processor.ProcessInteraction(BuildPending("Alice", "Bob", "musk"));
            }

            var bob = _database.GetProfile("Bob");
            var vices = ViceInstance.LoadAll(bob);
            Assert.Equal(DoseProcessor.MaxAddictionLevel, vices[0].AddictionLevel);
        }

        [Fact]
        public void ProcessInteraction_DifferentVices_StoredSeparately()
        {
            SeedPair();
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "musk"));
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "drug"));

            var bob = _database.GetProfile("Bob");
            var vices = ViceInstance.LoadAll(bob);
            Assert.Equal(2, vices.Count);
            Assert.Contains(vices, v => v.Vice == "musk");
            Assert.Contains(vices, v => v.Vice == "drug");
        }

        [Fact]
        public void ProcessInteraction_SetsPerVicePerRecipientCooldownOnInitiator()
        {
            SeedPair();
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "musk"));

            var alice = _database.GetProfile("Alice");
            string key = DoseProcessor.CooldownTimerKey("musk", "Bob");
            Assert.True(alice.timers.ContainsKey(key));
            Assert.True(alice.timers[key].timerEnd > DateTime.UtcNow.AddDays(6));
            Assert.True(alice.timers[key].timerEnd <= DateTime.UtcNow.AddDays(7).AddMinutes(1));
        }

        [Fact]
        public void ProcessInteraction_DifferentVice_DoesNotShareCooldown()
        {
            SeedPair();
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "musk"));

            var alice = _database.GetProfile("Alice");
            Assert.True(alice.timers.ContainsKey(DoseProcessor.CooldownTimerKey("musk", "Bob")));
            Assert.False(alice.timers.ContainsKey(DoseProcessor.CooldownTimerKey("drug", "Bob")));
        }

        [Fact]
        public void ProcessInteraction_SelfDose_PersistsViceOnSameProfile()
        {
            // Self-dose is explicitly allowed — initiator and recipient are the same user.
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            _processor.ProcessInteraction(BuildPending("Alice", "Alice", "musk"));

            var alice = _database.GetProfile("Alice");
            var vices = ViceInstance.LoadAll(alice);
            Assert.Single(vices);
            Assert.Equal(1, vices[0].AddictionLevel);
            // The dose cooldown is on the (initiator → recipient → vice) tuple; for a
            // self-dose this is the same profile, but it should still be set so a
            // self-dose can't spam.
            Assert.True(alice.timers.ContainsKey(DoseProcessor.CooldownTimerKey("musk", "Alice")));
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            SeedPair();
            var pending = BuildPending("Alice", "Bob", "musk");
            _database.AddPendingCommand(pending);

            _processor.ProcessInteraction(pending);

            Assert.Null(_database.GetPendingCommand(pending.Id));
        }

        // -------------------------------------------------------------------
        // IntensifyExistingVices — climax integration helper
        // -------------------------------------------------------------------

        [Fact]
        public void IntensifyExistingVices_PresentVice_BumpsLevel()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "cum", "Alice");

            var intensified = DoseProcessor.IntensifyExistingVices(bob, new[] { "cum", "pre", "seminal" });

            Assert.Single(intensified);
            Assert.Equal("cum", intensified[0]);
            var vices = ViceInstance.LoadAll(bob);
            Assert.Equal(2, vices.First(v => v.Vice == "cum").AddictionLevel);
            // "pre" and "seminal" were absent and stayed absent — climax does not introduce
            // new vices.
            Assert.DoesNotContain(vices, v => v.Vice == "pre");
            Assert.DoesNotContain(vices, v => v.Vice == "seminal");
        }

        [Fact]
        public void IntensifyExistingVices_AbsentVice_NoOp()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").Build();

            var intensified = DoseProcessor.IntensifyExistingVices(bob, new[] { "cum", "pre", "seminal" });

            Assert.Empty(intensified);
            Assert.Empty(ViceInstance.LoadAll(bob));
        }

        [Fact]
        public void IntensifyExistingVices_AlreadyMaxedVice_StillReportedButLevelUnchanged()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "cum", "Alice");
            // Push to cap.
            var entries = ViceInstance.LoadAll(bob);
            entries[0].AddictionLevel = DoseProcessor.MaxAddictionLevel;
            ViceInstance.SaveAll(bob, entries);

            var intensified = DoseProcessor.IntensifyExistingVices(bob, new[] { "cum" });

            // Player still sees the "addictions sharpen" flavor — but the level stays
            // pinned at the cap.
            Assert.Single(intensified);
            Assert.Equal(DoseProcessor.MaxAddictionLevel,
                ViceInstance.LoadAll(bob).First(v => v.Vice == "cum").AddictionLevel);
        }

        [Fact]
        public void IntensifyExistingVices_MultiplePresent_IntensifiesAll()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "cum", "Alice");
            DoseProcessor.ApplyDose(bob, "pre", "Alice");

            var intensified = DoseProcessor.IntensifyExistingVices(bob, new[] { "cum", "pre", "seminal" });

            Assert.Equal(new[] { "cum", "pre" }, intensified.ToArray());
            var vices = ViceInstance.LoadAll(bob);
            Assert.Equal(2, vices.First(v => v.Vice == "cum").AddictionLevel);
            Assert.Equal(2, vices.First(v => v.Vice == "pre").AddictionLevel);
        }

        [Fact]
        public void ApplyDose_DifferentDosers_LatestRecorded()
        {
            // The DosedBy field tracks the most recent doser for natural read-back.
            var bob = new ProfileBuilder().WithUserName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "musk", "Alice");
            DoseProcessor.ApplyDose(bob, "musk", "Carol");

            var vices = ViceInstance.LoadAll(bob);
            Assert.Equal("Carol", vices[0].DosedBy);
            Assert.Equal(2, vices[0].AddictionLevel);
        }

        [Fact]
        public void ReadAddictionLevel_PresentVice_ReturnsLevel()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "musk", "Alice");
            DoseProcessor.ApplyDose(bob, "musk", "Alice");

            Assert.Equal(2, DoseProcessor.ReadAddictionLevel(bob, "musk"));
        }

        [Fact]
        public void ReadAddictionLevel_AbsentVice_ReturnsZero()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").Build();
            Assert.Equal(0, DoseProcessor.ReadAddictionLevel(bob, "musk"));
        }

        private void SeedPair()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
        }

        private static PendingCommand BuildPending(string initiator, string recipient, string vice)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = DoseProcessor.DoseType,
                    identifier = vice,
                    investmentLevel = "consequence",
                    interactionTime = DateTime.UtcNow,
                },
            };
        }
    }
}
