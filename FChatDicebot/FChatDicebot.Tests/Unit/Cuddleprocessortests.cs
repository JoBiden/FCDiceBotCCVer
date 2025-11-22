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
    /// Unit tests for CuddleProcessor.
    /// Tests interaction processing logic in isolation.
    /// </summary>
    [Collection("Database")]
    public class CuddleProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly CuddleProcessor _processor;

        public CuddleProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset(); // Ensure clean state for each test
            _database = _fixture.Database;
            _processor = new CuddleProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsCuddle()
        {
            // Assert
            Assert.Equal("cuddle", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCasual()
        {
            // Assert
            Assert.Equal("casual", _processor.InvestmentLevel);
        }

        [Fact]
        public void ProcessInteraction_ValidCuddle_ReturnsCuddleString()
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
                    type = "cuddle",
                    identifier = "",
                    investmentLevel = "casual"
                },
                awaitingConsentFrom = "Bob"
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            string result = _processor.ProcessInteraction(pendingCommand);

            // Assert
            Assert.Equal("cuddle", result);
        }

        [Fact]
        public void ProcessInteraction_FirstCuddle_IncrementsBothCounts()
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
                    type = "cuddle",
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

            Assert.True(aliceProfile.counts.ContainsKey("cuddle"));
            Assert.Equal(1, aliceProfile.counts["cuddle"]);

            Assert.True(bobProfile.counts.ContainsKey("cuddle"));
            Assert.Equal(1, bobProfile.counts["cuddle"]);
        }

        [Fact]
        public void ProcessInteraction_MultipleCuddles_AccumulatesCounts()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCount("cuddle", 5)
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithCount("cuddle", 3)
                .BuildAndSave(_database);

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "cuddle",
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

            Assert.Equal(6, aliceProfile.counts["cuddle"]);
            Assert.Equal(4, bobProfile.counts["cuddle"]);
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
                    type = "cuddle",
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
        public void GetCompletionMessage_IncludesCuddleDescriptor()
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
            Assert.Contains("cuddle", message);

            // Should contain one of the cuddle descriptors (we can't predict which due to random)
            bool hasDescriptor = message.Contains("Cute.") ||
                                message.Contains("lewd") ||
                                message.Contains("salatious") ||
                                message.Contains("Hot!") ||
                                message.Contains("cozy") ||
                                message.Contains("big spoon") ||
                                message.Contains("little spoon");
            Assert.True(hasDescriptor, "Message should contain a cuddle descriptor");
        }

        [Fact]
        public void GetCompletionMessage_QueenContract_AddsEicon()
        {
            // Arrange
            var queenContract = new ProfileBuilder()
                .WithUserName("Queen Contract")
                .WithDisplayName("Queen Contract")
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            // Act
            string message = _processor.GetCompletionMessage(queenContract, recipient, "");

            // Assert
            Assert.Contains("[eicon]qchug[eicon]", message);
        }

        [Fact]
        public void GetCompletionMessage_QueenContractAndRin_AddsRinEicon()
        {
            // Arrange
            var queenContract = new ProfileBuilder()
                .WithUserName("Queen Contract")
                .WithDisplayName("Queen Contract")
                .BuildAndSave(_database);

            var rin = new ProfileBuilder()
                .WithUserName("The Corrupted Rin")
                .WithDisplayName("The Corrupted Rin")
                .BuildAndSave(_database);

            // Act
            string message = _processor.GetCompletionMessage(queenContract, rin, "");

            // Assert
            Assert.Contains("[eicon]rin_lap[eicon]", message);
        }

        [Fact]
        public void GetConsentWarning_UsesCuddleWording()
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
            string warning = _processor.GetConsentWarning(initiator, recipient, "");

            // Assert
            Assert.Contains("Alice", warning);
            Assert.Contains("Bob", warning);
            Assert.Contains("cuddle", warning);
            Assert.Contains("!consent", warning);
        }

        public void Dispose()
        {
            // Optional: cleanup specific to this test class if needed
        }
    }
}
