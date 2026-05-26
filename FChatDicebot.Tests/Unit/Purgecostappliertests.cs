using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Tests for PurgeCostApplier — the shared cost engine used by !detox now and the
    /// future !purge / !cleanse. Each cost type has its own happy-path test, plus the
    /// fallback cases (RandomCurse degrading to RandomBreak, LostTrainingPoint degrading
    /// to MissedWork when the caller has no trained skills).
    /// </summary>
    [Collection("Database")]
    public class PurgeCostApplierTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public PurgeCostApplierTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;

            _fixture.SeedIdentifier(new Identifier
            {
                type = "arm",
                description = "arm",
                categories = new[] { "break" },
            });
        }

        public void Dispose() { }

        [Fact]
        public void Apply_MissedWork_SetsWorkTimerToNextMidnight()
        {
            var bob = MakeBob();

            var result = PurgeCostApplier.Apply(_database, bob, PurgeCostType.MissedWork);

            Assert.True(result.Applied);
            Assert.Contains("!work", result.Description);
            var reloaded = _database.GetProfile("Bob");
            Assert.True(reloaded.timers.ContainsKey("work"));
            // timerEnd should be the next UTC midnight (within a generous bound).
            DateTime expected = DateTime.UtcNow.Date.AddDays(1);
            Assert.True(Math.Abs((reloaded.timers["work"].timerEnd - expected).TotalSeconds) < 5);
        }

        [Fact]
        public void Apply_RandomBreak_AppliesBreakAndPicksFromCatalog()
        {
            var bob = MakeBob();

            var result = PurgeCostApplier.Apply(_database, bob, PurgeCostType.RandomBreak, new Random(1));

            Assert.True(result.Applied);
            var reloaded = _database.GetProfile("Bob");
            var breaks = BreakInstance.LoadAllWithTick(reloaded);
            Assert.Single(breaks);
            // Catalog has only "arm" — so it must be the picked part.
            Assert.Equal("arm", breaks[0].Part);
            // Duration is in [1, 3].
            Assert.InRange(breaks[0].Severity,
                PurgeCostApplier.RandomBreakMinDays, PurgeCostApplier.RandomBreakMaxDays);
        }

        [Fact]
        public void Apply_RandomCurse_DegradesToRandomBreak_WithWitchFraming()
        {
            // Curse-and-Cleanse hasn't shipped — RandomCurse must not silently no-op. The
            // applier degrades to a random-break cost wrapped in the user-picked "witch"
            // narrative so the substitution reads as in-universe flavor rather than a TODO.
            // When the curse system lands, swap this branch for the real curse and the
            // assertion should track the new wording.
            var bob = MakeBob();

            var result = PurgeCostApplier.Apply(_database, bob, PurgeCostType.RandomCurse, new Random(1));

            Assert.True(result.Applied);
            Assert.Contains("ask a witch for help", result.Description);
            Assert.Contains("a broken arm", result.Description); // catalog has only "arm"
            var reloaded = _database.GetProfile("Bob");
            Assert.Single(BreakInstance.LoadAllWithTick(reloaded));
        }

        [Fact]
        public void Apply_LostTrainingPoint_DecrementsTraining()
        {
            var bob = MakeBob();
            bob.trainings["magic"] = 3;
            _database.SetProfile("Bob", bob);

            var result = PurgeCostApplier.Apply(_database, bob, PurgeCostType.LostTrainingPoint, new Random(1));

            Assert.True(result.Applied);
            var reloaded = _database.GetProfile("Bob");
            Assert.Equal(2, reloaded.trainings["magic"]);
            Assert.Contains("magic", result.Description);
        }

        [Fact]
        public void Apply_LostTrainingPoint_AllZero_DegradesToMissedWork()
        {
            var bob = MakeBob();
            bob.trainings["magic"] = 0;
            _database.SetProfile("Bob", bob);

            var result = PurgeCostApplier.Apply(_database, bob, PurgeCostType.LostTrainingPoint, new Random(1));

            Assert.True(result.Applied);
            var reloaded = _database.GetProfile("Bob");
            Assert.True(reloaded.timers.ContainsKey("work"));
            Assert.Equal(0, reloaded.trainings["magic"]); // unchanged
        }

        [Fact]
        public void Apply_LostTrainingPoint_NoTrainingsAtAll_DegradesToMissedWork()
        {
            var bob = MakeBob();
            // trainings is empty by default — no skills to lose.

            var result = PurgeCostApplier.Apply(_database, bob, PurgeCostType.LostTrainingPoint, new Random(1));

            Assert.True(result.Applied);
            var reloaded = _database.GetProfile("Bob");
            Assert.True(reloaded.timers.ContainsKey("work"));
        }

        [Fact]
        public void Apply_NullProfile_NoOp()
        {
            var result = PurgeCostApplier.Apply(_database, null, PurgeCostType.MissedWork);
            Assert.False(result.Applied);
        }

        [Fact]
        public void Apply_RandomBreak_EmptyCatalog_DegradesToMissedWork()
        {
            // Clear the seeded "arm" so the break catalog is empty.
            _database.ClearCollection("Identifiers");
            var bob = MakeBob();

            var result = PurgeCostApplier.Apply(_database, bob, PurgeCostType.RandomBreak, new Random(1));

            Assert.True(result.Applied);
            var reloaded = _database.GetProfile("Bob");
            Assert.True(reloaded.timers.ContainsKey("work"));
        }

        private Profile MakeBob()
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            return _database.GetProfile("Bob");
        }
    }
}
