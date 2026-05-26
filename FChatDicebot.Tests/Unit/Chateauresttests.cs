using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit.BotCommands
{
    [Collection("Database")]
    public class ChateauRestTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ChateauRestTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        public void Dispose() { }

        // -------------------------------------------------------------------
        // Pre-validation paths (no work performed)
        // -------------------------------------------------------------------

        [Fact]
        public void ExecuteRest_UnknownUser_ReturnsErrorPrivately()
        {
            var result = ChateauRest.ExecuteRest(_database, "Ghost", null);
            Assert.Equal(string.Empty, result.ChannelMessage);
            Assert.NotEqual(string.Empty, result.PrivateMessage);
        }

        [Fact]
        public void ExecuteRest_NoActiveBreaks_ReturnsErrorPrivately()
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            var result = ChateauRest.ExecuteRest(_database, "Bob", null);
            Assert.Equal(string.Empty, result.ChannelMessage);
            Assert.Contains("don't have any body parts in need of !rest", result.PrivateMessage);
        }

        [Fact]
        public void ExecuteRest_AlreadyRestedToday_Rejects()
        {
            SeedBobWithBreaks(("mouth", 3));
            var bob = _database.GetProfile("Bob");
            bob.timers[ChateauRest.RestUsedTimerKey] = new CoolDown
            {
                timerStart = DateTime.UtcNow,
                timerEnd = DateTime.UtcNow.AddHours(5),
            };
            _database.SetProfile("Bob", bob);

            var result = ChateauRest.ExecuteRest(_database, "Bob", null);
            Assert.Equal(string.Empty, result.ChannelMessage);
            Assert.Contains("already rested", result.PrivateMessage);
        }

        [Fact]
        public void ExecuteRest_AlreadyWorkedToday_Rejects()
        {
            SeedBobWithBreaks(("mouth", 3));
            var bob = _database.GetProfile("Bob");
            bob.timers[ChateauRest.WorkTimerKey] = new CoolDown
            {
                timerStart = DateTime.UtcNow,
                timerEnd = DateTime.UtcNow.AddHours(5),
            };
            _database.SetProfile("Bob", bob);

            var result = ChateauRest.ExecuteRest(_database, "Bob", null);
            Assert.Equal(string.Empty, result.ChannelMessage);
            Assert.Contains("already worked", result.PrivateMessage);
        }

        [Fact]
        public void ExecuteRest_NamedPartNotBroken_Rejects()
        {
            SeedBobWithBreaks(("mouth", 3));
            var result = ChateauRest.ExecuteRest(_database, "Bob", "wing");
            Assert.Equal(string.Empty, result.ChannelMessage);
            Assert.Contains("isn't broken", result.PrivateMessage);
        }

        // -------------------------------------------------------------------
        // Happy paths
        // -------------------------------------------------------------------

        [Fact]
        public void ExecuteRest_NoArgument_HealsAllActiveBreaksByOne()
        {
            SeedBobWithBreaks(("mouth", 3), ("wing", 5));
            var result = ChateauRest.ExecuteRest(_database, "Bob", null);
            Assert.NotEqual(string.Empty, result.ChannelMessage);

            var bob = _database.GetProfile("Bob");
            var breaks = BreakInstance.LoadAll(bob);
            Assert.Equal(2, breaks.Count);
            Assert.Equal(2, breaks.Find(b => b.Part == "mouth").Severity);
            Assert.Equal(4, breaks.Find(b => b.Part == "wing").Severity);
        }

        [Fact]
        public void ExecuteRest_NamedPart_OnlyHealsThatPart()
        {
            SeedBobWithBreaks(("mouth", 3), ("wing", 5));
            var result = ChateauRest.ExecuteRest(_database, "Bob", "mouth");
            Assert.NotEqual(string.Empty, result.ChannelMessage);

            var bob = _database.GetProfile("Bob");
            var breaks = BreakInstance.LoadAll(bob);
            Assert.Equal(2, breaks.Count);
            Assert.Equal(2, breaks.Find(b => b.Part == "mouth").Severity);
            Assert.Equal(5, breaks.Find(b => b.Part == "wing").Severity);
        }

        [Fact]
        public void ExecuteRest_HealsToZero_RemovesEntry()
        {
            SeedBobWithBreaks(("mouth", 1));
            var result = ChateauRest.ExecuteRest(_database, "Bob", null);
            Assert.NotEqual(string.Empty, result.ChannelMessage);
            Assert.Contains("fully recovered", result.ChannelMessage);

            var bob = _database.GetProfile("Bob");
            var breaks = BreakInstance.LoadAll(bob);
            Assert.Empty(breaks);
        }

        [Fact]
        public void ExecuteRest_SetsRestUsedTimerToNextMidnight()
        {
            SeedBobWithBreaks(("mouth", 3));
            ChateauRest.ExecuteRest(_database, "Bob", null);

            var bob = _database.GetProfile("Bob");
            Assert.True(bob.timers.ContainsKey(ChateauRest.RestUsedTimerKey));
            Assert.Equal(DateTime.UtcNow.Date.AddDays(1), bob.timers[ChateauRest.RestUsedTimerKey].timerEnd);
        }

        [Fact]
        public void ExecuteRest_AlsoSetsWorkTimerToBlockWork()
        {
            SeedBobWithBreaks(("mouth", 3));
            ChateauRest.ExecuteRest(_database, "Bob", null);

            var bob = _database.GetProfile("Bob");
            Assert.True(bob.timers.ContainsKey(ChateauRest.WorkTimerKey));
            Assert.True(bob.timers[ChateauRest.WorkTimerKey].timerEnd > DateTime.UtcNow);
        }

        // -------------------------------------------------------------------
        // Setup helpers
        // -------------------------------------------------------------------

        private void SeedBobWithBreaks(params (string Part, int Severity)[] breaks)
        {
            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);
            BreakInstance.SaveAll(bob, new List<BreakInstance>(System.Linq.Enumerable.Select(breaks, b =>
                new BreakInstance
                {
                    Part = b.Part,
                    Severity = b.Severity,
                    BrokenBy = "Alice",
                    BrokenAt = DateTime.UtcNow,
                    LastTickedAt = DateTime.UtcNow.Date,
                })));
            _database.SetProfile("Bob", bob);
        }
    }
}
