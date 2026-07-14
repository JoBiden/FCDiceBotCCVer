using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
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
    /// Unit tests for PetProcessor (directional casual: petgive / pettake). The custom eicon
    /// belongs to the recipient (the one being petted), not the initiator — the reverse of the
    /// other directional casuals.
    /// </summary>
    [Collection("Database")]
    public class PetProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly PetProcessor _processor;

        public PetProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new PetProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsPet()
        {
            Assert.Equal("pet", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCasual()
        {
            Assert.Equal("casual", _processor.InvestmentLevel);
        }

        [Fact]
        public void GroupSpec_IsDirectional_GiveTake()
        {
            var spec = _processor.GroupSpec;
            Assert.NotNull(spec);
            Assert.Equal(GroupCountKind.Directional, spec.Kind);
            Assert.Equal("petgive", spec.GiveKey);
            Assert.Equal("pettake", spec.TakeKey);
        }

        [Fact]
        public void ProcessInteraction_ValidPet_ReturnsPetString()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = MakePending("Alice", "Bob");
            _database.AddPendingCommand(pendingCommand);

            string result = _processor.ProcessInteraction(pendingCommand);

            Assert.Equal("pet", result);
        }

        [Fact]
        public void ProcessInteraction_FirstPet_IncrementsDifferentCounts()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = MakePending("Alice", "Bob");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            // Initiator is the one petting (petgive); recipient is petted (pettake).
            Assert.Equal(1, alice.counts["petgive"]);
            Assert.Equal(1, bob.counts["pettake"]);
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
            Assert.Contains(interactions, i => i.type == "pet");
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
                Assert.Contains("Bob the Bold", message);
                Assert.DoesNotContain("{petgiver}", message);
                Assert.DoesNotContain("{pettaker}", message);
            }
        }

        [Fact]
        public void CompletionEicon_UsesRecipientsEicon_NotInitiators()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            InteractionEiconSupport.SetInteractionEicon(initiator, "pet", "petterIcon");
            InteractionEiconSupport.SetInteractionEicon(recipient, "pet", "beingpetIcon");

            string message = _processor.GetCompletionMessageWithStatusEffects(initiator, recipient, "", "pet");

            // The one being petted (recipient) is the eicon subject.
            Assert.Contains("beingpetIcon", message);
            Assert.DoesNotContain("petterIcon", message);
        }

        [Fact]
        public void GroupEiconSuffix_ShowsEachRecipientsEicon_NotInitiators()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            var carol = new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").BuildAndSave(_database);

            InteractionEiconSupport.SetInteractionEicon(initiator, "pet", "petterIcon");
            InteractionEiconSupport.SetInteractionEicon(bob, "pet", "bobPet");
            InteractionEiconSupport.SetInteractionEicon(carol, "pet", "carolPet");

            string suffix = _processor.GetGroupEiconSuffix("pet", initiator, new List<Profile> { bob, carol });

            Assert.Contains("bobPet", suffix);
            Assert.Contains("carolPet", suffix);
            Assert.DoesNotContain("petterIcon", suffix);
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
                    type = "pet",
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
