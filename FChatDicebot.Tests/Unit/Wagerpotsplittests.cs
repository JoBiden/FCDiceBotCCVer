using FChatDicebot.DiceFunctions.Wager;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Pure tests for the per-currency-column pot-split math (no database needed).
    /// </summary>
    public class WagerPotSplitTests
    {
        [Fact]
        public void Split_AppliesPercentsPerCurrencyColumn_DustToFirst()
        {
            var totals = new Dictionary<string, int> { { "copper", 90 }, { "rosequartz", 60 } };
            var winners = new List<string> { "Alice", "Bob" };
            var percents = new List<int> { 70, 30 };

            var awards = WagerGameSupport.ComputePotSplit(totals, winners, percents);

            // Runner-up gets floor(total*pct); first place gets the remainder (its share + dust).
            Assert.Equal(63, awards["Alice"]["copper"]);     // 90 - floor(90*0.30)=27
            Assert.Equal(27, awards["Bob"]["copper"]);
            Assert.Equal(42, awards["Alice"]["rosequartz"]); // 60 - floor(60*0.30)=18
            Assert.Equal(18, awards["Bob"]["rosequartz"]);
        }

        [Fact]
        public void Split_ConservesEveryColumn_NoDustLost()
        {
            var totals = new Dictionary<string, int> { { "copper", 10 } };
            var winners = new List<string> { "A", "B", "C" };
            var percents = new List<int> { 33, 33, 34 };

            var awards = WagerGameSupport.ComputePotSplit(totals, winners, percents);

            int sum = awards["A"]["copper"] + awards["B"]["copper"] + awards["C"]["copper"];
            Assert.Equal(10, sum);                 // nothing lost to rounding
            Assert.Equal(4, awards["A"]["copper"]); // 10 - 3 - 3 (B and C floor to 3 each)
        }

        [Fact]
        public void Split_SinglePlaceOrNoSplit_WholeColumnToFirst()
        {
            var totals = new Dictionary<string, int> { { "copper", 50 } };
            var winners = new List<string> { "Alice", "Bob" };
            var percents = new List<int> { 100 }; // only one place defined

            var awards = WagerGameSupport.ComputePotSplit(totals, winners, percents);

            Assert.Equal(50, awards["Alice"]["copper"]);
            Assert.False(awards.ContainsKey("Bob"));
        }

        [Fact]
        public void Split_WinnerTakeAll_SingleWinner()
        {
            var totals = new Dictionary<string, int> { { "copper", 25 }, { "gold", 7 } };
            var winners = new List<string> { "Alice" };
            var percents = new List<int> { 100 };

            var awards = WagerGameSupport.ComputePotSplit(totals, winners, percents);

            Assert.Equal(25, awards["Alice"]["copper"]);
            Assert.Equal(7, awards["Alice"]["gold"]);
        }

        [Fact]
        public void Split_EmptyInputs_ReturnNothing()
        {
            Assert.Empty(WagerGameSupport.ComputePotSplit(null, new List<string> { "A" }, new List<int> { 100 }));
            Assert.Empty(WagerGameSupport.ComputePotSplit(new Dictionary<string, int> { { "copper", 5 } }, new List<string>(), new List<int>()));
        }
    }
}
