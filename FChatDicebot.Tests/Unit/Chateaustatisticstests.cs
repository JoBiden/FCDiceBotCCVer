using FChatDicebot.BotCommands;
using FChatDicebot.BotCommands.Support;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Unit tests for ChateauStatistics and ChateauStatisticsSupport pure render/aggregation
    /// helpers. All tests pass data structures in directly — none touch the database.
    /// </summary>
    public class ChateauStatisticsTests
    {
        // --- Net tilt phrasing (user-approved wording from the spec) ---

        [Fact]
        public void FormatNetTilt_CorruptionAhead_RendersConquers()
        {
            Assert.Equal("Corruption Conquers Purity by 817", ChateauStatistics.FormatNetTilt(1204, 387));
        }

        [Fact]
        public void FormatNetTilt_PurityAhead_RendersPrevails()
        {
            Assert.Equal("Purity Prevails over Corruption by 100", ChateauStatistics.FormatNetTilt(50, 150));
        }

        [Fact]
        public void FormatNetTilt_Tied_RendersBalanced()
        {
            Assert.Equal("Balanced, as all things should be", ChateauStatistics.FormatNetTilt(100, 100));
        }

        [Fact]
        public void FormatNetTilt_BothZero_RendersBalanced()
        {
            Assert.Equal("Balanced, as all things should be", ChateauStatistics.FormatNetTilt(0, 0));
        }

        // --- Empty-state rendering: every drill-down must produce coherent text ---

        [Fact]
        public void BuildStatistics_EmptyEverything_HidesZeroLines()
        {
            string result = ChateauStatistics.BuildStatistics(
                profiles: new List<Profile>(),
                monsterStats: new List<MonsterStats>(),
                plantInteractions: new List<Interaction>(),
                petrifyInteractions: new List<Interaction>(),
                infestInteractions: new List<Interaction>(),
                purgeInteractions: new List<Interaction>(),
                climaxforInteractions: new List<Interaction>(),
                climaxInteractions: new List<Interaction>(),
                corruptInteractions: new List<Interaction>(),
                purifyInteractions: new List<Interaction>());

            // Section headers always show, but the zero-count rows beneath them are hidden.
            Assert.Contains("Population", result);
            Assert.Contains("Influence", result);
            Assert.Contains("Workforce", result);
            Assert.Contains("!flora", result);
            Assert.DoesNotContain("Converted to Monsterkind", result);
            Assert.DoesNotContain("Monsters birthed", result);
            Assert.DoesNotContain("Parasites spread", result);
            Assert.DoesNotContain("Climaxes recorded", result);
            Assert.DoesNotContain("Corruption Cultivated", result);
            Assert.DoesNotContain("Balanced, as all things should be", result);
            Assert.DoesNotContain("Total employees", result);
            Assert.DoesNotContain("Most earned currencies", result);
        }

        [Fact]
        public void BuildStatistics_WithData_RendersTotalsAndSuperlatives()
        {
            var profiles = new List<Profile>
            {
                new Profile { userName = "a", displayName = "A", characteristics = new Dictionary<string, string> { { "monster", "goblin" }, { "job", "maid" } }, currencies = new Dictionary<string, int> { { "gold", 100 } }, jobExperience = new Dictionary<string, int> { { "maid", 7 } } },
                new Profile { userName = "b", displayName = "B", characteristics = new Dictionary<string, string> { { "monster", "goblin" }, { "job", "cook" } }, currencies = new Dictionary<string, int> { { "gold", 50 }, { "silvercoin", 1000 } } },
            };
            var monsterStats = new List<MonsterStats>
            {
                new MonsterStats { Id = "monster:goblin", PregnancyCount = 10, OffspringCount = 30 },
                new MonsterStats { Id = "monster:ogre", PregnancyCount = 2, OffspringCount = 4 },
                new MonsterStats { Id = "category:beast", PregnancyCount = 12, OffspringCount = 34 }, // must not double-count
            };
            var corruptInteractions = new List<Interaction> { new Interaction { type = "corrupt", identifier = "corrupt|7" } };
            var purifyInteractions = new List<Interaction> { new Interaction { type = "purify", identifier = "purify|3" } };

            string result = ChateauStatistics.BuildStatistics(
                profiles, monsterStats,
                plantInteractions: new List<Interaction>(),
                petrifyInteractions: new List<Interaction>(),
                infestInteractions: new List<Interaction>(),
                purgeInteractions: new List<Interaction>(),
                climaxforInteractions: new List<Interaction>(),
                climaxInteractions: new List<Interaction>(),
                corruptInteractions, purifyInteractions);

            // Two monsterized, both goblins — most common: goblin.
            Assert.Contains("Converted to Monsterkind: 2 (most common: goblin)", result);
            // Birthed total only from monster:* rows, not category:*. Should be 30 + 4 = 34.
            Assert.Contains("Monsters birthed: 34 (most bred: goblin)", result);
            // Corruption ahead by 4.
            Assert.Contains("Corruption Conquers Purity by 4", result);
            // Workforce totals.
            Assert.Contains("Total employees: 2", result);
            Assert.Contains("Duties completed: 7", result);
            // Top currencies — silvercoin > gold by raw count.
            Assert.Contains("Silvercoin: 1,000", result);
            Assert.Contains("Gold: 150", result);
        }

        [Fact]
        public void BuildStatistics_TiedMostBred_SurfacesBoth()
        {
            var monsterStats = new List<MonsterStats>
            {
                new MonsterStats { Id = "monster:goblin", OffspringCount = 47 },
                new MonsterStats { Id = "monster:ogre", OffspringCount = 47 },
            };
            string result = ChateauStatistics.BuildStatistics(
                profiles: new List<Profile>(),
                monsterStats: monsterStats,
                plantInteractions: new List<Interaction>(),
                petrifyInteractions: new List<Interaction>(),
                infestInteractions: new List<Interaction>(),
                purgeInteractions: new List<Interaction>(),
                climaxforInteractions: new List<Interaction>(),
                climaxInteractions: new List<Interaction>(),
                corruptInteractions: new List<Interaction>(),
                purifyInteractions: new List<Interaction>());
            Assert.Contains("most bred: goblin, ogre", result);
        }

        // --- Support helper tests ---

        [Fact]
        public void OffspringByMonsterType_FiltersOutCategoryRows()
        {
            var stats = new List<MonsterStats>
            {
                new MonsterStats { Id = "monster:lamia", OffspringCount = 5 },
                new MonsterStats { Id = "category:snake", OffspringCount = 99 },
                new MonsterStats { Id = "monster:goblin", OffspringCount = 12 },
            };
            var result = ChateauStatisticsSupport.OffspringByMonsterType(stats);
            Assert.Equal(2, result.Count);
            Assert.Equal(5, result["lamia"]);
            Assert.Equal(12, result["goblin"]);
            Assert.DoesNotContain("snake", result.Keys);
        }

        [Fact]
        public void TryParseOffspringEntry_StandardFormat_Parses()
        {
            bool ok = ChateauStatisticsSupport.TryParseOffspringEntry(
                "2026-05-26: goblin brood of 3 (parent: Alice)",
                out string monster, out int brood);
            Assert.True(ok);
            Assert.Equal("goblin", monster);
            Assert.Equal(3, brood);
        }

        [Fact]
        public void TryParseOffspringEntry_Malformed_ReturnsFalse()
        {
            Assert.False(ChateauStatisticsSupport.TryParseOffspringEntry("garbage", out _, out _));
            Assert.False(ChateauStatisticsSupport.TryParseOffspringEntry(null, out _, out _));
            Assert.False(ChateauStatisticsSupport.TryParseOffspringEntry(string.Empty, out _, out _));
        }

        [Fact]
        public void SiredByMonsterType_AcrossProfiles_ParsesParent()
        {
            var profiles = new List<Profile>
            {
                new Profile { userName = "x", lists = new Dictionary<string, List<string>> { { "offspring", new List<string> { "2026-05-26: goblin brood of 3 (parent: Alice)" } } } },
                new Profile { userName = "y", lists = new Dictionary<string, List<string>> { { "offspring", new List<string> { "2026-05-26: goblin brood of 2 (parent: Alice)", "2026-05-26: ogre brood of 1 (parent: Bob)" } } } },
            };
            var sired = ChateauStatisticsSupport.SiredByMonsterType(profiles, "Alice");
            Assert.Equal(5, sired["goblin"]);
            Assert.DoesNotContain("ogre", sired.Keys);
        }

        [Fact]
        public void SumCorruptionVolume_UsesIdentifierTail()
        {
            var interactions = new List<Interaction>
            {
                new Interaction { type = "corrupt", identifier = "corrupt|7" },
                new Interaction { type = "corrupt", identifier = "corrupt|3" },
                new Interaction { type = "corrupt", identifier = "corrupt" }, // tail missing → defaults 1
            };
            int sum = ChateauStatisticsSupport.SumCorruptionVolume(interactions, CorruptionProcessor.CorruptType);
            Assert.Equal(11, sum);
        }

        [Fact]
        public void CountCurrentParasiteHosts_DistinctHostsPerParasite()
        {
            var profileA = new Profile { userName = "a", lists = new Dictionary<string, List<string>>() };
            ParasiteInstance.SaveAll(profileA, new List<ParasiteInstance>
            {
                new ParasiteInstance { Parasite = "tentacles" },
                new ParasiteInstance { Parasite = "tentacles" }, // dup on same host doesn't double-count
                new ParasiteInstance { Parasite = "paraslime" },
            });
            var profileB = new Profile { userName = "b", lists = new Dictionary<string, List<string>>() };
            ParasiteInstance.SaveAll(profileB, new List<ParasiteInstance> { new ParasiteInstance { Parasite = "tentacles" } });

            var result = ChateauStatisticsSupport.CountCurrentParasiteHosts(new[] { profileA, profileB });
            Assert.Equal(2, result["tentacles"]);
            Assert.Equal(1, result["paraslime"]);
        }
    }
}
