using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    [Collection("Database")]
    public class MonsterizeProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly MonsterizeProcessor _processor;

        public MonsterizeProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new MonsterizeProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsMonsterize()
        {
            Assert.Equal("monsterize", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsConsequence()
        {
            Assert.Equal("consequence", _processor.InvestmentLevel);
        }

        [Fact]
        public void ValidateInteraction_NoMonsterTypeProvided_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_RecipientOnActiveCooldown_ReturnsFailure()
        {
            // The processor's own cooldown gate (relocated from the command) — a stray
            // pending that races past the command-time check must still be rejected here.
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithTimer(MonsterizeProcessor.CooldownTimerKey, DateTime.UtcNow.AddDays(3))
                .BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "dragon");

            Assert.False(result.IsValid);
            Assert.Contains("monsterize", result.ErrorMessage);
            Assert.Contains("Bob", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_RecipientExpiredCooldown_Allowed()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder()
                .WithUserName("Bob")
                .WithTimer(MonsterizeProcessor.CooldownTimerKey, DateTime.UtcNow.AddDays(-1))
                .BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "dragon");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ProcessInteraction_SetsMonsterType()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "monsterize",
                    identifier = "dragon",
                    investmentLevel = "consequence"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.Equal("dragon", bobProfile.characteristics["monster"]);
            Assert.True(bobProfile.timers.ContainsKey("monsterize"));
        }

        public void Dispose()
        {
        }
    }
}
