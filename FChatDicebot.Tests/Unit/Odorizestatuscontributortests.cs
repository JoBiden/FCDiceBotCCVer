using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.InteractionProcessors.StatusEffectContributors;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    [Collection("Database")]
    public class OdorizeStatusContributorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly OdorizeStatusContributor _contributor;

        public OdorizeStatusContributorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _contributor = new OdorizeStatusContributor(_database);
        }

        public void Dispose() { }

        [Fact]
        public void Contribute_NoScents_ReturnsEmpty()
        {
            var profile = new ProfileBuilder().WithUserName("Bob").Build();

            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "kiss", isInitiator: false);

            Assert.Empty(result.ConsentWarnings);
            Assert.Empty(result.CompletionAppendix);
            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void Contribute_NullProfile_ReturnsEmpty()
        {
            var result = _contributor.Contribute(null, StatusEffectCallSite.Completion, "kiss", isInitiator: false);

            Assert.Empty(result.ConsentWarnings);
            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Contribute_Consent_EmitsFragmentWithoutDecrement()
        {
            var bob = SeedBobWithScent("musk", layers: 3, remainingMentions: 9);

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Consent, "kiss", isInitiator: false);

            Assert.Single(result.ConsentWarnings);
            Assert.Empty(result.CompletionAppendix);
            // No state mutation on Consent.
            var fresh = _database.GetProfile("Bob");
            var layers = ScentLayer.LoadAll(fresh);
            Assert.Equal(9, layers[0].RemainingMentions);
            Assert.Equal(3, layers[0].Layers);
        }

        [Fact]
        public void Contribute_Completion_EmitsFragmentAndDecrements()
        {
            var bob = SeedBobWithScent("musk", layers: 3, remainingMentions: 9);

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", isInitiator: false);

            Assert.Single(result.CompletionAppendix);
            Assert.Empty(result.ConsentWarnings);
            // RemainingMentions ticked from 9 to 8; layer didn't drop yet (9 → 8 is still > (3-1)*3=6).
            var fresh = _database.GetProfile("Bob");
            var layers = ScentLayer.LoadAll(fresh);
            Assert.Equal(8, layers[0].RemainingMentions);
            Assert.Equal(3, layers[0].Layers);
        }

        [Theory]
        [InlineData(1, "lingers faintly")]
        [InlineData(2, "clings to")]
        [InlineData(3, "hangs heavy on")]
        [InlineData(4, "fills the air around")]
        [InlineData(5, "thick")]
        public void DescribeLayer_DefaultScent_MatchesLayerLevel(int layerCount, string expectedSubstring)
        {
            string fragment = OdorizeStatusContributor.DescribeLayer(layerCount, scentIdentifier: null, scentName: "wood", appliedBy: "Alice", subjectName: "Bob");
            Assert.Contains(expectedSubstring, fragment);
            // No category → default "a wood scent" rendering.
            Assert.Contains("a wood scent", fragment, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DescribeLayer_AboveCap_ClampsTo5LayerTemplate()
        {
            string fragment = OdorizeStatusContributor.DescribeLayer(99, scentIdentifier: null, scentName: "wood", appliedBy: "Alice", subjectName: "Bob");
            Assert.Contains("thick", fragment);
            // 5-tier should be bold, not italic.
            Assert.Contains("[b]thick[/b]", fragment);
        }

        [Fact]
        public void DescribeLayer_PersonalCategory_UsesAppliedByPossessive()
        {
            var ident = new Identifier { type = "musk", categories = new[] { "personal", "scent" } };

            string thick = OdorizeStatusContributor.DescribeLayer(5, ident, "musk", appliedBy: "Alice", subjectName: "Bob");
            string clings = OdorizeStatusContributor.DescribeLayer(2, ident, "musk", appliedBy: "Alice", subjectName: "Bob");

            Assert.Contains("Alice's musk", thick);
            Assert.Equal("Alice's musk clings to Bob.", clings);
        }

        [Fact]
        public void DescribeLayer_ScentOfCategory_UsesAScentOfForm()
        {
            var ident = new Identifier { type = "lemonade", categories = new[] { "scentof", "scent" } };

            string thick = OdorizeStatusContributor.DescribeLayer(5, ident, "lemonade", appliedBy: "Alice", subjectName: "Bob");
            string clings = OdorizeStatusContributor.DescribeLayer(2, ident, "lemonade", appliedBy: "Alice", subjectName: "Bob");

            Assert.Contains("a scent of lemonade", thick);
            // Sentence-initial position capitalizes the leading "a" of the phrase.
            Assert.Equal("A scent of lemonade clings to Bob.", clings);
        }

        [Fact]
        public void Contribute_LayerDropsAt3MentionBoundary()
        {
            // 5-layer / 15 mentions → expect descriptor to step down every 3 mentions.
            var bob = SeedBobWithScent("musk", layers: 5, remainingMentions: 15);

            // Mentions 1, 2, 3: Layers stays 5 → "thick" three times, then Layers drops to 4.
            for (int i = 0; i < 3; i++)
            {
                var r = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", isInitiator: false);
                Assert.Contains("thick", r.CompletionAppendix[0]);
            }

            var afterThree = _database.GetProfile("Bob");
            var layers = ScentLayer.LoadAll(afterThree);
            Assert.Equal(4, layers[0].Layers);
            Assert.Equal(12, layers[0].RemainingMentions);

            // Mention 4: descriptor uses pre-decrement Layers=4 → "fills the air".
            var nextTier = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", isInitiator: false);
            Assert.Contains("fills the air", nextTier.CompletionAppendix[0]);
        }

        [Fact]
        public void Contribute_FullLifecycle_FivePerDescriptor_FifteenMentionsTotal()
        {
            var bob = SeedBobWithScent("musk", layers: 5, remainingMentions: 15);
            var descriptorsSeen = new List<string>();

            for (int i = 0; i < 15; i++)
            {
                var r = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", isInitiator: false);
                Assert.Single(r.CompletionAppendix);
                descriptorsSeen.Add(r.CompletionAppendix[0]);
            }

            // Three of each tier, in order: thick → fills the air → hangs heavy → clings → lingers faintly.
            Assert.Equal(3, descriptorsSeen.FindAll(s => s.Contains("thick")).Count);
            Assert.Equal(3, descriptorsSeen.FindAll(s => s.Contains("fills the air")).Count);
            Assert.Equal(3, descriptorsSeen.FindAll(s => s.Contains("hangs heavy")).Count);
            Assert.Equal(3, descriptorsSeen.FindAll(s => s.Contains("clings to")).Count);
            Assert.Equal(3, descriptorsSeen.FindAll(s => s.Contains("lingers faintly")).Count);

            // 16th mention: nothing left.
            var after = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", isInitiator: false);
            Assert.Empty(after.CompletionAppendix);

            // Entry has been removed from the profile.
            var fresh = _database.GetProfile("Bob");
            Assert.Empty(ScentLayer.LoadAll(fresh));
        }

        [Fact]
        public void Contribute_OdorizeInteraction_SkipsSelfReferentialMention()
        {
            var bob = SeedBobWithScent("musk", layers: 3, remainingMentions: 9);

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "odorize", isInitiator: false);

            // No fragment emitted, no decrement happened.
            Assert.Empty(result.CompletionAppendix);
            var fresh = _database.GetProfile("Bob");
            var layers = ScentLayer.LoadAll(fresh);
            Assert.Equal(9, layers[0].RemainingMentions);
        }

        [Fact]
        public void Contribute_MultipleScents_AllContributeAndDecrement()
        {
            var bob = SeedBobWithScents(
                new ScentLayer { Scent = "musk", Layers = 2, RemainingMentions = 6, AppliedBy = "Alice", LastAppliedAt = DateTime.UtcNow },
                new ScentLayer { Scent = "ozone", Layers = 1, RemainingMentions = 3, AppliedBy = "Carol", LastAppliedAt = DateTime.UtcNow });

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", isInitiator: false);

            Assert.Equal(2, result.CompletionAppendix.Count);

            var fresh = _database.GetProfile("Bob");
            var layers = ScentLayer.LoadAll(fresh);
            Assert.Equal(2, layers.Count);
            foreach (var layer in layers)
            {
                if (layer.Scent == "musk") Assert.Equal(5, layer.RemainingMentions);
                if (layer.Scent == "ozone") Assert.Equal(2, layer.RemainingMentions);
            }
        }

        [Fact]
        public void Contribute_ExhaustedLayer_RemovesEntry()
        {
            // RemainingMentions=1, Layers=1: one more contribute should remove the entry.
            var bob = SeedBobWithScent("musk", layers: 1, remainingMentions: 1);

            _contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", isInitiator: false);

            var fresh = _database.GetProfile("Bob");
            Assert.Empty(ScentLayer.LoadAll(fresh));
        }

        [Fact]
        public void Contribute_StaleZeroRemaining_GetsCleanedUp()
        {
            // Corrupt state: RemainingMentions=0 already. Should be removed silently with no fragment.
            var bob = SeedBobWithScent("musk", layers: 2, remainingMentions: 0);

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", isInitiator: false);

            Assert.Empty(result.CompletionAppendix);
            var fresh = _database.GetProfile("Bob");
            Assert.Empty(ScentLayer.LoadAll(fresh));
        }

        [Fact]
        public void Contribute_Persists_ViaInjectedDatabase()
        {
            var bob = SeedBobWithScent("musk", layers: 1, remainingMentions: 3);

            // Drive three mentions; profile should be persisted to DB each time.
            for (int i = 0; i < 3; i++)
            {
                _contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", isInitiator: false);
            }

            // Fetch a brand-new Profile object from the DB and confirm the entry is gone
            // — proving persistence happened, not just in-memory mutation.
            var fresh = _database.GetProfile("Bob");
            Assert.Empty(ScentLayer.LoadAll(fresh));
        }

        private Profile SeedBobWithScent(string scent, int layers, int remainingMentions)
        {
            return SeedBobWithScents(new ScentLayer
            {
                Scent = scent,
                Layers = layers,
                RemainingMentions = remainingMentions,
                AppliedBy = "Alice",
                LastAppliedAt = DateTime.UtcNow
            });
        }

        private Profile SeedBobWithScents(params ScentLayer[] scents)
        {
            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            ScentLayer.SaveAll(bob, new List<ScentLayer>(scents));
            _database.SetProfile("Bob", bob);
            return _database.GetProfile("Bob");
        }
    }
}
