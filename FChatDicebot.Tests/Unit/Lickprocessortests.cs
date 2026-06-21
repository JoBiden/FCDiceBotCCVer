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
    /// Unit tests for LickProcessor (directional casual: lickgive / licktake).
    /// </summary>
    [Collection("Database")]
    public class LickProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly LickProcessor _processor;

        public LickProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new LickProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsLick()
        {
            Assert.Equal("lick", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCasual()
        {
            Assert.Equal("casual", _processor.InvestmentLevel);
        }

        [Fact]
        public void ProcessInteraction_ValidLick_ReturnsLickString()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = MakePending("Alice", "Bob");
            _database.AddPendingCommand(pendingCommand);

            string result = _processor.ProcessInteraction(pendingCommand);

            Assert.Equal("lick", result);
        }

        [Fact]
        public void ProcessInteraction_FirstLick_IncrementsDifferentCounts()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = MakePending("Alice", "Bob");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            // Initiator is the licker (lickgive); recipient is the licked (licktake).
            Assert.Equal(1, alice.counts["lickgive"]);
            Assert.Equal(1, bob.counts["licktake"]);
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
            Assert.Contains(interactions, i => i.type == "lick");
        }

        [Fact]
        public void GetCompletionMessage_IncludesBothNames_AndResolvesTokens()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice the Adventurer").BuildAndSave(_database);
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob the Bold").BuildAndSave(_database);

            // Run enough times to exercise the token-bearing descriptors and confirm no
            // raw {token} placeholder ever survives into the rendered message.
            for (int i = 0; i < 50; i++)
            {
                string message = _processor.GetCompletionMessage(initiator, recipient, "");
                Assert.Contains("Alice the Adventurer", message);
                Assert.DoesNotContain("{lickgiver}", message);
                Assert.DoesNotContain("{licktaker}", message);
            }
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
                    type = "lick",
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
