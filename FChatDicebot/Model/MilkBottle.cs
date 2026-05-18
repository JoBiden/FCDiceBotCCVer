using MongoDB.Bson.Serialization.Attributes;
using System;

namespace FChatDicebot.Model
{
    /// <summary>
    /// One milking session's worth of bottled fluid sitting in a Profile's milkInventory.
    /// Stored per-milking (not aggregated) so different sessions of the same
    /// (substance, sourceName) keep their distinct milkedAt timestamps and corruption tags
    /// for display and FIFO sell ordering.
    /// </summary>
    public class MilkBottle
    {
        /// <summary>Identifier name from the substance/vice catalog (e.g. "cum", "milk").</summary>
        public string substance { get; set; }

        /// <summary>Recipient's userName at the time of milking. Frozen — does not follow renames.</summary>
        public string sourceName { get; set; }

        /// <summary>When the milking happened. Used for FIFO sell ordering and tiebreakers.</summary>
        public DateTime milkedAt { get; set; }

        /// <summary>How many bottles this milking session produced. Always &gt;= 1.</summary>
        public int quantity { get; set; }

        /// <summary>
        /// Optional flavor tag derived from the recipient's corruption value at milking time:
        /// "corrupt" (corruption &lt;= -CorruptionTagThreshold), "purified" (&gt;= +threshold),
        /// or null in the neutral band. Drives sell-price multipliers in
        /// <see cref="ChateauCurrency"/>.
        /// </summary>
        [BsonIgnoreIfNull]
        public string corruptionTag { get; set; }
    }
}
