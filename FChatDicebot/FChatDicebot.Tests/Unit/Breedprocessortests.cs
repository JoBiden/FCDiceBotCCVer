using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Commitment;
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
    public class BreedProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly BreedProcessor _processor;

        public BreedProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new BreedProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsBreed()
        {
            Assert.Equal("breed", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCommitment()
        {
            Assert.Equal("commitment", _processor.InvestmentLevel);
        }

        [Fact]
        public void GetInteractionVerb_PastTense_ReturnsBred()
        {
            Assert.Equal("bred", _processor.GetInteractionVerb(InteractionProcessorBase.VerbTense.Past));
        }

        [Fact]
        public void ValidateInteraction_EmptyMonster_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_UnknownMonster_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "unknownmonster");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_KnownMonster_ReturnsSuccess()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "slime",
                description = "a viscous slime",
                categories = new[] { "monster" }
            });

            var result = _processor.ValidateInteraction("Alice", "Bob", "slime");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ProcessInteraction_AppendsPregnancyToRecipient()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "slime",
                description = "a viscous slime",
                categories = new[] { "monster" },
                gestationDays = 3,
                broodSizeMin = 2,
                broodSizeMax = 2
            });

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "slime");
            _database.AddPendingCommand(pendingCommand);

            DateTime before = DateTime.UtcNow;
            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            Assert.Single(bob.pregnancies);
            var pregnancy = bob.pregnancies.First();
            Assert.Equal("Alice", pregnancy.Initiator);
            Assert.Equal("slime", pregnancy.MonsterType);
            Assert.Equal(2, pregnancy.BroodSize);
            Assert.True(pregnancy.ConceivedAt >= before.AddSeconds(-1));
            Assert.True(pregnancy.ReadyAt >= pregnancy.ConceivedAt.AddDays(3).AddSeconds(-1));
            Assert.True(pregnancy.ReadyAt <= pregnancy.ConceivedAt.AddDays(3).AddSeconds(1));
            Assert.False(string.IsNullOrEmpty(pregnancy.Id));
        }

        [Fact]
        public void ProcessInteraction_UnknownMonster_UsesDefaults()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "mystery");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            Assert.Equal(1, pregnancy.BroodSize);
            Assert.True(pregnancy.ReadyAt <= pregnancy.ConceivedAt.AddDays(1).AddSeconds(1));
        }

        [Fact]
        public void ProcessInteraction_BroodSizeWithinRange()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "wasp",
                description = "a wasp brood",
                categories = new[] { "monster" },
                gestationDays = 1,
                broodSizeMin = 3,
                broodSizeMax = 7
            });

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "wasp");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            Assert.InRange(pregnancy.BroodSize, 3, 7);
        }

        [Fact]
        public void ProcessInteraction_GestationCappedAtSevenDays()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "elephant",
                description = "an elephant",
                categories = new[] { "monster" },
                gestationDays = 365,
                broodSizeMin = 1,
                broodSizeMax = 1
            });

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "elephant");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            Assert.True(pregnancy.ReadyAt <= pregnancy.ConceivedAt.AddDays(BreedProcessor.MaxGestationDays).AddSeconds(1));
        }

        [Fact]
        public void ProcessInteraction_SetsSymmetricPairCooldown()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "slime");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            Assert.True(alice.timers.ContainsKey(BreedProcessor.PairTimerKey("Bob")));
            Assert.True(bob.timers.ContainsKey(BreedProcessor.PairTimerKey("Alice")));
            Assert.True(alice.timers[BreedProcessor.PairTimerKey("Bob")].timerEnd > DateTime.UtcNow);
        }

        [Fact]
        public void ProcessInteraction_MultiplePregnanciesAccumulate()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "slime")));
            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "wasp")));

            var bob = _database.GetProfile("Bob");
            Assert.Equal(2, bob.pregnancies.Count);
            Assert.Contains(bob.pregnancies, p => p.MonsterType == "slime");
            Assert.Contains(bob.pregnancies, p => p.MonsterType == "wasp");
        }

        [Fact]
        public void ProcessInteraction_PregnancyPersistsAcrossReloads()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "slime")));

            var reloaded = _database.GetProfile("Bob");
            Assert.Single(reloaded.pregnancies);
            Assert.Equal("slime", reloaded.pregnancies[0].MonsterType);
        }

        private PendingCommand BuildPendingCommand(string initiator, string recipient, string monster)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = "breed",
                    identifier = monster,
                    investmentLevel = "commitment"
                }
            };
        }

        private PendingCommand SaveAndReturn(PendingCommand pendingCommand)
        {
            _database.AddPendingCommand(pendingCommand);
            return pendingCommand;
        }

        public void Dispose()
        {
        }
    }
}
