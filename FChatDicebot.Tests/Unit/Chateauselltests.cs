using FChatDicebot;
using FChatDicebot.BotCommands;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for ChateauSell.SellBottles — the pure inventory-mutation helper underlying
    /// the !sell command. Operates directly on a Profile instance, so no database fixture
    /// is needed.
    ///
    /// Sell order is LIFO (newest first); a single MilkBottle entry can be partially
    /// consumed, leaving its remainder under the same milkedAt timestamp.
    /// </summary>
    public class ChateauSellTests
    {
        // -------------------------------------------------------------------
        // No-op cases
        // -------------------------------------------------------------------

        [Fact]
        public void SellBottles_EmptyInventory_ReturnsZeroEverything()
        {
            var profile = new ProfileBuilder().Build();

            var result = ChateauSell.SellBottles(profile, null, null, 1);

            Assert.Equal(0, result.BottlesSold);
            Assert.Equal(0, result.PayoutCopper);
            Assert.Equal(0, result.BottlesReturned);
        }

        [Fact]
        public void SellBottles_FilterMatchesNothing_NoMutation()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle("cum", "Bob", 2, null, hour: 1))
                .Build();

            var result = ChateauSell.SellBottles(profile, substanceFilter: "milk", sourceFilter: null, requestedAmount: 5);

            Assert.Equal(0, result.BottlesSold);
            Assert.Single(profile.milkInventory); // untouched
        }

        [Fact]
        public void SellBottles_ZeroOrNegativeAmount_NoMutation()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle("cum", "Bob", 2, null, hour: 1))
                .Build();

            var result = ChateauSell.SellBottles(profile, null, null, requestedAmount: 0);

            Assert.Equal(0, result.BottlesSold);
            Assert.Single(profile.milkInventory);
        }

        // -------------------------------------------------------------------
        // LIFO and partial consumption
        // -------------------------------------------------------------------

        [Fact]
        public void SellBottles_AmountOne_SellsMostRecentBottle()
        {
            // Two entries: hour:1 (older) and hour:5 (newest). Selling 1 should consume
            // 1 bottle from the newest entry.
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle("cum", "Bob", 2, null, hour: 1))
                .WithMilkBottle(NewBottle("cum", "Bob", 1, null, hour: 5))
                .Build();

            var result = ChateauSell.SellBottles(profile, null, null, requestedAmount: 1);

            Assert.Equal(1, result.BottlesSold);
            // The hour:5 entry had qty 1, so it should be removed entirely.
            Assert.Single(profile.milkInventory);
            Assert.Equal(DateTime.UtcNow.Date.AddHours(1), profile.milkInventory[0].milkedAt);
        }

        [Fact]
        public void SellBottles_AmountSpansMultipleEntries_LIFO()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle("cum", "Bob", 2, null, hour: 1))
                .WithMilkBottle(NewBottle("cum", "Bob", 2, null, hour: 2))
                .WithMilkBottle(NewBottle("cum", "Bob", 2, null, hour: 3))
                .Build();

            // Sell 3 — should consume newest (qty 2 from hour:3) + 1 from hour:2.
            var result = ChateauSell.SellBottles(profile, null, null, requestedAmount: 3);

            Assert.Equal(3, result.BottlesSold);
            Assert.Equal(2, profile.milkInventory.Count);
            // hour:3 fully consumed
            Assert.DoesNotContain(profile.milkInventory, b => b.milkedAt == DateTime.UtcNow.Date.AddHours(3));
            // hour:2 has 1 remaining
            var middle = profile.milkInventory.Find(b => b.milkedAt == DateTime.UtcNow.Date.AddHours(2));
            Assert.Equal(1, middle.quantity);
            // hour:1 untouched
            var oldest = profile.milkInventory.Find(b => b.milkedAt == DateTime.UtcNow.Date.AddHours(1));
            Assert.Equal(2, oldest.quantity);
        }

        [Fact]
        public void SellBottles_AmountExceedsInventory_SellsWhatYouHave()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle("cum", "Bob", 2, null, hour: 1))
                .Build();

            var result = ChateauSell.SellBottles(profile, null, null, requestedAmount: 100);

            Assert.Equal(2, result.BottlesSold);
            Assert.Empty(profile.milkInventory);
        }

        [Fact]
        public void SellBottles_PartiallyConsumesNewestEntry()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle("cum", "Bob", 3, null, hour: 2))
                .Build();

            var result = ChateauSell.SellBottles(profile, null, null, requestedAmount: 1);

            Assert.Equal(1, result.BottlesSold);
            Assert.Single(profile.milkInventory);
            Assert.Equal(2, profile.milkInventory[0].quantity);
        }

        // -------------------------------------------------------------------
        // Filters
        // -------------------------------------------------------------------

        [Fact]
        public void SellBottles_SubstanceFilter_SkipsOtherSubstances()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle("milk", "Bob", 1, null, hour: 5))
                .WithMilkBottle(NewBottle("cum",  "Bob", 1, null, hour: 1))
                .Build();

            // Even though "milk" is newer, the filter narrows it to "cum".
            var result = ChateauSell.SellBottles(profile, substanceFilter: "cum", sourceFilter: null, requestedAmount: 5);

            Assert.Equal(1, result.BottlesSold);
            Assert.Single(profile.milkInventory);
            Assert.Equal("milk", profile.milkInventory[0].substance);
        }

        [Fact]
        public void SellBottles_SourceFilter_SkipsOtherSources()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle("cum", "Alice", 1, null, hour: 5))
                .WithMilkBottle(NewBottle("cum", "Bob",   1, null, hour: 1))
                .Build();

            var result = ChateauSell.SellBottles(profile, substanceFilter: null, sourceFilter: "Bob", requestedAmount: 5);

            Assert.Equal(1, result.BottlesSold);
            Assert.Single(profile.milkInventory);
            Assert.Equal("Alice", profile.milkInventory[0].sourceName);
        }

        // -------------------------------------------------------------------
        // Pricing
        // -------------------------------------------------------------------

        [Fact]
        public void SellBottles_StandardTier_NoTag_BasePrice()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle("cum", "Bob", 3, null, hour: 1))
                .Build();

            var result = ChateauSell.SellBottles(profile, null, null, 100);

            Assert.Equal(3 * ChateauCurrency.StandardBottlePrice, result.PayoutCopper);
        }

        [Fact]
        public void SellBottles_CorruptTag_AppliesMultiplier()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle("cum", "Bob", 2, ChateauCurrency.CorruptTag, hour: 1))
                .Build();

            var result = ChateauSell.SellBottles(profile, null, null, 100);

            int perBottle = ChateauCurrency.GetSellPricePerBottle("cum", ChateauCurrency.CorruptTag);
            Assert.Equal(2 * perBottle, result.PayoutCopper);
        }

        [Fact]
        public void SellBottles_PurifiedTag_AppliesMultiplier()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle("cum", "Bob", 2, ChateauCurrency.PurifiedTag, hour: 1))
                .Build();

            var result = ChateauSell.SellBottles(profile, null, null, 100);

            int perBottle = ChateauCurrency.GetSellPricePerBottle("cum", ChateauCurrency.PurifiedTag);
            Assert.Equal(2 * perBottle, result.PayoutCopper);
        }

        [Fact]
        public void SellBottles_BottlesReturned_OneToOneWithBottlesSold()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle("cum", "Bob", 3, null, hour: 1))
                .Build();

            var result = ChateauSell.SellBottles(profile, null, null, 100);

            Assert.Equal(3, result.BottlesSold);
            Assert.Equal(3, result.BottlesReturned);
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private static MilkBottle NewBottle(string substance, string source, int qty, string tag, int hour)
        {
            return new MilkBottle
            {
                substance = substance,
                sourceName = source,
                quantity = qty,
                corruptionTag = tag,
                // milkedAt is encoded as DateTime.UtcNow.Date + hour offset so tests can
                // pin a deterministic LIFO order without relying on wall-clock micro
                // ordering between consecutive `new MilkBottle` calls.
                milkedAt = DateTime.UtcNow.Date.AddHours(hour),
            };
        }
    }
}
