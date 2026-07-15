using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Casual;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Unit tests for LapsitProcessor — the shared processor behind !lap and !sit. The
    /// typed verb (carried on Interaction.type / identifier) decides which side the
    /// initiator plays: !lap → initiator is the lap (lapsittake); !sit → initiator is the
    /// sitter (lapsitgive).
    /// </summary>
    [Collection("Database")]
    public class LapsitProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly LapsitProcessor _processor;

        public LapsitProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new LapsitProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsLap()
        {
            Assert.Equal("lap", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCasual()
        {
            Assert.Equal("casual", _processor.InvestmentLevel);
        }

        [Theory]
        [InlineData("sit", true)]
        [InlineData("SIT", true)]
        [InlineData("lap", false)]
        [InlineData("anything-else", false)]
        public void InitiatorIsSitter_DependsOnVerb(string verb, bool expected)
        {
            Assert.Equal(expected, LapsitProcessor.InitiatorIsSitter(verb));
        }

        [Fact]
        public void ProcessInteraction_Lap_InitiatorIsTheLap()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = MakePending("Alice", "Bob", "lap");
            _database.AddPendingCommand(pendingCommand);

            string result = _processor.ProcessInteraction(pendingCommand);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            // !lap: initiator is the lap (lapsittake), recipient sits (lapsitgive).
            Assert.Equal("lap", result);
            Assert.Equal(1, alice.counts["lapsittake"]);
            Assert.Equal(1, bob.counts["lapsitgive"]);
        }

        [Fact]
        public void ProcessInteraction_Sit_InitiatorIsTheSitter()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = MakePending("Alice", "Bob", "sit");
            _database.AddPendingCommand(pendingCommand);

            string result = _processor.ProcessInteraction(pendingCommand);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            // !sit: initiator sits (lapsitgive), recipient is the lap (lapsittake).
            Assert.Equal("sit", result);
            Assert.Equal(1, alice.counts["lapsitgive"]);
            Assert.Equal(1, bob.counts["lapsittake"]);
        }

        [Fact]
        public void ProcessInteraction_SavesInteractionToHistory_AndDeletesPending()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = MakePending("Alice", "Bob", "lap");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var interactions = _database.GetInteractionsByInitiator("Alice");
            Assert.Contains(interactions, i => i.type == "lap");
            Assert.Null(_database.GetPendingCommandAwaitingConsent("Bob"));
        }

        [Fact]
        public void GetCompletionMessage_Lap_UsesLapOpener()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            string message = _processor.GetCompletionMessage(initiator, recipient, "lap");

            Assert.Contains("Alice", message);
            Assert.Contains("Bob", message);
            Assert.Contains("onto their lap", message);
            Assert.DoesNotContain("{lapsitgiver}", message);
        }

        [Fact]
        public void GetCompletionMessage_Sit_UsesSitOpener()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            // Run several times: the sit opener has two variants and the descriptor pool is
            // random, so exercise both and confirm no raw token survives.
            for (int i = 0; i < 50; i++)
            {
                string message = _processor.GetCompletionMessage(initiator, recipient, "sit");
                Assert.Contains("Alice", message);
                Assert.Contains("Bob", message);
                Assert.Contains("lap", message);
                Assert.DoesNotContain("{lapsitgiver}", message);
            }
        }

        [Fact]
        public void GetGroupCompletionMessage_StackOfThreeOrMore_NeverUsesTwoPersonDescriptor()
        {
            // Feedback 6a5592e6: "does it count as a stack if it's only two" must not show
            // up under a completion message describing a 3+ stack. The descriptor draw is
            // random, so run enough times to visit the whole pool.
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var consenters = new List<Profile>
            {
                new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database),
                new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").BuildAndSave(_database),
            };

            for (int i = 0; i < 100; i++)
            {
                string message = _processor.GetGroupCompletionMessage(initiator, consenters, "lap");
                Assert.DoesNotContain("only two", message);
            }
        }

        [Fact]
        public void GetGroupCompletionMessage_GroupThatResolvedToTwoPeople_KeepsTwoPersonDescriptor()
        {
            // A group where only one seat consented is still just two people, so the
            // two-person tease stays in the pool. With 5 descriptors, 200 draws miss one
            // only with negligible probability.
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var consenters = new List<Profile>
            {
                new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database),
            };

            bool seenTwoPersonLine = false;
            for (int i = 0; i < 200 && !seenTwoPersonLine; i++)
            {
                seenTwoPersonLine = _processor.GetGroupCompletionMessage(initiator, consenters, "lap")
                    .Contains("only two");
            }
            Assert.True(seenTwoPersonLine);
        }

        private static PendingCommand MakePending(string initiator, string recipient, string verb)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = verb,
                    identifier = verb,
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
