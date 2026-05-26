using FChatDicebot.InteractionProcessors;
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
    /// Tests for BreakStatusContributor. Pure logic — no database fixture required since
    /// the contributor reads breaks straight off the profile object.
    /// </summary>
    public class BreakStatusContributorTests
    {
        private readonly BreakStatusContributor _contributor = new BreakStatusContributor();

        private static Profile MakeProfileWithBreaks(string name, params string[] parts)
        {
            var profile = new ProfileBuilder().WithUserName(name).WithDisplayName(name).Build();
            var breaks = parts.Select(p => new BreakInstance
            {
                Part = p,
                Severity = 5,
                BrokenBy = "TestSetter",
                BrokenAt = DateTime.UtcNow,
                LastTickedAt = DateTime.UtcNow.Date,
            }).ToList();
            BreakInstance.SaveAll(profile, breaks);
            return profile;
        }

        // -------------------------------------------------------------------
        // Empty / out-of-scope cases
        // -------------------------------------------------------------------

        [Fact]
        public void Contribute_NoBreaks_ReturnsEmpty()
        {
            var profile = new ProfileBuilder().WithUserName("Bob").Build();
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "kiss", parentIdentifier: "", isInitiator: false);
            Assert.Empty(result.Blockers);
            Assert.Empty(result.CompletionAppendix);
            Assert.Empty(result.ConsentWarnings);
        }

        [Fact]
        public void Contribute_NullProfile_ReturnsEmpty()
        {
            var result = _contributor.Contribute(null, StatusEffectCallSite.Completion, "kiss", parentIdentifier: "", isInitiator: false);
            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void Contribute_SelfReferentialBreakType_ReturnsEmpty()
        {
            var profile = MakeProfileWithBreaks("Bob", "mouth");
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "break", parentIdentifier: "", isInitiator: false);
            Assert.Empty(result.Blockers);
            Assert.Empty(result.CompletionAppendix);
        }

        [Theory]
        [InlineData("consume")]
        [InlineData("petrify")]
        [InlineData("plant")]
        [InlineData("corrupt")]
        [InlineData("purify")]
        [InlineData("bond")]
        [InlineData("objectify")]
        [InlineData("entitle")]
        [InlineData("employ")]
        [InlineData("monsterize")]
        [InlineData("rename")]
        [InlineData("dressup")]
        public void Contribute_UntouchedInteraction_EmitsNothing(string untouched)
        {
            var profile = MakeProfileWithBreaks("Bob", "mouth", "body");
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, untouched, parentIdentifier: "", isInitiator: false);
            Assert.Empty(result.Blockers);
            Assert.Empty(result.CompletionAppendix);
        }

        // -------------------------------------------------------------------
        // Section B — recipient-side blockers
        // -------------------------------------------------------------------

        [Theory]
        [InlineData("kiss", "mouth")]
        [InlineData("kiss", "tongue")]
        [InlineData("feed", "mouth")]
        [InlineData("feed", "tongue")]
        [InlineData("cuddle", "body")]
        [InlineData("cuddle", "torso")]
        [InlineData("cuddle", "arm")]
        [InlineData("handhold", "hand")]
        [InlineData("spank", "ass")]
        [InlineData("spank", "body")]
        [InlineData("spank", "torso")]
        [InlineData("climaxfor", "dick")]
        [InlineData("climaxfor", "ball")]
        [InlineData("climaxfor", "pussy")]
        [InlineData("climaxfor", "ass")]
        public void Contribute_RecipientBlockingPart_EmitsRecipientBlocker(string interactionType, string brokenPart)
        {
            var profile = MakeProfileWithBreaks("Bob", brokenPart);
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Consent, interactionType, parentIdentifier: "", isInitiator: false);
            Assert.Single(result.Blockers);
            Assert.True(result.Blockers[0].BlocksRecipient);
            Assert.False(result.Blockers[0].BlocksInitiator);
            Assert.Contains(brokenPart, result.Blockers[0].Reason);
        }

        [Fact]
        public void Contribute_RecipientUnrelatedPart_DoesNotBlock_EmitsPassThrough()
        {
            var profile = MakeProfileWithBreaks("Bob", "wing");
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "kiss", parentIdentifier: "", isInitiator: false);
            Assert.Empty(result.Blockers);
            // F — pass-through fires for unrelated breaks
            Assert.Single(result.CompletionAppendix);
            Assert.Contains("wing", result.CompletionAppendix[0]);
            Assert.Contains("still sore", result.CompletionAppendix[0]);
        }

        // -------------------------------------------------------------------
        // Section B — initiator-side blockers
        // -------------------------------------------------------------------

        [Theory]
        [InlineData("climax", "dick")]
        [InlineData("climax", "ball")]
        [InlineData("climax", "pussy")]
        [InlineData("climax", "ass")]
        [InlineData("bully", "body")]
        [InlineData("bully", "torso")]
        public void Contribute_InitiatorBlockingPart_EmitsInitiatorBlocker(string interactionType, string brokenPart)
        {
            var profile = MakeProfileWithBreaks("Alice", brokenPart);
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Consent, interactionType, parentIdentifier: "", isInitiator: true);
            Assert.Single(result.Blockers);
            Assert.True(result.Blockers[0].BlocksInitiator);
            Assert.False(result.Blockers[0].BlocksRecipient);
            Assert.Contains(brokenPart, result.Blockers[0].Reason);
        }

        [Fact]
        public void Contribute_InitiatorBullyTorso_BlocksOnlyOnInitiatorSide()
        {
            // Bully blocks the initiator side only. Recipient-side call should not block.
            var profile = MakeProfileWithBreaks("Bob", "torso");
            var asRecipient = _contributor.Contribute(profile, StatusEffectCallSite.Consent, "bully", parentIdentifier: "", isInitiator: false);
            Assert.Empty(asRecipient.Blockers);
        }

        [Fact]
        public void Contribute_ClimaxForRecipientSide_Blocks()
        {
            // climaxfor (recipient is climaxer): broken pussy on recipient blocks.
            var profile = MakeProfileWithBreaks("Bob", "pussy");
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Consent, "climaxfor", parentIdentifier: "", isInitiator: false);
            Assert.Single(result.Blockers);
            Assert.True(result.Blockers[0].BlocksRecipient);
        }

        [Fact]
        public void Contribute_ClimaxForInitiatorSide_DoesNotBlock()
        {
            // For climaxfor, the recipient is the climaxer — initiator's broken anatomy is
            // irrelevant.
            var profile = MakeProfileWithBreaks("Alice", "dick");
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Consent, "climaxfor", parentIdentifier: "", isInitiator: true);
            Assert.Empty(result.Blockers);
        }

        // -------------------------------------------------------------------
        // Section D — !breed flavor (never blocks)
        // -------------------------------------------------------------------

        [Fact]
        public void Contribute_Breed_NeverBlocks_EvenWithRelevantBreaks()
        {
            var profile = MakeProfileWithBreaks("Bob", "pussy", "ass");
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Consent, "breed", parentIdentifier: "", isInitiator: false);
            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void Contribute_Breed_EmitsCustomFlavorAtCompletion()
        {
            var profile = MakeProfileWithBreaks("Bob", "pussy");
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "breed", parentIdentifier: "", isInitiator: false);
            Assert.Single(result.CompletionAppendix);
            Assert.Contains("who knows how", result.CompletionAppendix[0]);
            Assert.Contains("pussy", result.CompletionAppendix[0]);
            Assert.Contains("it's", result.CompletionAppendix[0]);  // singular
        }

        [Fact]
        public void Contribute_Breed_MultipleRelevantParts_PluralAgreement()
        {
            var profile = MakeProfileWithBreaks("Bob", "pussy", "ass");
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "breed", parentIdentifier: "", isInitiator: false);
            Assert.Single(result.CompletionAppendix);
            Assert.Contains("they're", result.CompletionAppendix[0]);
        }

        [Fact]
        public void Contribute_Breed_IrrelevantBreaks_NoFlavor()
        {
            // wing/mouth are not breed-relevant — no flavor fires.
            var profile = MakeProfileWithBreaks("Bob", "wing", "mouth");
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "breed", parentIdentifier: "", isInitiator: false);
            Assert.Empty(result.CompletionAppendix);
        }

        // -------------------------------------------------------------------
        // Section F — pass-through "still sore from earlier"
        // -------------------------------------------------------------------

        [Fact]
        public void Contribute_NonBlocking_EmitsPassThrough_ListsAllActiveBreaks()
        {
            var profile = MakeProfileWithBreaks("Bob", "wing", "nose");
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "kiss", parentIdentifier: "", isInitiator: false);
            Assert.Empty(result.Blockers);
            Assert.Single(result.CompletionAppendix);
            Assert.Contains("wing", result.CompletionAppendix[0]);
            Assert.Contains("nose", result.CompletionAppendix[0]);
            Assert.Contains("still sore", result.CompletionAppendix[0]);
        }

        [Fact]
        public void Contribute_PassThrough_OnlyAtCompletion()
        {
            var profile = MakeProfileWithBreaks("Bob", "wing");
            var consent = _contributor.Contribute(profile, StatusEffectCallSite.Consent, "kiss", parentIdentifier: "", isInitiator: false);
            Assert.Empty(consent.CompletionAppendix);
            Assert.Empty(consent.ConsentWarnings);
        }

        [Fact]
        public void Contribute_PassThrough_OnlyOnRecipientSide()
        {
            // Initiator-side pass-through is suppressed — F lists the recipient's breaks
            // only.
            var profile = MakeProfileWithBreaks("Alice", "wing");
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "kiss", parentIdentifier: "", isInitiator: true);
            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Contribute_PassThrough_SuppressedWhenBlockerFires()
        {
            // Block-replacement is the sole fragment; pass-through doesn't double up.
            var profile = MakeProfileWithBreaks("Bob", "mouth", "wing");
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "kiss", parentIdentifier: "", isInitiator: false);
            Assert.Single(result.Blockers);
            Assert.Empty(result.CompletionAppendix);
        }

        // -------------------------------------------------------------------
        // Wording helpers — singular/plural and comma+and joining
        // -------------------------------------------------------------------

        [Fact]
        public void ComposeBlockReason_SingularPart_UsesIs()
        {
            var msg = BreakStatusContributor.ComposeBlockReason("Bob", new List<string> { "mouth" }, "kiss");
            Assert.Equal("Bob's mouth is too broken for kissing.", msg);
        }

        [Fact]
        public void ComposeBlockReason_TwoParts_JoinsWithAnd()
        {
            var msg = BreakStatusContributor.ComposeBlockReason("Bob", new List<string> { "body", "torso" }, "cuddle");
            Assert.Equal("Bob's body and torso are too broken for cuddling.", msg);
        }

        [Fact]
        public void ComposeBlockReason_ThreeParts_OxfordComma()
        {
            var msg = BreakStatusContributor.ComposeBlockReason("Bob", new List<string> { "body", "torso", "ass" }, "spank");
            Assert.Equal("Bob's body, torso, and ass are too broken for spanking.", msg);
        }

        [Fact]
        public void ComposeBlockReason_UnknownInteraction_FallsBackToLiteralName()
        {
            var msg = BreakStatusContributor.ComposeBlockReason("Bob", new List<string> { "wing" }, "skydive");
            Assert.Contains("skydive", msg);
        }

        [Fact]
        public void ComposeBreedFlavor_SingularPart_UsesItsAgreement()
        {
            var msg = BreakStatusContributor.ComposeBreedFlavor("Bob", new List<string> { "dick" });
            Assert.Equal("...who knows how, with Bob's dick in the state it's in.", msg);
        }

        [Fact]
        public void ComposeBreedFlavor_PluralParts_UsesTheyreAgreement()
        {
            var msg = BreakStatusContributor.ComposeBreedFlavor("Bob", new List<string> { "pussy", "ass" });
            Assert.Equal("...who knows how, with Bob's pussy and ass in the state they're in.", msg);
        }

        [Fact]
        public void ComposeSoreFromEarlier_SingularPart_UsesIs()
        {
            var msg = BreakStatusContributor.ComposeSoreFromEarlier("Bob", new List<string> { "wing" });
            Assert.Equal("...Bob's wing is still sore from earlier.", msg);
        }

        [Fact]
        public void ComposeSoreFromEarlier_PluralParts_UsesAreAndOxfordComma()
        {
            var msg = BreakStatusContributor.ComposeSoreFromEarlier("Bob", new List<string> { "wing", "nose", "tongue" });
            Assert.Equal("...Bob's wing, nose, and tongue are still sore from earlier.", msg);
        }
    }
}
