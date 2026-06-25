using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Unit tests for ClimaxforProcessor. Covers both interaction-type keys
    /// (<c>climaxfor</c> and <c>climax</c>) since the same processor instance
    /// backs both verbs.
    /// </summary>
    [Collection("Database")]
    public class ClimaxforProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly ClimaxforProcessor _processor;

        public ClimaxforProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new ClimaxforProcessor(_database);
        }

        public void Dispose() { }

        // -------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------

        [Fact]
        public void InteractionType_ReturnsClimaxfor()
        {
            Assert.Equal("climaxfor", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsInvolved()
        {
            Assert.Equal("involved", _processor.InvestmentLevel);
        }

        // -------------------------------------------------------------------
        // ResolveClimaxer / ResolvePartner
        // -------------------------------------------------------------------

        [Fact]
        public void ResolveClimaxer_Climaxfor_OtherTarget_IsInitiator()
        {
            Assert.Equal("Alice",
                ClimaxforProcessor.ResolveClimaxer(ClimaxforProcessor.ClimaxforType, "Alice", "Bob"));
        }

        [Fact]
        public void ResolveClimaxer_Climax_OtherTarget_IsRecipient()
        {
            Assert.Equal("Bob",
                ClimaxforProcessor.ResolveClimaxer(ClimaxforProcessor.ClimaxType, "Alice", "Bob"));
        }

        [Fact]
        public void ResolveClimaxer_SelfTarget_AlwaysInitiator_RegardlessOfType()
        {
            Assert.Equal("Alice",
                ClimaxforProcessor.ResolveClimaxer(ClimaxforProcessor.ClimaxforType, "Alice", "Alice"));
            Assert.Equal("Alice",
                ClimaxforProcessor.ResolveClimaxer(ClimaxforProcessor.ClimaxType, "Alice", "Alice"));
        }

        [Fact]
        public void ResolvePartner_SelfTarget_IsNull()
        {
            Assert.Null(ClimaxforProcessor.ResolvePartner(ClimaxforProcessor.ClimaxforType, "Alice", "Alice"));
        }

        [Fact]
        public void ResolvePartner_Climaxfor_OtherTarget_IsRecipient()
        {
            Assert.Equal("Bob",
                ClimaxforProcessor.ResolvePartner(ClimaxforProcessor.ClimaxforType, "Alice", "Bob"));
        }

        [Fact]
        public void ResolvePartner_Climax_OtherTarget_IsInitiator()
        {
            Assert.Equal("Alice",
                ClimaxforProcessor.ResolvePartner(ClimaxforProcessor.ClimaxType, "Alice", "Bob"));
        }

        // -------------------------------------------------------------------
        // ValidateInteraction
        // -------------------------------------------------------------------

        [Fact]
        public void ValidateInteraction_SelfTarget_RejectsAsDefensiveGuard()
        {
            // Self-target is allowed at the command layer via PerformSelfTarget, which
            // never enters the processor's normal flow. Processor validation rejects it
            // so a stray self-pending can't sneak through and double-credit counts.
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Alice", string.Empty);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_NonExistentInitiator_Fails()
        {
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Ghost", "Bob", string.Empty);

            Assert.False(result.IsValid);
            Assert.Contains("Ghost", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_NonExistentRecipient_Fails()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Ghost", string.Empty);

            Assert.False(result.IsValid);
            Assert.Contains("Ghost", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_BothExist_Succeeds()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", string.Empty);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_ClimaxforInitiatorChastity_Blocks()
        {
            // !climaxfor: the initiator is the climaxer, so the initiator's chastity blocks.
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);
            CurseProcessor.ApplyCurse(alice, "chastity", "TestSetup");
            _database.SetProfile("Alice", alice);

            var result = _processor.ValidateInteraction(
                "Alice", "Bob", ClimaxforProcessor.ComposeIdentifier(ClimaxforProcessor.ClimaxforType, 0));

            Assert.False(result.IsValid);
            Assert.Contains("chastity", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_ClimaxforRecipientChastity_PartnerStaysFree()
        {
            // !climaxfor: the recipient is only the partner, so their chastity must not block.
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            CurseProcessor.ApplyCurse(bob, "chastity", "TestSetup");
            _database.SetProfile("Bob", bob);

            var result = _processor.ValidateInteraction(
                "Alice", "Bob", ClimaxforProcessor.ComposeIdentifier(ClimaxforProcessor.ClimaxforType, 0));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_ClimaxRecipientChastity_Blocks()
        {
            // !climax: the recipient is the climaxer, so the recipient's chastity blocks.
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            CurseProcessor.ApplyCurse(bob, "chastity", "TestSetup");
            _database.SetProfile("Bob", bob);

            var result = _processor.ValidateInteraction(
                "Alice", "Bob", ClimaxforProcessor.ComposeIdentifier(ClimaxforProcessor.ClimaxType, 0));

            Assert.False(result.IsValid);
            Assert.Contains("chastity", result.ErrorMessage);
        }

        [Fact]
        public void ValidateInteraction_ClimaxInitiatorChastity_PartnerStaysFree()
        {
            // !climax: the initiator is only the partner, so their chastity must not block.
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);
            CurseProcessor.ApplyCurse(alice, "chastity", "TestSetup");
            _database.SetProfile("Alice", alice);

            var result = _processor.ValidateInteraction(
                "Alice", "Bob", ClimaxforProcessor.ComposeIdentifier(ClimaxforProcessor.ClimaxType, 0));

            Assert.True(result.IsValid);
        }

        // -------------------------------------------------------------------
        // ValidateSelfTarget
        // -------------------------------------------------------------------

        [Fact]
        public void ValidateSelfTarget_NoBlockers_Succeeds()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);

            var result = _processor.ValidateSelfTarget("Alice", ClimaxforProcessor.ClimaxforType);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateSelfTarget_NonExistentUser_Fails()
        {
            var result = _processor.ValidateSelfTarget("Ghost", ClimaxforProcessor.ClimaxforType);

            Assert.False(result.IsValid);
            Assert.Contains("Ghost", result.ErrorMessage);
        }

        [Theory]
        [InlineData("climax")]
        [InlineData("climaxfor")]
        public void ValidateSelfTarget_ChastityCurse_BlocksBothVerbs(string typeKey)
        {
            // Regression: a chastity-cursed resident's solo climax bypassed the consent flow
            // and auto-resolved before the curse could gate it. The solo climaxer is always
            // the same person, so chastity must block whichever verb they typed.
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            CurseProcessor.ApplyCurse(alice, "chastity", "TestSetup");
            _database.SetProfile("Alice", alice);

            var result = _processor.ValidateSelfTarget("Alice", typeKey);

            Assert.False(result.IsValid);
            Assert.Contains("chastity", result.ErrorMessage);
        }

        // -------------------------------------------------------------------
        // PerformSelfTarget
        // -------------------------------------------------------------------

        [Fact]
        public void PerformSelfTarget_IncrementsClimaxtake_NotClimaxgive()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);

            _processor.PerformSelfTarget("Alice", ClimaxforProcessor.ClimaxforType);

            var alice = _database.GetProfile("Alice");
            Assert.Equal(1, CountOrZero(alice, "climaxtake"));
            Assert.Equal(0, CountOrZero(alice, "climaxgive"));
        }

        [Fact]
        public void PerformSelfTarget_BumpsTodayDailyCount()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);

            _processor.PerformSelfTarget("Alice", ClimaxforProcessor.ClimaxforType);

            var alice = _database.GetProfile("Alice");
            string todayKey = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            Assert.Equal(1, alice.dailyClimaxCounts[todayKey]);
        }

        [Fact]
        public void PerformSelfTarget_PersistsInteractionRecord()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);

            _processor.PerformSelfTarget("Alice", ClimaxforProcessor.ClimaxforType);

            var interactions = _database.GetInteractionsByInitiator("Alice");
            Assert.Contains(interactions, i =>
                i.type == ClimaxforProcessor.ClimaxforType &&
                i.recipient == "Alice");
        }

        [Fact]
        public void PerformSelfTarget_ClimaxType_RecordsTypedVerb()
        {
            // Bare !climax falls through to self-target; the interaction record should
            // still preserve which alias the user typed (analytics affordance).
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);

            _processor.PerformSelfTarget("Alice", ClimaxforProcessor.ClimaxType);

            var interactions = _database.GetInteractionsByInitiator("Alice");
            Assert.Contains(interactions, i => i.type == ClimaxforProcessor.ClimaxType);
        }

        [Fact]
        public void PerformSelfTarget_ReturnsAloneFlavorCompletion()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);

            string message = _processor.PerformSelfTarget("Alice", ClimaxforProcessor.ClimaxforType);

            Assert.Contains("Alice makes a mess of themselves", message);
        }

        // -------------------------------------------------------------------
        // ProcessInteraction (other-target consent flow)
        // -------------------------------------------------------------------

        [Fact]
        public void ProcessInteraction_Climaxfor_InitiatorIsClimaxer_GetsClimaxtake()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", ClimaxforProcessor.ClimaxforType);
            _database.AddPendingCommand(pending);
            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            Assert.Equal(1, CountOrZero(alice, "climaxtake"));
            Assert.Equal(1, CountOrZero(bob, "climaxgive"));
        }

        [Fact]
        public void ProcessInteraction_Climax_RecipientIsClimaxer_GetsClimaxtake()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", ClimaxforProcessor.ClimaxType);
            _database.AddPendingCommand(pending);
            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            // Inverted: Bob is the climaxer, Alice is the helping partner.
            Assert.Equal(1, CountOrZero(bob, "climaxtake"));
            Assert.Equal(1, CountOrZero(alice, "climaxgive"));
        }

        [Fact]
        public void ProcessInteraction_Climaxfor_BumpsInitiatorsDailyCount()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", ClimaxforProcessor.ClimaxforType);
            _database.AddPendingCommand(pending);
            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            string today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            Assert.Equal(1, DailyOrZero(alice, today));
            // Bob did NOT climax, so his daily counter should not bump.
            Assert.Equal(0, DailyOrZero(bob, today));
        }

        [Fact]
        public void ProcessInteraction_Climax_BumpsRecipientsDailyCount()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", ClimaxforProcessor.ClimaxType);
            _database.AddPendingCommand(pending);
            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            string today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            Assert.Equal(1, DailyOrZero(bob, today));
            Assert.Equal(0, DailyOrZero(alice, today));
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", ClimaxforProcessor.ClimaxforType);
            _database.AddPendingCommand(pending);
            _processor.ProcessInteraction(pending);

            Assert.Null(_database.GetPendingCommand(pending.Id));
        }

        [Fact]
        public void ProcessInteraction_StampsIdentifierWithTypeAndCount()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", ClimaxforProcessor.ClimaxType);
            _database.AddPendingCommand(pending);
            _processor.ProcessInteraction(pending);

            Assert.Equal(ClimaxforProcessor.ClimaxType,
                ClimaxforProcessor.ParseTypeFromIdentifier(pending.pendingInteraction.identifier));
            Assert.Equal(1, ClimaxforProcessor.ParseDailyCountFromIdentifier(pending.pendingInteraction.identifier));
        }

        // -------------------------------------------------------------------
        // Daily count pruning
        // -------------------------------------------------------------------

        [Fact]
        public void IncrementDailyClimaxCount_DropsEntriesOlderThanRetentionWindow()
        {
            var profile = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDailyClimaxCount("2024-01-01", 7)
                .Build();

            int newCount = ClimaxforProcessor.IncrementDailyClimaxCount(profile);

            Assert.Equal(1, newCount);
            Assert.False(profile.dailyClimaxCounts.ContainsKey("2024-01-01"));
        }

        [Fact]
        public void IncrementDailyClimaxCount_KeepsEntriesInsideRetentionWindow()
        {
            string recentKey = DateTime.UtcNow.Date.AddDays(-5).ToString("yyyy-MM-dd");
            var profile = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDailyClimaxCount(recentKey, 4)
                .Build();

            ClimaxforProcessor.IncrementDailyClimaxCount(profile);

            Assert.True(profile.dailyClimaxCounts.ContainsKey(recentKey));
            Assert.Equal(4, profile.dailyClimaxCounts[recentKey]);
        }

        [Fact]
        public void IncrementDailyClimaxCount_AccumulatesAcrossCalls()
        {
            var profile = new ProfileBuilder().WithUserName("Alice").Build();

            int first = ClimaxforProcessor.IncrementDailyClimaxCount(profile);
            int second = ClimaxforProcessor.IncrementDailyClimaxCount(profile);
            int third = ClimaxforProcessor.IncrementDailyClimaxCount(profile);

            Assert.Equal(1, first);
            Assert.Equal(2, second);
            Assert.Equal(3, third);
        }

        // -------------------------------------------------------------------
        // GetCompletionMessage flavor
        // -------------------------------------------------------------------

        [Fact]
        public void GetCompletionMessage_SelfTarget_UsesAloneTemplate()
        {
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            string id = ClimaxforProcessor.ComposeIdentifier(ClimaxforProcessor.ClimaxforType, 1);

            string message = _processor.GetCompletionMessage(alice, alice, id);

            Assert.Contains("Alice makes a mess of themselves", message);
        }

        [Fact]
        public void GetCompletionMessage_Climaxfor_UsesForTemplate()
        {
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            string id = ClimaxforProcessor.ComposeIdentifier(ClimaxforProcessor.ClimaxforType, 1);

            string message = _processor.GetCompletionMessage(alice, bob, id);

            Assert.Contains("Alice shudders in ecstasy for Bob", message);
        }

        [Fact]
        public void GetCompletionMessage_Climax_UsesMakesTemplate()
        {
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            string id = ClimaxforProcessor.ComposeIdentifier(ClimaxforProcessor.ClimaxType, 1);

            string message = _processor.GetCompletionMessage(alice, bob, id);

            Assert.Contains("Alice brings Bob to a pleasure filled climax", message);
        }

        [Fact]
        public void GetCompletionMessage_HighCount_DoesNotUseDiminishingVolumeLanguage()
        {
            // Regression test: repeat climaxes describe endurance and amazement, NEVER
            // diminishing volume. Sample the descriptor pool many times across the
            // high-count branch to make sure no descriptor sneaks in any of the banned words.
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            string id = ClimaxforProcessor.ComposeIdentifier(ClimaxforProcessor.ClimaxforType, 8);

            for (int i = 0; i < 50; i++)
            {
                string message = _processor.GetCompletionMessage(alice, alice, id);
                Assert.DoesNotContain("dribble", message, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("diminish", message, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("smaller", message, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("less and less", message, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("running dry", message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void GetCompletionMessage_HighCount_NeverSurfacesLowCountSignatures()
        {
            // Tight regression test on the per-count pool routing: the high-count branch
            // must never return a descriptor that's only in the count==1 / count==2 pools
            // (or vice versa). PickFlavorDescriptor instantiates a fresh Random per call so
            // descriptor variety across a tight loop is flaky — we don't try to verify
            // every descriptor surfaces; we just verify the wrong pool's signatures stay
            // out across many samples.
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            string highId = ClimaxforProcessor.ComposeIdentifier(ClimaxforProcessor.ClimaxforType,
                ClimaxforProcessor.InhumanStaminaThreshold);

            for (int i = 0; i < 50; i++)
            {
                string highMessage = _processor.GetCompletionMessage(alice, alice, highId);
                // count==1 pool signatures must not surface at Inhuman threshold.
                Assert.DoesNotContain("sweet release", highMessage);
                Assert.DoesNotContain("What a display", highMessage);
                Assert.DoesNotContain("may there be many more to cum", highMessage, StringComparison.OrdinalIgnoreCase);
                // count==2 pool signature
                Assert.DoesNotContain("really build momentum", highMessage);
                // count==3 pool signature
                Assert.DoesNotContain("Rule of three", highMessage);
            }
        }

        // -------------------------------------------------------------------
        // Consent warning wording
        // -------------------------------------------------------------------

        [Fact]
        public void GetConsentWarning_Climaxfor_UsesClimaxForWording()
        {
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            string warning = _processor.GetConsentWarning(alice, bob, string.Empty, ClimaxforProcessor.ClimaxforType);

            Assert.Contains("Alice is about to climax for Bob", warning);
            Assert.Contains("responsible for the mess", warning);
            Assert.Contains("!consent", warning);
        }

        [Fact]
        public void GetConsentWarning_Climax_UsesMakeYouClimaxWording()
        {
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            string warning = _processor.GetConsentWarning(alice, bob, string.Empty, ClimaxforProcessor.ClimaxType);

            Assert.Contains("Alice is ready to make Bob climax", warning);
            Assert.Contains("made a mess of", warning);
            Assert.Contains("!consent", warning);
        }

        // -------------------------------------------------------------------
        // Identifier round-trip
        // -------------------------------------------------------------------

        [Fact]
        public void Identifier_RoundTrip_PreservesTypeAndCount()
        {
            string id = ClimaxforProcessor.ComposeIdentifier(ClimaxforProcessor.ClimaxType, 7);

            Assert.Equal(ClimaxforProcessor.ClimaxType, ClimaxforProcessor.ParseTypeFromIdentifier(id));
            Assert.Equal(7, ClimaxforProcessor.ParseDailyCountFromIdentifier(id));
        }

        [Fact]
        public void ParseType_EmptyOrBare_DefaultsToClimaxfor()
        {
            Assert.Equal(ClimaxforProcessor.ClimaxforType, ClimaxforProcessor.ParseTypeFromIdentifier(string.Empty));
            Assert.Equal(ClimaxforProcessor.ClimaxforType, ClimaxforProcessor.ParseTypeFromIdentifier("climaxfor"));
        }

        [Fact]
        public void ParseDailyCount_EmptyOrBare_DefaultsToOne()
        {
            Assert.Equal(1, ClimaxforProcessor.ParseDailyCountFromIdentifier(string.Empty));
            Assert.Equal(1, ClimaxforProcessor.ParseDailyCountFromIdentifier("climaxfor"));
        }

        // -------------------------------------------------------------------
        // Test helpers
        // -------------------------------------------------------------------

        private static int CountOrZero(Profile profile, string key)
        {
            if (profile == null || profile.counts == null) return 0;
            return profile.counts.TryGetValue(key, out int value) ? value : 0;
        }

        private static int DailyOrZero(Profile profile, string dayKey)
        {
            if (profile == null || profile.dailyClimaxCounts == null) return 0;
            return profile.dailyClimaxCounts.TryGetValue(dayKey, out int value) ? value : 0;
        }

        private PendingCommand BuildPending(string initiator, string recipient, string typeKey)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = typeKey,
                    identifier = ClimaxforProcessor.ComposeIdentifier(typeKey, 0),
                    investmentLevel = "involved",
                    interactionTime = DateTime.UtcNow,
                },
            };
        }

        // -------------------------------------------------------------------
        // Auto-dose integration (climax intensifies cum/pre/seminal on the partner)
        // -------------------------------------------------------------------

        [Fact]
        public void ProcessInteraction_Climaxfor_IntensifiesPartnerExistingCumVice()
        {
            // Alice climaxes for Bob (Alice is the climaxer). Bob is the partner; if he
            // already carries a "cum" vice, climax should intensify it by 1.
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            var bob = _database.GetProfile("Bob");
            DoseProcessor.ApplyDose(bob, "cum", "PriorDoser");
            _database.SetProfile("Bob", bob);

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", ClimaxforProcessor.ClimaxforType));

            var reloaded = _database.GetProfile("Bob");
            var cum = ViceInstance.LoadAll(reloaded).First(v => v.Vice == "cum");
            Assert.Equal(2, cum.AddictionLevel);
        }

        [Fact]
        public void ProcessInteraction_Climaxfor_DoesNotCreateAbsentVicesOnPartner()
        {
            // Bob has no vices at all; a climax should NOT introduce cum/pre/seminal —
            // intensify-only.
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", ClimaxforProcessor.ClimaxforType));

            var reloaded = _database.GetProfile("Bob");
            Assert.Empty(ViceInstance.LoadAll(reloaded));
        }

        [Fact]
        public void ProcessInteraction_Climax_IntensifiesInitiatorAsPartner()
        {
            // !climax inverts the role: Alice is the initiator but Bob is the climaxer.
            // The partner (= non-climaxer) is Alice. Alice's existing "pre" should rise.
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            var alice = _database.GetProfile("Alice");
            DoseProcessor.ApplyDose(alice, "pre", "PriorDoser");
            _database.SetProfile("Alice", alice);

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", ClimaxforProcessor.ClimaxType));

            var reloaded = _database.GetProfile("Alice");
            Assert.Equal(2, ViceInstance.LoadAll(reloaded).First(v => v.Vice == "pre").AddictionLevel);
            // Bob (the climaxer) gets no auto-dose — he was the one climaxing.
            var bob = _database.GetProfile("Bob");
            Assert.Empty(ViceInstance.LoadAll(bob));
        }

        [Fact]
        public void PerformSelfTarget_IntensifiesClimaxersOwnVices()
        {
            // Solo climaxes have no partner — self-target intensifies the climaxer
            // themselves (no escape from your own essence).
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var alice = _database.GetProfile("Alice");
            DoseProcessor.ApplyDose(alice, "seminal", "PriorDoser");
            _database.SetProfile("Alice", alice);

            _processor.PerformSelfTarget("Alice", ClimaxforProcessor.ClimaxforType);

            var reloaded = _database.GetProfile("Alice");
            Assert.Equal(2, ViceInstance.LoadAll(reloaded).First(v => v.Vice == "seminal").AddictionLevel);
        }

        [Fact]
        public void PerformSelfTarget_NoExistingVices_DoesNotIntroduceAny()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);

            _processor.PerformSelfTarget("Alice", ClimaxforProcessor.ClimaxforType);

            var reloaded = _database.GetProfile("Alice");
            Assert.Empty(ViceInstance.LoadAll(reloaded));
        }

        [Fact]
        public void ProcessInteraction_AnyIntensification_AppendsAddictionFragmentToCompletion()
        {
            // The flavor fragment is built in ProcessInteraction and consumed by
            // GetCompletionMessage. Verify the channel message includes the sharpening
            // line when a vice was intensified.
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            var bob = _database.GetProfile("Bob");
            DoseProcessor.ApplyDose(bob, "cum", "PriorDoser");
            _database.SetProfile("Bob", bob);

            var pending = BuildPending("Alice", "Bob", ClimaxforProcessor.ClimaxforType);
            _processor.ProcessInteraction(pending);
            var alice = _database.GetProfile("Alice");
            var reloadedBob = _database.GetProfile("Bob");
            string message = _processor.GetCompletionMessage(alice, reloadedBob, pending.pendingInteraction.identifier);

            Assert.Contains("getting more and more hooked on cum", message);
        }

        [Fact]
        public void ProcessInteraction_NoIntensification_DoesNotAppendAddictionFragment()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pending = BuildPending("Alice", "Bob", ClimaxforProcessor.ClimaxforType);
            _processor.ProcessInteraction(pending);
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            string message = _processor.GetCompletionMessage(alice, bob, pending.pendingInteraction.identifier);

            Assert.DoesNotContain("hooked on", message);
        }
    }
}
