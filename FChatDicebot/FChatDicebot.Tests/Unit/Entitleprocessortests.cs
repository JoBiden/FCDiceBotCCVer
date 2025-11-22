using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Commitment;
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
    /// <summary>
    /// Unit tests for EntitleProcessor.
    /// Tests interaction processing logic in isolation.
    /// </summary>
    [Collection("Database")]
    public class EntitleProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly EntitleProcessor _processor;

        public EntitleProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset(); // Ensure clean state for each test
            _database = _fixture.Database;
            _processor = new EntitleProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsEntitle()
        {
            // Assert
            Assert.Equal("entitle", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCommitment()
        {
            // Assert
            Assert.Equal("commitment", _processor.InvestmentLevel);
        }

        [Fact]
        public void ValidateInteraction_NoTitleProvided_ReturnsFailure()
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
            Assert.False(result.IsValid);
            Assert.Contains("must specify a title", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_TitleTooLong_ReturnsFailure()
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

            string longTitle = new string('a', 101); // 101 characters

            // Act
            var result = _processor.ValidateInteraction("Alice", "Bob", longTitle);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("too long", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_TitleContainsSystemMarker_ReturnsFailure()
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

            string titleWithMarker = "The·Chosen·One";

            // Act
            var result = _processor.ValidateInteraction("Alice", "Bob", titleWithMarker);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("reserved for system-granted titles", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_ValidTitle_ReturnsSuccess()
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
            var result = _processor.ValidateInteraction("Alice", "Bob", "The Brave");

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ProcessInteraction_AddsNewTitle()
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
                    type = "entitle",
                    identifier = "The Brave",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.NotNull(bobProfile.titles);
            Assert.Single(bobProfile.titles);
            Assert.Equal("The Brave", bobProfile.titles[0].titleText);
            Assert.Equal("Alice", bobProfile.titles[0].givenBy);
        }

        [Fact]
        public void ProcessInteraction_MultipleTitles_Accumulates()
        {
            // Arrange
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var charlie = new ProfileBuilder()
                .WithUserName("Charlie")
                .WithDisplayName("Charlie")
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            // First title from Alice
            var pendingCommand1 = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "entitle",
                    identifier = "The Brave",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand1);
            _processor.ProcessInteraction(pendingCommand1);

            // Clear cooldown
            var bobProfile = _database.GetProfile("Bob");
            bobProfile.timers["entitle"] = new CoolDown { timerEnd = DateTime.UtcNow.AddDays(-1) };
            _database.SetProfile("Bob", bobProfile);

            // Second title from Charlie
            var pendingCommand2 = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Charlie",
                    recipient = "Bob",
                    type = "entitle",
                    identifier = "The Wise",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand2);
            _processor.ProcessInteraction(pendingCommand2);

            // Assert
            bobProfile = _database.GetProfile("Bob");
            Assert.NotNull(bobProfile.titles);
            Assert.Equal(2, bobProfile.titles.Count);
            Assert.Contains(bobProfile.titles, t => t.titleText == "The Brave" && t.givenBy == "Alice");
            Assert.Contains(bobProfile.titles, t => t.titleText == "The Wise" && t.givenBy == "Charlie");
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
                    type = "entitle",
                    identifier = "The Brave",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.True(bobProfile.timers.ContainsKey("entitle"));
            Assert.True(bobProfile.timers["entitle"].timerEnd > DateTime.UtcNow);
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
                    type = "entitle",
                    identifier = "The Brave",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var interactions = _database.GetInteractionsByInitiator("Alice");
            Assert.NotEmpty(interactions);
            Assert.Contains(interactions, i => i.type == "entitle" && i.identifier == "The Brave");
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
                    type = "entitle",
                    identifier = "The Brave",
                    investmentLevel = "commitment"
                },
                awaitingConsentFrom = "Bob"
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var pendingAfter = _database.GetPendingCommandAwaitingConsent("Bob");
            Assert.Null(pendingAfter);
        }

        [Fact]
        public void GetCompletionMessage_IncludesTitle()
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
            string message = _processor.GetCompletionMessage(initiator, recipient, "The Brave");

            // Assert
            Assert.Contains("Alice the Adventurer", message);
            Assert.Contains("Bob the Bold", message);
            Assert.Contains("The Brave", message);
            Assert.Contains("title", message);
        }

        [Fact]
        public void GetConsentWarning_IncludesTitleAndWarning()
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
            string warning = _processor.GetConsentWarning(initiator, recipient, "The Brave");

            // Assert
            Assert.Contains("Alice", warning);
            Assert.Contains("The Brave", warning);
            Assert.Contains("commitment", warning);
            Assert.Contains("!consent", warning);
        }

        public void Dispose()
        {
            // Optional: cleanup specific to this test class if needed
        }
    }
}
