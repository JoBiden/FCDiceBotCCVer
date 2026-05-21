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
    public class ObjectifyProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly ObjectifyProcessor _processor;

        public ObjectifyProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new ObjectifyProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsObjectify()
        {
            Assert.Equal("objectify", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCommitment()
        {
            Assert.Equal("commitment", _processor.InvestmentLevel);
        }

        [Fact]
        public void ValidateInteraction_NoObjectTypeProvided_ReturnsFailure()
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
                .WithTimer(ObjectifyProcessor.CooldownTimerKey, DateTime.UtcNow.AddHours(6))
                .BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "chair");

            Assert.False(result.IsValid);
            Assert.Contains("objectify", result.ErrorMessage);
            Assert.Contains("Bob", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_RecipientExpiredCooldown_Allowed()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder()
                .WithUserName("Bob")
                .WithTimer(ObjectifyProcessor.CooldownTimerKey, DateTime.UtcNow.AddHours(-2))
                .BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "chair");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ProcessInteraction_SetsObjectType()
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
                    type = "objectify",
                    identifier = "chair",
                    investmentLevel = "consequence"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.Equal("chair", bobProfile.characteristics["objectType"]);
            Assert.True(bobProfile.timers.ContainsKey("objectify"));
        }

        public void Dispose()
        {
        }
    }
}
