using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.BotCommands
{
    [Collection("Database")]
    public class ChateauBirthTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ChateauBirthTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        [Fact]
        public void ExecuteBirth_NoPregnancies_ReturnsError()
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            Assert.Equal(string.Empty, result.ChannelMessage);
            Assert.Contains("aren't pregnant", result.PrivateMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExecuteBirth_PregnancyNotReady_ReturnsErrorWithTimeRemaining()
        {
            CreateCarrierWithPregnancies("Bob",
                BuildPregnancy("Alice", "slime", conceivedDaysAgo: 0, readyInDays: 3, broodSize: 1));

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            Assert.Equal(string.Empty, result.ChannelMessage);
            Assert.Contains("ready", result.PrivateMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExecuteBirth_SingleReady_BirthsIt()
        {
            CreateCarrierWithPregnancies("Bob",
                BuildPregnancy("Alice", "slime", conceivedDaysAgo: 5, readyInDays: -2, broodSize: 1));

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            Assert.Equal(string.Empty, result.PrivateMessage);
            Assert.Contains("slime", result.ChannelMessage);

            var bob = _database.GetProfile("Bob");
            Assert.Empty(bob.pregnancies);
        }

        [Fact]
        public void ExecuteBirth_MultipleReady_NoIndex_ListsAllReadyPrivately()
        {
            CreateCarrierWithPregnancies("Bob",
                BuildPregnancy("Alice", "slime", conceivedDaysAgo: 5, readyInDays: -2, broodSize: 1),
                BuildPregnancy("Carol", "wasp", conceivedDaysAgo: 3, readyInDays: -1, broodSize: 4));

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            // Channel gets a short prompt; private gets the numbered list.
            Assert.Contains("multiple pregnancies", result.ChannelMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Bob", result.ChannelMessage);
            Assert.Contains("1:", result.PrivateMessage);
            Assert.Contains("2:", result.PrivateMessage);
            Assert.Contains("slime", result.PrivateMessage);
            Assert.Contains("wasp", result.PrivateMessage);
            Assert.Contains("Alice", result.PrivateMessage);
            Assert.Contains("Carol", result.PrivateMessage);

            // No actual birthing happened.
            var bob = _database.GetProfile("Bob");
            Assert.Equal(2, bob.pregnancies.Count);
        }

        [Fact]
        public void ExecuteBirth_MultipleReady_NotReadyOnesExcludedFromList()
        {
            CreateCarrierWithPregnancies("Bob",
                BuildPregnancy("Alice", "slime", conceivedDaysAgo: 5, readyInDays: -2, broodSize: 1),
                BuildPregnancy("Carol", "wasp", conceivedDaysAgo: 0, readyInDays: 3, broodSize: 4), // not ready
                BuildPregnancy("Dave", "kobold", conceivedDaysAgo: 4, readyInDays: -1, broodSize: 2));

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            // Two ready → multi-listing path.
            Assert.Contains("slime", result.PrivateMessage);
            Assert.Contains("kobold", result.PrivateMessage);
            Assert.DoesNotContain("wasp", result.PrivateMessage);
        }

        [Fact]
        public void ExecuteBirth_ExplicitIndex_BirthsThatPregnancy()
        {
            CreateCarrierWithPregnancies("Bob",
                BuildPregnancy("Alice", "slime", conceivedDaysAgo: 5, readyInDays: -2, broodSize: 1),
                BuildPregnancy("Carol", "wasp", conceivedDaysAgo: 3, readyInDays: -1, broodSize: 4));

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new[] { "2" });

            Assert.Contains("wasp", result.ChannelMessage);
            Assert.DoesNotContain("multiple pregnancies", result.ChannelMessage, StringComparison.OrdinalIgnoreCase);

            var bob = _database.GetProfile("Bob");
            Assert.Single(bob.pregnancies);
            Assert.Equal("slime", bob.pregnancies[0].MonsterType);
        }

        [Fact]
        public void ExecuteBirth_IndexOutOfRange_ReturnsError()
        {
            CreateCarrierWithPregnancies("Bob",
                BuildPregnancy("Alice", "slime", conceivedDaysAgo: 2, readyInDays: -1, broodSize: 1));

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new[] { "5" });

            Assert.Equal(string.Empty, result.ChannelMessage);
            Assert.Contains("out of range", result.PrivateMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExecuteBirth_All_BirthsEveryReady()
        {
            CreateCarrierWithPregnancies("Bob",
                BuildPregnancy("Alice", "slime", conceivedDaysAgo: 5, readyInDays: -2, broodSize: 1),
                BuildPregnancy("Carol", "wasp", conceivedDaysAgo: 3, readyInDays: -1, broodSize: 4),
                BuildPregnancy("Dave", "kobold", conceivedDaysAgo: 0, readyInDays: 3, broodSize: 1)); // not ready

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new[] { "all" });

            Assert.Equal(string.Empty, result.PrivateMessage);
            Assert.Contains("slime", result.ChannelMessage);
            Assert.Contains("wasp", result.ChannelMessage);
            Assert.DoesNotContain("kobold", result.ChannelMessage);

            var bob = _database.GetProfile("Bob");
            Assert.Single(bob.pregnancies);
            Assert.Equal("kobold", bob.pregnancies[0].MonsterType);
            Assert.Equal(2, bob.counts["birth"]); // bumped per birth
        }

        [Fact]
        public void ExecuteBirth_AppendsOffspringListEntry()
        {
            CreateCarrierWithPregnancies("Bob",
                BuildPregnancy("Alice", "slime", conceivedDaysAgo: 2, readyInDays: -1, broodSize: 3));

            ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            var bob = _database.GetProfile("Bob");
            Assert.True(bob.lists.ContainsKey("offspring"));
            var entry = bob.lists["offspring"].Single();
            Assert.Contains("slime", entry);
            Assert.Contains("3", entry);
            Assert.Contains("Alice", entry);
            Assert.Equal(1, bob.counts["birth"]);
        }

        [Fact]
        public void ExecuteBirth_RareTwins_UsesSpecialPhrasing()
        {
            var twinPregnancy = BuildPregnancy("Alice", "imp", conceivedDaysAgo: 2, readyInDays: -1, broodSize: 2);
            twinPregnancy.IsRareTwins = true;
            CreateCarrierWithPregnancies("Bob", twinPregnancy);

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            Assert.Contains("twins", result.ChannelMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("miraculous", result.ChannelMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("imp", result.ChannelMessage);
        }

        [Fact]
        public void ExecuteBirth_BroodOfOne_UsesAnOrA()
        {
            CreateCarrierWithPregnancies("Bob",
                BuildPregnancy("Alice", "imp", conceivedDaysAgo: 2, readyInDays: -1, broodSize: 1));

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            Assert.Contains("imp", result.ChannelMessage);
            Assert.DoesNotContain("brood of", result.ChannelMessage);
            Assert.DoesNotContain("twins", result.ChannelMessage);
        }

        [Fact]
        public void ExecuteBirth_IncrementsGlobalOffspringCounters()
        {
            var pregnancy = BuildPregnancy("Alice", "lamia", conceivedDaysAgo: 2, readyInDays: -1, broodSize: 5);
            pregnancy.Categories = new List<string> { "monster", "snake", "beast" };
            CreateCarrierWithPregnancies("Bob", pregnancy);

            ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            var monsterStats = _database.GetMonsterStats(BreedProcessor.MonsterStatsKey("lamia"));
            Assert.NotNull(monsterStats);
            Assert.Equal(0, monsterStats.PregnancyCount); // birth doesn't bump pregnancy count
            Assert.Equal(5, monsterStats.OffspringCount);

            var snakeStats = _database.GetMonsterStats(BreedProcessor.CategoryStatsKey("snake"));
            Assert.Equal(5, snakeStats.OffspringCount);

            var beastStats = _database.GetMonsterStats(BreedProcessor.CategoryStatsKey("beast"));
            Assert.Equal(5, beastStats.OffspringCount);
        }

        [Fact]
        public void ExecuteBirth_All_AggregatesOffspringCountersAcrossBirths()
        {
            var p1 = BuildPregnancy("Alice", "slime", conceivedDaysAgo: 5, readyInDays: -2, broodSize: 3);
            p1.Categories = new List<string> { "slime" };
            var p2 = BuildPregnancy("Alice", "slime", conceivedDaysAgo: 4, readyInDays: -1, broodSize: 4);
            p2.Categories = new List<string> { "slime" };
            CreateCarrierWithPregnancies("Bob", p1, p2);

            ChateauBirth.ExecuteBirth(_database, "Bob", new[] { "all" });

            var stats = _database.GetMonsterStats(BreedProcessor.MonsterStatsKey("slime"));
            Assert.Equal(7, stats.OffspringCount);
            var slimeCat = _database.GetMonsterStats(BreedProcessor.CategoryStatsKey("slime"));
            Assert.Equal(7, slimeCat.OffspringCount);
        }

        [Fact]
        public void ExecuteBirth_UnknownUser_ReturnsError()
        {
            var result = ChateauBirth.ExecuteBirth(_database, "Ghost", new string[0]);
            Assert.Equal(string.Empty, result.ChannelMessage);
            Assert.NotEqual(string.Empty, result.PrivateMessage);
        }

        // ---- Mystery pregnancies (feedback 6a51d2fa): masked until birth reveals ----

        [Fact]
        public void BuildGestationStatusMessage_MysteryPregnancies_MaskTheSpecies()
        {
            var random = BuildPregnancy("Alice", "slime", conceivedDaysAgo: 0, readyInDays: 3, broodSize: 2);
            random.MysteryKind = Pregnancy.MysteryRandom;
            var mixed = BuildPregnancy("Carol", Pregnancy.MysteryMixed, conceivedDaysAgo: 0, readyInDays: 2, broodSize: 2);
            mixed.MysteryKind = Pregnancy.MysteryMixed;
            mixed.Children = new List<BroodChild>
            {
                new BroodChild { Species = "wasp" },
                new BroodChild { Species = "dragon" },
            };
            CreateCarrierWithPregnancies("Bob", random, mixed);

            string msg = ChateauBirth.BuildGestationStatusMessage(_database, "Bob");

            Assert.Contains("???", msg);
            Assert.Contains("mixed brood", msg);
            Assert.DoesNotContain("slime", msg);
            Assert.DoesNotContain("wasp", msg);
            Assert.DoesNotContain("dragon", msg);
        }

        [Fact]
        public void ExecuteBirth_ReadyList_MasksMysterySpecies()
        {
            var normal = BuildPregnancy("Alice", "wasp", conceivedDaysAgo: 5, readyInDays: -2, broodSize: 4);
            var random = BuildPregnancy("Carol", "slime", conceivedDaysAgo: 4, readyInDays: -1, broodSize: 1);
            random.MysteryKind = Pregnancy.MysteryRandom;
            CreateCarrierWithPregnancies("Bob", normal, random);

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            Assert.Contains("wasp", result.PrivateMessage);
            Assert.Contains("???", result.PrivateMessage);
            Assert.DoesNotContain("slime", result.PrivateMessage);
        }

        [Fact]
        public void ExecuteBirth_RandomMystery_RevealsTheSpecies()
        {
            var pregnancy = BuildPregnancy("Alice", "slime", conceivedDaysAgo: 5, readyInDays: -2, broodSize: 1);
            pregnancy.MysteryKind = Pregnancy.MysteryRandom;
            CreateCarrierWithPregnancies("Bob", pregnancy);

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            Assert.Contains("Who would have guessed?", result.ChannelMessage);
            Assert.Contains("slime", result.ChannelMessage);
        }

        [Fact]
        public void ExecuteBirth_MixedBrood_EnumeratesLitterAndCountsPerChild()
        {
            var pregnancy = BuildPregnancy("Alice", Pregnancy.MysteryMixed, conceivedDaysAgo: 5, readyInDays: -2, broodSize: 3);
            pregnancy.MysteryKind = Pregnancy.MysteryMixed;
            pregnancy.Children = new List<BroodChild>
            {
                new BroodChild { Species = "slime", Categories = new List<string> { "monster", "slime" } },
                new BroodChild { Species = "slime", Categories = new List<string> { "monster", "slime" } },
                new BroodChild { Species = "dragon", Categories = new List<string> { "monster", "dragon" } },
            };
            CreateCarrierWithPregnancies("Bob", pregnancy);

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            Assert.Contains("mixed brood of 3", result.ChannelMessage);
            Assert.Contains("2 slimes", result.ChannelMessage);
            Assert.Contains("a dragon", result.ChannelMessage);

            // Offspring counters land per child; "mixed" is not a monster.
            Assert.Equal(2, _database.GetMonsterStats(BreedProcessor.MonsterStatsKey("slime")).OffspringCount);
            Assert.Equal(1, _database.GetMonsterStats(BreedProcessor.MonsterStatsKey("dragon")).OffspringCount);
            Assert.Null(_database.GetMonsterStats(BreedProcessor.MonsterStatsKey("mixed")));
            Assert.Equal(3, _database.GetMonsterStats(BreedProcessor.CategoryStatsKey("monster")).OffspringCount);

            // The offspring log records the composition.
            var bob = _database.GetProfile("Bob");
            Assert.Contains(bob.lists["offspring"], entry => entry.Contains("mixed (slime, slime, dragon)"));
        }

        [Fact]
        public void ExecuteBirth_MixedBroodOfOne_UsesSoleChildWording()
        {
            var pregnancy = BuildPregnancy("Alice", Pregnancy.MysteryMixed, conceivedDaysAgo: 5, readyInDays: -2, broodSize: 1);
            pregnancy.MysteryKind = Pregnancy.MysteryMixed;
            pregnancy.Children = new List<BroodChild>
            {
                new BroodChild { Species = "dragon", Categories = new List<string> { "monster", "dragon" } },
            };
            CreateCarrierWithPregnancies("Bob", pregnancy);

            var result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0]);

            Assert.Contains("sole child", result.ChannelMessage);
            Assert.Contains("a dragon", result.ChannelMessage);
        }

        [Fact]
        public void DescribeMixedLitter_GroupsBySpeciesWithSerialComma()
        {
            var children = new List<BroodChild>
            {
                new BroodChild { Species = "slime" },
                new BroodChild { Species = "wasp" },
                new BroodChild { Species = "slime" },
                new BroodChild { Species = "imp" },
            };

            Assert.Equal("2 slimes, a wasp, and an imp", ChateauBirth.DescribeMixedLitter(children));
        }

        private void CreateCarrierWithPregnancies(string userName, params Pregnancy[] pregnancies)
        {
            var carrier = new ProfileBuilder()
                .WithUserName(userName)
                .WithDisplayName(userName)
                .BuildAndSave(_database);

            carrier.pregnancies = new List<Pregnancy>(pregnancies);
            _database.SetProfile(userName, carrier);
        }

        private static Pregnancy BuildPregnancy(string initiator, string monsterType, int conceivedDaysAgo, int readyInDays, int broodSize)
        {
            return new Pregnancy
            {
                Id = Guid.NewGuid().ToString("N"),
                Initiator = initiator,
                MonsterType = monsterType,
                ConceivedAt = DateTime.UtcNow.AddDays(-conceivedDaysAgo),
                ReadyAt = DateTime.UtcNow.AddDays(readyInDays),
                BroodSize = broodSize
            };
        }

        public void Dispose()
        {
        }
    }
}
