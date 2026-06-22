using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    [Collection("Database")]
    public class CorruptionProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly CorruptionProcessor _processor;
        private readonly Random _originalRng;

        public CorruptionProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new CorruptionProcessor(_database);
            // Snapshot and replace the static RNG so easter-egg-sensitive tests are
            // deterministic. Restore in Dispose so other test classes aren't disturbed.
            _originalRng = CorruptionProcessor.Rng;
            CorruptionProcessor.Rng = new Random(0);
        }

        public void Dispose()
        {
            CorruptionProcessor.Rng = _originalRng;
        }

        [Fact]
        public void InteractionType_ReturnsCorrupt()
        {
            // The processor is also registered as "purify" in the registry, but its own
            // InteractionType property returns the primary key.
            Assert.Equal("corrupt", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCommitment()
        {
            Assert.Equal("commitment", _processor.InvestmentLevel);
        }

        // -------------------------------------------------------------------
        // Verb / sign composition
        // -------------------------------------------------------------------

        [Theory]
        [InlineData("corrupt", 5, "corrupt")]
        [InlineData("corrupt", -5, "purify")]   // negative amount flips direction
        [InlineData("purify", 5, "purify")]
        [InlineData("purify", -5, "corrupt")]   // negative amount flips direction
        [InlineData("corrupt", 0, "corrupt")]   // zero keeps typed verb (no-op anyway)
        public void EffectiveVerb_FlipsOnNegativeAmount(string typed, int amount, string expected)
        {
            Assert.Equal(expected, CorruptionProcessor.EffectiveVerb(typed, amount));
        }

        [Theory]
        [InlineData("corrupt", -1)]
        [InlineData("purify", 1)]
        [InlineData("unknown", 0)]
        public void VerbSign_ReturnsExpected(string verb, int expected)
        {
            Assert.Equal(expected, CorruptionProcessor.VerbSign(verb));
        }

        // -------------------------------------------------------------------
        // Identifier round-trip
        // -------------------------------------------------------------------

        [Fact]
        public void Identifier_RoundTrip_PreservesVerbAndMagnitude()
        {
            string id = CorruptionProcessor.ComposeIdentifier("corrupt", 7);
            Assert.Equal("corrupt", CorruptionProcessor.ParseVerbFromIdentifier(id));
            Assert.Equal(7, CorruptionProcessor.ParseMagnitudeFromIdentifier(id));
        }

        [Fact]
        public void Identifier_ParseDefaults_OnGarbage()
        {
            // Defends against historical interaction records that predate this scheme.
            Assert.Equal("corrupt", CorruptionProcessor.ParseVerbFromIdentifier(null));
            Assert.Equal(1, CorruptionProcessor.ParseMagnitudeFromIdentifier(null));
            Assert.Equal(1, CorruptionProcessor.ParseMagnitudeFromIdentifier("corrupt|bogus"));
            Assert.Equal(1, CorruptionProcessor.ParseMagnitudeFromIdentifier("nopipe"));
        }

        // -------------------------------------------------------------------
        // Quota math
        // -------------------------------------------------------------------

        [Fact]
        public void RemainingQuota_ClampsAtZero()
        {
            Assert.Equal(CorruptionProcessor.DailyMagnitudeLimit, CorruptionProcessor.RemainingQuota(0));
            Assert.Equal(0, CorruptionProcessor.RemainingQuota(CorruptionProcessor.DailyMagnitudeLimit));
            Assert.Equal(0, CorruptionProcessor.RemainingQuota(CorruptionProcessor.DailyMagnitudeLimit + 5));
        }

        [Fact]
        public void RecordUsedQuota_PrunesStaleDates()
        {
            var alice = new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            DateTime yesterday = new DateTime(2026, 5, 16, 0, 0, 0, DateTimeKind.Utc);
            DateTime today = new DateTime(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
            alice.dailyMagnitudes[CorruptionProcessor.QuotaKey("Bob", yesterday)] = 7;

            CorruptionProcessor.RecordUsedQuota(alice, "Bob", today, 3);

            Assert.False(alice.dailyMagnitudes.ContainsKey(CorruptionProcessor.QuotaKey("Bob", yesterday)));
            Assert.Equal(3, alice.dailyMagnitudes[CorruptionProcessor.QuotaKey("Bob", today)]);
        }

        [Fact]
        public void RecordUsedQuota_LeavesOtherRecipientsAlone()
        {
            var alice = new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            DateTime today = new DateTime(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
            alice.dailyMagnitudes[CorruptionProcessor.QuotaKey("Carol", today)] = 4;

            CorruptionProcessor.RecordUsedQuota(alice, "Bob", today, 6);

            Assert.Equal(4, alice.dailyMagnitudes[CorruptionProcessor.QuotaKey("Carol", today)]);
            Assert.Equal(6, alice.dailyMagnitudes[CorruptionProcessor.QuotaKey("Bob", today)]);
        }

        // -------------------------------------------------------------------
        // ProcessInteraction
        // -------------------------------------------------------------------

        [Fact]
        public void ProcessInteraction_Corrupt_AppliesNegativeDelta()
        {
            SeedPair();

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "corrupt", magnitude: 5));

            var bob = _database.GetProfile("Bob");
            Assert.Equal("-5", bob.characteristics["corruption"]);
        }

        [Fact]
        public void ProcessInteraction_Purify_AppliesPositiveDelta()
        {
            SeedPair();

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "purify", magnitude: 4));

            var bob = _database.GetProfile("Bob");
            Assert.Equal("4", bob.characteristics["corruption"]);
        }

        [Fact]
        public void ProcessInteraction_AccumulatesAcrossCalls()
        {
            SeedPair();

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "corrupt", magnitude: 3));
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "purify", magnitude: 2));

            var bob = _database.GetProfile("Bob");
            Assert.Equal("-1", bob.characteristics["corruption"]);
        }

        [Fact]
        public void ProcessInteraction_DailyQuotaPartiallyClampsSecondCall()
        {
            // 6 then 5 → second clamps to 4. Verifies the spec's headline behavior.
            SeedPair();

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "corrupt", magnitude: 6));
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "purify", magnitude: 5));

            var bob = _database.GetProfile("Bob");
            // -6 + 4 = -2
            Assert.Equal("-2", bob.characteristics["corruption"]);

            var alice = _database.GetProfile("Alice");
            string quotaKey = CorruptionProcessor.QuotaKey("Bob", DateTime.UtcNow.Date);
            Assert.Equal(10, alice.dailyMagnitudes[quotaKey]);
        }

        [Fact]
        public void ProcessInteraction_QuotaExhausted_ZeroDeltaAndNoCorruptionChange()
        {
            SeedPair();

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "corrupt", magnitude: 10));
            // Third call: quota fully spent.
            var bob = _database.GetProfile("Bob");
            int corruptionBefore = int.Parse(bob.characteristics["corruption"]);

            var pending = BuildPending("Alice", "Bob", "corrupt", magnitude: 5);
            _processor.ProcessInteraction(pending);

            bob = _database.GetProfile("Bob");
            Assert.Equal(corruptionBefore, int.Parse(bob.characteristics["corruption"]));
            // Identifier should report applied=0 so the completion message suppresses
            // channel output and the initiator private-note path fires instead.
            Assert.Equal(0, CorruptionProcessor.ParseMagnitudeFromIdentifier(pending.pendingInteraction.identifier));
        }

        [Fact]
        public void ProcessInteraction_DifferentRecipients_DoNotShareQuota()
        {
            // Alice exhausts quota on Bob, then corrupts Carol — should still work.
            SeedPair();
            new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").BuildAndSave(_database);

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "corrupt", magnitude: 10));
            _processor.ProcessInteraction(BuildPending("Alice", "Carol", "corrupt", magnitude: 7));

            var carol = _database.GetProfile("Carol");
            Assert.Equal("-7", carol.characteristics["corruption"]);
        }

        [Fact]
        public void ProcessInteraction_DifferentInitiators_DoNotShareQuota()
        {
            // Alice and Carol can both corrupt Bob fully on the same day.
            SeedPair();
            new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").BuildAndSave(_database);

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "corrupt", magnitude: 10));
            _processor.ProcessInteraction(BuildPending("Carol", "Bob", "corrupt", magnitude: 10));

            var bob = _database.GetProfile("Bob");
            Assert.Equal("-20", bob.characteristics["corruption"]);
        }

        [Fact]
        public void ProcessInteraction_SelfTarget_AppliesAndUsesQuota()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);

            _processor.ProcessInteraction(BuildPending("Alice", "Alice", "purify", magnitude: 6));

            var alice = _database.GetProfile("Alice");
            Assert.Equal("6", alice.characteristics["corruption"]);
            int used = CorruptionProcessor.GetUsedQuota(alice, "Alice", DateTime.UtcNow.Date);
            Assert.Equal(6, used);
        }

        [Fact]
        public void ProcessInteraction_IncrementsDirectionalCounts()
        {
            SeedPair();

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "corrupt", magnitude: 3));
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "purify", magnitude: 2));

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            Assert.Equal(1, alice.counts["corruptgive"]);
            Assert.Equal(1, alice.counts["purifygive"]);
            Assert.Equal(1, bob.counts["corrupttake"]);
            Assert.Equal(1, bob.counts["purifytake"]);
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            SeedPair();
            var pending = BuildPending("Alice", "Bob", "corrupt", magnitude: 3);
            _database.AddPendingCommand(pending);

            _processor.ProcessInteraction(pending);

            Assert.Null(_database.GetPendingCommand(pending.Id));
        }

        [Fact]
        public void ProcessInteraction_StampsAppliedMagnitudeIntoIdentifier()
        {
            // After clamping, the in-memory identifier should report the applied magnitude
            // so GetCompletionMessage (called right after) emits truthful numbers.
            SeedPair();

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "corrupt", magnitude: 6));
            var pending = BuildPending("Alice", "Bob", "purify", magnitude: 5);
            _processor.ProcessInteraction(pending);

            Assert.Equal(4, CorruptionProcessor.ParseMagnitudeFromIdentifier(pending.pendingInteraction.identifier));
            Assert.Equal("purify", CorruptionProcessor.ParseVerbFromIdentifier(pending.pendingInteraction.identifier));
        }

        // -------------------------------------------------------------------
        // GetCompletionMessage
        // -------------------------------------------------------------------

        [Fact]
        public void GetCompletionMessage_CorruptFromZero_DescribesIncreasingCorruption()
        {
            SeedPair();
            var pending = BuildPending("Alice", "Bob", "corrupt", magnitude: 3);
            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            string msg = _processor.GetCompletionMessage(alice, bob, pending.pendingInteraction.identifier);

            Assert.Contains("Their corruption has increased by 3", msg);
            Assert.Contains("3 degrees of corruption", msg);
            // Raw signed integer should not leak into the channel-visible text.
            Assert.DoesNotContain("-3", msg);
        }

        [Fact]
        public void GetCompletionMessage_PurifyFromZero_DescribesIncreasingPurity()
        {
            SeedPair();
            var pending = BuildPending("Alice", "Bob", "purify", magnitude: 4);
            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            string msg = _processor.GetCompletionMessage(alice, bob, pending.pendingInteraction.identifier);

            Assert.Contains("Their purity has increased by 4", msg);
            Assert.Contains("4 degrees of purity", msg);
        }

        [Fact]
        public void GetCompletionMessage_PurifyOnPositiveBob_DescribesIncreasingPurity()
        {
            // 2 → 7 via purify 5: still on purity side, magnitude went up → "increasing purity".
            SeedPair();
            SeedRecipientCorruption("Bob", 2);

            var pending = BuildPending("Alice", "Bob", "purify", magnitude: 5);
            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            string msg = _processor.GetCompletionMessage(alice, bob, pending.pendingInteraction.identifier);

            Assert.Contains("Their purity has increased by 5", msg);
            Assert.Contains("7 degrees of purity", msg);
        }

        [Fact]
        public void GetCompletionMessage_PurifyOnNegativeBob_DescribesDecreasingCorruption()
        {
            // -7 → -2 via purify 5: still on corruption side, magnitude went down → "decreasing corruption".
            SeedPair();
            SeedRecipientCorruption("Bob", -7);

            var pending = BuildPending("Alice", "Bob", "purify", magnitude: 5);
            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            string msg = _processor.GetCompletionMessage(alice, bob, pending.pendingInteraction.identifier);

            Assert.Contains("Their corruption has decreased by 5", msg);
            Assert.Contains("2 degrees of corruption", msg);
        }

        [Fact]
        public void GetCompletionMessage_PurifyCrossesZero_DescribesEndSide()
        {
            // -2 → 3 via purify 5: crosses zero, ends on purity side → "increasing purity".
            SeedPair();
            SeedRecipientCorruption("Bob", -2);

            var pending = BuildPending("Alice", "Bob", "purify", magnitude: 5);
            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            string msg = _processor.GetCompletionMessage(alice, bob, pending.pendingInteraction.identifier);

            Assert.Contains("Their purity has increased by 5", msg);
            Assert.Contains("3 degrees of purity", msg);
        }

        [Fact]
        public void GetCompletionMessage_LandsExactlyAtZero_UsesEternalTugOfWarPhrase()
        {
            // +5 → 0 via corrupt 5: ends at neutral, special phrasing on the trailing clause.
            // Side is chosen from prior value (purity), action is "decreased".
            SeedPair();
            SeedRecipientCorruption("Bob", 5);

            var pending = BuildPending("Alice", "Bob", "corrupt", magnitude: 5);
            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            string msg = _processor.GetCompletionMessage(alice, bob, pending.pendingInteraction.identifier);

            Assert.Contains("Their purity has decreased by 5", msg);
            Assert.Contains("leaving them once again neutral in the eternal tug of war of purity and corruption", msg);
            // The "X degrees of …" pattern should NOT appear when neutral.
            Assert.DoesNotContain("degrees of", msg);
        }

        [Fact]
        public void GetCompletionMessage_QuotaExhausted_ReturnsEmptyAndStashesPrivateNoteMatchingCommandWording()
        {
            // TOCTOU edge: queued pending consents but quota was eaten meanwhile. The
            // channel-visible message should be suppressed and the initiator should get
            // a private heads-up — identical wording to the command-time refusal so the
            // two paths can't drift.
            SeedPair();
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "corrupt", magnitude: 10));
            var doomed = BuildPending("Alice", "Bob", "corrupt", magnitude: 4);
            _processor.ProcessInteraction(doomed);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            string msg = _processor.GetCompletionMessage(alice, bob, doomed.pendingInteraction.identifier);

            Assert.Equal(string.Empty, msg);
            string privateNote = _processor.GetAndClearInitiatorPrivateMessage();
            // Should exactly match the centralized quota-exhausted helper output (modulo the
            // time-until-reset suffix, which depends on the current second).
            Assert.StartsWith(
                "You can only increase or decrease someone's corruption/purity by 10 per day! "
                + "Please respect that 'Commitment' interactions are meant to be meaningful, and not spammed. "
                + "You can corrupt Bob further in",
                privateNote);
        }

        [Fact]
        public void GetAndClearInitiatorPrivateMessage_ReturnsEmptyAfterDrain()
        {
            SeedPair();
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "corrupt", magnitude: 10));
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "corrupt", magnitude: 4));

            Assert.NotEmpty(_processor.GetAndClearInitiatorPrivateMessage());
            // Second drain should yield nothing — the field was cleared by the first call.
            Assert.Empty(_processor.GetAndClearInitiatorPrivateMessage());
        }

        [Fact]
        public void GetCompletionMessage_EasterEggSubstitutesUnPurifyForCorrupt()
        {
            // Force the easter egg to fire by replacing Rng with a stub that always rolls 0.
            CorruptionProcessor.Rng = new AlwaysZeroRandom();
            SeedPair();
            var pending = BuildPending("Alice", "Bob", "corrupt", magnitude: 2);
            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            string msg = _processor.GetCompletionMessage(alice, bob, pending.pendingInteraction.identifier);

            // "has un-purified Bob" with the substituted past tense.
            Assert.Contains("un-purified", msg);
            Assert.DoesNotContain("corrupted Bob", msg);
        }

        [Fact]
        public void GetCompletionMessage_EasterEggSubstitutesUnCorruptForPurify()
        {
            CorruptionProcessor.Rng = new AlwaysZeroRandom();
            SeedPair();
            var pending = BuildPending("Alice", "Bob", "purify", magnitude: 2);
            _processor.ProcessInteraction(pending);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            string msg = _processor.GetCompletionMessage(alice, bob, pending.pendingInteraction.identifier);

            Assert.Contains("un-corrupted", msg);
            Assert.DoesNotContain("purified Bob", msg);
        }

        [Fact]
        public void EasterEggRoll_FiresAtRoughlyAdvertisedRate()
        {
            // Statistical sanity: 1/20 expected, 200 trials → expect ~10. Tolerance is wide
            // to avoid flakiness on the seeded RNG.
            CorruptionProcessor.Rng = new Random(42);
            int hits = 0;
            const int trials = 200;
            for (int i = 0; i < trials; i++)
            {
                if (CorruptionProcessor.RollEasterEgg()) hits++;
            }
            Assert.True(hits > 0, "Easter egg never fired in 200 trials — suspicious.");
            Assert.True(hits < trials / 2, "Easter egg fired suspiciously often: " + hits + "/" + trials);
        }

        // -------------------------------------------------------------------
        // GetConsentWarning
        // -------------------------------------------------------------------

        [Fact]
        public void GetConsentWarning_CorruptOnNeutralBob_DescribesIncreasingCorruption()
        {
            // Bob at 0 → -3 after corrupt 3: end side is corruption, increasing.
            SeedPair();
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string warning = _processor.GetConsentWarning(alice, bob, CorruptionProcessor.ComposeIdentifier("corrupt", 3));

            Assert.Contains("Alice wants to corrupt Bob", warning);
            Assert.Contains("increasing their corruption by 3", warning);
            Assert.Contains("Do you !consent to being corrupted?", warning);
            // No current/projected numbers in the consent line per user direction.
            Assert.DoesNotContain("currently", warning);
            Assert.DoesNotContain("become", warning);
        }

        [Fact]
        public void GetConsentWarning_PurifyOnNegativeBob_DescribesDecreasingCorruption()
        {
            // Bob at -7 → -2 after purify 5: end side is still corruption, decreasing.
            SeedPair();
            SeedRecipientCorruption("Bob", -7);
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string warning = _processor.GetConsentWarning(alice, bob, CorruptionProcessor.ComposeIdentifier("purify", 5));

            Assert.Contains("Alice wants to purify Bob", warning);
            Assert.Contains("decreasing their corruption by 5", warning);
            Assert.Contains("Do you !consent to being purified?", warning);
        }

        [Fact]
        public void GetConsentWarning_PurifyOnPositiveBob_DescribesIncreasingPurity()
        {
            // Bob at 2 → 7 after purify 5: end side is purity, increasing.
            SeedPair();
            SeedRecipientCorruption("Bob", 2);
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string warning = _processor.GetConsentWarning(alice, bob, CorruptionProcessor.ComposeIdentifier("purify", 5));

            Assert.Contains("increasing their purity by 5", warning);
        }

        [Fact]
        public void GetConsentWarning_Corrupt_DisclosesVisibilityAndQuotaRule()
        {
            // B3 + shape-D: corrupting direction discloses corruption visibility and states
            // the per-day magnitude quota by the initiator's display name.
            SeedPair();
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string warning = _processor.GetConsentWarning(alice, bob, CorruptionProcessor.ComposeIdentifier("corrupt", 3));

            Assert.Contains("Corruption will be visible in most interactions, and in anything milked from you.", warning);
            Assert.Contains("Alice can only corrupt you by 10 per day.", warning);
            Assert.DoesNotContain("Purity will be visible", warning);
        }

        [Fact]
        public void GetConsentWarning_Purify_DisclosesPurityVisibility()
        {
            // Direction-adaptive: a purifying action speaks of purity, not corruption.
            SeedPair();
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string warning = _processor.GetConsentWarning(alice, bob, CorruptionProcessor.ComposeIdentifier("purify", 3));

            Assert.Contains("Purity will be visible in most interactions, and in anything milked from you.", warning);
            Assert.Contains("Alice can only purify you by 10 per day.", warning);
            Assert.DoesNotContain("Corruption will be visible", warning);
        }

        [Fact]
        public void GetConsentWarning_NoQuotaSpentToday_OmitsConsumedClause()
        {
            // Consumed clause is suppressed when nothing has been spent yet.
            SeedPair();
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string warning = _processor.GetConsentWarning(alice, bob, CorruptionProcessor.ComposeIdentifier("corrupt", 3));

            Assert.DoesNotContain("has already", warning);
        }

        [Fact]
        public void GetConsentWarning_QuotaPartlySpentToday_ShowsConsumedClause()
        {
            // When the initiator has already spent some of today's budget against this
            // recipient, the prompt names how much.
            SeedPair();
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            CorruptionProcessor.RecordUsedQuota(alice, bob.userName, DateTime.UtcNow.Date, 2);

            string warning = _processor.GetConsentWarning(alice, bob, CorruptionProcessor.ComposeIdentifier("corrupt", 3));

            Assert.Contains("Alice has already corrupted you by 2 today.", warning);
        }

        // -------------------------------------------------------------------
        // LevelChange direction resolver
        // -------------------------------------------------------------------

        [Theory]
        [InlineData(2, 7, "purity", "increasing", "increased")]      // both purity, magnitude up
        [InlineData(7, 2, "purity", "decreasing", "decreased")]      // both purity, magnitude down
        [InlineData(-7, -2, "corruption", "decreasing", "decreased")] // both corruption, magnitude down
        [InlineData(-2, -7, "corruption", "increasing", "increased")] // both corruption, magnitude up
        [InlineData(-2, 3, "purity", "increasing", "increased")]     // cross-zero, ends on purity
        [InlineData(2, -3, "corruption", "increasing", "increased")] // cross-zero, ends on corruption
        [InlineData(0, -5, "corruption", "increasing", "increased")] // from neutral to corruption
        [InlineData(0, 5, "purity", "increasing", "increased")]      // from neutral to purity
        [InlineData(5, 0, "purity", "decreasing", "decreased")]      // landing at zero from purity
        [InlineData(-5, 0, "corruption", "decreasing", "decreased")] // landing at zero from corruption
        public void LevelChange_DescribesEndStateCorrectly(int oldValue, int newValue, string expectedSide, string expectedPresent, string expectedPast)
        {
            var change = CorruptionProcessor.LevelChange.From(oldValue, newValue);
            Assert.Equal(expectedSide, change.Side);
            Assert.Equal(expectedPresent, change.ActionPresent);
            Assert.Equal(expectedPast, change.ActionPast);
        }

        [Fact]
        public void DescribeCurrentLevel_ZeroEndsWithEternalTugPhrase()
        {
            string phrase = CorruptionProcessor.DescribeCurrentLevel(0);
            Assert.Equal("leaving them once again neutral in the eternal tug of war of purity and corruption", phrase);
        }

        [Theory]
        [InlineData(-50, "leaving them with 50 degrees of corruption")]
        [InlineData(-1, "leaving them with 1 degrees of corruption")]
        [InlineData(1, "leaving them with 1 degrees of purity")]
        [InlineData(50, "leaving them with 50 degrees of purity")]
        public void DescribeCurrentLevel_NonZeroUsesDegreesOfSide(int value, string expected)
        {
            Assert.Equal(expected, CorruptionProcessor.DescribeCurrentLevel(value));
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private void SeedPair()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
        }

        private void SeedRecipientCorruption(string userName, int corruption)
        {
            var profile = _database.GetProfile(userName);
            profile.characteristics[CorruptionProcessor.CorruptionCharacteristicKey] = corruption.ToString();
            _database.SetProfile(userName, profile);
        }

        private static PendingCommand BuildPending(string initiator, string recipient, string effectiveVerb, int magnitude)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = effectiveVerb,
                    identifier = CorruptionProcessor.ComposeIdentifier(effectiveVerb, magnitude),
                    investmentLevel = "commitment",
                    interactionTime = DateTime.UtcNow,
                    extraParameters = new MongoDB.Bson.BsonArray { magnitude }
                }
            };
        }

        /// <summary>Random subclass whose Next(anyBound) always returns 0 — forces the easter egg to fire.</summary>
        private class AlwaysZeroRandom : Random
        {
            public override int Next(int maxValue) => 0;
            public override int Next() => 0;
            public override int Next(int minValue, int maxValue) => minValue;
        }
    }
}
