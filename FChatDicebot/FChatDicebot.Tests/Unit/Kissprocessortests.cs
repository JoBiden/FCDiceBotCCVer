using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
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
    /// Unit tests for KissProcessor.
    /// Tests interaction processing logic in isolation.
    /// </summary>
    [Collection("Database")]
    public class KissProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly KissProcessor _processor;

        public KissProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset(); // Ensure clean state for each test
            _database = _fixture.Database;
            _processor = new KissProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsKiss()
        {
            // Assert
            Assert.Equal("kiss", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCasual()
        {
            // Assert
            Assert.Equal("casual", _processor.InvestmentLevel);
        }

        [Fact]
        public void ProcessInteraction_ValidKiss_ReturnsKissString()
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
                    type = "kiss",
                    identifier = "", // Casual interactions typically have no identifier
                    investmentLevel = "casual"
                },
                awaitingConsentFrom = "Bob"
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            string result = _processor.ProcessInteraction(pendingCommand);

            // Assert
            Assert.Equal("kiss", result);
        }

        [Fact]
        public void ProcessInteraction_FirstKiss_IncrementsBothCounts()
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
                    type = "kiss",
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

            Assert.True(aliceProfile.counts.ContainsKey("kiss"));
            Assert.Equal(1, aliceProfile.counts["kiss"]);

            Assert.True(bobProfile.counts.ContainsKey("kiss"));
            Assert.Equal(1, bobProfile.counts["kiss"]);
        }

        [Fact]
        public void ProcessInteraction_MultipleKisses_AccumulatesCounts()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCount("kiss", 5)
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithCount("kiss", 3)
                .BuildAndSave(_database);

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "kiss",
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

            Assert.Equal(6, aliceProfile.counts["kiss"]);
            Assert.Equal(4, bobProfile.counts["kiss"]);
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
                    type = "kiss",
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
        public void GetCompletionMessage_IncludesKissDescriptor()
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
            Assert.Contains("Mwah!", message);
            Assert.Contains("Alice the Adventurer", message);
            Assert.Contains("Bob the Bold", message);
            Assert.Contains("kiss", message);

            // Should contain one of the kiss descriptors (we can't predict which due to random)
            bool hasDescriptor = message.Contains("cute.") ||
                                message.Contains("lewd") ||
                                message.Contains("salatious") ||
                                message.Contains("hot!") ||
                                message.Contains("wet") ||
                                message.Contains("short and sweet") ||
                                message.Contains("slow and sensual") ||
                                message.Contains("casual peck") ||
                                message.Contains("doki");
            Assert.True(hasDescriptor, "Message should contain a kiss descriptor");
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
            Assert.Contains("[eicon]qckiss[/eicon]", message);
        }

        [Fact]
        public void GetConsentWarning_UsesSmoochWording()
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
            Assert.Contains("smooch", warning);
            Assert.Contains("!consent", warning);
        }

        public void Dispose()
        {
            // Optional: cleanup specific to this test class if needed
        }
    }
}