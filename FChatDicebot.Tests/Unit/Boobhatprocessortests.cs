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
    /// Unit tests for BoobhatProcessor (directional casual: boobhatgive / boobhattake).
    /// </summary>
    [Collection("Database")]
    public class BoobhatProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly BoobhatProcessor _processor;

        public BoobhatProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new BoobhatProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsBoobhat()
        {
            Assert.Equal("boobhat", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCasual()
        {
            Assert.Equal("casual", _processor.InvestmentLevel);
        }

        [Fact]
        public void ProcessInteraction_ValidBoobhat_ReturnsBoobhatString()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = MakePending("Alice", "Bob");
            _database.AddPendingCommand(pendingCommand);

            string result = _processor.ProcessInteraction(pendingCommand);

            Assert.Equal("boobhat", result);
        }

        [Fact]
        public void ProcessInteraction_FirstBoobhat_IncrementsDifferentCounts()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = MakePending("Alice", "Bob");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            // Initiator provides the hat (boobhatgive); recipient wears it (boobhattake).
            Assert.Equal(1, alice.counts["boobhatgive"]);
            Assert.Equal(1, bob.counts["boobhattake"]);
        }

        [Fact]
        public void ProcessInteraction_SavesInteractionToHistory()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = MakePending("Alice", "Bob");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var interactions = _database.GetInteractionsByInitiator("Alice");
            Assert.Contains(interactions, i => i.type == "boobhat");
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = MakePending("Alice", "Bob");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var pendingAfter = _database.GetPendingCommandAwaitingConsent("Bob");
            Assert.Null(pendingAfter);
        }

        [Fact]
        public void GetCompletionMessage_IncludesBothNames()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice the Adventurer").BuildAndSave(_database);
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob the Bold").BuildAndSave(_database);

            string message = _processor.GetCompletionMessage(initiator, recipient, "");

            Assert.Contains("Alice the Adventurer", message);
            Assert.Contains("Bob the Bold", message);
        }

        private static PendingCommand MakePending(string initiator, string recipient)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = "boobhat",
                    identifier = "",
                    investmentLevel = "casual"
                },
                awaitingConsentFrom = recipient
            };
        }

        public void Dispose()
        {
        }
    }
}
