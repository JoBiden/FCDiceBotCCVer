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
    public class ConsumeProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly ConsumeProcessor _processor;

        public ConsumeProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new ConsumeProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsConsume()
        {
            Assert.Equal("consume", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsConsequence()
        {
            Assert.Equal("consequence", _processor.InvestmentLevel);
        }

        [Fact]
        public void ProcessInteraction_SetsCooldownTimer()
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
                    type = "consume",
                    identifier = "",
                    investmentLevel = "consequence"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.True(bobProfile.timers.ContainsKey("consume"));
            Assert.True(bobProfile.timers["consume"].timerEnd > DateTime.UtcNow);
        }

        [Fact]
        public void ProcessInteraction_SavesInteractionToHistory()
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
                    type = "consume",
                    identifier = "",
                    investmentLevel = "consequence"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var interactions = _database.GetInteractionsByInitiator("Alice");
            Assert.Contains(interactions, i => i.type == "consume");
        }

        [Fact]
        public void GetCompletionMessage_IncludesSwallowMessage()
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

            // Act
            string message = _processor.GetCompletionMessage(initiator, recipient, "");

            // Assert
            Assert.Contains("Alice", message);
            Assert.Contains("Bob", message);
            Assert.Contains("swallow", message);
        }

        public void Dispose()
        {
        }
    }
}
