using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for !purge (ChateauPurge). Mirrors the ChateauDetox shape — pure ExecutePurge
    /// against the database fixture. Cost-application paths are covered indirectly through
    /// the channel message; per-cost-type unit tests live in PurgeCostApplierTests.
    /// </summary>
    [Collection("Database")]
    public class ChateauPurgeTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ChateauPurgeTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;

            // Seed breakable parts so RandomBreak (used by tentacles) has something to pick.
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
            // Seed parasite identifiers referenced by tests below — only needed for rendering
            // lookups in error paths; purge itself doesn't read the identifier for the
            // success case.
            _fixture.SeedIdentifier(new Identifier
            {
                type = "lustleeches",
                description = "heart-shaped lust leeches",
                categories = new[] { InfestProcessor.ParasiteCategory },
            });
        }

        public void Dispose() { }

        [Fact]
        public void ExecutePurge_NoParasites_PrivateMessageOnly()
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var result = ChateauPurge.ExecutePurge(_database, "Bob", specifiedParasite: null);

            Assert.Empty(result.ChannelMessage);
            Assert.Contains("aren't currently infested", result.PrivateMessage);
            Assert.Contains("!infest you", result.PrivateMessage);
        }

        [Fact]
        public void ExecutePurge_LustLeeches_RendersWithColorTag()
        {
            SeedInfestedBob("lustleeches", spread: false);

            var result = ChateauPurge.ExecutePurge(_database, "Bob", "lustleeches", new Random(1));

            // The rendered name should be the styled "lust leeches" phrase, not the raw token.
            Assert.Contains("[color=purple]lust leeches[/color]", result.PrivateMessage);
            Assert.DoesNotContain("purges lustleeches", result.PrivateMessage);
        }

        [Fact]
        public void ExecutePurge_SingleParasite_NoArg_PurgesAtCost()
        {
            SeedInfestedBob("paraslime", spread: false);

            var result = ChateauPurge.ExecutePurge(_database, "Bob", specifiedParasite: null, new Random(1));

            // Purge outcomes go to the private message (detox precedent) — channel stays silent.
            Assert.Empty(result.ChannelMessage);
            Assert.Contains("purges paraslime", result.PrivateMessage);
            // paraslime → MissedWork → "work" timer set.
            var bob = _database.GetProfile("Bob");
            Assert.True(bob.timers.ContainsKey("work"));
            Assert.Empty(ParasiteInstance.LoadAll(bob));
        }

        [Fact]
        public void ExecutePurge_MultipleParasites_NoArg_AsksWhich()
        {
            SeedInfestedBob("paraslime", spread: false);
            ApplyAdditional("Bob", "tentacles");

            var result = ChateauPurge.ExecutePurge(_database, "Bob", specifiedParasite: null, new Random(1));

            Assert.Empty(result.ChannelMessage);
            Assert.Contains("more than one parasite", result.PrivateMessage);
            Assert.Contains("paraslime", result.PrivateMessage);
            Assert.Contains("tentacles", result.PrivateMessage);
            // Nothing was purged.
            Assert.Equal(2, ParasiteInstance.LoadAll(_database.GetProfile("Bob")).Count);
        }

        [Fact]
        public void ExecutePurge_NamedParasite_PurgesAtParasiteSpecificCost()
        {
            SeedInfestedBob("paraslime", spread: false);
            ApplyAdditional("Bob", "tentacles");

            var result = ChateauPurge.ExecutePurge(_database, "Bob", "tentacles", new Random(1));

            Assert.Contains("purges tentacles", result.PrivateMessage);
            Assert.Empty(result.ChannelMessage);
            // tentacles → RandomBreak → break instance appended.
            var bob = _database.GetProfile("Bob");
            Assert.NotEmpty(BreakInstance.LoadAllWithTick(bob));
            // paraslime is still there.
            var parasites = ParasiteInstance.LoadAll(bob);
            Assert.Single(parasites);
            Assert.Equal("paraslime", parasites[0].Parasite);
        }

        [Fact]
        public void ExecutePurge_NamedParasite_NotPresent_ListsInventory()
        {
            SeedInfestedBob("paraslime", spread: false);

            var result = ChateauPurge.ExecutePurge(_database, "Bob", "tentacles", new Random(1));

            Assert.Empty(result.ChannelMessage);
            Assert.Contains("aren't currently infested with tentacles", result.PrivateMessage);
            Assert.Contains("paraslime", result.PrivateMessage);
        }

        [Fact]
        public void ExecutePurge_NamedParasite_CaseInsensitive()
        {
            SeedInfestedBob("paraslime", spread: false);

            var result = ChateauPurge.ExecutePurge(_database, "Bob", "PARASLIME", new Random(1));

            Assert.Contains("purges paraslime", result.PrivateMessage);
            Assert.Empty(result.ChannelMessage);
            Assert.Empty(ParasiteInstance.LoadAll(_database.GetProfile("Bob")));
        }

        [Fact]
        public void ExecutePurge_SpreadInfestation_WithinGrace_NoCost()
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            var bob = _database.GetProfile("Bob");
            InfestProcessor.ApplyInfestation(bob, "paraslime", "Alice",
                spreadFromContact: true, gracePeriod: TimeSpan.FromHours(24));
            _database.SetProfile("Bob", bob);

            var result = ChateauPurge.ExecutePurge(_database, "Bob", "paraslime", new Random(1));

            Assert.Contains("catches the paraslime infestation early", result.PrivateMessage);
            Assert.Empty(result.ChannelMessage);
            // No work timer was set since cost was skipped.
            var bobAfter = _database.GetProfile("Bob");
            Assert.False(bobAfter.timers.ContainsKey("work"));
            Assert.Empty(ParasiteInstance.LoadAll(bobAfter));
        }

        [Fact]
        public void ExecutePurge_SpreadInfestation_PastGrace_AppliesCost()
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            var bob = _database.GetProfile("Bob");
            InfestProcessor.ApplyInfestation(bob, "paraslime", "Alice",
                spreadFromContact: true, gracePeriod: TimeSpan.FromHours(24));
            // Push GraceUntil into the past so we're outside the window.
            var entries = ParasiteInstance.LoadAll(bob);
            entries[0].GraceUntil = DateTime.UtcNow.AddHours(-1);
            ParasiteInstance.SaveAll(bob, entries);
            _database.SetProfile("Bob", bob);

            var result = ChateauPurge.ExecutePurge(_database, "Bob", "paraslime", new Random(1));

            // Cost applied — paraslime is MissedWork by default.
            Assert.Contains("purges paraslime", result.PrivateMessage);
            Assert.Empty(result.ChannelMessage);
            var bobAfter = _database.GetProfile("Bob");
            Assert.True(bobAfter.timers.ContainsKey("work"));
        }

        [Fact]
        public void ExecutePurge_UnknownUser_PrivateMessage()
        {
            var result = ChateauPurge.ExecutePurge(_database, "Nobody", specifiedParasite: null);

            Assert.Empty(result.ChannelMessage);
            Assert.NotEmpty(result.PrivateMessage);
        }

        private void SeedInfestedBob(string parasite, bool spread)
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            var bob = _database.GetProfile("Bob");
            InfestProcessor.ApplyInfestation(bob, parasite, "Alice",
                spreadFromContact: spread,
                gracePeriod: spread ? TimeSpan.FromHours(24) : TimeSpan.Zero);
            _database.SetProfile("Bob", bob);
        }

        private void ApplyAdditional(string userName, string parasite)
        {
            var profile = _database.GetProfile(userName);
            InfestProcessor.ApplyInfestation(profile, parasite, "Alice",
                spreadFromContact: false, gracePeriod: TimeSpan.Zero);
            _database.SetProfile(userName, profile);
        }
    }
}
