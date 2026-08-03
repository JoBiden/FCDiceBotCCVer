using FChatDicebot;
using FChatDicebot.Database;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Count-milestone titles have to be granted by the command that moved the count.
    /// <c>ChateauConsent</c> sweeps after every consented interaction, so interaction counts are
    /// covered; commands off that path (<c>!pledge</c>, <c>!abandonpledge</c>, <c>!work</c>,
    /// <c>!volunteer</c>) must sweep for themselves. When one doesn't, the title is not lost —
    /// it is stranded, and the resident's next consented interaction announces it, reading as if
    /// that unrelated interaction had earned it. That is feedback 6a69a8a2: a pledge to bond with
    /// one resident surfaced "Made A Promise" during a !milk of another, 42 minutes later.
    ///
    /// These tests pin the mechanism the fix relies on rather than driving the commands, which
    /// need a live <c>BotMain</c> to run.
    /// </summary>
    [Collection("Database")]
    public class CountTitleTimingTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public CountTitleTimingTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        public void Dispose()
        {
            _fixture.Reset();
        }

        [Fact]
        public void PledgesActive_FirstPledge_EarnsMadeAPromiseImmediately()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            _database.IncrementCount("Alice", "pledgesactive");

            string notification = ChateauSystemTitles.CheckAndGrantTitles("Alice");

            Assert.Contains("Made A Promise", notification);
            Assert.Contains("Title Time!", notification);
        }

        [Fact]
        public void PledgesAbandoned_FirstAbandon_EarnsBrokeTheirPromiseImmediately()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            _database.IncrementCount("Alice", "pledgesabandoned");

            string notification = ChateauSystemTitles.CheckAndGrantTitles("Alice");

            Assert.Contains("Broke Their Promise", notification);
        }

        [Fact]
        public void PledgeThatChecksItsOwnTitles_StrandsNothingForTheNextInteraction()
        {
            // The regression. First sweep stands in for the fixed !pledge; the second for
            // whatever the resident does next (ChateauConsent's post-interaction sweep).
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            _database.IncrementCount("Alice", "pledgesactive");

            string atPledgeTime = ChateauSystemTitles.CheckAndGrantTitles("Alice");
            string atNextInteraction = ChateauSystemTitles.CheckAndGrantTitles("Alice");

            Assert.Contains("Made A Promise", atPledgeTime);
            Assert.Equal(string.Empty, atNextInteraction);
        }

        [Fact]
        public void UncheckedCountChange_LeavesTheTitleForALaterUnrelatedAction()
        {
            // The bug's shape, kept as documentation: skip the sweep at pledge time and the
            // title waits, then lands on an interaction that had nothing to do with it.
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            _database.IncrementCount("Alice", "pledgesactive");

            // ... time passes, Alice milks someone else; ChateauConsent sweeps afterwards.
            _database.IncrementCount("Alice", "milkgive");
            string atUnrelatedInteraction = ChateauSystemTitles.CheckAndGrantTitles("Alice");

            Assert.Contains("Made A Promise", atUnrelatedInteraction);
            Assert.Contains("Milker", atUnrelatedInteraction);
        }

        [Fact]
        public void GrantedTitlesAreRecordedOnTheProfileAsSystemTitles()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            _database.IncrementCount("Alice", "pledgesactive");

            ChateauSystemTitles.CheckAndGrantTitles("Alice");

            var profile = _database.GetProfile("Alice");
            var title = profile.titles.FirstOrDefault(t => t.titleText == "Made A Promise");
            Assert.NotNull(title);
            Assert.True(title.IsSystemTitle);
        }

        [Fact]
        public void PledgeCountLabels_StillMatchTheMilestoneTable()
        {
            // !pledge and !abandonpledge write these three labels. If one is renamed on either
            // side, the titles silently stop being earned — the sweep would find nothing and
            // report nothing, which looks identical to "no new titles".
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            _database.ChangeCount("Alice", "pledgesactive", 25);
            _database.ChangeCount("Alice", "pledgesfulfilled", 25);
            _database.ChangeCount("Alice", "pledgesabandoned", 25);

            string notification = ChateauSystemTitles.CheckAndGrantTitles("Alice");

            Assert.Contains("Lazy", notification);
            Assert.Contains("Whose Word Is Law", notification);
            Assert.Contains("Chopped A Cherry Tree In A Past Life", notification);
        }
    }
}
