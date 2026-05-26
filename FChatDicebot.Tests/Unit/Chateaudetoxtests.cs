using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for the !detox self-command. Mirrors the ChateauWashTests shape — pure
    /// ExecuteDetox helper against the database fixture so we don't have to spin up the
    /// bot. Cost-application paths are covered indirectly through the channel message;
    /// see PurgeCostApplierTests for the per-cost-type unit tests.
    /// </summary>
    [Collection("Database")]
    public class ChateauDetoxTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ChateauDetoxTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;

            // Seed a couple of breakable parts so the RandomBreak cost can pick one.
            _fixture.SeedIdentifier(new Identifier
            {
                type = "arm",
                description = "arm",
                categories = new[] { "break" },
            });
            _fixture.SeedIdentifier(new Identifier
            {
                type = "leg",
                description = "leg",
                categories = new[] { "break" },
            });
        }

        public void Dispose() { }

        [Fact]
        public void ExecuteDetox_NoVices_PrivateMessageOnly()
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var result = ChateauDetox.ExecuteDetox(_database, "Bob", specifiedVice: null);

            Assert.Empty(result.ChannelMessage);
            Assert.Contains("don't currently have any addictive vices", result.PrivateMessage);
        }

        [Fact]
        public void ExecuteDetox_NamedVice_RemovedAndCostApplied()
        {
            SeedDosedBob("musk", level: 3);
            // MissedWork has a deterministic effect (work timer) so we use that here —
            // RandomBreak is tested in PurgeCostApplierTests with a seeded RNG.
            var result = ChateauDetox.ExecuteDetox(_database, "Bob", "musk", PurgeCostType.MissedWork, new Random(1));

            // Success is delivered as a private message — no channel announcement of who is
            // suffering withdrawals.
            Assert.Empty(result.ChannelMessage);
            Assert.Contains("manages to avoid musk for a whole day", result.PrivateMessage);
            Assert.Contains("!work", result.PrivateMessage);

            var bob = _database.GetProfile("Bob");
            Assert.Empty(ViceInstance.LoadAll(bob));
            Assert.True(bob.timers.ContainsKey("work"));
        }

        [Fact]
        public void ExecuteDetox_DefaultTarget_PicksHighestLevel()
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            var bob = _database.GetProfile("Bob");
            DoseProcessor.ApplyDose(bob, "musk", "Alice"); // level 1
            DoseProcessor.ApplyDose(bob, "drug", "Alice"); // level 1
            DoseProcessor.ApplyDose(bob, "drug", "Alice"); // level 2 — highest
            DoseProcessor.ApplyDose(bob, "liquor", "Alice"); // level 1
            _database.SetProfile("Bob", bob);

            var result = ChateauDetox.ExecuteDetox(_database, "Bob", specifiedVice: null, PurgeCostType.MissedWork, new Random(1));

            Assert.Contains("manages to avoid drug for a whole day", result.PrivateMessage);
            var after = ViceInstance.LoadAll(_database.GetProfile("Bob"));
            Assert.Equal(2, after.Count);
            Assert.DoesNotContain(after, v => v.Vice == "drug");
        }

        [Fact]
        public void ExecuteDetox_DefaultTarget_TiesBrokenByEarliestFirstDosedAt()
        {
            // Two vices at the same level — the older one wins (FirstDosedAt earlier).
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            var bob = _database.GetProfile("Bob");
            DoseProcessor.ApplyDose(bob, "musk", "Alice");
            // Push musk back in time so it's clearly older than the next dose.
            var entries = ViceInstance.LoadAll(bob);
            entries[0].FirstDosedAt = DateTime.UtcNow.AddDays(-3);
            ViceInstance.SaveAll(bob, entries);
            DoseProcessor.ApplyDose(bob, "drug", "Alice");
            _database.SetProfile("Bob", bob);

            var result = ChateauDetox.ExecuteDetox(_database, "Bob", specifiedVice: null, PurgeCostType.MissedWork, new Random(1));

            Assert.Contains("manages to avoid musk for a whole day", result.PrivateMessage);
        }

        [Fact]
        public void ExecuteDetox_NamedVice_NotPresent_PrivateMessageListsInventory()
        {
            SeedDosedBob("musk", level: 1);

            var result = ChateauDetox.ExecuteDetox(_database, "Bob", specifiedVice: "drug");

            Assert.Empty(result.ChannelMessage);
            Assert.Contains("aren't currently addicted to drug", result.PrivateMessage);
            Assert.Contains("musk", result.PrivateMessage);
        }

        [Fact]
        public void ExecuteDetox_UnknownUser_PrivateMessage()
        {
            var result = ChateauDetox.ExecuteDetox(_database, "Nobody", specifiedVice: null);

            Assert.Empty(result.ChannelMessage);
            Assert.NotEmpty(result.PrivateMessage);
        }

        [Fact]
        public void ExecuteDetox_NamedVice_CaseInsensitive()
        {
            SeedDosedBob("musk", level: 1);

            var result = ChateauDetox.ExecuteDetox(_database, "Bob", "MUSK", PurgeCostType.MissedWork, new Random(1));

            Assert.Contains("manages to avoid musk for a whole day", result.PrivateMessage);
            Assert.Empty(ViceInstance.LoadAll(_database.GetProfile("Bob")));
        }

        private void SeedDosedBob(string vice, int level)
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            var bob = _database.GetProfile("Bob");
            for (int i = 0; i < level; i++)
            {
                DoseProcessor.ApplyDose(bob, vice, "Alice");
            }
            _database.SetProfile("Bob", bob);
        }
    }
}
