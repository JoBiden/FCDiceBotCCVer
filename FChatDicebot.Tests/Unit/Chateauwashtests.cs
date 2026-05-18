using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit.BotCommands
{
    [Collection("Database")]
    public class ChateauWashTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ChateauWashTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        public void Dispose() { }

        [Fact]
        public void ExecuteWash_UnknownUser_ReturnsError()
        {
            var result = ChateauWash.ExecuteWash(_database, "Ghost", null);

            Assert.Equal(string.Empty, result.ChannelMessage);
            Assert.NotEqual(string.Empty, result.PrivateMessage);
        }

        [Fact]
        public void ExecuteWash_NoScents_ReturnsErrorPrivately()
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var result = ChateauWash.ExecuteWash(_database, "Bob", null);

            Assert.Equal(string.Empty, result.ChannelMessage);
            Assert.Contains("squeaky clean", result.PrivateMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExecuteWash_SpecifiedScent_DecrementsLayer()
        {
            SeedBobWithScents(
                new ScentLayer { Scent = "musk", Layers = 3, RemainingMentions = 9, AppliedBy = "Alice", LastAppliedAt = DateTime.UtcNow });

            var result = ChateauWash.ExecuteWash(_database, "Bob", "musk");

            Assert.Contains("musk", result.ChannelMessage);
            var bob = _database.GetProfile("Bob");
            var layers = ScentLayer.LoadAll(bob);
            Assert.Single(layers);
            Assert.Equal(2, layers[0].Layers);
            Assert.Equal(6, layers[0].RemainingMentions);
        }

        [Fact]
        public void ExecuteWash_NoArgument_PicksHighestSaturation()
        {
            SeedBobWithScents(
                new ScentLayer { Scent = "ozone", Layers = 1, RemainingMentions = 3, AppliedBy = "Alice", LastAppliedAt = DateTime.UtcNow },
                new ScentLayer { Scent = "musk", Layers = 4, RemainingMentions = 12, AppliedBy = "Carol", LastAppliedAt = DateTime.UtcNow });

            var result = ChateauWash.ExecuteWash(_database, "Bob", null);

            Assert.Contains("musk", result.ChannelMessage);
            var bob = _database.GetProfile("Bob");
            var layers = ScentLayer.LoadAll(bob);
            Assert.Equal(2, layers.Count);
            foreach (var l in layers)
            {
                if (l.Scent == "musk") Assert.Equal(3, l.Layers);
                if (l.Scent == "ozone") Assert.Equal(1, l.Layers);
            }
        }

        [Fact]
        public void ExecuteWash_LayerHitsZero_RemovesEntry()
        {
            SeedBobWithScents(
                new ScentLayer { Scent = "musk", Layers = 1, RemainingMentions = 3, AppliedBy = "Alice", LastAppliedAt = DateTime.UtcNow });

            var result = ChateauWash.ExecuteWash(_database, "Bob", "musk");

            Assert.Contains("musk", result.ChannelMessage);
            Assert.Contains("gone entirely", result.ChannelMessage);
            var bob = _database.GetProfile("Bob");
            Assert.Empty(ScentLayer.LoadAll(bob));
        }

        [Fact]
        public void ExecuteWash_AlreadyWashedToday_ReturnsErrorPrivately()
        {
            SeedBobWithScents(
                new ScentLayer { Scent = "musk", Layers = 2, RemainingMentions = 6, AppliedBy = "Alice", LastAppliedAt = DateTime.UtcNow });
            var bob = _database.GetProfile("Bob");
            bob.timers["wash_used"] = new CoolDown { timerEnd = DateTime.UtcNow.AddHours(6) };
            _database.SetProfile("Bob", bob);

            var result = ChateauWash.ExecuteWash(_database, "Bob", "musk");

            Assert.Equal(string.Empty, result.ChannelMessage);
            Assert.Contains("already washed", result.PrivateMessage, StringComparison.OrdinalIgnoreCase);

            // State unchanged.
            var after = _database.GetProfile("Bob");
            var layers = ScentLayer.LoadAll(after);
            Assert.Equal(2, layers[0].Layers);
            Assert.Equal(6, layers[0].RemainingMentions);
        }

        [Fact]
        public void ExecuteWash_StaleTimer_AllowsWash()
        {
            SeedBobWithScents(
                new ScentLayer { Scent = "musk", Layers = 2, RemainingMentions = 6, AppliedBy = "Alice", LastAppliedAt = DateTime.UtcNow });
            var bob = _database.GetProfile("Bob");
            bob.timers["wash_used"] = new CoolDown { timerEnd = DateTime.UtcNow.AddHours(-2) };
            _database.SetProfile("Bob", bob);

            var result = ChateauWash.ExecuteWash(_database, "Bob", "musk");

            Assert.NotEqual(string.Empty, result.ChannelMessage);
            var after = _database.GetProfile("Bob");
            Assert.Equal(1, ScentLayer.LoadAll(after)[0].Layers);
        }

        [Fact]
        public void ExecuteWash_SetsCooldownToNextDayMidnight()
        {
            SeedBobWithScents(
                new ScentLayer { Scent = "musk", Layers = 2, RemainingMentions = 6, AppliedBy = "Alice", LastAppliedAt = DateTime.UtcNow });

            DateTime before = DateTime.UtcNow;
            ChateauWash.ExecuteWash(_database, "Bob", "musk");

            var bob = _database.GetProfile("Bob");
            Assert.True(bob.timers.ContainsKey("wash_used"));
            DateTime end = bob.timers["wash_used"].timerEnd;
            // Should be tomorrow's midnight UTC, i.e. before.Date.AddDays(1).
            Assert.Equal(before.Date.AddDays(1), end);
        }

        [Fact]
        public void ExecuteWash_SpecifiedScentNotPresent_ListsExistingScents()
        {
            SeedBobWithScents(
                new ScentLayer { Scent = "musk", Layers = 2, RemainingMentions = 6, AppliedBy = "Alice", LastAppliedAt = DateTime.UtcNow },
                new ScentLayer { Scent = "lemonade", Layers = 1, RemainingMentions = 3, AppliedBy = "Carol", LastAppliedAt = DateTime.UtcNow });

            var result = ChateauWash.ExecuteWash(_database, "Bob", "swampgas");

            Assert.Equal(string.Empty, result.ChannelMessage);
            // The error names what they asked for and lists what they actually have.
            Assert.Contains("swampgas", result.PrivateMessage);
            Assert.Contains("musk", result.PrivateMessage);
            Assert.Contains("lemonade", result.PrivateMessage);
            Assert.Contains("2 layers", result.PrivateMessage);
            Assert.Contains("1 layer", result.PrivateMessage);

            // Real scent untouched, no cooldown set.
            var bob = _database.GetProfile("Bob");
            Assert.Equal(2, ScentLayer.LoadAll(bob).Count);
            Assert.False(bob.timers.ContainsKey("wash_used"));
        }

        private void SeedBobWithScents(params ScentLayer[] scents)
        {
            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            ScentLayer.SaveAll(bob, new List<ScentLayer>(scents));
            _database.SetProfile("Bob", bob);
        }
    }
}
