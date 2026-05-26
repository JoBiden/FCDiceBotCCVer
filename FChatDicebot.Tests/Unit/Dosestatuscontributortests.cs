using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.InteractionProcessors.StatusEffectContributors;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Tests for DoseStatusContributor. Pure logic — no database fixture, the contributor
    /// reads off the in-memory Profile object. Uses a seeded Random so the craving roll
    /// is deterministic per test.
    /// </summary>
    public class DoseStatusContributorTests
    {
        // ----- IsSatisfiedBy (static helper) -----

        [Theory]
        [InlineData("feed", "musk", "musk", true)]
        [InlineData("FEED", "MUSK", "musk", true)] // case-insensitive
        [InlineData("odorize", "musk", "musk", true)]
        [InlineData("dose", "musk", "musk", true)]
        [InlineData("kiss", "musk", "musk", false)]    // unrelated interaction
        [InlineData("climaxfor", "musk", "musk", false)] // climax doses, doesn't satisfy
        [InlineData("climax", "musk", "musk", false)]
        [InlineData("feed", "drug", "musk", false)]    // identifier mismatch
        [InlineData("feed", "", "musk", false)]        // empty identifier
        [InlineData("feed", "musk", "", false)]        // empty vice
        public void IsSatisfiedBy_MatchesSpec(string interactionType, string identifier, string vice, bool expected)
        {
            Assert.Equal(expected, DoseStatusContributor.IsSatisfiedBy(interactionType, identifier, vice));
        }

        // ----- SymmetricInvocation -----

        [Fact]
        public void SymmetricInvocation_IsTrue()
        {
            var contributor = new DoseStatusContributor();
            Assert.True(contributor.SymmetricInvocation);
        }

        // ----- Empty / null cases -----

        [Fact]
        public void Contribute_NullProfile_ReturnsEmpty()
        {
            var contributor = new DoseStatusContributor(new Random(42));
            var result = contributor.Contribute(null, StatusEffectCallSite.Completion, "kiss", parentIdentifier: "", isInitiator: false);
            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Contribute_NoVices_ReturnsEmpty()
        {
            var contributor = new DoseStatusContributor(new Random(42));
            var profile = new ProfileBuilder().WithUserName("Bob").Build();
            var result = contributor.Contribute(profile, StatusEffectCallSite.Completion, "kiss", parentIdentifier: "", isInitiator: false);
            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Contribute_ConsentCallSite_StaysSilent()
        {
            // The spec puts craving / satisfaction text in the completion message, not the
            // consent prompt — surfacing it earlier would spoil the consent UX.
            var contributor = new DoseStatusContributor(new Random(42));
            var bob = new ProfileBuilder().WithDisplayName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "musk", "Alice");
            var entries = ViceInstance.LoadAll(bob);
            entries[0].AddictionLevel = 10;
            ViceInstance.SaveAll(bob, entries);

            var result = contributor.Contribute(bob, StatusEffectCallSite.Consent, "kiss", parentIdentifier: "", isInitiator: false);

            Assert.Empty(result.CompletionAppendix);
            Assert.Empty(result.ConsentWarnings);
        }

        // ----- Satisfaction (feed / odorize / dose with matching identifier) -----

        [Fact]
        public void Contribute_FeedOfMatchingSubstance_EmitsSatisfactionNotCraving()
        {
            var contributor = new DoseStatusContributor(new Random(42));
            var bob = new ProfileBuilder().WithDisplayName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "musk", "Alice");

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "feed", parentIdentifier: "musk", isInitiator: false);

            Assert.Single(result.CompletionAppendix);
            Assert.Contains("musk", result.CompletionAppendix[0]);
            Assert.Contains("been craving", result.CompletionAppendix[0]); // satisfaction marker (unique to satisfaction template)
        }

        [Fact]
        public void Contribute_OdorizeOfMatchingScent_EmitsSatisfaction()
        {
            var contributor = new DoseStatusContributor(new Random(42));
            var bob = new ProfileBuilder().WithDisplayName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "liquor", "Alice");

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "odorize", parentIdentifier: "liquor", isInitiator: false);

            Assert.Single(result.CompletionAppendix);
            Assert.Contains("liquor", result.CompletionAppendix[0]);
            Assert.Contains("been craving", result.CompletionAppendix[0]);
        }

        [Fact]
        public void Contribute_DoseOfSameVice_EmitsSatisfaction()
        {
            var contributor = new DoseStatusContributor(new Random(42));
            var bob = new ProfileBuilder().WithDisplayName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "drug", "Alice");

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, DoseProcessor.DoseType, parentIdentifier: "drug", isInitiator: false);

            Assert.Single(result.CompletionAppendix);
            Assert.Contains("drug", result.CompletionAppendix[0]);
            Assert.Contains("been craving", result.CompletionAppendix[0]);
        }

        [Fact]
        public void Contribute_FeedOfDifferentSubstance_DoesNotSatisfy()
        {
            // Feed of a substance that ISN'T the dosed vice doesn't quiet the craving —
            // it may still trigger a craving roll. Use a max-level vice to force the roll
            // to fire deterministically.
            var contributor = new DoseStatusContributor(new Random(42));
            var bob = new ProfileBuilder().WithDisplayName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "musk", "Alice");
            var entries = ViceInstance.LoadAll(bob);
            entries[0].AddictionLevel = 10; // 100% craving chance
            ViceInstance.SaveAll(bob, entries);

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "feed", parentIdentifier: "drug", isInitiator: false);

            Assert.Single(result.CompletionAppendix);
            Assert.DoesNotContain("been craving", result.CompletionAppendix[0]);
            // Verifies the *craving* fragment template was picked, not the satisfaction one.
            Assert.Contains("musk", result.CompletionAppendix[0]);
        }

        // ----- Craving probability scales with AddictionLevel -----

        [Fact]
        public void Contribute_Level10_AlwaysCraves()
        {
            // 100% chance at cap — every trial should produce a fragment.
            var contributor = new DoseStatusContributor(new Random(1));
            var bob = new ProfileBuilder().WithDisplayName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "musk", "Alice");
            var entries = ViceInstance.LoadAll(bob);
            entries[0].AddictionLevel = 10;
            ViceInstance.SaveAll(bob, entries);

            for (int i = 0; i < 20; i++)
            {
                var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", parentIdentifier: "", isInitiator: false);
                Assert.Single(result.CompletionAppendix);
            }
        }

        [Fact]
        public void Contribute_Level1_FiresAboutTenPercent_OverManyTrials()
        {
            // Statistical sanity check: at AddictionLevel 1 (10% chance), 1000 trials should
            // land in a wide band around 100. The test is loose on purpose so RNG variance
            // doesn't flake.
            var contributor = new DoseStatusContributor(new Random(7));
            var bob = new ProfileBuilder().WithDisplayName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "musk", "Alice");

            int hits = 0;
            for (int i = 0; i < 1000; i++)
            {
                var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", parentIdentifier: "", isInitiator: false);
                if (result.CompletionAppendix.Count > 0) hits++;
            }

            // Expect ~100. Generous bounds; tighter would flake.
            Assert.InRange(hits, 50, 180);
        }

        // ----- Multiple vices: dosed-vice satisfaction + other-vice craving co-exist -----

        [Fact]
        public void Contribute_WithinDose_DosedViceSatisfiesAndOtherVicesStillRoll()
        {
            // The user's spec: during a !dose with vice X, the X craving gets satisfied
            // by the dose itself, and OTHER active vices on the same profile still roll
            // cravings as normal.
            var contributor = new DoseStatusContributor(new Random(3));
            var bob = new ProfileBuilder().WithDisplayName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "musk", "Alice");
            DoseProcessor.ApplyDose(bob, "drug", "Alice");
            // Pin both to max so the craving fires deterministically for the non-matched one.
            var entries = ViceInstance.LoadAll(bob);
            foreach (var e in entries) e.AddictionLevel = 10;
            ViceInstance.SaveAll(bob, entries);

            // Simulate a !dose of musk (parentIdentifier == musk).
            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, DoseProcessor.DoseType, parentIdentifier: "musk", isInitiator: false);

            Assert.Equal(2, result.CompletionAppendix.Count);
            string muskFragment = result.CompletionAppendix.First(f => f.Contains("musk"));
            string drugFragment = result.CompletionAppendix.First(f => f.Contains("drug"));
            Assert.Contains("been craving", muskFragment); // musk satisfied
            Assert.DoesNotContain("been craving", drugFragment); // drug still craving
        }

        // ----- Climax: never satisfies (it doses instead) -----

        [Fact]
        public void Contribute_ClimaxNeverSatisfies()
        {
            // Climaxing applies an auto-dose of cum/pre/seminal to the partner; the
            // contributor doesn't recognize climax as a satisfaction trigger. With a
            // max-level "cum" vice and a climax interaction, we expect a craving fragment
            // (not a satisfaction fragment) — the climaxer's body still wants its fix.
            var contributor = new DoseStatusContributor(new Random(5));
            var bob = new ProfileBuilder().WithDisplayName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "cum", "Alice");
            var entries = ViceInstance.LoadAll(bob);
            entries[0].AddictionLevel = 10;
            ViceInstance.SaveAll(bob, entries);

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "climaxfor", parentIdentifier: "climaxfor|1", isInitiator: false);

            Assert.Single(result.CompletionAppendix);
            Assert.DoesNotContain("been craving", result.CompletionAppendix[0]);
        }

        // ----- AddictionLevel doesn't change on craving roll -----

        [Fact]
        public void Contribute_CravingRollDoesNotMutateAddictionLevel()
        {
            // Per spec: only !dose escalates, only !detox removes. Surfacing a craving
            // fragment must not bump the level.
            var contributor = new DoseStatusContributor(new Random(1));
            var bob = new ProfileBuilder().WithDisplayName("Bob").Build();
            DoseProcessor.ApplyDose(bob, "musk", "Alice");
            var entries = ViceInstance.LoadAll(bob);
            entries[0].AddictionLevel = 5;
            ViceInstance.SaveAll(bob, entries);

            for (int i = 0; i < 20; i++)
            {
                contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", parentIdentifier: "", isInitiator: false);
            }

            Assert.Equal(5, ViceInstance.LoadAll(bob)[0].AddictionLevel);
        }

        // ----- Malformed JSON in vices list doesn't break contributor -----

        [Fact]
        public void Contribute_MalformedViceEntry_Skipped()
        {
            var contributor = new DoseStatusContributor(new Random(42));
            var bob = new ProfileBuilder().WithUserName("Bob").Build();
            // Inject a malformed blob alongside a real vice; ViceInstance.LoadAll silently
            // drops the bad entry, the contributor proceeds with the good one.
            bob.lists[ViceInstance.VicesListKey] = new List<string>
            {
                "not-json",
                "{\"Vice\":\"musk\",\"AddictionLevel\":10}",
            };

            var result = contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", parentIdentifier: "", isInitiator: false);
            // At level 10 the craving roll is 100%, so we get exactly one fragment from
            // the surviving entry.
            Assert.Single(result.CompletionAppendix);
            Assert.Contains("musk", result.CompletionAppendix[0]);
        }
    }
}
