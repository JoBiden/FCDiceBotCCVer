using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for the !cleanse self-command. Mirrors the ChateauDetox / ChateauPurge shape —
    /// pure ExecuteCleanse helper against the database fixture, no bot spin-up. Cost-
    /// application paths are covered by PurgeCostApplierTests.
    /// </summary>
    [Collection("Database")]
    public class ChateauCleanseTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ChateauCleanseTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;

            // Seed a couple of breakable parts so the RandomBreak cost path (e.g. for the
            // hunger / horny curses) can resolve.
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
        public void ExecuteCleanse_NoCurses_PrivateMessageOnly()
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var result = ChateauCleanse.ExecuteCleanse(_database, "Bob", specifiedCurse: null);

            Assert.Empty(result.ChannelMessage);
            Assert.Contains("aren't currently burdened by any curses", result.PrivateMessage);
        }

        [Fact]
        public void ExecuteCleanse_OneCurseNoArg_RemovesAndAppliesCost()
        {
            // Chastity's cleanse cost is MissedWork — deterministic, so we don't need a
            // seeded rng to assert the after-state.
            SeedCursedBob("chastity");

            var result = ChateauCleanse.ExecuteCleanse(_database, "Bob", null, new Random(1));

            Assert.Empty(result.ChannelMessage);
            Assert.Contains("Bob is cleansed of the [b]chastity[/b] curse", result.PrivateMessage);
            // MissedWork → work timer set.
            var bob = _database.GetProfile("Bob");
            Assert.Empty(CurseInstance.LoadAll(bob));
            Assert.True(bob.timers.ContainsKey("work"));
        }

        [Fact]
        public void ExecuteCleanse_NamedCurse_RemovesNamedOnly()
        {
            SeedCursedBob("mooing");
            var bob = _database.GetProfile("Bob");
            CurseProcessor.ApplyCurse(bob, "blushing", "Alice");
            _database.SetProfile("Bob", bob);

            var result = ChateauCleanse.ExecuteCleanse(_database, "Bob", "mooing", new Random(1));

            Assert.Contains("[b]mooing[/b]", result.PrivateMessage);
            bob = _database.GetProfile("Bob");
            var remaining = CurseInstance.LoadAll(bob);
            Assert.Single(remaining);
            Assert.Equal("blushing", remaining[0].Curse);
        }

        [Fact]
        public void ExecuteCleanse_MultipleCursesNoArg_AsksWhich()
        {
            SeedCursedBob("mooing");
            var bob = _database.GetProfile("Bob");
            CurseProcessor.ApplyCurse(bob, "blushing", "Alice");
            _database.SetProfile("Bob", bob);

            var result = ChateauCleanse.ExecuteCleanse(_database, "Bob", null, new Random(1));

            Assert.Empty(result.ChannelMessage);
            Assert.Contains("multiple curses", result.PrivateMessage);
            Assert.Contains("mooing", result.PrivateMessage);
            Assert.Contains("blushing", result.PrivateMessage);
            // No curse removed when ambiguous.
            bob = _database.GetProfile("Bob");
            Assert.Equal(2, CurseInstance.LoadAll(bob).Count);
        }

        [Fact]
        public void ExecuteCleanse_NamedCurseNotCarried_RefusesAndListsOptions()
        {
            SeedCursedBob("mooing");

            var result = ChateauCleanse.ExecuteCleanse(_database, "Bob", "chastity", new Random(1));

            Assert.Contains("aren't currently cursed with [b]chastity[/b]", result.PrivateMessage);
            Assert.Contains("mooing", result.PrivateMessage);
            // Nothing removed.
            var bob = _database.GetProfile("Bob");
            Assert.Single(CurseInstance.LoadAll(bob));
        }

        [Fact]
        public void ExecuteCleanse_CurseInstance_PersistsRemoval()
        {
            SeedCursedBob("vibrating");

            ChateauCleanse.ExecuteCleanse(_database, "Bob", "vibrating", new Random(1));

            var bob = _database.GetProfile("Bob");
            Assert.Empty(CurseInstance.LoadAll(bob));
        }

        [Fact]
        public void ExecuteCleanse_OutputIsPrivateNotChannel()
        {
            // Mirrors !detox / !purge precedent — cleansing should not broadcast in channel.
            SeedCursedBob("chastity");

            var result = ChateauCleanse.ExecuteCleanse(_database, "Bob", null, new Random(1));

            Assert.Empty(result.ChannelMessage);
            Assert.False(string.IsNullOrEmpty(result.PrivateMessage));
        }

        [Fact]
        public void ExecuteCleanse_UnknownCallerProfile_PrivateRefusal()
        {
            var result = ChateauCleanse.ExecuteCleanse(_database, "Nobody", null);

            Assert.Empty(result.ChannelMessage);
            Assert.False(string.IsNullOrEmpty(result.PrivateMessage));
        }

        private void SeedCursedBob(string curseName)
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            var bob = _database.GetProfile("Bob");
            CurseProcessor.ApplyCurse(bob, curseName, "Alice");
            _database.SetProfile("Bob", bob);
        }
    }
}
