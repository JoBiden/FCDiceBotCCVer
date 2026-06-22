using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for the !feedback / !feedbacklist feature: the pure ParseFeedback and
    /// BuildFeedbackList helpers, plus the AddFeedback / GetRecentFeedback DB operations.
    /// </summary>
    [Collection("Database")]
    public class ChateauFeedbackListTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ChateauFeedbackListTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        public void Dispose()
        {
            _fixture.Reset();
        }

        #region ParseFeedback

        [Fact]
        public void ParseFeedback_NullOrEmpty_ReturnsGeneralAndEmptyText()
        {
            var fromNull = ChateauFeedback.ParseFeedback(null);
            Assert.Equal("general", fromNull.category);
            Assert.Equal("", fromNull.text);

            var fromEmpty = ChateauFeedback.ParseFeedback(new string[] { });
            Assert.Equal("general", fromEmpty.category);
            Assert.Equal("", fromEmpty.text);
        }

        [Fact]
        public void ParseFeedback_NoCategoryToken_DefaultsToGeneralAndKeepsAllText()
        {
            var result = ChateauFeedback.ParseFeedback(new[] { "please", "add", "dancing", "emotes" });

            Assert.Equal("general", result.category);
            Assert.Equal("please add dancing emotes", result.text);
        }

        [Fact]
        public void ParseFeedback_LeadingCategoryToken_StrippedAndStored()
        {
            var result = ChateauFeedback.ParseFeedback(new[] { "bug", "the", "login", "broke" });

            Assert.Equal("bug", result.category);
            Assert.Equal("the login broke", result.text);
        }

        [Fact]
        public void ParseFeedback_CategoryToken_IsCaseInsensitive_AndPreservesTextCase()
        {
            var result = ChateauFeedback.ParseFeedback(new[] { "IDEA", "Add", "a", "Dance", "Emote" });

            Assert.Equal("idea", result.category);
            Assert.Equal("Add a Dance Emote", result.text);
        }

        [Fact]
        public void ParseFeedback_CategoryTokenWithNoText_LeavesTextEmpty()
        {
            // Run() treats empty text as a rejected submission, so "!feedback bug" is not accepted.
            var result = ChateauFeedback.ParseFeedback(new[] { "bug" });

            Assert.Equal("bug", result.category);
            Assert.Equal("", result.text);
        }

        #endregion

        #region BuildFeedbackList

        [Fact]
        public void BuildFeedbackList_NullOrEmpty_ReturnsEmptyState()
        {
            Assert.Equal(ChateauFeedbackList.EmptyStateMessage, ChateauFeedbackList.BuildFeedbackList(null, DateTime.UtcNow));
            Assert.Equal(ChateauFeedbackList.EmptyStateMessage, ChateauFeedbackList.BuildFeedbackList(new List<FeedbackEntry>(), DateTime.UtcNow));
        }

        [Fact]
        public void BuildFeedbackList_RendersNewestFirst_WithNameCategoryTextAndRelativeTime()
        {
            DateTime now = new DateTime(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

            var oldest = MakeEntry("Alice", "Alice the Adventurer", "idea", "oldest idea", now.AddHours(-3));
            var middle = MakeEntry("Bob", "Bob the Bold", "bug", "middle bug", now.AddHours(-1));
            var newest = MakeEntry("Cara", "Cara the Clever", "general", "newest note", now.AddMinutes(-5));

            // Intentionally unordered input — the builder must sort newest-first itself.
            string output = ChateauFeedbackList.BuildFeedbackList(new List<FeedbackEntry> { oldest, newest, middle }, now);

            int iNewest = output.IndexOf("newest note", StringComparison.Ordinal);
            int iMiddle = output.IndexOf("middle bug", StringComparison.Ordinal);
            int iOldest = output.IndexOf("oldest idea", StringComparison.Ordinal);

            Assert.True(iNewest >= 0 && iMiddle >= 0 && iOldest >= 0);
            Assert.True(iNewest < iMiddle, "newest should render before middle");
            Assert.True(iMiddle < iOldest, "middle should render before oldest");

            // Name, category and relative time are all surfaced.
            Assert.Contains("Cara the Clever", output);
            Assert.Contains("general", output);
            Assert.Contains("5 minutes ago", output);
        }

        [Fact]
        public void BuildFeedbackList_NullCategory_RendersAsGeneral()
        {
            DateTime now = DateTime.UtcNow;
            var entry = MakeEntry("Alice", "Alice", null, "some text", now.AddMinutes(-2));

            string output = ChateauFeedbackList.BuildFeedbackList(new List<FeedbackEntry> { entry }, now);

            Assert.Contains("general", output);
        }

        [Fact]
        public void BuildFeedbackList_NoDisplayName_FallsBackToUserName()
        {
            DateTime now = DateTime.UtcNow;
            var entry = MakeEntry("Alice", null, "idea", "anonymous-ish", now.AddMinutes(-2));

            string output = ChateauFeedbackList.BuildFeedbackList(new List<FeedbackEntry> { entry }, now);

            Assert.Contains("Alice", output);
        }

        [Fact]
        public void BuildFeedbackList_PastCharCap_TruncatesWithNote()
        {
            DateTime now = DateTime.UtcNow;
            var entries = new List<FeedbackEntry>();
            for (int i = 0; i < 10; i++)
            {
                entries.Add(MakeEntry("User" + i, "User" + i, "general", new string('x', 100), now.AddMinutes(-i)));
            }

            // Force truncation with a small cap.
            string output = ChateauFeedbackList.BuildFeedbackList(entries, now, maxChars: 300);

            Assert.Contains("output truncated", output);
            // At least one but not all 10 entries rendered.
            Assert.Contains("User0", output);
            Assert.DoesNotContain("User9", output);
        }

        #endregion

        #region AddFeedback / GetRecentFeedback

        [Fact]
        public void AddFeedback_ThenGetRecent_ReturnsEntry()
        {
            var entry = MakeEntry("Alice", "Alice", "bug", "it broke", DateTime.UtcNow);

            _database.AddFeedback(entry);

            var recent = _database.GetRecentFeedback(10);
            Assert.Single(recent);
            Assert.Equal("Alice", recent[0].submitterUserName);
            Assert.Equal("bug", recent[0].category);
            Assert.Equal("it broke", recent[0].text);
        }

        [Fact]
        public void GetRecentFeedback_ReturnsNewestFirst()
        {
            DateTime now = DateTime.UtcNow;
            _database.AddFeedback(MakeEntry("Alice", "Alice", "general", "oldest", now.AddHours(-2)));
            _database.AddFeedback(MakeEntry("Bob", "Bob", "general", "newest", now.AddMinutes(-1)));
            _database.AddFeedback(MakeEntry("Cara", "Cara", "general", "middle", now.AddHours(-1)));

            var recent = _database.GetRecentFeedback(10);

            Assert.Equal(3, recent.Count);
            Assert.Equal("newest", recent[0].text);
            Assert.Equal("middle", recent[1].text);
            Assert.Equal("oldest", recent[2].text);
        }

        [Fact]
        public void GetRecentFeedback_RespectsCountLimit()
        {
            DateTime now = DateTime.UtcNow;
            _database.AddFeedback(MakeEntry("Alice", "Alice", "general", "first", now.AddHours(-3)));
            _database.AddFeedback(MakeEntry("Bob", "Bob", "general", "second", now.AddHours(-2)));
            _database.AddFeedback(MakeEntry("Cara", "Cara", "general", "third", now.AddHours(-1)));

            var recent = _database.GetRecentFeedback(2);

            Assert.Equal(2, recent.Count);
            // The two newest, in order.
            Assert.Equal("third", recent[0].text);
            Assert.Equal("second", recent[1].text);
        }

        [Fact]
        public void GetRecentFeedback_NoSubmissions_ReturnsEmpty()
        {
            var recent = _database.GetRecentFeedback(10);
            Assert.Empty(recent);
        }

        #endregion

        private static FeedbackEntry MakeEntry(string userName, string displayName, string category, string text, DateTime submittedAt)
        {
            return new FeedbackEntry
            {
                submitterUserName = userName,
                submitterDisplayName = displayName,
                category = category,
                text = text,
                sourceChannel = null,
                submittedAt = submittedAt
            };
        }
    }
}
