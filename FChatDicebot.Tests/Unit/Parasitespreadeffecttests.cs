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
    /// <summary>
    /// Tests for ParasiteSpreadEffect — the cross-party post-interaction hook that rolls
    /// spread on non-casual non-infest interactions and adds a fresh ParasiteInstance with a
    /// grace window to the partner. Direct unit tests use a seeded Random so spread is
    /// deterministic; integration coverage drives spread through
    /// InteractionProcessorBase.GetCompletionMessageWithStatusEffects.
    /// </summary>
    [Collection("Database")]
    public class ParasiteSpreadEffectTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ParasiteSpreadEffectTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        public void Dispose() { }

        [Fact]
        public void Spread_NonCasual_RollHits_AddsParasiteToPartner()
        {
            var (alice, bob) = BuildPair();
            CarryParasite(alice, "paraslime");

            var effect = new ParasiteSpreadEffect(new AlwaysSpreadRandom());
            var fragments = effect.OnInteractionCompleted(
                alice, bob, "kiss", "involved", "", _database);

            var bobParasites = ParasiteInstance.LoadAll(bob);
            Assert.Single(bobParasites);
            Assert.Equal("paraslime", bobParasites[0].Parasite);
            Assert.True(bobParasites[0].SpreadFromContact);
            Assert.True(bobParasites[0].GraceUntil > DateTime.UtcNow.AddHours(23));
            Assert.Single(fragments);
            // "Meanwhile, paraslime has taken the opportunity to spread from Alice to Bob!"
            Assert.Contains("paraslime has taken the opportunity to spread from Alice to Bob", fragments[0]);
            Assert.Contains("24 hour window", fragments[0]);
        }

        [Fact]
        public void Spread_PluralParasite_UsesHaveVerb()
        {
            var (alice, bob) = BuildPair();
            CarryParasite(alice, "tentacles");

            var effect = new ParasiteSpreadEffect(new AlwaysSpreadRandom());
            var fragments = effect.OnInteractionCompleted(
                alice, bob, "kiss", "involved", "", _database);

            // Plural-shaped name → "have" not "has".
            Assert.Contains("tentacles have taken", fragments[0]);
        }

        [Fact]
        public void Spread_RollMisses_DoesNothing()
        {
            var (alice, bob) = BuildPair();
            CarryParasite(alice, "paraslime");

            var effect = new ParasiteSpreadEffect(new NeverSpreadRandom());
            var fragments = effect.OnInteractionCompleted(
                alice, bob, "kiss", "involved", "", _database);

            Assert.Empty(ParasiteInstance.LoadAll(bob));
            Assert.Empty(fragments);
        }

        [Fact]
        public void Spread_Casual_SkipsRollEntirely()
        {
            var (alice, bob) = BuildPair();
            CarryParasite(alice, "paraslime");

            // AlwaysSpreadRandom would otherwise transfer — the casual skip wins.
            var effect = new ParasiteSpreadEffect(new AlwaysSpreadRandom());
            var fragments = effect.OnInteractionCompleted(
                alice, bob, "kiss", "casual", "", _database);

            Assert.Empty(ParasiteInstance.LoadAll(bob));
            Assert.Empty(fragments);
        }

        [Fact]
        public void Spread_InfestInteraction_SkippedToAvoidLoop()
        {
            // When !infest itself runs, the parent recipient has just been infested. A naive
            // spread roll would catch that fresh parasite and send it right back to the
            // initiator.
            var (alice, bob) = BuildPair();
            CarryParasite(bob, "paraslime");

            var effect = new ParasiteSpreadEffect(new AlwaysSpreadRandom());
            var fragments = effect.OnInteractionCompleted(
                alice, bob, InfestProcessor.InfestType, "consequence", "paraslime", _database);

            Assert.Empty(ParasiteInstance.LoadAll(alice));
            Assert.Empty(fragments);
        }

        [Fact]
        public void Spread_SelfTarget_NoSpread()
        {
            // initiator == recipient: nothing to spread to.
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            CarryParasite(alice, "paraslime");

            var effect = new ParasiteSpreadEffect(new AlwaysSpreadRandom());
            var fragments = effect.OnInteractionCompleted(
                alice, alice, "dose", "consequence", "musk", _database);

            Assert.Single(ParasiteInstance.LoadAll(alice));
            Assert.Empty(fragments);
        }

        [Fact]
        public void Spread_PartnerAlreadyInfested_SameParasite_SkipsThatParasite()
        {
            var (alice, bob) = BuildPair();
            CarryParasite(alice, "paraslime");
            CarryParasite(bob, "paraslime");

            var effect = new ParasiteSpreadEffect(new AlwaysSpreadRandom());
            var fragments = effect.OnInteractionCompleted(
                alice, bob, "kiss", "involved", "", _database);

            // Bob already had it — no second copy, no fragment.
            Assert.Single(ParasiteInstance.LoadAll(bob));
            Assert.Empty(fragments);
        }

        [Fact]
        public void Spread_MultipleParasitesOnSource_AllRolledIndependently()
        {
            var (alice, bob) = BuildPair();
            CarryParasite(alice, "paraslime");
            CarryParasite(alice, "tentacles");

            var effect = new ParasiteSpreadEffect(new AlwaysSpreadRandom());
            var fragments = effect.OnInteractionCompleted(
                alice, bob, "kiss", "involved", "", _database);

            var bobParasites = ParasiteInstance.LoadAll(bob);
            Assert.Equal(2, bobParasites.Count);
            Assert.Equal(2, fragments.Count);
        }

        [Fact]
        public void Spread_SymmetricBothDirections_RecipientToInitiatorAndBack()
        {
            // Each side carries a different parasite — both spread.
            var (alice, bob) = BuildPair();
            CarryParasite(alice, "paraslime");
            CarryParasite(bob, "tentacles");

            var effect = new ParasiteSpreadEffect(new AlwaysSpreadRandom());
            effect.OnInteractionCompleted(alice, bob, "kiss", "involved", "", _database);

            var aliceParasites = ParasiteInstance.LoadAll(alice);
            var bobParasites = ParasiteInstance.LoadAll(bob);
            Assert.Equal(2, aliceParasites.Count);
            Assert.Equal(2, bobParasites.Count);
            Assert.Contains(aliceParasites, p => p.Parasite == "tentacles" && p.SpreadFromContact);
            Assert.Contains(bobParasites, p => p.Parasite == "paraslime" && p.SpreadFromContact);
        }

        [Fact]
        public void Spread_PersistsMutatedProfiles()
        {
            var (alice, bob) = BuildPair();
            CarryParasite(alice, "paraslime");

            var effect = new ParasiteSpreadEffect(new AlwaysSpreadRandom());
            effect.OnInteractionCompleted(alice, bob, "kiss", "involved", "", _database);

            // Reload from DB — the spread should have called SetProfile.
            var bobReloaded = _database.GetProfile("Bob");
            Assert.Single(ParasiteInstance.LoadAll(bobReloaded));
        }

        [Fact]
        public void Spread_ChanceHonoredOverManyTrials()
        {
            // Drive 1000 trials with the production Random; expect 50–200 transfers at 10%
            // (wide range so the test isn't flaky on a bad seed).
            int spreads = 0;
            var effect = new ParasiteSpreadEffect(new Random(42));
            for (int i = 0; i < 1000; i++)
            {
                var initiator = new ProfileBuilder().WithUserName("Init" + i).Build();
                var recipient = new ProfileBuilder().WithUserName("Recv" + i).Build();
                CarryParasite(initiator, "paraslime");

                effect.OnInteractionCompleted(initiator, recipient, "kiss", "involved", "", null);

                if (ParasiteInstance.LoadAll(recipient).Count > 0) spreads++;
            }
            Assert.InRange(spreads, 50, 200);
        }

        // ===================================================================
        // Integration: drive the spread through GetCompletionMessageWithStatusEffects
        // ===================================================================

        [Fact]
        public void HookFiresInsideCompletionWrapper_AndFragmentsAppendToCompletionMessage()
        {
            // Set up: registered effect that always spreads, and a vanilla processor whose
            // base completion message is non-empty. We use a real DoseProcessor against
            // already-set-up DB state — but easier to use a TestProcessor with explicit
            // investment level.
            PostInteractionEffectRegistry.Clear();
            StatusEffectRegistry.Clear();
            try
            {
                PostInteractionEffectRegistry.RegisterEffect(new ParasiteSpreadEffect(new AlwaysSpreadRandom()));

                var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
                var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
                CarryParasite(alice, "paraslime");

                var processor = new HostProcessor(_database);
                string output = processor.GetCompletionMessageWithStatusEffects(alice, bob, identifier: "");

                Assert.Contains("paraslime has taken the opportunity to spread from Alice to Bob", output);
                // Spread persisted to the database (the hook is meant to mutate, not just
                // emit text).
                Assert.Single(ParasiteInstance.LoadAll(_database.GetProfile("Bob")));
            }
            finally
            {
                PostInteractionEffectRegistry.Clear();
                StatusEffectRegistry.Clear();
            }
        }

        [Fact]
        public void HookSwallowsExceptions_DoesNotBreakHostMessage()
        {
            PostInteractionEffectRegistry.Clear();
            StatusEffectRegistry.Clear();
            try
            {
                PostInteractionEffectRegistry.RegisterEffect(new ThrowingPostEffect());

                var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
                var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

                var processor = new HostProcessor(_database);
                string output = processor.GetCompletionMessageWithStatusEffects(alice, bob, identifier: "");

                Assert.Contains("Alice test-interacts with Bob", output);
            }
            finally
            {
                PostInteractionEffectRegistry.Clear();
                StatusEffectRegistry.Clear();
            }
        }

        // ===================================================================
        // Helpers
        // ===================================================================

        private (Profile, Profile) BuildPair()
        {
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            return (alice, bob);
        }

        private void CarryParasite(Profile profile, string parasite)
        {
            InfestProcessor.ApplyInfestation(profile, parasite, "Carol",
                spreadFromContact: false, gracePeriod: TimeSpan.Zero);
            if (!string.IsNullOrEmpty(profile.userName))
            {
                _database.SetProfile(profile.userName, profile);
            }
        }

        /// <summary>Random that always returns 0 — every spread roll succeeds.</summary>
        private class AlwaysSpreadRandom : Random
        {
            public override double NextDouble() => 0.0;
        }

        /// <summary>Random that always returns 1 — no spread roll ever succeeds.</summary>
        private class NeverSpreadRandom : Random
        {
            public override double NextDouble() => 0.999999;
        }

        private class ThrowingPostEffect : IPostInteractionEffect
        {
            public List<string> OnInteractionCompleted(
                Profile initiator, Profile recipient,
                string interactionType, string investmentLevel, string parentIdentifier,
                IChateauDatabase database)
            {
                throw new InvalidOperationException("post-effect went sideways");
            }
        }

        /// <summary>
        /// Vanilla "involved" host so the wrapper composes a host message and then appends
        /// effect fragments to it. Casual host would silently skip the spread roll.
        /// </summary>
        private class HostProcessor : InteractionProcessorBase
        {
            public override string InteractionType => "kiss";
            public override string InvestmentLevel => "involved";

            public HostProcessor(IChateauDatabase database) : base(database) { }

            public override string ProcessInteraction(PendingCommand command) => InteractionType;

            public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
                => initiatorProfile.displayName + " test-interacts with " + recipientProfile.displayName + ".";
        }
    }
}
