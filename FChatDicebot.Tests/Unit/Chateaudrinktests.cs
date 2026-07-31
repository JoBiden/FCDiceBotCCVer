using FChatDicebot;
using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for ChateauDrink.Execute — the pure drink logic behind !drink.
    ///
    /// The behaviour that distinguishes drinking from selling: the bottle is emptied rather than
    /// removed, so its serial stays in the collection as proof of what was drunk, and no bottle
    /// currency is credited because the physical empty is the keepsake instead.
    /// </summary>
    [Collection("Database")]
    public class ChateauDrinkTests
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ChateauDrinkTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;

            _fixture.SeedIdentifier(new Identifier
            {
                type = "cum",
                description = "cum",
                categories = new[] { "substance", "vice" }
            });
        }

        // -------------------------------------------------------------------
        // Consumption
        // -------------------------------------------------------------------

        [Fact]
        public void Execute_EmptiesBottleButKeepsItAndItsSerial()
        {
            SeedDrinker("Alice", Bottle(42, "cum", "Bob", hour: 1));

            var result = ChateauDrink.Execute(_database, "Alice", null, 0, NeverRolls());

            Assert.NotNull(result.Bottle);
            var stored = Assert.Single(_database.GetProfile("Alice").milkInventory);
            Assert.Equal(42, stored.serial);
            Assert.True(stored.IsEmpty);
        }

        [Fact]
        public void Execute_CreditsNoBottleCurrency()
        {
            // Selling hands the empty to the Chateau for a bottle currency; drinking keeps it,
            // so the tally must not move.
            SeedDrinker("Alice", Bottle(1, "cum", "Bob", hour: 1));

            ChateauDrink.Execute(_database, "Alice", null, 0, NeverRolls());

            var currencies = _database.GetProfile("Alice").currencies;
            Assert.False(currencies != null
                && currencies.TryGetValue(ChateauCurrency.BottleCurrency, out int bottles)
                && bottles > 0);
        }

        [Fact]
        public void Execute_ImplicitSelectionTakesNewestFull()
        {
            SeedDrinker("Alice",
                Bottle(1, "cum", "Bob", hour: 1),
                Bottle(2, "cum", "Bob", hour: 9));

            var result = ChateauDrink.Execute(_database, "Alice", null, 0, NeverRolls());

            Assert.Equal(2, result.Bottle.serial);
        }

        [Fact]
        public void Execute_ImplicitSelectionSkipsEmpties()
        {
            // The empty is newest, but an empty is never picked up implicitly.
            var empty = Bottle(2, "cum", "Bob", hour: 9);
            empty.emptiedAt = DateTime.UtcNow;
            SeedDrinker("Alice", Bottle(1, "cum", "Bob", hour: 1), empty);

            var result = ChateauDrink.Execute(_database, "Alice", null, 0, NeverRolls());

            Assert.Equal(1, result.Bottle.serial);
        }

        [Fact]
        public void Execute_SerialSelectsThatExactBottle()
        {
            SeedDrinker("Alice",
                Bottle(11, "cum", "Bob", hour: 9),
                Bottle(12, "cum", "Bob", hour: 1));

            var result = ChateauDrink.Execute(_database, "Alice", null, 12, NeverRolls());

            Assert.Equal(12, result.Bottle.serial);
        }

        [Fact]
        public void Execute_UnknownSerial_ChangesNothing()
        {
            SeedDrinker("Alice", Bottle(11, "cum", "Bob", hour: 1));

            var result = ChateauDrink.Execute(_database, "Alice", null, 99, NeverRolls());

            Assert.Null(result.Bottle);
            Assert.Contains("#99", result.PrivateMessage);
            Assert.False(_database.GetProfile("Alice").milkInventory[0].IsEmpty);
        }

        [Fact]
        public void Execute_AlreadyEmptySerial_GetsItsOwnMessage()
        {
            // Distinct from "not in your collection": the resident does hold #11, they just
            // already drank it, and the useful remedy is different.
            var empty = Bottle(11, "cum", "Bob", hour: 1);
            empty.emptiedAt = DateTime.UtcNow;
            SeedDrinker("Alice", empty);

            var result = ChateauDrink.Execute(_database, "Alice", null, 11, NeverRolls());

            Assert.Null(result.Bottle);
            Assert.Contains("already empty", result.PrivateMessage);
        }

        [Fact]
        public void Execute_EmptyCollection_ExplainsWhereBottlesComeFrom()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);

            var result = ChateauDrink.Execute(_database, "Alice", null, 0, NeverRolls());

            Assert.Null(result.Bottle);
            Assert.Equal(ChateauBottles.EmptyCollectionText, result.PrivateMessage);
            Assert.Empty(result.ChannelMessage);
        }

        [Fact]
        public void Execute_SubstanceFilterMiss_ChangesNothing()
        {
            SeedDrinker("Alice", Bottle(1, "cum", "Bob", hour: 1));

            var result = ChateauDrink.Execute(_database, "Alice", "milk", 0, NeverRolls());

            Assert.Null(result.Bottle);
            Assert.False(_database.GetProfile("Alice").milkInventory[0].IsEmpty);
        }

        // -------------------------------------------------------------------
        // Corruption transfer
        // -------------------------------------------------------------------

        [Fact]
        public void Execute_CorruptBottle_ShiftsDrinkerTowardCorruption()
        {
            SeedDrinker("Alice", Bottle(1, "cum", "Bob", hour: 1, tag: ChateauCurrency.CorruptTag));

            var result = ChateauDrink.Execute(_database, "Alice", null, 0, NeverRolls());

            Assert.Equal(-1, result.CorruptionApplied);
            Assert.Equal(-1, CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice")));
        }

        [Fact]
        public void Execute_PurifiedBottle_ShiftsDrinkerTowardPurity()
        {
            SeedDrinker("Alice", Bottle(1, "cum", "Bob", hour: 1, tag: ChateauCurrency.PurifiedTag));

            var result = ChateauDrink.Execute(_database, "Alice", null, 0, NeverRolls());

            Assert.Equal(1, result.CorruptionApplied);
            Assert.Equal(1, CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice")));
        }

        [Fact]
        public void Execute_UntaggedBottle_MovesNothing()
        {
            SeedDrinker("Alice", Bottle(1, "cum", "Bob", hour: 1));

            var result = ChateauDrink.Execute(_database, "Alice", null, 0, NeverRolls());

            Assert.Equal(0, result.CorruptionApplied);
            Assert.False(result.BudgetSpent);
            Assert.Equal(0, CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice")));
        }

        [Fact]
        public void Execute_PastDailyBudget_StillDrinksButMovesNothing()
        {
            var bottles = Enumerable.Range(1, ChateauCurrency.DrinkCorruptionDailyLimit + 1)
                .Select(i => Bottle(i, "cum", "Bob", hour: i, tag: ChateauCurrency.CorruptTag))
                .ToArray();
            SeedDrinker("Alice", bottles);

            for (int i = 0; i < ChateauCurrency.DrinkCorruptionDailyLimit; i++)
            {
                ChateauDrink.Execute(_database, "Alice", null, 0, NeverRolls());
            }
            int atBudget = CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice"));

            var overBudget = ChateauDrink.Execute(_database, "Alice", null, 0, NeverRolls());

            // The bottle is still drunk and still narrated, it just stops moving the needle.
            Assert.NotNull(overBudget.Bottle);
            Assert.True(overBudget.BudgetSpent);
            Assert.Equal(0, overBudget.CorruptionApplied);
            Assert.Equal(atBudget, CorruptionProcessor.ReadCorruption(_database.GetProfile("Alice")));
            Assert.Contains("metabolize", overBudget.ChannelMessage);
        }

        [Fact]
        public void Execute_DrinkBudget_DoesNotTouchCorruptQuota()
        {
            SeedDrinker("Alice", Bottle(1, "cum", "Bob", hour: 1, tag: ChateauCurrency.CorruptTag));

            ChateauDrink.Execute(_database, "Alice", null, 0, NeverRolls());

            var magnitudes = _database.GetProfile("Alice").dailyMagnitudes;
            Assert.Contains(magnitudes, kv => kv.Key.StartsWith("drinkshift_"));
            Assert.DoesNotContain(magnitudes, kv => kv.Key.StartsWith("corruption_"));
        }

        [Fact]
        public void RecordUsedDrinkShift_PrunesStaleDays()
        {
            var profile = new ProfileBuilder().Build();
            profile.dailyMagnitudes = new System.Collections.Generic.Dictionary<string, int>
            {
                { ChateauDrink.DrinkShiftQuotaKey(DateTime.UtcNow.Date.AddDays(-3)), 3 },
            };

            ChateauDrink.RecordUsedDrinkShift(profile, DateTime.UtcNow.Date, 1);

            Assert.Single(profile.dailyMagnitudes);
            Assert.Equal(1, ChateauDrink.GetUsedDrinkShift(profile, DateTime.UtcNow.Date));
        }

        // -------------------------------------------------------------------
        // Addiction
        // -------------------------------------------------------------------

        [Fact]
        public void Execute_AddictionRoll_IntensifiesAnExistingVice()
        {
            var profile = SeedDrinker("Alice", Bottle(1, "cum", "Bob", hour: 1));
            ViceInstance.SaveAll(profile, new System.Collections.Generic.List<ViceInstance>
            {
                new ViceInstance { Vice = "cum", AddictionLevel = 2, DosedBy = "Bob", FirstDosedAt = DateTime.UtcNow },
            });
            _database.SetProfile("Alice", profile);

            var result = ChateauDrink.Execute(_database, "Alice", null, 0, AlwaysRolls());

            Assert.True(result.AddictionIntensified);
            var vice = ViceInstance.LoadAll(_database.GetProfile("Alice")).Single();
            Assert.Equal(3, vice.AddictionLevel);
        }

        [Fact]
        public void Execute_AddictionRoll_NeverCreatesANewVice()
        {
            SeedDrinker("Alice", Bottle(1, "cum", "Bob", hour: 1));

            var result = ChateauDrink.Execute(_database, "Alice", null, 0, AlwaysRolls());

            Assert.False(result.AddictionIntensified);
            Assert.Empty(ViceInstance.LoadAll(_database.GetProfile("Alice")));
        }

        [Fact]
        public void Execute_SatisfyingACravingStillTightensTheHook()
        {
            // Drinking what you're addicted to both quiets the craving and deepens it. The two
            // are not exclusive: restricting the roll to unsatisfied drinks would make it
            // unreachable, since only an existing vice can intensify and an existing vice is
            // exactly what satisfaction requires.
            var profile = SeedDrinker("Alice", Bottle(1, "cum", "Bob", hour: 1));
            ViceInstance.SaveAll(profile, new System.Collections.Generic.List<ViceInstance>
            {
                new ViceInstance { Vice = "cum", AddictionLevel = 1, DosedBy = "Bob", FirstDosedAt = DateTime.UtcNow },
            });
            _database.SetProfile("Alice", profile);

            var result = ChateauDrink.Execute(_database, "Alice", null, 0, AlwaysRolls());

            Assert.True(result.AddictionIntensified);
            Assert.Equal(2, ViceInstance.LoadAll(_database.GetProfile("Alice")).Single().AddictionLevel);
        }

        // -------------------------------------------------------------------
        // Serial parsing
        // -------------------------------------------------------------------

        [Theory]
        [InlineData(new[] { "#142" }, 142)]
        [InlineData(new[] { "cum", "#7" }, 7)]
        [InlineData(new[] { "142" }, 0)]      // bare numbers are counts elsewhere, never serials
        [InlineData(new[] { "#0" }, 0)]
        [InlineData(new[] { "#abc" }, 0)]
        [InlineData(new string[0], 0)]
        public void ParseSerial_RequiresTheHashPrefix(string[] terms, int expected)
        {
            Assert.Equal(expected, ChateauDrink.ParseSerial(terms));
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private Profile SeedDrinker(string userName, params MilkBottle[] bottles)
        {
            var builder = new ProfileBuilder().WithUserName(userName).WithDisplayName(userName);
            foreach (var bottle in bottles) builder.WithMilkBottle(bottle);
            builder.BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            return _database.GetProfile(userName);
        }

        private static MilkBottle Bottle(int serial, string substance, string source, int hour, string tag = null)
        {
            return BottleInventoryTests.NewBottle(serial, substance, source, hour, tag);
        }

        /// <summary>Rng whose NextDouble always clears the addiction threshold (no intensify).</summary>
        private static Random NeverRolls() => new FixedSampleRandom(0.999);

        /// <summary>Rng whose NextDouble always lands under the addiction threshold.</summary>
        private static Random AlwaysRolls() => new FixedSampleRandom(0.0);

        private class FixedSampleRandom : Random
        {
            private readonly double _sample;
            public FixedSampleRandom(double sample) { _sample = sample; }
            protected override double Sample() => _sample;
        }
    }
}
