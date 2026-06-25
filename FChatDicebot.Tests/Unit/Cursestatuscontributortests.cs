using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.InteractionProcessors.StatusEffectContributors;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Tests for CurseStatusContributor — disabler validation blockers and modifier
    /// completion fragments. Curse application/cleanse logic is covered separately in
    /// CurseProcessorTests / ChateauCleanseTests.
    /// </summary>
    public class CurseStatusContributorTests
    {
        private readonly CurseStatusContributor _contributor = new CurseStatusContributor();

        [Fact]
        public void Contribute_NoCurses_NoFragmentsNoBlockers()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").Build();

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Consent, "kiss", "", false);

            Assert.Empty(result.CompletionAppendix);
            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void Contribute_NullProfile_ReturnsEmpty()
        {
            var result = _contributor.Contribute(null, StatusEffectCallSite.Consent, "kiss", "", false);

            Assert.Empty(result.CompletionAppendix);
            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void Contribute_DisablerOnMatchingInteraction_EmitsBlocker()
        {
            // Cooties blocks both sides of !kiss. Recipient case.
            var bob = BuildBobWith("cooties");

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Consent, "kiss", "", isInitiator: false);

            Assert.Single(result.Blockers);
            Assert.True(result.Blockers[0].BlocksRecipient);
            Assert.False(result.Blockers[0].BlocksInitiator);
            Assert.Contains("cooties", result.Blockers[0].Reason);
            Assert.Equal("curse:cooties", result.Blockers[0].Source);
        }

        [Fact]
        public void Contribute_DisablerOnNonMatchingInteraction_NoBlocker()
        {
            // Cooties does NOT block !feed.
            var bob = BuildBobWith("cooties");

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Consent, "feed", "", isInitiator: false);

            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void Contribute_InitiatorOnlyDisabler_RecipientSideEmitsNothing()
        {
            // Greed blocks the paymentGive initiator only.
            var bob = BuildBobWith("greed");

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Consent, "paymentGive", "", isInitiator: false);

            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void Contribute_InitiatorOnlyDisabler_InitiatorSideBlocks()
        {
            var alice = BuildAliceWith("greed");

            var result = _contributor.Contribute(alice, StatusEffectCallSite.Consent, "paymentGive", "", isInitiator: true);

            Assert.Single(result.Blockers);
            Assert.True(result.Blockers[0].BlocksInitiator);
            Assert.False(result.Blockers[0].BlocksRecipient);
        }

        [Fact]
        public void Contribute_RecipientOnlyDisabler_InitiatorSideEmitsNothing()
        {
            // Hunger blocks the recipient of !feed; cursed initiator side is fine.
            var alice = BuildAliceWith("hunger");

            var result = _contributor.Contribute(alice, StatusEffectCallSite.Consent, "feed", "", isInitiator: true);

            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void Contribute_ChastityBlocksClimaxerSide_PerVerb()
        {
            // chastity blocks the climaxer. Per ResolveClimaxer the climaxer is the recipient
            // of !climax and the initiator of !climaxfor, so each verb blocks the opposite side.
            var alice = BuildAliceWith("chastity");
            var bob = BuildBobWith("chastity");

            var climaxResult = _contributor.Contribute(bob, StatusEffectCallSite.Consent, "climax", "", isInitiator: false);
            var climaxForResult = _contributor.Contribute(alice, StatusEffectCallSite.Consent, "climaxfor", "", isInitiator: true);

            Assert.Single(climaxResult.Blockers);
            Assert.True(climaxResult.Blockers[0].BlocksRecipient);
            Assert.Single(climaxForResult.Blockers);
            Assert.True(climaxForResult.Blockers[0].BlocksInitiator);
        }

        [Fact]
        public void Contribute_ChastityLeavesPartnerSideFree_PerVerb()
        {
            // The partner (non-climaxer) of a climax can still help while cursed: the initiator
            // of !climax and the recipient of !climaxfor are the partner, not the climaxer.
            var alice = BuildAliceWith("chastity");
            var bob = BuildBobWith("chastity");

            var climaxPartner = _contributor.Contribute(alice, StatusEffectCallSite.Consent, "climax", "", isInitiator: true);
            var climaxForPartner = _contributor.Contribute(bob, StatusEffectCallSite.Consent, "climaxfor", "", isInitiator: false);

            Assert.Empty(climaxPartner.Blockers);
            Assert.Empty(climaxForPartner.Blockers);
        }

        [Fact]
        public void Contribute_ModifierCurse_EmitsCompletionFragment()
        {
            var bob = BuildBobWith("mooing");

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", "", false);

            Assert.Single(result.CompletionAppendix);
            // Template is "Moooo says {subject}." with Bob substituted.
            Assert.Contains("Bob", result.CompletionAppendix[0]);
            Assert.Contains("Moooo", result.CompletionAppendix[0]);
        }

        [Fact]
        public void Contribute_ModifierCurseAtConsent_NoFragmentNoBlocker()
        {
            // Modifiers are completion-only — no consent fragments, no blockers.
            var bob = BuildBobWith("mooing");

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Consent, "kiss", "", false);

            Assert.Empty(result.CompletionAppendix);
            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void Contribute_MultipleModifiers_AllFragmentsComposed()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").Build();
            CurseProcessor.ApplyCurse(bob, "mooing", "Alice");
            CurseProcessor.ApplyCurse(bob, "blushing", "Alice");

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", "", false);

            Assert.Equal(2, result.CompletionAppendix.Count);
            Assert.Contains(result.CompletionAppendix, f => f.Contains("Moooo"));
            Assert.Contains(result.CompletionAppendix, f => f.Contains("blushes brighter"));
        }

        [Fact]
        public void Contribute_CurseInteraction_SelfReferenceSkipped()
        {
            // A !curse parent shouldn't surface modifier fragments — those would be noise
            // on top of the curse-itself completion message.
            var bob = BuildBobWith("mooing");

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Completion,
                CurseProcessor.CurseType, "", false);

            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Contribute_CleanseInteraction_SelfReferenceSkipped()
        {
            var bob = BuildBobWith("mooing");

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "cleanse", "", false);

            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Contribute_UnknownCurseInProfile_Skipped()
        {
            // A curse on the profile that isn't in CatalogMap (e.g. ghost entry from before
            // a removal) should not crash and should emit nothing.
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").Build();
            CurseProcessor.ApplyCurse(bob, "made-up-curse", "Alice");

            var result = _contributor.Contribute(bob, StatusEffectCallSite.Consent, "kiss", "", false);

            Assert.Empty(result.Blockers);
            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Contribute_DisablerAndModifierStack_BothSurface()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").Build();
            CurseProcessor.ApplyCurse(bob, "cooties", "Alice");
            CurseProcessor.ApplyCurse(bob, "blushing", "Alice");

            // At consent on !kiss: cooties blocks, blushing is silent (modifier at consent).
            var consentResult = _contributor.Contribute(bob, StatusEffectCallSite.Consent, "kiss", "", false);
            Assert.Single(consentResult.Blockers);
            Assert.Empty(consentResult.CompletionAppendix);

            // At completion on a non-blocked interaction: blushing surfaces, cooties is
            // silent (no fragment, only blocker semantics).
            var completionResult = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "feed", "", false);
            Assert.Single(completionResult.CompletionAppendix);
            Assert.Empty(completionResult.Blockers);
        }

        [Fact]
        public void SymmetricInvocation_IsFalse()
        {
            // Curses attach to a specific party; the contributor's invocation should follow
            // subject-only routing (recipient at completion by default).
            Assert.False(_contributor.SymmetricInvocation);
        }

        private static Profile BuildBobWith(string curse)
        {
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").Build();
            CurseProcessor.ApplyCurse(bob, curse, "Alice");
            return bob;
        }

        private static Profile BuildAliceWith(string curse)
        {
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").Build();
            CurseProcessor.ApplyCurse(alice, curse, "Carol");
            return alice;
        }
    }
}
