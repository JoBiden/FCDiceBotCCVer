using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Casual;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Unit tests for SpankProcessor.
    /// Tests interaction processing logic in isolation.
    /// </summary>
    [Collection("Database")]
    public class SpankProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly SpankProcessor _processor;

        public SpankProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset(); // Ensure clean state for each test
            _database = _fixture.Database;
            _processor = new SpankProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsSpank()
        {
            // Assert
            Assert.Equal("spank", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCasual()
        {
            // Assert
            Assert.Equal("casual", _processor.InvestmentLevel);
        }

        [Fact]
        public void ProcessInteraction_ValidSpank_ReturnsSpankString()
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
                    type = "spank",
                    identifier = "",
                    investmentLevel = "casual"
                },
                awaitingConsentFrom = "Bob"
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            string result = _processor.ProcessInteraction(pendingCommand);

            // Assert
            Assert.Equal("spank", result);
        }

        [Fact]
        public void ProcessInteraction_FirstSpank_IncrementsDifferentCounts()
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
                    type = "spank",
                    identifier = "",
                    investmentLevel = "casual"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var aliceProfile = _database.GetProfile("Alice");
            var bobProfile = _database.GetProfile("Bob");

            // Initiator gets "spankgive" count
            Assert.True(aliceProfile.counts.ContainsKey("spankgive"));
            Assert.Equal(1, aliceProfile.counts["spankgive"]);

            // Recipient gets "spanktake" count
            Assert.True(bobProfile.counts.ContainsKey("spanktake"));
            Assert.Equal(1, bobProfile.counts["spanktake"]);
        }

        [Fact]
        public void ProcessInteraction_MultipleSpanks_AccumulatesDifferentCounts()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCount("spankgive", 5)
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithCount("spanktake", 3)
                .BuildAndSave(_database);

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "spank",
                    identifier = "",
                    investmentLevel = "casual"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var aliceProfile = _database.GetProfile("Alice");
            var bobProfile = _database.GetProfile("Bob");

            Assert.Equal(6, aliceProfile.counts["spankgive"]);
            Assert.Equal(4, bobProfile.counts["spanktake"]);
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
                    type = "spank",
                    identifier = "",
                    investmentLevel = "casual"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert - Interaction should be saved to history
            var interactions = _database.GetInteractions("Alice", "Bob");
            Assert.NotEmpty(interactions);
            Assert.Contains(interactions, i => i.type == "spank");
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
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
                    type = "spank",
                    identifier = "",
                    investmentLevel = "casual"
                },
                awaitingConsentFrom = "Bob"
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert - Pending command should be deleted
            var pendingAfter = _database.GetPendingCommandAwaitingConsent("Bob");
            Assert.Null(pendingAfter);
        }

        [Fact]
        public void ValidateInteraction_NonExistentInitiator_ReturnsFailure()
        {
            // Arrange
            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            // Act
            var result = _processor.ValidateInteraction("NonExistentUser", "Bob", "");

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("NonExistentUser", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_NonExistentRecipient_ReturnsFailure()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            // Act
            var result = _processor.ValidateInteraction("Alice", "NonExistentUser", "");

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("NonExistentUser", result.ErrorMessage);
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
            var result = _processor.ValidateInteraction("Alice", "Bob", "");

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void GetCompletionMessage_IncludesSpankDescriptor()
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

            // Act
            string message = _processor.GetCompletionMessage(initiator, recipient, "");

            // Assert
            Assert.Contains("Alice the Adventurer", message);
            Assert.Contains("Bob the Bold", message);

            // Should contain one of the spank descriptors (we can't predict which due to random)
            bool hasDescriptor = message.Contains("sharp spank") ||
                                message.Contains("love tap") ||
                                message.Contains("smack") ||
                                message.Contains("red imprint") ||
                                message.Contains("booty grope") ||
                                message.Contains("impact");
            Assert.True(hasDescriptor, "Message should contain a spank descriptor");
        }

        [Fact]
        public void GetCompletionMessage_QueenContract_AddsEicon()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var queenContract = new ProfileBuilder()
                .WithUserName("Queen Contract")
                .WithDisplayName("Queen Contract")
                .BuildAndSave(_database);

            // Act
            string message = _processor.GetCompletionMessage(initiator, queenContract, "");

            // Assert
            Assert.Contains("[eicon]qcass[eicon]", message);
        }

        public void Dispose()
        {
            // Optional: cleanup specific to this test class if needed
        }
    }
}
