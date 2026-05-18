using FChatDicebot;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for the ChateauCurrency constants and price-calculation helpers. These are
    /// pure functions — no database needed — so the class isn't part of the Database
    /// collection.
    /// </summary>
    public class ChateauCurrencyTests
    {
        // -------------------------------------------------------------------
        // GetBasePrice
        // -------------------------------------------------------------------

        [Theory]
        [InlineData("milk")]
        [InlineData("water")]
        [InlineData("MILK")] // case-insensitive
        public void GetBasePrice_CommonTier(string substance)
        {
            Assert.Equal(ChateauCurrency.CommonBottlePrice, ChateauCurrency.GetBasePrice(substance));
        }

        [Theory]
        [InlineData("lustessence")]
        public void GetBasePrice_RareTier(string substance)
        {
            Assert.Equal(ChateauCurrency.RareBottlePrice, ChateauCurrency.GetBasePrice(substance));
        }

        [Theory]
        [InlineData("cum")]
        [InlineData("sweat")]
        [InlineData("saliva")]
        [InlineData("something-unknown-fallback")]
        public void GetBasePrice_StandardTier_DefaultForEverythingElse(string substance)
        {
            Assert.Equal(ChateauCurrency.StandardBottlePrice, ChateauCurrency.GetBasePrice(substance));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetBasePrice_NullOrEmpty_DefaultsToStandard(string substance)
        {
            // Defensive: a missing substance shouldn't crash or zero out the payout.
            Assert.Equal(ChateauCurrency.StandardBottlePrice, ChateauCurrency.GetBasePrice(substance));
        }

        // -------------------------------------------------------------------
        // GetSellPricePerBottle (with corruption multipliers)
        // -------------------------------------------------------------------

        [Fact]
        public void GetSellPricePerBottle_NoTag_BasePrice()
        {
            Assert.Equal(
                ChateauCurrency.StandardBottlePrice,
                ChateauCurrency.GetSellPricePerBottle("cum", null));
        }

        [Fact]
        public void GetSellPricePerBottle_CorruptTag_AppliesCorruptMultiplier()
        {
            // 25 * 1.5 = 37.5 → floor 37
            int expected = (int)System.Math.Floor(
                ChateauCurrency.StandardBottlePrice * ChateauCurrency.CorruptPriceMultiplier);
            Assert.Equal(expected, ChateauCurrency.GetSellPricePerBottle("cum", ChateauCurrency.CorruptTag));
        }

        [Fact]
        public void GetSellPricePerBottle_PurifiedTag_AppliesPurifiedMultiplier()
        {
            // 25 * 2 = 50
            int expected = (int)System.Math.Floor(
                ChateauCurrency.StandardBottlePrice * ChateauCurrency.PurifiedPriceMultiplier);
            Assert.Equal(expected, ChateauCurrency.GetSellPricePerBottle("cum", ChateauCurrency.PurifiedTag));
        }

        [Fact]
        public void GetSellPricePerBottle_UnknownTag_TreatedAsNoTag()
        {
            // A historical or unknown tag should fall through to the base multiplier (1x)
            // rather than crashing.
            Assert.Equal(
                ChateauCurrency.StandardBottlePrice,
                ChateauCurrency.GetSellPricePerBottle("cum", "ineffable"));
        }

        // -------------------------------------------------------------------
        // GetCorruptionTagForValue (thresholds)
        // -------------------------------------------------------------------

        [Theory]
        [InlineData(-100)]
        [InlineData(-10)]
        public void GetCorruptionTagForValue_AtOrBelowNegativeThreshold_ReturnsCorrupt(int corruption)
        {
            Assert.Equal(ChateauCurrency.CorruptTag, ChateauCurrency.GetCorruptionTagForValue(corruption));
        }

        [Theory]
        [InlineData(100)]
        [InlineData(10)]
        public void GetCorruptionTagForValue_AtOrAbovePositiveThreshold_ReturnsPurified(int corruption)
        {
            Assert.Equal(ChateauCurrency.PurifiedTag, ChateauCurrency.GetCorruptionTagForValue(corruption));
        }

        [Theory]
        [InlineData(-9)]
        [InlineData(0)]
        [InlineData(9)]
        public void GetCorruptionTagForValue_InNeutralBand_ReturnsNull(int corruption)
        {
            Assert.Null(ChateauCurrency.GetCorruptionTagForValue(corruption));
        }
    }
}
