using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    [Collection("Database")]
    public class OdorizeProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly OdorizeProcessor _processor;

        public OdorizeProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new OdorizeProcessor(_database);

            // Seed a "musk" scent identifier so ValidateInteraction's GetIdentifier check passes.
            _fixture.SeedIdentifier(new Identifier
            {
                type = "musk",
                description = "musk",
                categories = new[] { "scent" }
            });
        }

        public void Dispose() { }

        [Fact]
        public void InteractionType_ReturnsOdorize()
        {
            Assert.Equal("odorize", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsConsequence()
        {
            Assert.Equal("consequence", _processor.InvestmentLevel);
        }

        [Fact]
        public void ValidateInteraction_EmptyScent_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_UnknownScent_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "swampgas");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_KnownScent_Succeeds()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "musk");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_IdentifierMissingScentCategory_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);
            // Identifier exists but isn't tagged as a scent — should be rejected.
            _fixture.SeedIdentifier(new Identifier
            {
                type = "dragon",
                description = "a dragon",
                categories = new[] { "monster" }
            });

            var result = _processor.ValidateInteraction("Alice", "Bob", "dragon");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ProcessInteraction_FirstApplication_CreatesLayerWith3Mentions()
        {
            SeedPair();
            var pending = BuildPending("Alice", "Bob", "musk");

            _processor.ProcessInteraction(pending);

            var bob = _database.GetProfile("Bob");
            var layers = ScentLayer.LoadAll(bob);
            Assert.Single(layers);
            Assert.Equal("musk", layers[0].Scent);
            Assert.Equal(1, layers[0].Layers);
            Assert.Equal(3, layers[0].RemainingMentions);
            // AppliedBy stores the initiator's display name so descriptor text reads
            // naturally (e.g. "Alice's musk").
            Assert.Equal("Alice", layers[0].AppliedBy);
        }

        [Fact]
        public void ProcessInteraction_RestackSameScent_IncrementsLayerAndRefreshes()
        {
            SeedPair();

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "musk"));
            // Second odorize from the same initiator with the same scent.
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "musk"));

            var bob = _database.GetProfile("Bob");
            var layers = ScentLayer.LoadAll(bob);
            Assert.Single(layers);
            Assert.Equal(2, layers[0].Layers);
            Assert.Equal(6, layers[0].RemainingMentions);
        }

        [Fact]
        public void ProcessInteraction_StackCapsAtMaxLayers()
        {
            SeedPair();

            for (int i = 0; i < OdorizeProcessor.MaxLayers + 3; i++)
            {
                _processor.ProcessInteraction(BuildPending("Alice", "Bob", "musk"));
            }

            var bob = _database.GetProfile("Bob");
            var layers = ScentLayer.LoadAll(bob);
            Assert.Single(layers);
            Assert.Equal(OdorizeProcessor.MaxLayers, layers[0].Layers);
            Assert.Equal(OdorizeProcessor.MaxLayers * OdorizeProcessor.MentionsPerLayer, layers[0].RemainingMentions);
        }

        [Fact]
        public void ProcessInteraction_SetsPerPairScentCooldownOnInitiator()
        {
            SeedPair();

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "musk"));

            var alice = _database.GetProfile("Alice");
            string expectedKey = OdorizeProcessor.CooldownTimerKey("musk", "Bob");
            Assert.True(alice.timers.ContainsKey(expectedKey));
            Assert.True(alice.timers[expectedKey].timerEnd > DateTime.UtcNow.AddDays(6));
            Assert.True(alice.timers[expectedKey].timerEnd <= DateTime.UtcNow.AddDays(7).AddMinutes(1));
        }

        [Fact]
        public void ProcessInteraction_DifferentScent_DoesNotShareCooldown()
        {
            SeedPair();
            _fixture.SeedIdentifier(new Identifier { type = "ozone", description = "ozone", categories = new[] { "scent" } });

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "musk"));

            var alice = _database.GetProfile("Alice");
            Assert.True(alice.timers.ContainsKey(OdorizeProcessor.CooldownTimerKey("musk", "Bob")));
            Assert.False(alice.timers.ContainsKey(OdorizeProcessor.CooldownTimerKey("ozone", "Bob")));
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            SeedPair();
            var pending = BuildPending("Alice", "Bob", "musk");
            _database.AddPendingCommand(pending);

            _processor.ProcessInteraction(pending);

            Assert.Null(_database.GetPendingCommand(pending.Id));
        }

        [Fact]
        public void ApplyLayer_DifferentScents_StoredSeparately()
        {
            var profile = new ProfileBuilder().WithUserName("Bob").Build();

            OdorizeProcessor.ApplyLayer(profile, "musk", "Alice");
            OdorizeProcessor.ApplyLayer(profile, "ozone", "Alice");

            var layers = ScentLayer.LoadAll(profile);
            Assert.Equal(2, layers.Count);
        }

        private void SeedPair()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
        }

        private static PendingCommand BuildPending(string initiator, string recipient, string scent)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = "odorize",
                    identifier = scent,
                    investmentLevel = "consequence",
                    interactionTime = DateTime.UtcNow
                }
            };
        }
    }
}
