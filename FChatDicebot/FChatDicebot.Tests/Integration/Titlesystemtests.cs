using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Integration
{
    /// <summary>
    /// Integration tests for the Title system.
    /// Tests title granting, display, formatting, and integration with dossier.
    /// </summary>
    [Collection("Database")]
    public class TitleSystemTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public TitleSystemTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        [Fact]
        public void EntitleInteraction_GrantsTitle_WithCorrectMetadata()
        {
            // Arrange
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice the Adventurer")
                .BuildAndSave(_database);

            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob the Bold")
                .BuildAndSave(_database);

            var processor = new EntitleProcessor(_database);
            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "entitle",
                    identifier = "The Brave",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.NotNull(bobProfile.titles);
            Assert.Single(bobProfile.titles);

            var title = bobProfile.titles[0];
            Assert.Equal("The Brave", title.titleText);
            Assert.Equal("Alice", title.givenBy);
            Assert.False(title.IsSystemTitle);
            Assert.InRange(title.grantedTime, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
        }

        [Fact]
        public void SystemTitle_FormatsWithMarkers()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_database);

            // Act - Add a system title directly
            profile.titles = new System.Collections.Generic.List<Title>
            {
                new Title
                {
                    titleText = "Champion",
                    givenBy = "Chateau",
                    grantedTime = DateTime.UtcNow
                }
            };
            _database.SetProfile("TestUser", profile);

            // Assert
            var updatedProfile = _database.GetProfile("TestUser");
            var systemTitle = updatedProfile.titles[0];
            Assert.True(systemTitle.IsSystemTitle);
            Assert.Equal("·Champion·", systemTitle.GetFormattedTitle());
        }

        [Fact]
        public void UserGrantedTitle_FormatsWithoutMarkers()
        {
            // Arrange & Act
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var processor = new EntitleProcessor(_database);
            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "entitle",
                    identifier = "The Wise",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand);
            processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            var userTitle = bobProfile.titles[0];
            Assert.False(userTitle.IsSystemTitle);
            Assert.Equal("The Wise", userTitle.GetFormattedTitle());
        }

        [Fact]
        public void MultipleTitles_AccumulateCorrectly()
        {
            // Arrange
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var charlie = new ProfileBuilder()
                .WithUserName("Charlie")
                .WithDisplayName("Charlie")
                .BuildAndSave(_database);

            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var processor = new EntitleProcessor(_database);

            // Act - Grant multiple titles
            var title1 = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "entitle",
                    identifier = "The Brave",
                    investmentLevel = "commitment"
                }
            };
            _database.AddPendingCommand(title1);
            processor.ProcessInteraction(title1);

            // Clear cooldown
            var bobProfile = _database.GetProfile("Bob");
            bobProfile.timers["entitle"] = new CoolDown { timerEnd = DateTime.UtcNow.AddDays(-1) };
            _database.SetProfile("Bob", bobProfile);

            var title2 = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Charlie",
                    recipient = "Bob",
                    type = "entitle",
                    identifier = "The Wise",
                    investmentLevel = "commitment"
                }
            };
            _database.AddPendingCommand(title2);
            processor.ProcessInteraction(title2);

            // Assert
            bobProfile = _database.GetProfile("Bob");
            Assert.Equal(2, bobProfile.titles.Count);
            Assert.Contains(bobProfile.titles, t => t.titleText == "The Brave" && t.givenBy == "Alice");
            Assert.Contains(bobProfile.titles, t => t.titleText == "The Wise" && t.givenBy == "Charlie");
        }

        [Fact]
        public void GetDisplayedTitlesText_EmptyTitles_ReturnsEmpty()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_database);

            // Act
            string result = Utils.GetDisplayedTitlesText(profile);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetDisplayedTitlesText_NoDisplaySlots_ReturnsEmpty()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_database);

            profile.titles = new System.Collections.Generic.List<Title>
            {
                new Title { titleText = "The Brave", givenBy = "Alice" }
            };
            // Don't set displayedTitleSlots
            _database.SetProfile("TestUser", profile);

            // Act
            var updatedProfile = _database.GetProfile("TestUser");
            string result = Utils.GetDisplayedTitlesText(updatedProfile);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetDisplayedTitlesText_WithDisplayedSlots_ReturnsFormattedTitles()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_database);

            profile.titles = new System.Collections.Generic.List<Title>
            {
                new Title { titleText = "The Brave", givenBy = "Alice" },
                new Title { titleText = "Champion", givenBy = "Chateau" },
                new Title { titleText = "The Wise", givenBy = "Bob" }
            };

            // Display titles at indices 0 and 1
            profile.displayedTitleSlots = new int[] { 0, 1, -1, -1, -1, -1, -1, -1, -1 };
            _database.SetProfile("TestUser", profile);

            // Act
            var updatedProfile = _database.GetProfile("TestUser");
            string result = Utils.GetDisplayedTitlesText(updatedProfile);

            // Assert
            Assert.Equal("The Brave, ·Champion·", result);
        }

        [Fact]
        public void GetDisplayedTitlesText_OnlyEmptySlots_ReturnsEmpty()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_database);

            profile.titles = new System.Collections.Generic.List<Title>
            {
                new Title { titleText = "The Brave", givenBy = "Alice" }
            };

            // All slots empty
            profile.displayedTitleSlots = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1 };
            _database.SetProfile("TestUser", profile);

            // Act
            var updatedProfile = _database.GetProfile("TestUser");
            string result = Utils.GetDisplayedTitlesText(updatedProfile);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetDisplayedTitlesText_InvalidIndices_SkipsThem()
        {
            // Arrange
            var profile = new ProfileBuilder()
                .WithUserName("TestUser")
                .WithDisplayName("Test User")
                .BuildAndSave(_database);

            profile.titles = new System.Collections.Generic.List<Title>
            {
                new Title { titleText = "The Brave", givenBy = "Alice" }
            };

            // Include an invalid index (out of bounds)
            profile.displayedTitleSlots = new int[] { 0, 99, -1, -1, -1, -1, -1, -1, -1 };
            _database.SetProfile("TestUser", profile);

            // Act
            var updatedProfile = _database.GetProfile("TestUser");
            string result = Utils.GetDisplayedTitlesText(updatedProfile);

            // Assert
            Assert.Equal("The Brave", result);
        }

        [Fact]
        public void EntitleProcessor_SetsCooldown()
        {
            // Arrange
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var processor = new EntitleProcessor(_database);
            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "entitle",
                    identifier = "The Brave",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.True(bobProfile.timers.ContainsKey("entitle"));
            Assert.True(bobProfile.timers["entitle"].timerEnd > DateTime.UtcNow);
        }

        [Fact]
        public void EntitleProcessor_PreventsDuplicateTitles()
        {
            // Arrange
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            bob.titles = new System.Collections.Generic.List<Title>
            {
                new Title { titleText = "The Brave", givenBy = "Someone" }
            };
            _database.SetProfile("Bob", bob);

            var processor = new EntitleProcessor(_database);

            // Act
            var result = processor.ValidateInteraction("Alice", "Bob", "The Brave");

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("already has the title", result.ErrorMessage);
        }

        [Fact]
        public void EntitleProcessor_SavesInteractionToHistory()
        {
            // Arrange
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var processor = new EntitleProcessor(_database);
            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "entitle",
                    identifier = "The Brave",
                    investmentLevel = "commitment"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            processor.ProcessInteraction(pendingCommand);

            // Assert
            var interactions = _database.GetInteractions("Alice", "Bob");
            Assert.Contains(interactions, i => i.type == "entitle" && i.identifier == "The Brave");
        }

        public void Dispose()
        {
        }
    }
}
