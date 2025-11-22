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
            _dossier = new ChateauDossier();
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
                .WithCharacteristic("job", "maid")
                .WithCharacteristic("employer", "Boss")
                .BuildAndSave(_fixture.Database);

            // Act
            string result = InvokeBuildJobSection(profile);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("maid", result.ToLower());
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
                { "bondfealtyreceived", new List<string> { "Bonder" } }
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
            Assert.Contains("Experience", result);
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

        #endregion

        public void Dispose()
        {
        }
    }
}
