using FChatDicebot.Database;
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
    /// <summary>
    /// Unit tests for MarkProcessor.
    /// Tests interaction processing logic in isolation.
    /// </summary>
    [Collection("Database")]
    public class MarkProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly MarkProcessor _processor;

        public MarkProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset(); // Ensure clean state for each test
            _database = _fixture.Database;
            _processor = new MarkProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsMark()
        {
            // Assert
            Assert.Equal("mark", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCommitment()
        {
            // Assert
            Assert.Equal("commitment", _processor.InvestmentLevel);
        }

        [Fact]
        public void ValidateInteraction_InitiatorHasNoMark_ReturnsFailure()
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
            var result = _processor.ValidateInteraction("Alice", "Bob", "neck");

            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_NoBodypartSpecified_ReturnsFailure()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCharacteristic("mark", "♥")
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            // Act
            var result = _processor.ValidateInteraction("Alice", "Bob", "");

            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_ValidMarkWithBodypart_ReturnsSuccess()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCharacteristic("mark", "♥")
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            // Act
            var result = _processor.ValidateInteraction("Alice", "Bob", "neck");

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ProcessInteraction_AddsInitiatorToMarkList()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCharacteristic("mark", "♥")
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
                    type = "mark",
                    identifier = "neck",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.True(bobProfile.lists.ContainsKey("neckmarks"));
            Assert.Contains("Alice", bobProfile.lists["neckmarks"]);
        }

        [Fact]
        public void ProcessInteraction_MultipleMarksOnSameBodypart_Accumulates()
        {
            // Arrange
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCharacteristic("mark", "♥")
                .BuildAndSave(_database);

            var charlie = new ProfileBuilder()
                .WithUserName("Charlie")
                .WithDisplayName("Charlie")
                .WithCharacteristic("mark", "★")
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            // First mark from Alice
            var pendingCommand1 = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "mark",
                    identifier = "neck",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand1);
            _processor.ProcessInteraction(pendingCommand1);

            // Wait a day to clear cooldown
            var bobProfile = _database.GetProfile("Bob");
            bobProfile.timers["mark"] = new CoolDown { timerEnd = DateTime.UtcNow.AddDays(-1) };
            _database.SetProfile("Bob", bobProfile);

            // Second mark from Charlie
            var pendingCommand2 = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Charlie",
                    recipient = "Bob",
                    type = "mark",
                    identifier = "neck",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand2);
            _processor.ProcessInteraction(pendingCommand2);

            // Assert
            bobProfile = _database.GetProfile("Bob");
            Assert.True(bobProfile.lists.ContainsKey("neckmarks"));
            Assert.Contains("Alice", bobProfile.lists["neckmarks"]);
            Assert.Contains("Charlie", bobProfile.lists["neckmarks"]);
            Assert.Equal(2, bobProfile.lists["neckmarks"].Count);
        }

        [Fact]
        public void ProcessInteraction_SetsCooldownTimer()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCharacteristic("mark", "♥")
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
                    type = "mark",
                    identifier = "neck",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.True(bobProfile.timers.ContainsKey("mark"));
            Assert.True(bobProfile.timers["mark"].timerEnd > DateTime.UtcNow);
        }

        [Fact]
        public void ProcessInteraction_QueenContract_AddsQueenMark()
        {
            // Arrange
            var queenContract = new ProfileBuilder()
                .WithUserName("Queen Contract")
                .WithDisplayName("Queen Contract")
                .WithCharacteristic("mark", "[eicon]qcmark[/eicon]")
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
                    initiator = "Queen Contract",
                    recipient = "Bob",
                    type = "mark",
                    identifier = "neck",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.True(bobProfile.characteristics.ContainsKey("queenMark"));
            Assert.Equal("[eicon]qcmark[/eicon]", bobProfile.characteristics["queenMark"]);
        }

        [Fact]
        public void ProcessInteraction_SavesInteractionToHistory()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCharacteristic("mark", "♥")
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
                    type = "mark",
                    identifier = "neck",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var interactions = _database.GetInteractions("Alice", "Bob");
            Assert.NotEmpty(interactions);
            Assert.Contains(interactions, i => i.type == "mark" && i.identifier == "neck");
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCharacteristic("mark", "♥")
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
                    type = "mark",
                    identifier = "neck",
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
        public void GetCompletionMessage_IncludesMarkAndBodypart()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice the Adventurer")
                .WithCharacteristic("mark", "♥")
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob the Bold")
                .BuildAndSave(_database);

            // Act
            string message = _processor.GetCompletionMessage(initiator, recipient, "neck");

            // Assert
            Assert.Contains("Alice the Adventurer", message);
            Assert.Contains("Bob the Bold", message);
            Assert.Contains("♥", message);
            Assert.Contains("neck", message);
        }

        [Fact]
        public void GetConsentWarning_IncludesBodypartAndWarning()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCharacteristic("mark", "♥")
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            // Act
            string warning = _processor.GetConsentWarning(initiator, recipient, "neck");

            // Assert
            Assert.Contains("Alice", warning);
            Assert.Contains("Bob", warning);
            Assert.Contains("neck", warning);
            Assert.Contains("!consent", warning);
        }

        public void Dispose()
        {
            // Optional: cleanup specific to this test class if needed
        }
    }
}
