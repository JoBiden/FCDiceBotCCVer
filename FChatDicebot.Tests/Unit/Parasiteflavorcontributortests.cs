using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.InteractionProcessors.StatusEffectContributors;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Tests for ParasiteFlavorContributor — the per-parasite completion flavor that fires
    /// at <see cref="ParasiteFlavorContributor.FlavorChance"/> per parasite per interaction,
    /// on every investment level except !infest itself.
    /// </summary>
    public class ParasiteFlavorContributorTests
    {
        [Fact]
        public void Contribute_NoParasites_NoFragments()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").Build();
            var contributor = new ParasiteFlavorContributor(new AlwaysFireRandom());

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", "", false);

            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Contribute_ConsentPhase_SilentEvenWithParasites()
        {
            // Consent fragments could spoil the prompt — dose's precedent is completion-only.
            var bob = BuildBobWith("paraslime");
            var contributor = new ParasiteFlavorContributor(new AlwaysFireRandom());

            var result = contributor.Contribute(bob, StatusEffectCallSite.Consent, "kiss", "", false);

            Assert.Empty(result.ConsentWarnings);
            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Contribute_InfestInteraction_Skipped()
        {
            // The !infest completion already speaks about the parasite — flavor would be
            // redundant text right on top of it.
            var bob = BuildBobWith("paraslime");
            var contributor = new ParasiteFlavorContributor(new AlwaysFireRandom());

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion,
                InfestProcessor.InfestType, "paraslime", false);

            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Contribute_CasualInteraction_StillFires()
        {
            // Per spec: flavor surfaces on every investment level, including casual.
            var bob = BuildBobWith("paraslime");
            var contributor = new ParasiteFlavorContributor(new AlwaysFireRandom());

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", "", false);

            Assert.Single(result.CompletionAppendix);
            Assert.Contains("Bob", result.CompletionAppendix[0]);
            Assert.Contains("paraslime", result.CompletionAppendix[0]);
        }

        [Fact]
        public void Contribute_RollMisses_NoFragment()
        {
            var bob = BuildBobWith("paraslime");
            var contributor = new ParasiteFlavorContributor(new NeverFireRandom());

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", "", false);

            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Contribute_UnknownParasite_NoFragment()
        {
            // Parasites not in the flavor map (e.g. custom parasites added via the deferred
            // !defineparasite flow) silently contribute nothing.
            var bob = BuildBobWith("mystery-parasite");
            var contributor = new ParasiteFlavorContributor(new AlwaysFireRandom());

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", "", false);

            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Contribute_MultipleParasites_AllRollIndependently()
        {
            var bob = BuildBobWith("paraslime");
            InfestProcessor.ApplyInfestation(bob, "tentacles", "Carol",
                spreadFromContact: false, gracePeriod: TimeSpan.Zero);
            InfestProcessor.ApplyInfestation(bob, "love", "Carol",
                spreadFromContact: false, gracePeriod: TimeSpan.Zero);

            var contributor = new ParasiteFlavorContributor(new AlwaysFireRandom());
            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", "", false);

            // All three should land a fragment when the roll always succeeds.
            Assert.Equal(3, result.CompletionAppendix.Count);
        }

        [Fact]
        public void Contribute_SubjectSubstituted_UsesDisplayNameWhenPresent()
        {
            var bob = BuildBobWith("paraslime", displayName: "Bobby");
            var contributor = new ParasiteFlavorContributor(new AlwaysFireRandom());

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", "", false);

            Assert.Contains("Bobby", result.CompletionAppendix[0]);
        }

        [Fact]
        public void Contribute_SubjectSubstituted_FallsBackToUserName()
        {
            var bob = BuildBobWith("paraslime", displayName: "");
            var contributor = new ParasiteFlavorContributor(new AlwaysFireRandom());

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", "", false);

            Assert.Contains("Bob", result.CompletionAppendix[0]);
        }

        [Fact]
        public void Contribute_NullProfile_EmptyFragments()
        {
            var contributor = new ParasiteFlavorContributor(new AlwaysFireRandom());
            var result = contributor.Contribute(null, StatusEffectCallSite.Completion, "kiss", "", false);
            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void FlavorChance_OverManyTrials_StaysNearTarget()
        {
            // 25% baseline; expect 150–350 hits over 1000 trials with a real Random.
            int hits = 0;
            var contributor = new ParasiteFlavorContributor(new Random(42));
            for (int i = 0; i < 1000; i++)
            {
                var bob = BuildBobWith("paraslime");
                var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", "", false);
                if (result.CompletionAppendix.Count > 0) hits++;
            }
            Assert.InRange(hits, 150, 350);
        }

        [Fact]
        public void SymmetricInvocation_IsTrue()
        {
            // Both initiator and recipient can be carriers — symmetric invocation ensures
            // both sides have their flavors surfaced at completion.
            Assert.True(new ParasiteFlavorContributor().SymmetricInvocation);
        }

        [Theory]
        [InlineData("paraslime", "[color=pink]paraslime[/color]")]
        [InlineData("bimboslime", "[color=pink]slime[/color]")]
        [InlineData("lustleeches", "[color=purple]lust leeches[/color]")]
        [InlineData("love", "[color=pink]love")]
        [InlineData("tentacles", "parasitic tentacles")]
        [InlineData("nymites", "familial warmth")]
        public void FlavorMap_AllCanonicalParasites_HaveDistinctiveText(string parasite, string distinctive)
        {
            // Spot-check that each canonical parasite has the right flavor wiring.
            Assert.True(ParasiteFlavorContributor.FlavorMap.ContainsKey(parasite));
            Assert.Contains(distinctive, ParasiteFlavorContributor.FlavorMap[parasite]);
        }

        private static Profile BuildBobWith(string parasite, string displayName = "Bob")
        {
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName(displayName).Build();
            InfestProcessor.ApplyInfestation(bob, parasite, "Alice",
                spreadFromContact: false, gracePeriod: TimeSpan.Zero);
            return bob;
        }

        /// <summary>Random where every roll succeeds.</summary>
        private class AlwaysFireRandom : Random
        {
            public override double NextDouble() => 0.0;
        }

        /// <summary>Random where every roll fails.</summary>
        private class NeverFireRandom : Random
        {
            public override double NextDouble() => 0.999999;
        }
    }
}
