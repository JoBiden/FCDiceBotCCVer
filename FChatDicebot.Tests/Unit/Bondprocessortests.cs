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
    public class BondProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly BondProcessor _processor;

        public BondProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new BondProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsBond()
        {
            Assert.Equal("bond", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCommitment()
        {
            Assert.Equal("commitment", _processor.InvestmentLevel);
        }

        [Fact]
        public void ValidateInteraction_NoBondTypeProvided_ReturnsFailure()
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
        }

        [Fact]
        public void ProcessInteraction_CreatesSymmetricalBondLists()
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
                    type = "bond",
                    identifier = "soul",
                    investmentLevel = "consequence"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var aliceProfile = _database.GetProfile("Alice");
            var bobProfile = _database.GetProfile("Bob");

            Assert.True(aliceProfile.lists.ContainsKey("bondsoulinitiated"));
            Assert.Contains("Bob", aliceProfile.lists["bondsoulinitiated"]);

            Assert.True(bobProfile.lists.ContainsKey("bondsoulreceived"));
            Assert.Contains("Alice", bobProfile.lists["bondsoulreceived"]);
        }

        [Fact]
        public void ProcessInteraction_SetsCooldownForBoth()
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
                    type = "bond",
                    identifier = "soul",
                    investmentLevel = "consequence"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var aliceProfile = _database.GetProfile("Alice");
            var bobProfile = _database.GetProfile("Bob");

            Assert.True(aliceProfile.timers.ContainsKey("bond"));
            Assert.True(bobProfile.timers.ContainsKey("bond"));
            Assert.True(aliceProfile.timers["bond"].timerEnd > DateTime.UtcNow);
            Assert.True(bobProfile.timers["bond"].timerEnd > DateTime.UtcNow);
        }

        // -------------------------------------------------------------------
        // ValidateInteraction — base-class coverage
        // -------------------------------------------------------------------

        [Fact]
        public void ValidateInteraction_NonExistentInitiator_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("NonExistentUser", "Bob", "soul");

            Assert.False(result.IsValid);
            Assert.Contains("NonExistentUser", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_NonExistentRecipient_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "NonExistentUser", "soul");

            Assert.False(result.IsValid);
            Assert.Contains("NonExistentUser", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_BothUsersAndIdentifier_ReturnsSuccess()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "soul");

            Assert.True(result.IsValid);
        }

        // -------------------------------------------------------------------
        // ProcessInteraction — persistence + accumulation
        // -------------------------------------------------------------------

        [Fact]
        public void ProcessInteraction_SavesInteractionToHistory()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", "soul");
            _processor.ProcessInteraction(pending);

            var saved = _database.GetInteractionsByInitiator("Alice");
            Assert.Single(saved);
            Assert.Equal("bond", saved[0].type);
            Assert.Equal("soul", saved[0].identifier);
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", "soul");
            _database.AddPendingCommand(pending);
            _processor.ProcessInteraction(pending);

            Assert.Null(_database.GetPendingCommand(pending.Id));
        }

        [Fact]
        public void ProcessInteraction_DifferentBondTypes_StoredInSeparateLists()
        {
            // The list name format is "bond{type}initiated" / "bond{type}received", so a
            // soul bond and a pet bond live in independent slots and don't collide.
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "soul"));
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "pet"));

            var alice = _database.GetProfile("Alice");
            Assert.True(alice.lists.ContainsKey("bondsoulinitiated"));
            Assert.True(alice.lists.ContainsKey("bondpetinitiated"));
            Assert.Single(alice.lists["bondsoulinitiated"]);
            Assert.Single(alice.lists["bondpetinitiated"]);
        }

        // -------------------------------------------------------------------
        // GetCompletionMessage
        // -------------------------------------------------------------------

        [Fact]
        public void GetCompletionMessage_IncludesBothDisplayNames()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice the Brave").Build();
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob the Bold").Build();

            string message = _processor.GetCompletionMessage(initiator, recipient, "soul");

            Assert.Contains("Alice the Brave", message);
            Assert.Contains("Bob the Bold", message);
        }

        [Fact]
        public void GetCompletionMessage_ContainsBondVerbiage()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").Build();
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").Build();

            string message = _processor.GetCompletionMessage(initiator, recipient, "soul");

            // Mutual-bond phrasing — both directions of the relationship.
            Assert.Contains("is now", message);
            Assert.Contains("bright future together", message);
        }

        // -------------------------------------------------------------------
        // GetConsentWarning
        // -------------------------------------------------------------------

        [Fact]
        public void GetConsentWarning_IncludesNotTakenLightlyWarning()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").Build();
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").Build();

            string warning = _processor.GetConsentWarning(initiator, recipient, "soul");

            Assert.Contains("Alice", warning);
            Assert.Contains("Bob", warning);
            Assert.Contains("[b]", warning);
            Assert.Contains("not be taken lightly", warning);
            Assert.Contains("!consent", warning);
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private static PendingCommand BuildPending(string initiator, string recipient, string bondType)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    Id = ObjectId.GenerateNewId(),
                    initiator = initiator,
                    recipient = recipient,
                    type = "bond",
                    identifier = bondType,
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
