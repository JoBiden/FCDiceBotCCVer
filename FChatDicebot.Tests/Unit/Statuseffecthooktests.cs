using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Unit tests for the status-effect hook infrastructure: contributors registered in
    /// <see cref="StatusEffectRegistry"/>, aggregated by
    /// <c>InteractionProcessorBase.GetActiveStatusEffects</c>, surfaced through default
    /// <see cref="InteractionProcessorBase.GetConsentWarning"/> and
    /// <see cref="InteractionProcessorBase.ValidateInteraction"/>.
    /// </summary>
    [Collection("Database")]
    public class StatusEffectHookTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly TestProcessor _processor;

        public StatusEffectHookTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;

            // Tests inject their own contributors; start every test with an empty registry.
            StatusEffectRegistry.Clear();

            _processor = new TestProcessor(_database);
        }

        public void Dispose()
        {
            // Don't leak per-test contributors into other test classes that may run after.
            StatusEffectRegistry.Clear();
        }

        // -------------------------------------------------------------------
        // Aggregation behavior
        // -------------------------------------------------------------------

        [Fact]
        public void NoContributors_ReturnsEmptyFragments()
        {
            var profile = new ProfileBuilder().Build();

            var result = _processor.CallGetActiveStatusEffects(profile, StatusEffectCallSite.Consent);

            Assert.Empty(result.ConsentWarnings);
            Assert.Empty(result.CompletionAppendix);
            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void NullProfile_ReturnsEmptyFragments()
        {
            StatusEffectRegistry.RegisterContributor(
                new FakeContributor(consentWarning: "[scent fragment]"));

            var result = _processor.CallGetActiveStatusEffects(null, StatusEffectCallSite.Consent);

            Assert.Empty(result.ConsentWarnings);
            Assert.Empty(result.CompletionAppendix);
            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void Consent_CallSite_ReturnsConsentWarningOnly()
        {
            StatusEffectRegistry.RegisterContributor(new FakeContributor(
                consentWarning: "(consent text)",
                completionAppendix: "(completion text)"));
            var profile = new ProfileBuilder().Build();

            var result = _processor.CallGetActiveStatusEffects(profile, StatusEffectCallSite.Consent);

            // Fragments are stored verbatim (no leading whitespace); the base spacing convention
            // is applied at composition time in AppendStatusFragments, not in the helper.
            Assert.Single(result.ConsentWarnings);
            Assert.Equal("(consent text)", result.ConsentWarnings[0]);
            // FakeContributor returns both lists regardless of call site, so completion text
            // also flows through; the discrimination is the contributor's responsibility.
            Assert.Single(result.CompletionAppendix);
        }

        [Fact]
        public void Completion_CallSite_ContributorSeesCorrectSite()
        {
            var fake = new FakeContributor(
                consentWarning: "(consent text)",
                completionAppendix: "(completion text)");
            StatusEffectRegistry.RegisterContributor(fake);
            var profile = new ProfileBuilder().Build();

            _processor.CallGetActiveStatusEffects(profile, StatusEffectCallSite.Completion);

            Assert.Equal(StatusEffectCallSite.Completion, fake.LastCallSite);
        }

        [Fact]
        public void Contributor_ReceivesInteractionTypeAndInitiatorFlag()
        {
            var fake = new FakeContributor();
            StatusEffectRegistry.RegisterContributor(fake);
            var profile = new ProfileBuilder().Build();

            _processor.CallGetActiveStatusEffects(profile, StatusEffectCallSite.Consent, isInitiator: true);

            Assert.Equal("testinteraction", fake.LastInteractionType);
            Assert.True(fake.LastIsInitiator);
        }

        [Fact]
        public void MultipleContributors_MergeInRegistrationOrder()
        {
            StatusEffectRegistry.RegisterContributor(new FakeContributor(consentWarning: "AAA"));
            StatusEffectRegistry.RegisterContributor(new FakeContributor(consentWarning: "BBB"));
            StatusEffectRegistry.RegisterContributor(new FakeContributor(consentWarning: "CCC"));
            var profile = new ProfileBuilder().Build();

            var result = _processor.CallGetActiveStatusEffects(profile, StatusEffectCallSite.Consent);

            Assert.Equal(new[] { "AAA", "BBB", "CCC" }, result.ConsentWarnings);
        }

        [Fact]
        public void ContributorThatThrows_DoesNotBreakHelper()
        {
            StatusEffectRegistry.RegisterContributor(new ThrowingContributor());
            StatusEffectRegistry.RegisterContributor(new FakeContributor(consentWarning: "after-throw"));
            var profile = new ProfileBuilder().Build();

            var result = _processor.CallGetActiveStatusEffects(profile, StatusEffectCallSite.Consent);

            Assert.Single(result.ConsentWarnings);
            Assert.Equal("after-throw", result.ConsentWarnings[0]);
        }

        // -------------------------------------------------------------------
        // Odorize-style fade: contributor mutates its own profile state
        // -------------------------------------------------------------------

        [Fact]
        public void OdorizeStyleContributor_DecrementsCounterOnEachCall()
        {
            var profile = new ProfileBuilder().Build();
            profile.characteristics["odorize_musk_uses"] = "3";
            StatusEffectRegistry.RegisterContributor(new FakeOdorizeContributor("odorize_musk_uses", "(musk lingers)"));

            var first = _processor.CallGetActiveStatusEffects(profile, StatusEffectCallSite.Completion);
            Assert.Single(first.CompletionAppendix);
            Assert.Equal("2", profile.characteristics["odorize_musk_uses"]);

            var second = _processor.CallGetActiveStatusEffects(profile, StatusEffectCallSite.Completion);
            Assert.Single(second.CompletionAppendix);
            Assert.Equal("1", profile.characteristics["odorize_musk_uses"]);

            var third = _processor.CallGetActiveStatusEffects(profile, StatusEffectCallSite.Completion);
            Assert.Single(third.CompletionAppendix);
            Assert.Equal("0", profile.characteristics["odorize_musk_uses"]);

            // Exhausted: no fragment, counter does not go negative.
            var fourth = _processor.CallGetActiveStatusEffects(profile, StatusEffectCallSite.Completion);
            Assert.Empty(fourth.CompletionAppendix);
            Assert.Equal("0", profile.characteristics["odorize_musk_uses"]);
        }

        // -------------------------------------------------------------------
        // Break-style blockers
        // -------------------------------------------------------------------

        [Fact]
        public void BreakStyleBlocker_AppearsInBlockersList()
        {
            StatusEffectRegistry.RegisterContributor(new FakeBreakContributor(
                bodyPart: "mouth",
                blockedInteractionType: "testinteraction",
                reason: "Bob's mouth is broken and cannot be kissed."));
            var profile = new ProfileBuilder().WithDisplayName("Bob").Build();

            var result = _processor.CallGetActiveStatusEffects(profile, StatusEffectCallSite.Consent);

            Assert.Single(result.Blockers);
            var blocker = result.Blockers[0];
            Assert.True(blocker.BlocksRecipient);
            Assert.False(blocker.BlocksInitiator);
            Assert.Equal("break:mouth", blocker.Source);
            Assert.Contains("Bob's mouth is broken", blocker.Reason);
        }

        [Fact]
        public void BreakBlocker_DoesNotApplyToUnrelatedInteractionType()
        {
            StatusEffectRegistry.RegisterContributor(new FakeBreakContributor(
                bodyPart: "mouth",
                blockedInteractionType: "feed",   // Different interaction
                reason: "irrelevant"));
            var profile = new ProfileBuilder().Build();

            var result = _processor.CallGetActiveStatusEffects(profile, StatusEffectCallSite.Consent);

            Assert.Empty(result.Blockers);
        }

        // -------------------------------------------------------------------
        // Default GetConsentWarning / ValidateInteraction integration
        // -------------------------------------------------------------------

        [Fact]
        public void DefaultGetConsentWarning_AppendsConsentFragments_WithSingleSpace()
        {
            StatusEffectRegistry.RegisterContributor(new FakeContributor(consentWarning: "[musk lingers]"));

            var initiator = new ProfileBuilder().WithDisplayName("Alice").Build();
            var recipient = new ProfileBuilder().WithDisplayName("Bob").Build();

            string warning = _processor.GetConsentWarning(initiator, recipient, identifier: "");

            Assert.Contains("Alice", warning);
            Assert.Contains("Bob", warning);
            Assert.EndsWith("Do you !consent? [musk lingers]", warning);
            Assert.DoesNotContain("?  [musk", warning);
        }

        [Fact]
        public void DefaultGetConsentWarning_MultipleFragments_EachGetsOneLeadingSpace()
        {
            StatusEffectRegistry.RegisterContributor(new FakeContributor(consentWarning: "[scent A]"));
            StatusEffectRegistry.RegisterContributor(new FakeContributor(consentWarning: "[scent B]"));

            var initiator = new ProfileBuilder().WithDisplayName("Alice").Build();
            var recipient = new ProfileBuilder().WithDisplayName("Bob").Build();

            string warning = _processor.GetConsentWarning(initiator, recipient, identifier: "");

            Assert.EndsWith("Do you !consent? [scent A] [scent B]", warning);
        }

        [Fact]
        public void DefaultGetConsentWarning_EmptyFragmentSkipped()
        {
            StatusEffectRegistry.RegisterContributor(new FakeContributor(consentWarning: ""));
            StatusEffectRegistry.RegisterContributor(new FakeContributor(consentWarning: "[real]"));

            var initiator = new ProfileBuilder().WithDisplayName("Alice").Build();
            var recipient = new ProfileBuilder().WithDisplayName("Bob").Build();

            string warning = _processor.GetConsentWarning(initiator, recipient, identifier: "");

            Assert.EndsWith("Do you !consent? [real]", warning);
        }

        [Fact]
        public void DefaultValidateInteraction_RecipientBlocker_FailsValidation()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            StatusEffectRegistry.RegisterContributor(new FakeBreakContributor(
                bodyPart: "mouth",
                blockedInteractionType: "testinteraction",
                reason: "Bob's mouth is broken."));

            var result = _processor.ValidateInteraction("Alice", "Bob", "");

            Assert.False(result.IsValid);
            Assert.Equal("Bob's mouth is broken.", result.ErrorMessage);
        }

        [Fact]
        public void DefaultValidateInteraction_NoBlockers_Succeeds()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);
            StatusEffectRegistry.RegisterContributor(
                new FakeContributor(consentWarning: " (just text)"));

            var result = _processor.ValidateInteraction("Alice", "Bob", "");

            Assert.True(result.IsValid);
        }

        // ===================================================================
        // Test doubles
        // ===================================================================

        /// <summary>
        /// Concrete processor that does nothing real but exposes the protected helper.
        /// </summary>
        private class TestProcessor : InteractionProcessorBase
        {
            public override string InteractionType => "testinteraction";
            public override string InvestmentLevel => "casual";

            public TestProcessor(IChateauDatabase database) : base(database) { }

            public override string ProcessInteraction(PendingCommand command) => InteractionType;

            public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
                => $"{initiatorProfile.displayName} test-interacts with {recipientProfile.displayName}.";

            public StatusEffectFragments CallGetActiveStatusEffects(
                Profile profile, StatusEffectCallSite callSite, bool isInitiator = false)
                => GetActiveStatusEffects(profile, callSite, isInitiator);
        }

        /// <summary>
        /// Inert contributor that echoes whatever fragments it was constructed with and records
        /// the arguments of its most recent invocation. Always returns both lists; the helper
        /// is expected to forward them verbatim.
        /// </summary>
        private class FakeContributor : IStatusEffectContributor
        {
            private readonly string _consentWarning;
            private readonly string _completionAppendix;

            public StatusEffectCallSite LastCallSite;
            public string LastInteractionType;
            public bool LastIsInitiator;

            public FakeContributor(string consentWarning = null, string completionAppendix = null)
            {
                _consentWarning = consentWarning;
                _completionAppendix = completionAppendix;
            }

            public StatusEffectFragments Contribute(
                Profile profile, StatusEffectCallSite callSite, string interactionType, bool isInitiator)
            {
                LastCallSite = callSite;
                LastInteractionType = interactionType;
                LastIsInitiator = isInitiator;

                var f = new StatusEffectFragments();
                if (_consentWarning != null) f.ConsentWarnings.Add(_consentWarning);
                if (_completionAppendix != null) f.CompletionAppendix.Add(_completionAppendix);
                return f;
            }
        }

        private class ThrowingContributor : IStatusEffectContributor
        {
            public StatusEffectFragments Contribute(
                Profile profile, StatusEffectCallSite callSite, string interactionType, bool isInitiator)
            {
                throw new InvalidOperationException("contributor went sideways");
            }
        }

        /// <summary>
        /// Mimics odorize fade-by-mention: reads a use counter off the profile, emits a fragment
        /// while > 0, and decrements in place. Demonstrates that contributors are responsible
        /// for their own state mutation; the helper is just an aggregator.
        /// </summary>
        private class FakeOdorizeContributor : IStatusEffectContributor
        {
            private readonly string _counterKey;
            private readonly string _fragment;

            public FakeOdorizeContributor(string counterKey, string fragment)
            {
                _counterKey = counterKey;
                _fragment = fragment;
            }

            public StatusEffectFragments Contribute(
                Profile profile, StatusEffectCallSite callSite, string interactionType, bool isInitiator)
            {
                var fragments = new StatusEffectFragments();
                if (profile?.characteristics == null) return fragments;
                if (!profile.characteristics.ContainsKey(_counterKey)) return fragments;
                if (!int.TryParse(profile.characteristics[_counterKey], out int uses)) return fragments;
                if (uses <= 0) return fragments;

                fragments.CompletionAppendix.Add(_fragment);
                profile.characteristics[_counterKey] = (uses - 1).ToString();
                return fragments;
            }
        }

        /// <summary>
        /// Mimics break: emits a recipient blocker only when the parent interaction matches.
        /// </summary>
        private class FakeBreakContributor : IStatusEffectContributor
        {
            private readonly string _bodyPart;
            private readonly string _blockedInteractionType;
            private readonly string _reason;

            public FakeBreakContributor(string bodyPart, string blockedInteractionType, string reason)
            {
                _bodyPart = bodyPart;
                _blockedInteractionType = blockedInteractionType;
                _reason = reason;
            }

            public StatusEffectFragments Contribute(
                Profile profile, StatusEffectCallSite callSite, string interactionType, bool isInitiator)
            {
                var fragments = new StatusEffectFragments();
                if (!string.Equals(interactionType, _blockedInteractionType, StringComparison.OrdinalIgnoreCase))
                {
                    return fragments;
                }

                fragments.Blockers.Add(new ValidationBlock
                {
                    Reason = _reason,
                    Source = "break:" + _bodyPart,
                    BlocksRecipient = !isInitiator,
                    BlocksInitiator = isInitiator
                });
                return fragments;
            }
        }
    }
}
