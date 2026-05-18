using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    [Collection("Database")]
    public class TrainProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly TrainProcessor _processor;

        public TrainProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new TrainProcessor(_database);
        }

        public void Dispose()
        {
        }

        [Fact]
        public void InteractionType_ReturnsTrain()
        {
            Assert.Equal("train", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCommitment()
        {
            Assert.Equal("commitment", _processor.InvestmentLevel);
        }

        // ─── Progression rule tests ───────────────────────────────────────────

        [Fact]
        public void ApplyProgression_BothBelowThreshold_BothAdvance()
        {
            TrainProcessor.ApplyProgression(3, 5, out int init, out int rec);
            Assert.Equal(4, init);
            Assert.Equal(6, rec);
        }

        [Fact]
        public void ApplyProgression_BothAtZero_BothAdvance()
        {
            TrainProcessor.ApplyProgression(0, 0, out int init, out int rec);
            Assert.Equal(1, init);
            Assert.Equal(1, rec);
        }

        [Fact]
        public void ApplyProgression_LowBelowThresholdHighAtThreshold_OnlyLowAdvances()
        {
            TrainProcessor.ApplyProgression(9, 10, out int init, out int rec);
            Assert.Equal(10, init);
            Assert.Equal(10, rec);
        }

        [Fact]
        public void ApplyProgression_MixedTutorRecipientHigh_OnlyInitiatorAdvances()
        {
            TrainProcessor.ApplyProgression(5, 20, out int init, out int rec);
            Assert.Equal(6, init);
            Assert.Equal(20, rec);
        }

        [Fact]
        public void ApplyProgression_MixedTutorInitiatorHigh_OnlyRecipientAdvances()
        {
            TrainProcessor.ApplyProgression(20, 5, out int init, out int rec);
            Assert.Equal(20, init);
            Assert.Equal(6, rec);
        }

        [Fact]
        public void ApplyProgression_EqualAtThreshold_BothAdvance()
        {
            TrainProcessor.ApplyProgression(10, 10, out int init, out int rec);
            Assert.Equal(11, init);
            Assert.Equal(11, rec);
        }

        [Fact]
        public void ApplyProgression_EqualHigh_BothAdvance()
        {
            TrainProcessor.ApplyProgression(50, 50, out int init, out int rec);
            Assert.Equal(51, init);
            Assert.Equal(51, rec);
        }

        [Fact]
        public void ApplyProgression_AdjacentHighSmallGap_OnlyLowerAdvances()
        {
            TrainProcessor.ApplyProgression(45, 50, out int init, out int rec);
            Assert.Equal(46, init);
            Assert.Equal(50, rec);
        }

        [Fact]
        public void ApplyProgression_AdjacentHighWideGap_OnlyLowerAdvances()
        {
            // Per spec assumptions: above gap-10, lower still advances; higher stays.
            TrainProcessor.ApplyProgression(30, 50, out int init, out int rec);
            Assert.Equal(31, init);
            Assert.Equal(50, rec);
        }

        [Fact]
        public void ApplyProgression_AtCap_DoesNotAdvancePastHundred()
        {
            TrainProcessor.ApplyProgression(100, 100, out int init, out int rec);
            Assert.Equal(100, init);
            Assert.Equal(100, rec);
        }

        [Fact]
        public void ApplyProgression_HigherAtCap_LowerStillAdvances()
        {
            TrainProcessor.ApplyProgression(50, 100, out int init, out int rec);
            Assert.Equal(51, init);
            Assert.Equal(100, rec);
        }

        // ─── Validation tests ─────────────────────────────────────────────────

        [Fact]
        public void ValidateInteraction_EmptyTraining_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_UnknownTraining_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "fakeunknown");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_IdentifierMissingTrainingCategory_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "musk",
                description = "musk",
                categories = new[] { "scent" }
            });

            var result = _processor.ValidateInteraction("Alice", "Bob", "musk");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_KnownTraining_ReturnsSuccess()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "magic",
                description = "magic",
                categories = new[] { "training" }
            });

            var result = _processor.ValidateInteraction("Alice", "Bob", "magic");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_SelfTarget_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "magic",
                categories = new[] { "training" }
            });

            var result = _processor.ValidateInteraction("Alice", "Alice", "magic");

            Assert.False(result.IsValid);
        }

        // ─── ProcessInteraction tests ─────────────────────────────────────────

        [Fact]
        public void ProcessInteraction_BothFromZero_AdvancesBothByOne()
        {
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            Assert.Equal(1, alice.trainings["magic"]);
            Assert.Equal(1, bob.trainings["magic"]);
        }

        [Fact]
        public void ProcessInteraction_MixedTutorHighRecipient_OnlyInitiatorAdvances()
        {
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithTrainingLevel("magic", 30).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithTrainingLevel("magic", 50).BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            Assert.Equal(31, alice.trainings["magic"]);
            Assert.Equal(50, bob.trainings["magic"]);
        }

        [Fact]
        public void ProcessInteraction_EqualHighLevels_BothAdvance()
        {
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithTrainingLevel("magic", 50).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithTrainingLevel("magic", 50).BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            Assert.Equal(51, alice.trainings["magic"]);
            Assert.Equal(51, bob.trainings["magic"]);
        }

        [Fact]
        public void ProcessInteraction_CapAtHundred()
        {
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithTrainingLevel("magic", 100).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithTrainingLevel("magic", 100).BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            Assert.Equal(100, alice.trainings["magic"]);
            Assert.Equal(100, bob.trainings["magic"]);
        }

        [Fact]
        public void ProcessInteraction_SetsSymmetricPairCooldown()
        {
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            Assert.True(alice.timers.ContainsKey(TrainProcessor.PairTimerKey("Bob")));
            Assert.True(bob.timers.ContainsKey(TrainProcessor.PairTimerKey("Alice")));
            Assert.True(alice.timers[TrainProcessor.PairTimerKey("Bob")].timerEnd > DateTime.UtcNow);
        }

        [Fact]
        public void ProcessInteraction_PairCooldownIsCrossTraining()
        {
            // Per spec: the (initiator, recipient) pair-lock binds across ALL training
            // types, not per-training. So training magic should set the same single
            // pair-key timer that would block a same-day flight session.
            SeedMagicIdentifier();
            _fixture.SeedIdentifier(new Identifier { type = "flight", categories = new[] { "training" } });

            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            var alice = _database.GetProfile("Alice");
            // The single pair-timer applies regardless of which training was used.
            Assert.True(alice.timers.ContainsKey(TrainProcessor.PairTimerKey("Bob")));
            // And there is NOT a separate per-training key.
            Assert.False(alice.timers.ContainsKey("train_pair_magic_Bob"));
        }

        [Fact]
        public void ProcessInteraction_IncrementsCountsOnBothSides()
        {
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            Assert.Equal(1, alice.counts["traingive"]);
            Assert.Equal(1, bob.counts["traintake"]);
        }

        [Fact]
        public void ProcessInteraction_RecordsInteractionInHistory()
        {
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            var interactions = _database.GetInteractionsByType("train");
            Assert.Single(interactions);
            Assert.Equal("Alice", interactions[0].initiator);
            Assert.Equal("Bob", interactions[0].recipient);
            Assert.Equal("magic", interactions[0].identifier);
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic"));
            _processor.ProcessInteraction(pending);

            Assert.Null(_database.GetPendingCommand(pending.Id));
        }

        // ─── Title-award tests ────────────────────────────────────────────────

        [Fact]
        public void ProcessInteraction_CrossingApprenticeThreshold_AwardsTitle()
        {
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithTrainingLevel("magic", 9).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithTrainingLevel("magic", 9).BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            var alice = _database.GetProfile("Alice");
            Assert.Contains(alice.titles, t => t.IsSystemTitle && t.titleText == "Magic Apprentice");
        }

        [Fact]
        public void ProcessInteraction_NotCrossingThreshold_AwardsNothing()
        {
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithTrainingLevel("magic", 5).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithTrainingLevel("magic", 5).BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            var alice = _database.GetProfile("Alice");
            Assert.DoesNotContain(alice.titles, t =>
                t.IsSystemTitle && (t.titleText.EndsWith(" Apprentice")
                                 || t.titleText.EndsWith(" Adept")
                                 || t.titleText.EndsWith(" Master")
                                 || t.titleText.EndsWith(" Grandmaster")));
        }

        [Fact]
        public void ProcessInteraction_CrossingGrandmasterThreshold_AwardsTitle()
        {
            // Both at 99 → equal high → both advance to 100, crossing the grandmaster
            // threshold for both. (And re-crossing the lower tiers wouldn't happen since
            // they're already past them.)
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithTrainingLevel("magic", 99).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithTrainingLevel("magic", 99).BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            Assert.Contains(alice.titles, t => t.IsSystemTitle && t.titleText == "Magic Grandmaster");
            Assert.Contains(bob.titles, t => t.IsSystemTitle && t.titleText == "Magic Grandmaster");
        }

        [Fact]
        public void ProcessInteraction_TitlesArePerTraining()
        {
            // Cross apprentice threshold in two different trainings — each should yield
            // a distinct title.
            SeedMagicIdentifier();
            _fixture.SeedIdentifier(new Identifier { type = "flight", categories = new[] { "training" } });

            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithTrainingLevel("magic", 9)
                .WithTrainingLevel("flight", 9).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithTrainingLevel("magic", 9)
                .WithTrainingLevel("flight", 9).BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            // Pair-lock will now block another Alice/Bob session same day, so train a
            // fresh pair for the flight title.
            new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol")
                .WithTrainingLevel("flight", 9).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Dan").WithDisplayName("Dan")
                .WithTrainingLevel("flight", 9).BuildAndSave(_database);
            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Carol", "Dan", "flight")));

            var alice = _database.GetProfile("Alice");
            var carol = _database.GetProfile("Carol");
            Assert.Contains(alice.titles, t => t.IsSystemTitle && t.titleText == "Magic Apprentice");
            Assert.DoesNotContain(alice.titles, t => t.IsSystemTitle && t.titleText == "Flight Apprentice");
            Assert.Contains(carol.titles, t => t.IsSystemTitle && t.titleText == "Flight Apprentice");
        }

        [Fact]
        public void ProcessInteraction_DoesNotDoubleAwardSameTitle()
        {
            // Once awarded, the title should not re-appear if level moves further up.
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithTrainingLevel("magic", 9)
                .WithTrainingLevel("flight", 0).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithTrainingLevel("magic", 9).BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic")));

            // Now Alice trains with someone else (different pair-lock) on the same day.
            new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol")
                .WithTrainingLevel("magic", 9).BuildAndSave(_database);
            // Re-read Alice so subsequent ProcessInteraction observes the latest titles.
            // (BuildAndSave for Alice was earlier; her profile in DB now has level=10 +
            // apprentice title.)
            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Carol", "magic")));

            var alice = _database.GetProfile("Alice");
            int apprenticeCount = alice.titles.Count(t => t.IsSystemTitle && t.titleText == "Magic Apprentice");
            Assert.Equal(1, apprenticeCount);
        }

        // ─── Consent warning / completion message smoke tests ─────────────────

        [Fact]
        public void GetConsentWarning_HasApprovedShape()
        {
            SeedMagicIdentifier();
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithTrainingLevel("magic", 30).BuildAndSave(_database);
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithTrainingLevel("magic", 50).BuildAndSave(_database);

            string warning = _processor.GetConsentWarning(alice, bob, "magic");

            Assert.Equal("Alice wants to do some magic training with Bob! Do you !consent to improving your skills together?", warning);
        }

        [Fact]
        public void GetCompletionMessage_BothAdvance_UsesBothPracticedPhrasing()
        {
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithTrainingLevel("magic", 5).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithTrainingLevel("magic", 5).BuildAndSave(_database);

            var pending = SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic"));
            _processor.ProcessInteraction(pending);
            var aliceAfter = _database.GetProfile("Alice");
            var bobAfter = _database.GetProfile("Bob");
            string completion = _processor.GetCompletionMessage(aliceAfter, bobAfter, "magic");

            Assert.Equal("Alice and Bob do some magic training! They both feel a little more practiced now.", completion);
        }

        [Fact]
        public void GetCompletionMessage_AsymmetricCase_UsesTutoringPhrasing()
        {
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithTrainingLevel("magic", 30).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithTrainingLevel("magic", 50).BuildAndSave(_database);

            var pending = SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic"));
            _processor.ProcessInteraction(pending);
            var aliceAfter = _database.GetProfile("Alice");
            var bobAfter = _database.GetProfile("Bob");
            string completion = _processor.GetCompletionMessage(aliceAfter, bobAfter, "magic");

            // Bob (recipient) is the tutor here — he stayed at 50 while Alice advanced.
            Assert.Equal("Alice and Bob do some magic training! Bob was a lot more knowledgeable, so only Alice benefitted from the time spent.", completion);
        }

        [Fact]
        public void GetCompletionMessage_NeitherAdvanced_UsesShowingOffPhrasing()
        {
            // Both at the cap — neither can advance.
            SeedMagicIdentifier();
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithTrainingLevel("magic", 100).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithTrainingLevel("magic", 100).BuildAndSave(_database);

            var pending = SaveAndReturn(BuildPendingCommand("Alice", "Bob", "magic"));
            _processor.ProcessInteraction(pending);
            var aliceAfter = _database.GetProfile("Alice");
            var bobAfter = _database.GetProfile("Bob");
            string completion = _processor.GetCompletionMessage(aliceAfter, bobAfter, "magic");

            Assert.Equal("Alice and Bob do some magic training! They're both just showing off at this point, there's not much more for them to learn.", completion);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private PendingCommand BuildPendingCommand(string initiator, string recipient, string training)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = "train",
                    identifier = training,
                    investmentLevel = "commitment"
                }
            };
        }

        private PendingCommand SaveAndReturn(PendingCommand pending)
        {
            _database.AddPendingCommand(pending);
            return pending;
        }

        private void SeedMagicIdentifier()
        {
            _fixture.SeedIdentifier(new Identifier
            {
                type = "magic",
                description = "magic",
                categories = new[] { "training" }
            });
        }
    }
}
