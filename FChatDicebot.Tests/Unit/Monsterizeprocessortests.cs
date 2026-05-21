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

        // -------------------------------------------------------------------
        // ValidateInteraction — base-class coverage
        // -------------------------------------------------------------------

        [Fact]
        public void ValidateInteraction_NonExistentInitiator_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("NonExistentUser", "Bob", "dragon");

            Assert.False(result.IsValid);
            Assert.Contains("NonExistentUser", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_NonExistentRecipient_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "NonExistentUser", "dragon");

            Assert.False(result.IsValid);
            Assert.Contains("NonExistentUser", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_BothUsersAndIdentifier_ReturnsSuccess()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "dragon");

            Assert.True(result.IsValid);
        }

        // -------------------------------------------------------------------
        // ProcessInteraction — persistence + cooldown
        // -------------------------------------------------------------------

        [Fact]
        public void ProcessInteraction_SetsSevenDayCooldown()
        {
            // Spec: monsterize locks the recipient for 7 days from the next UTC midnight.
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", "dragon");
            _processor.ProcessInteraction(pending);

            var bob = _database.GetProfile("Bob");
            Assert.True(bob.timers.ContainsKey(MonsterizeProcessor.CooldownTimerKey));
            // Cooldown lands between 6 and 8 days out (allowing for date-rounding slack).
            Assert.True(bob.timers[MonsterizeProcessor.CooldownTimerKey].timerEnd > DateTime.UtcNow.AddDays(6));
            Assert.True(bob.timers[MonsterizeProcessor.CooldownTimerKey].timerEnd <= DateTime.UtcNow.AddDays(8));
        }

        [Fact]
        public void ProcessInteraction_SavesInteractionToHistory()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "dragon"));

            var saved = _database.GetInteractionsByInitiator("Alice");
            Assert.Single(saved);
            Assert.Equal("monsterize", saved[0].type);
            Assert.Equal("dragon", saved[0].identifier);
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", "dragon");
            _database.AddPendingCommand(pending);
            _processor.ProcessInteraction(pending);

            Assert.Null(_database.GetPendingCommand(pending.Id));
        }

        // -------------------------------------------------------------------
        // GetCompletionMessage
        // -------------------------------------------------------------------

        [Fact]
        public void GetCompletionMessage_IncludesBothDisplayNamesAndMonsterText()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice the Brave").Build();
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob the Bold").Build();

            string message = _processor.GetCompletionMessage(initiator, recipient, "dragon");

            Assert.Contains("Alice the Brave", message);
            Assert.Contains("Bob the Bold", message);
            Assert.Contains("dragon", message);
            Assert.Contains("monsterkind", message);
        }

        // -------------------------------------------------------------------
        // GetConsentWarning
        // -------------------------------------------------------------------

        [Fact]
        public void GetConsentWarning_IncludesNotTakenLightlyAndWorkCheckWarning()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").Build();
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").Build();

            string warning = _processor.GetConsentWarning(initiator, recipient, "dragon");

            Assert.Contains("Alice", warning);
            Assert.Contains("Bob", warning);
            Assert.Contains("dragon", warning);
            Assert.Contains("[b]", warning);
            Assert.Contains("not be taken lightly", warning);
            // The Consequence-tier warning calls out !work integration.
            Assert.Contains("!work", warning);
            Assert.Contains("!consent", warning);
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private static PendingCommand BuildPending(string initiator, string recipient, string monsterType)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    Id = ObjectId.GenerateNewId(),
                    initiator = initiator,
                    recipient = recipient,
                    type = "monsterize",
                    identifier = monsterType,
                    investmentLevel = "consequence",
                    interactionTime = DateTime.UtcNow,
                }
            };
        }

        public void Dispose()
        {
        }
    }
}
