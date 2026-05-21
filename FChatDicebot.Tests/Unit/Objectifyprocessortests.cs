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

        // -------------------------------------------------------------------
        // ValidateInteraction — base-class coverage
        // -------------------------------------------------------------------

        [Fact]
        public void ValidateInteraction_NonExistentInitiator_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("NonExistentUser", "Bob", "chair");

            Assert.False(result.IsValid);
            Assert.Contains("NonExistentUser", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_NonExistentRecipient_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "NonExistentUser", "chair");

            Assert.False(result.IsValid);
            Assert.Contains("NonExistentUser", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_BothUsersAndIdentifier_ReturnsSuccess()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "chair");

            Assert.True(result.IsValid);
        }

        // -------------------------------------------------------------------
        // ProcessInteraction — persistence + cooldown
        // -------------------------------------------------------------------

        [Fact]
        public void ProcessInteraction_SetsOneDayCooldown()
        {
            // Spec: objectify locks the recipient for 1 day from the next UTC midnight.
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", "chair");
            _processor.ProcessInteraction(pending);

            var bob = _database.GetProfile("Bob");
            Assert.True(bob.timers.ContainsKey(ObjectifyProcessor.CooldownTimerKey));
            // Cooldown lands between 0 and 2 days out (UTC-midnight rounding can give >1d
            // when the test runs early in the day, or just under 1d on later runs).
            Assert.True(bob.timers[ObjectifyProcessor.CooldownTimerKey].timerEnd > DateTime.UtcNow);
            Assert.True(bob.timers[ObjectifyProcessor.CooldownTimerKey].timerEnd <= DateTime.UtcNow.AddDays(2));
        }

        [Fact]
        public void ProcessInteraction_SavesInteractionToHistory()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "chair"));

            var saved = _database.GetInteractionsByInitiator("Alice");
            Assert.Single(saved);
            Assert.Equal("objectify", saved[0].type);
            Assert.Equal("chair", saved[0].identifier);
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", "chair");
            _database.AddPendingCommand(pending);
            _processor.ProcessInteraction(pending);

            Assert.Null(_database.GetPendingCommand(pending.Id));
        }

        // -------------------------------------------------------------------
        // GetCompletionMessage
        // -------------------------------------------------------------------

        [Fact]
        public void GetCompletionMessage_IncludesBothDisplayNamesAndObjectText()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice the Brave").Build();
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob the Bold").Build();

            string message = _processor.GetCompletionMessage(initiator, recipient, "chair");

            Assert.Contains("Alice the Brave", message);
            Assert.Contains("Bob the Bold", message);
            Assert.Contains("chair", message);
            Assert.Contains("some sort of", message);
        }

        // -------------------------------------------------------------------
        // GetConsentWarning
        // -------------------------------------------------------------------

        [Fact]
        public void GetConsentWarning_IncludesNotTakenLightlyWarning()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").Build();
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").Build();

            string warning = _processor.GetConsentWarning(initiator, recipient, "chair");

            Assert.Contains("Alice", warning);
            Assert.Contains("Bob", warning);
            Assert.Contains("chair", warning);
            Assert.Contains("[b]", warning);
            Assert.Contains("not be taken lightly", warning);
            Assert.Contains("!consent", warning);
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private static PendingCommand BuildPending(string initiator, string recipient, string objectType)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    Id = ObjectId.GenerateNewId(),
                    initiator = initiator,
                    recipient = recipient,
                    type = "objectify",
                    identifier = objectType,
                    investmentLevel = "commitment",
                    interactionTime = DateTime.UtcNow,
                }
            };
        }

        public void Dispose()
        {
        }
    }
}
