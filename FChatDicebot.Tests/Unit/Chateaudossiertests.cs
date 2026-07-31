using FChatDicebot.BotCommands;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Unit tests for ChateauDossier builder functions.
    /// Tests that string builders only operate when associated profile fields are populated.
    /// </summary>
    [Collection("Database")]
    public class ChateauDossierTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly ChateauDossier _dossier;

        public ChateauDossierTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _dossier = new ChateauDossier(_fixture.Database);
        }

        #region BuildJobSection Tests

        [Fact]
        public void BuildJobSection_NoJob_ReturnsEmpty()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            // Act
            string result = InvokeBuildJobSection(profile);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void BuildJobSection_WithJob_NoEmployer_ReturnsJobOnly()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCharacteristic("job", "butler")
                .BuildAndSave(_fixture.Database);

            // Act
            string result = InvokeBuildJobSection(profile);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("butler", result.ToLower());
            Assert.DoesNotContain("working under", result);
        }

        [Fact]
        public void BuildJobSection_WithJobAndEmployer_ReturnsBoth()
        {
            // Arrange
            var employer = new ProfileBuilder()
                .WithUserName("Boss")
                .WithDisplayName("The Boss")
                .BuildAndSave(_fixture.Database);

            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCharacteristic("job", "butler")
                .WithCharacteristic("employer", "Boss")
                .BuildAndSave(_fixture.Database);

            // Act
            string result = InvokeBuildJobSection(profile);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("butler", result.ToLower());
            Assert.Contains("The Boss", result);
            Assert.Contains("working under", result);
        }

        #endregion

        #region BuildCasualInteractionsSection Tests

        [Fact]
        public void BuildCasualInteractionsSection_NoCounts_ReturnsEmpty()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            // Act
            string result = InvokeBuildCasualInteractionsSection(profile);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void BuildCasualInteractionsSection_OnlyNonCasualCounts_ReturnsEmpty()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCount("feedgive", 5) // This is involved, not casual
                .BuildAndSave(_fixture.Database);

            // Act
            string result = InvokeBuildCasualInteractionsSection(profile);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void BuildCasualInteractionsSection_WithCasualCounts_ReturnsFormatted()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCount("kiss", 10)
                .WithCount("cuddle", 5)
                .BuildAndSave(_fixture.Database);

            // Act
            string result = InvokeBuildCasualInteractionsSection(profile);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("Casual interactions", result);
            Assert.Contains("10", result); // kiss count
            Assert.Contains("5", result);  // cuddle count
        }

        #endregion

        #region BuildMarksSection Tests

        [Fact]
        public void BuildMarksSection_NoLists_ReturnsEmpty()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            // Act
            string result = InvokeBuildMarksSection(profile);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void BuildMarksSection_NoMarks_ReturnsEmpty()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            profile.lists = new Dictionary<string, List<string>>
            {
                { "somelist", new List<string> { "item1" } }
            };
            _fixture.Database.SetProfile("TestUser", profile);

            // Act
            var updatedProfile = _fixture.Database.GetProfile("TestUser");
            string result = InvokeBuildMarksSection(updatedProfile);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void BuildMarksSection_WithMarks_ReturnsFormatted()
        {
            // Arrange
            var marker = new ProfileBuilder()
                .WithUserName("Marker")
                .WithDisplayName("The Marker")
                .WithCharacteristic("mark", "♥")
                .BuildAndSave(_fixture.Database);

            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            profile.lists = new Dictionary<string, List<string>>
            {
                { "neckmarks", new List<string> { "Marker" } }
            };
            _fixture.Database.SetProfile("TestUser", profile);

            // Act
            var updatedProfile = _fixture.Database.GetProfile("TestUser");
            string result = InvokeBuildMarksSection(updatedProfile);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("Marks", result);
            Assert.Contains("♥", result);
        }

        #endregion

        #region BuildBondsSection Tests

        [Fact]
        public void BuildBondsSection_NoLists_ReturnsEmpty()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            // Act
            string result = InvokeBuildBondsSection(profile);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void BuildBondsSection_NoBonds_ReturnsEmpty()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            profile.lists = new Dictionary<string, List<string>>
            {
                { "somelist", new List<string> { "item1" } }
            };
            _fixture.Database.SetProfile("TestUser", profile);

            // Act
            var updatedProfile = _fixture.Database.GetProfile("TestUser");
            string result = InvokeBuildBondsSection(updatedProfile);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void BuildBondsSection_WithBonds_ReturnsFormatted()
        {
            // Arrange
            var bonder = new ProfileBuilder()
                .WithUserName("Bonder")
                .WithDisplayName("The Bonder")
                .BuildAndSave(_fixture.Database);

            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            profile.lists = new Dictionary<string, List<string>>
            {
                { "bondloyaltyreceived", new List<string> { "Bonder" } }
            };
            _fixture.Database.SetProfile("TestUser", profile);

            // Act
            var updatedProfile = _fixture.Database.GetProfile("TestUser");
            string result = InvokeBuildBondsSection(updatedProfile);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("Bonds", result);
            Assert.Contains("The Bonder", result);
        }

        #endregion

        #region BuildJobExperienceSection Tests

        [Fact]
        public void BuildJobExperienceSection_NoExperience_ReturnsEmpty()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            // Act
            string result = InvokeBuildJobExperienceSection(profile);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void BuildJobExperienceSection_WithExperience_ReturnsFormatted()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            profile.jobExperience = new Dictionary<string, int>
            {
                { "butler", 5 },
                { "maid", 3 }
            };
            _fixture.Database.SetProfile("TestUser", profile);

            // Act
            var updatedProfile = _fixture.Database.GetProfile("TestUser");
            string result = InvokeBuildJobExperienceSection(updatedProfile);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("Days of experience", result);
            Assert.Contains("5", result);
            Assert.Contains("3", result);
        }

        #endregion

        #region BuildNameTitleSpecialties Tests

        [Fact]
        public void BuildNameTitleSpecialties_NoTitles_ReturnsJustName()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            // Act
            string result = InvokeBuildNameTitleSpecialties(profile, "TestUser");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("Test User", result);
        }

        [Fact]
        public void BuildNameTitleSpecialties_WithMonster_IncludesMonster()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCharacteristic("monster", "dragon")
                .BuildAndSave(_fixture.Database);

            // Act
            string result = InvokeBuildNameTitleSpecialties(profile, "TestUser");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("Dragon", result);
        }

        [Fact]
        public void BuildNameTitleSpecialties_WithDisplayedTitles_IncludesTitles()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            profile.titles = new List<Title>
            {
                new Title { titleText = "The Brave", givenBy = "Alice" }
            };
            profile.displayedTitleSlots = new int[] { 0, -1, -1, -1, -1, -1, -1, -1, -1 };
            _fixture.Database.SetProfile("TestUser", profile);

            // Act
            var updatedProfile = _fixture.Database.GetProfile("TestUser");
            string result = InvokeBuildNameTitleSpecialties(updatedProfile, "TestUser");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("The Brave", result);
        }

        [Fact]
        public void BuildNameTitleSpecialties_WithSystemTitle_FormatsCorrectly()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            profile.titles = new List<Title>
            {
                new Title { titleText = "Champion", givenBy = "Chateau" }
            };
            profile.displayedTitleSlots = new int[] { 0, -1, -1, -1, -1, -1, -1, -1, -1 };
            _fixture.Database.SetProfile("TestUser", profile);

            // Act
            var updatedProfile = _fixture.Database.GetProfile("TestUser");
            string result = InvokeBuildNameTitleSpecialties(updatedProfile, "TestUser");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("·Champion·", result);
        }

        // Regression: a profile with a monster but no specialties used to render
        // "{name} the {Monster}; \n" — a stray "; " separator after the monster type.
        // The fix only emits "; " when a specialist title actually follows.
        [Fact]
        public void BuildNameTitleSpecialties_MonsterWithoutSpecialty_NoStrayTrailingSeparator()
        {
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCharacteristic("monster", "dragon")
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildNameTitleSpecialties(profile, "TestUser");

            Assert.Contains("Dragon", result);
            Assert.DoesNotContain("; [/u]", result);
            Assert.DoesNotContain("; \n", result);
            Assert.DoesNotContain("Dragon; ", result);
        }

        // Regression companion: a profile with neither monster nor specialty shouldn't
        // hang a trailing "the " after the display name.
        [Fact]
        public void BuildNameTitleSpecialties_NoMonsterOrSpecialty_NoStrayTrailingThe()
        {
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildNameTitleSpecialties(profile, "TestUser");

            Assert.DoesNotContain("the [/u]", result);
            Assert.DoesNotContain("the \n", result);
        }

        #endregion

        #region New dossier blocks (Sired / Birthed / Has planted / Currently employs / Titles / Most abundant currency)

        [Fact]
        public void BuildBirthedSection_ParsesOffspringList()
        {
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithListItem("offspring", "2026-05-26: goblin brood of 3 (parent: Alice)")
                .WithListItem("offspring", "2026-05-27: goblin brood of 2 (parent: Bob)")
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildBirthedSection(profile);

            Assert.Contains("Birthed", result);
            Assert.Contains("Goblins", result);
            Assert.Contains("5", result);
        }

        [Fact]
        public void BuildSiredSection_FindsParentMarkerAcrossProfiles()
        {
            new ProfileBuilder()
                .WithUserName("Carrier")
                .WithDisplayName("Carrier")
                .WithListItem("offspring", "2026-05-26: ogre brood of 4 (parent: Alice)")
                .BuildAndSave(_fixture.Database);
            new ProfileBuilder()
                .WithUserName("Other")
                .WithDisplayName("Other")
                .WithListItem("offspring", "2026-05-26: goblin brood of 7 (parent: Bob)") // not Alice
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildSiredSection("Alice");

            Assert.Contains("Sired", result);
            Assert.Contains("Ogres", result);
            Assert.Contains("4", result);
            Assert.DoesNotContain("Goblin", result);
        }

        [Fact]
        public void BuildCurrentlyEmploysSection_ScansEmployerCharacteristic()
        {
            new ProfileBuilder().WithUserName("Boss").WithDisplayName("Boss").BuildAndSave(_fixture.Database);
            new ProfileBuilder()
                .WithUserName("Worker1")
                .WithDisplayName("Worker 1")
                .WithCharacteristic("employer", "Boss")
                .WithCharacteristic("job", "maid")
                .BuildAndSave(_fixture.Database);
            new ProfileBuilder()
                .WithUserName("Worker2")
                .WithDisplayName("Worker 2")
                .WithCharacteristic("employer", "Boss")
                .WithCharacteristic("job", "maid")
                .BuildAndSave(_fixture.Database);
            new ProfileBuilder()
                .WithUserName("Other")
                .WithDisplayName("Other")
                .WithCharacteristic("employer", "Boss")
                .WithCharacteristic("job", "cook")
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildCurrentlyEmploysSection("Boss");

            Assert.Contains("Currently employs", result);
            // Roles are pluralised by headcount and name their employees, mirroring Bonds.
            Assert.Contains(ReadoutText.RowIndent + "[u]Maids:[/u] Worker 1, Worker 2", result);
            Assert.Contains(ReadoutText.RowIndent + "[u]Cook:[/u] Other", result);
            // Two roles is under the threshold, so nothing is hidden.
            Assert.DoesNotContain("[spoiler]", result);
        }

        [Fact]
        public void BuildCurrentlyEmploysSection_UnderThreshold_NoSpoilerOrSummary()
        {
            new ProfileBuilder().WithUserName("Boss").WithDisplayName("Boss").BuildAndSave(_fixture.Database);
            foreach (string job in new[] { "maid", "cook", "artist", "athlete", "bartender", "builder" })
            {
                new ProfileBuilder()
                    .WithUserName("W_" + job)
                    .WithDisplayName("W " + job)
                    .WithCharacteristic("employer", "Boss")
                    .WithCharacteristic("job", job)
                    .BuildAndSave(_fixture.Database);
            }

            string result = InvokeBuildCurrentlyEmploysSection("Boss");

            // Exactly at SpoilerLineThreshold (6 roles) — still fully visible.
            Assert.DoesNotContain("[spoiler]", result);
            Assert.DoesNotContain("residents across", result);
        }

        [Fact]
        public void BuildCurrentlyEmploysSection_OverThreshold_CollapsesWithCountSummary()
        {
            new ProfileBuilder().WithUserName("Boss").WithDisplayName("Boss").BuildAndSave(_fixture.Database);
            string[] jobs = { "maid", "cook", "artist", "athlete", "bartender", "builder", "brute" };
            foreach (string job in jobs)
            {
                new ProfileBuilder()
                    .WithUserName("W_" + job)
                    .WithDisplayName("W " + job)
                    .WithCharacteristic("employer", "Boss")
                    .WithCharacteristic("job", job)
                    .BuildAndSave(_fixture.Database);
            }

            string result = InvokeBuildCurrentlyEmploysSection("Boss");

            // 7 roles is past the threshold: header and scale stay visible, detail collapses.
            Assert.Contains(ReadoutText.Section("Currently employs", ReadoutDomain.Relationship) + " 7 residents across 7 roles [spoiler]", result);
            Assert.EndsWith("[/spoiler]\n", result);
            // Every employee is still present inside the spoiler — collapsing hides, never truncates.
            foreach (string job in jobs)
            {
                Assert.Contains("W " + job, result);
            }
        }

        [Fact]
        public void BuildCurrentlyEmploysSection_OrdersByHeadcountThenNamesAlphabetically()
        {
            new ProfileBuilder().WithUserName("Boss").WithDisplayName("Boss").BuildAndSave(_fixture.Database);
            new ProfileBuilder().WithUserName("Solo").WithDisplayName("Solo")
                .WithCharacteristic("employer", "Boss").WithCharacteristic("job", "cook")
                .BuildAndSave(_fixture.Database);
            foreach (string name in new[] { "Zara", "Alice", "Mabel" })
            {
                new ProfileBuilder().WithUserName(name).WithDisplayName(name)
                    .WithCharacteristic("employer", "Boss").WithCharacteristic("job", "maid")
                    .BuildAndSave(_fixture.Database);
            }

            string result = InvokeBuildCurrentlyEmploysSection("Boss");

            // Biggest team leads, and names within a role read alphabetically.
            Assert.True(result.IndexOf("Maids:") < result.IndexOf("Cook:"));
            Assert.Contains(ReadoutText.RowIndent + "[u]Maids:[/u] Alice, Mabel, Zara", result);
        }

        [Fact]
        public void BuildTitlesEarnedSection_CountsAllTitlesIncludingSystem()
        {
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            profile.titles = new List<Title>
            {
                new Title { titleText = "The Brave", givenBy = "Alice" },
                new Title { titleText = "Champion", givenBy = "Chateau" },
                new Title { titleText = "Conqueror", givenBy = "Bob" },
            };
            _fixture.Database.SetProfile("TestUser", profile);
            var updated = _fixture.Database.GetProfile("TestUser");

            string result = InvokeBuildTitlesEarnedSection(updated);

            Assert.Contains("Titles Earned", result);
            Assert.Contains("3", result);
        }

        [Fact]
        public void BuildMostAbundantCurrencySection_PicksHighestRawCount()
        {
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCurrency("gold", 4)
                .WithCurrency("lustessence", 5) // raw count beats gold even though gold is "worth more"
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildMostAbundantCurrencySection(profile);

            Assert.Contains("Richest Currency", result);
            Assert.Contains("5", result);
            Assert.Contains("lustessence", result);
        }

        [Fact]
        public void BuildInteractionCountsSection_RendersGiveTakeAndSummed()
        {
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCount("climaxtake", 12)
                .WithCount("cursetake", 3)
                .WithCount("markgive", 2)
                .WithCount("marktake", 4)
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildInteractionCountsSection(profile);

            Assert.Contains("Orgasms", result);          // climaxtake → "Orgasms"
            Assert.Contains("12", result);
            Assert.Contains("Curses Endured", result);
            Assert.Contains("Marks Shared", result);     // markgive+marktake summed
            Assert.Contains("6", result);                // 2 + 4 = 6
        }

        #endregion

        #region BuildLastSeenSection Tests (disposition #3 / L2)

        [Fact]
        public void BuildLastSeenSection_NoInteractions_ReturnsEmpty()
        {
            new ProfileBuilder().WithUserName("Target").WithDisplayName("Target").BuildAndSave(_fixture.Database);

            string result = InvokeBuildLastSeenSection("Target");

            Assert.Empty(result);
        }

        [Fact]
        public void BuildLastSeenSection_OnlyCasualInteractions_ReturnsEmpty()
        {
            // A casual-only history must not surface a "Last seen" line at all — casuals
            // are excluded entirely, not merely outranked by something more recent.
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_fixture.Database);
            new ProfileBuilder().WithUserName("Target").WithDisplayName("Target").BuildAndSave(_fixture.Database);

            _fixture.Database.AddInteraction(new Interaction
            {
                initiator = "Alice",
                recipient = "Target",
                type = "kiss",
                investmentLevel = "casual",
                interactionTime = DateTime.UtcNow
            });

            string result = InvokeBuildLastSeenSection("Target");

            Assert.Empty(result);
        }

        [Fact]
        public void BuildLastSeenSection_NewerCasualDoesNotOverrideOlderNonCasual()
        {
            // Regression test for disposition #3 / L2: a more recent casual interaction
            // (e.g. a kiss) must not bury an older but more meaningful interaction (e.g. a
            // feed) in the "Last seen" line.
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_fixture.Database);
            new ProfileBuilder().WithUserName("Target").WithDisplayName("Target").BuildAndSave(_fixture.Database);

            _fixture.Database.AddInteraction(new Interaction
            {
                initiator = "Alice",
                recipient = "Target",
                type = "feed",
                identifier = "cake",
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow.AddDays(-1)
            });
            _fixture.Database.AddInteraction(new Interaction
            {
                initiator = "Alice",
                recipient = "Target",
                type = "kiss",
                investmentLevel = "casual",
                interactionTime = DateTime.UtcNow
            });

            string result = InvokeBuildLastSeenSection("Target");

            Assert.Contains("fed", result);
        }

        [Fact]
        public void BuildLastSeenSection_PicksMostRecentNonCasual()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_fixture.Database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_fixture.Database);
            new ProfileBuilder().WithUserName("Target").WithDisplayName("Target").BuildAndSave(_fixture.Database);

            _fixture.Database.AddInteraction(new Interaction
            {
                initiator = "Alice",
                recipient = "Target",
                type = "feed",
                identifier = "cake",
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow.AddDays(-2)
            });
            _fixture.Database.AddInteraction(new Interaction
            {
                initiator = "Bob",
                recipient = "Target",
                type = "feed",
                identifier = "pie",
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow.AddDays(-1)
            });

            string result = InvokeBuildLastSeenSection("Target");

            Assert.Contains("Bob", result);
            Assert.Contains("pie", result);
        }

        #endregion

        #region Section shape rules (inline tallies vs. line lists vs. spoilers)

        [Fact]
        public void InlineSections_RenderAsASingleRow()
        {
            // The whole point of the layout pass: a block whose rows are "label: number"
            // is one wrapped row, not one line per entry. Previously "Currently employs"
            // and "Days of Experience" disagreed despite identical data shape.
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithJobExperience("maid", 5)
                .WithJobExperience("cook", 3)
                .WithJobExperience("artist", 2)
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildJobExperienceSection(profile);

            Assert.Equal(1, result.Split('\n').Length - 1); // exactly one trailing newline
            Assert.Contains("[u]Maid:[/u] [b]5[/b]", result);
            Assert.Contains("[u]Cook:[/u] [b]3[/b]", result);
            Assert.DoesNotContain("[spoiler]", result);
        }

        [Fact]
        public void InlineSections_DoNotTrailASeparator()
        {
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCount("kiss", 10)
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildCasualInteractionsSection(profile);

            Assert.EndsWith("[b]10[/b]\n", result);
        }

        [Fact]
        public void BuildBondsSection_OverThreshold_CollapsesWithCountSummary()
        {
            var builder = new ProfileBuilder().WithUserName("TestUser").WithDisplayName("Test User");
            string[] bondTypes = { "marriage", "sibling", "ally", "rival", "client", "pet", "thrall" };
            foreach (string bondType in bondTypes)
            {
                new ProfileBuilder().WithUserName("B_" + bondType).WithDisplayName("B " + bondType)
                    .BuildAndSave(_fixture.Database);
                builder.WithListItem("bond" + bondType + "initiated", "B_" + bondType);
            }
            var profile = builder.BuildAndSave(_fixture.Database);

            string result = InvokeBuildBondsSection(profile);

            Assert.Contains("7 across 7 kinds [spoiler]", result);
            Assert.EndsWith("[/spoiler]\n", result);
            foreach (string bondType in bondTypes)
            {
                Assert.Contains("B " + bondType, result);
            }
        }

        [Fact]
        public void BuildOffspringSection_MergesSiredAndBirthedIntoOneRow()
        {
            new ProfileBuilder()
                .WithUserName("Carrier")
                .WithDisplayName("Carrier")
                .WithListItem("offspring", "2026-05-26: ogre brood of 4 (parent: TestUser)")
                .BuildAndSave(_fixture.Database);
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithListItem("offspring", "2026-05-26: goblin brood of 5 (parent: Someone)")
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildOffspringSection(profile, "TestUser");

            Assert.StartsWith(ReadoutText.Section("Offspring", ReadoutDomain.Record), result);
            Assert.Contains("[u]Sired:[/u] Ogres: [b]4[/b]", result);
            Assert.Contains("[u]Birthed:[/u] Goblins: [b]5[/b]", result);
            Assert.Equal(1, result.Split('\n').Length - 1);
        }

        [Fact]
        public void BuildOffspringSection_RendersOneHalfAloneWhenTheOtherIsAbsent()
        {
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithListItem("offspring", "2026-05-26: goblin brood of 5 (parent: Someone)")
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildOffspringSection(profile, "TestUser");

            Assert.Contains("[u]Birthed:[/u] Goblins: [b]5[/b]", result);
            Assert.DoesNotContain("Sired", result);
        }

        [Fact]
        public void BuildAtAGlanceSection_MergesTitlesAndCurrency()
        {
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCurrency("gold", 12)
                .BuildAndSave(_fixture.Database);
            profile.titles = new List<Title> { new Title { titleText = "The Brave", givenBy = "Alice" } };
            _fixture.Database.SetProfile("TestUser", profile);

            string result = InvokeBuildAtAGlanceSection(_fixture.Database.GetProfile("TestUser"));

            Assert.Contains("[u]Titles Earned:[/u] [b]1[/b]", result);
            Assert.Contains("[u]Richest Currency:[/u] [b]12[/b] gold", result);
            Assert.Equal(1, result.Split('\n').Length - 1);
        }

        [Fact]
        public void BuildAtAGlanceSection_NoTitlesOrCurrency_ReturnsEmpty()
        {
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            Assert.Empty(InvokeBuildAtAGlanceSection(profile));
        }

        [Fact]
        public void BuildFullDossier_BareProfile_GetsRecentArrivalNote()
        {
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildFullDossier(profile, "TestUser");

            Assert.Contains("A recent arrival to the Chateau", result);
        }

        [Fact]
        public void BuildFullDossier_JobButNoInteractions_HasNoRecentArrivalNote()
        {
            // A job is content in its own right, so it suppresses the note even before the
            // resident has interacted with anyone.
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCharacteristic("job", "butler")
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildFullDossier(profile, "TestUser");

            Assert.DoesNotContain("A recent arrival to the Chateau", result);
            Assert.Contains("Currently working as a", result);
        }

        [Fact]
        public void BuildFullDossier_ProfileWithContent_HasNoRecentArrivalNote()
        {
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCount("kiss", 3)
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildFullDossier(profile, "TestUser");

            Assert.DoesNotContain("A recent arrival to the Chateau", result);
            Assert.Contains("Casual interactions", result);
        }

        [Fact]
        public void BuildFullDossier_GroupsRelationshipsBeforeTallies()
        {
            new ProfileBuilder().WithUserName("Hire").WithDisplayName("Hire")
                .WithCharacteristic("employer", "TestUser").WithCharacteristic("job", "maid")
                .BuildAndSave(_fixture.Database);
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCount("kiss", 3)
                .WithJobExperience("maid", 4)
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildFullDossier(profile, "TestUser");

            // Employs is a roster now, so it sits with the relationship blocks rather than
            // among the numeric tallies.
            Assert.True(result.IndexOf("Currently employs") < result.IndexOf("Casual interactions"));
            Assert.True(result.IndexOf("Casual interactions") < result.IndexOf("Days of experience"));
        }

        [Fact]
        public void BuildFullDossier_HasNoUnbalancedSpoilerTags()
        {
            new ProfileBuilder().WithUserName("Hire").WithDisplayName("Hire")
                .WithCharacteristic("employer", "TestUser").WithCharacteristic("job", "maid")
                .BuildAndSave(_fixture.Database);
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .WithCount("kiss", 3)
                // An unrecognised job used to emit an unclosed [spoiler] via JobToText,
                // swallowing every later section of the dossier.
                .WithJobExperience("notarealjob", 4)
                .BuildAndSave(_fixture.Database);

            string result = InvokeBuildFullDossier(profile, "TestUser");

            Assert.Equal(
                CountOccurrences(result, "[spoiler]"),
                CountOccurrences(result, "[/spoiler]"));
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            int count = 0, index = 0;
            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += needle.Length;
            }
            return count;
        }

        #endregion

        #region Helper Methods for Reflection

        private string InvokeBuildJobSection(Profile profile)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildJobSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile });
        }

        private string InvokeBuildCasualInteractionsSection(Profile profile)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildCasualInteractionsSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile });
        }

        private string InvokeBuildMarksSection(Profile profile)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildMarksSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile });
        }

        private string InvokeBuildBondsSection(Profile profile)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildBondsSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile });
        }

        private string InvokeBuildJobExperienceSection(Profile profile)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildJobExperienceSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile });
        }

        private string InvokeBuildNameTitleSpecialties(Profile profile, string targetUser)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildNameTitleSpecialties",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile, targetUser });
        }

        private string InvokeBuildBirthedSection(Profile profile)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildBirthedSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile });
        }

        private string InvokeBuildSiredSection(string targetUser)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildSiredSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { targetUser });
        }

        private string InvokeBuildLastSeenSection(string targetUser)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildLastSeenSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { targetUser });
        }

        private string InvokeBuildCurrentlyEmploysSection(string targetUser)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildCurrentlyEmploysSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { targetUser });
        }

        private string InvokeBuildTitlesEarnedSection(Profile profile)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildTitlesEarnedSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile });
        }

        private string InvokeBuildMostAbundantCurrencySection(Profile profile)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildMostAbundantCurrencySection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile });
        }

        private string InvokeBuildInteractionCountsSection(Profile profile)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildInteractionCountsSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile });
        }

        private string InvokeBuildOffspringSection(Profile profile, string targetUser)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildOffspringSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile, targetUser });
        }

        private string InvokeBuildAtAGlanceSection(Profile profile)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildAtAGlanceSection",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile });
        }

        private string InvokeBuildFullDossier(Profile profile, string targetUser)
        {
            var method = typeof(ChateauDossier).GetMethod("BuildFullDossier",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)method.Invoke(_dossier, new object[] { profile, targetUser });
        }

        #endregion

        public void Dispose()
        {
        }
    }
}
