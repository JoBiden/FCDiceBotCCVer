using System;
using System.Collections.Generic;

namespace FChatDicebot
{
    /// <summary>
    /// Constants and lookup tables for the bottle economy: milk-inventory pricing
    /// tiers, corruption-tag price multipliers, and the corruption-value thresholds
    /// used to assign tags at milking time.
    ///
    /// Currencies themselves are stored in <see cref="Model.Profile.currencies"/>
    /// (per-name int buckets). This class does not convert between denominations —
    /// copper / silver / gold remain independent currencies that the player may
    /// exchange manually. <see cref="SellPayoutCurrency"/> names the currency
    /// bucket <c>!sell</c> deposits its substance-value proceeds into;
    /// <see cref="BottleCurrency"/> names the side-currency credited one-per-bottle
    /// as the Chateau hands the empty back. The Chateau always provides the empties
    /// needed to <c>!milk</c>, so no profile-level "empty bottle" inventory exists.
    /// </summary>
    public static class ChateauCurrency
    {
        /// <summary>
        /// Currency bucket <c>!sell</c> deposits substance-value proceeds into.
        /// Pricing constants below are denominated in this currency.
        /// </summary>
        public const string SellPayoutCurrency = "copper";

        /// <summary>
        /// Side-currency returned to the seller as the Chateau hands the empty bottle
        /// back: one unit per sold bottle. Tracked in <see cref="Model.Profile.currencies"/>
        /// like any other currency.
        /// </summary>
        public const string BottleCurrency = "bottle";

        // -----------------------------------------------------------------------
        // !milk roll
        // -----------------------------------------------------------------------

        /// <summary>Inclusive lower bound for the milk bottle quantity roll.</summary>
        public const int MilkRollMin = 1;

        /// <summary>Inclusive upper bound for the milk bottle quantity roll.</summary>
        public const int MilkRollMax = 3;

        // -----------------------------------------------------------------------
        // Corruption tag thresholds (applied at milking time)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Recipient corruption value at or beyond which the milked bottles get
        /// a corruption tag. <c>corruption &lt;= -Threshold</c> ⇒ <c>"corrupt"</c>;
        /// <c>corruption &gt;= +Threshold</c> ⇒ <c>"purified"</c>; otherwise no tag.
        /// </summary>
        public const int CorruptionTagThreshold = 10;

        public const string CorruptTag = "corrupt";
        public const string PurifiedTag = "purified";

        // -----------------------------------------------------------------------
        // Sell pricing (in SellPayoutCurrency — see above)
        // -----------------------------------------------------------------------

        /// <summary>Base price per bottle for the "common" tier (water, milk).</summary>
        public const int CommonBottlePrice = 5;

        /// <summary>Base price per bottle for the "standard" tier (cum, sweat, saliva, etc.).</summary>
        public const int StandardBottlePrice = 25;

        /// <summary>Base price per bottle for the "rare" tier (special-flagged substances).</summary>
        public const int RareBottlePrice = 100;

        /// <summary>Multiplier applied to a bottle's base price when its corruption tag is "corrupt".</summary>
        public const double CorruptPriceMultiplier = 1.5;

        /// <summary>Multiplier applied to a bottle's base price when its corruption tag is "purified".</summary>
        public const double PurifiedPriceMultiplier = 2.0;

        /// <summary>
        /// Substances that fall in the "common" tier. Everything not in either the
        /// common or rare set falls through to the "standard" tier — so adding a
        /// new substance identifier without editing this file gets a sensible
        /// default rather than a crash.
        /// </summary>
        private static readonly HashSet<string> CommonSubstances = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "milk",
            "water",
        };

        /// <summary>
        /// Substances that fall in the "rare" tier. Empty by default; intended for
        /// substances later flagged rare in the identifier catalog (e.g. lustessence).
        /// </summary>
        private static readonly HashSet<string> RareSubstances = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "lustessence",
        };

        /// <summary>
        /// Resolve the base per-bottle price for a substance identifier. Unknown
        /// substances default to the standard tier so a missing entry doesn't
        /// produce a crash or a zero-value bottle.
        /// </summary>
        public static int GetBasePrice(string substance)
        {
            if (string.IsNullOrEmpty(substance)) return StandardBottlePrice;
            if (CommonSubstances.Contains(substance)) return CommonBottlePrice;
            if (RareSubstances.Contains(substance)) return RareBottlePrice;
            return StandardBottlePrice;
        }

        /// <summary>
        /// Final per-bottle sell price after applying the corruption-tag multiplier.
        /// Rounded down to a whole copper so the payout stays in integer currency.
        /// </summary>
        public static int GetSellPricePerBottle(string substance, string corruptionTag)
        {
            int basePrice = GetBasePrice(substance);
            double multiplier = 1.0;
            if (string.Equals(corruptionTag, CorruptTag, StringComparison.OrdinalIgnoreCase))
            {
                multiplier = CorruptPriceMultiplier;
            }
            else if (string.Equals(corruptionTag, PurifiedTag, StringComparison.OrdinalIgnoreCase))
            {
                multiplier = PurifiedPriceMultiplier;
            }
            return (int)Math.Floor(basePrice * multiplier);
        }

        /// <summary>
        /// Map a recipient's corruption value to the corruption tag a fresh
        /// milking should carry: <c>"corrupt"</c>, <c>"purified"</c>, or null in
        /// the neutral band.
        /// </summary>
        public static string GetCorruptionTagForValue(int corruption)
        {
            if (corruption <= -CorruptionTagThreshold) return CorruptTag;
            if (corruption >= CorruptionTagThreshold) return PurifiedTag;
            return null;
        }
    }
}
