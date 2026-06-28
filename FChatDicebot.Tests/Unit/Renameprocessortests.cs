using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
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
    public class RenameProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly RenameProcessor _processor;

        public RenameProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new RenameProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsRename()
        {
            Assert.Equal("rename", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsConsequence()
        {
            Assert.Equal("consequence", _processor.InvestmentLevel);
        }

        [Fact]
        public void ProcessInteraction_ChangesDisplayName()
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
                    type = "rename",
                    identifier = "",
                    investmentLevel = "consequence",
                    extraParameters = new BsonArray { "NewName" }
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.Contains("[s]Bob[/s]", bobProfile.displayName);
            Assert.Contains("NewName", bobProfile.displayName);
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
                    type = "rename",
                    identifier = "",
                    investmentLevel = "consequence",
                    extraParameters = new BsonArray { "NewName" }
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.True(bobProfile.timers.ContainsKey("rename"));
            Assert.True(bobProfile.timers["rename"].timerEnd > DateTime.UtcNow);
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
                    type = "rename",
                    identifier = "",
                    investmentLevel = "consequence",
                    extraParameters = new BsonArray { "NewName" }
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var interactions = _database.GetInteractionsByInitiator("Alice");
            Assert.Contains(interactions, i => i.type == "rename");
        }

        [Fact]
        public void GetCompletionMessage_AfterProcess_ShowsOldNameThenNewName()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice the Adventurer")
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob the Bold")
                .BuildAndSave(_database);

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "rename",
                    identifier = "Sir Reginald",
                    investmentLevel = "consequence",
                    extraParameters = new BsonArray { "Sir Reginald" }
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act: process applies the rename (mutating displayName to the strikethrough
            // form), then the completion message is generated on the same instance.
            _processor.ProcessInteraction(pendingCommand);
            var recipientAfter = _database.GetProfile("Bob");
            string message = _processor.GetCompletionMessage(initiator, recipientAfter, "Sir Reginald");

            // Assert: clean old name in slot 1, new name in slot 2 — no blank, no
            // strikethrough leaking into the "old name" slot.
            Assert.Equal(
                "Alice the Adventurer has made it known that Bob the Bold is to be known as Sir Reginald henceforth! All occurrences of their name in our records will be changed to reflect their new identity.",
                message);
            Assert.DoesNotContain("[s]", message);
        }

        [Fact]
        public void ValidateInteraction_BothUsersExist_ReturnsSuccess()
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
            var result = _processor.ValidateInteraction("Alice", "Bob", "NewName");

            // Assert
            Assert.True(result.IsValid);
        }

        public void Dispose()
        {
        }
    }
}
