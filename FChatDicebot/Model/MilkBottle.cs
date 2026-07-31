using MongoDB.Bson.Serialization.Attributes;
using System;

namespace FChatDicebot.Model
{
    /// <summary>
    /// One physical bottle sitting in a Profile's milkInventory, identified for life by its
    /// <see cref="serial"/>.
    ///
    /// Historically one entry represented a whole milking session and could carry
    /// <c>quantity &gt; 1</c>. Serial numbers identify a single bottle, so a milking that rolls
    /// 3 now writes three entries with three consecutive serials. <see cref="quantity"/> is
    /// retained so pre-serial documents still deserialize; nothing written since the serial
    /// migration ever sets it above 1.
    ///
    /// Drinking does not delete the bottle. It stamps <see cref="emptiedAt"/> and the empty
    /// stays in the collection under the same serial, keeping the substance, donor, and
    /// corruption tag it always had. Selling to the Chateau is what actually removes a bottle
    /// (the Chateau keeps it), which is what makes !sell and !drink a real choice rather than
    /// two spellings of the same thing.
    /// </summary>
    public class MilkBottle
    {
        /// <summary>
        /// Globally unique bottle number, issued from the shared counter at milking time and
        /// never reused. Zero only on documents predating the serial backfill.
        /// </summary>
        public int serial { get; set; }

        /// <summary>Identifier name from the substance/vice catalog (e.g. "cum", "milk").</summary>
        public string substance { get; set; }

        /// <summary>
        /// Donor's userName at the time of milking. Frozen — does not follow renames, so it
        /// must be resolved through <c>GetDisplayName</c> before reaching any player-facing
        /// string.
        /// </summary>
        public string sourceName { get; set; }

        /// <summary>When the milking happened. Used for newest-first ordering and tiebreakers.</summary>
        public DateTime milkedAt { get; set; }

        /// <summary>
        /// Legacy per-session bottle count. Always 1 for anything created since serials landed;
        /// kept so a pre-migration document with an aggregated count still deserializes.
        /// </summary>
        public int quantity { get; set; }

        /// <summary>
        /// Optional flavor tag derived from the donor's corruption value at milking time:
        /// "corrupt" (corruption &lt;= -CorruptionTagThreshold), "purified" (&gt;= +threshold),
        /// or null in the neutral band. Drives sell-price multipliers in
        /// <see cref="ChateauCurrency"/> and the corruption shift applied by <c>!drink</c>.
        /// </summary>
        [BsonIgnoreIfNull]
        public string corruptionTag { get; set; }

        /// <summary>
        /// Null while the bottle still has something in it; stamped when it is drunk. The
        /// bottle stays in the collection either way, so this doubles as the full/empty flag
        /// and as a record of when the contents were enjoyed.
        /// </summary>
        [BsonIgnoreIfNull]
        public DateTime? emptiedAt { get; set; }

        /// <summary>True once the contents have been drunk. Empties are never sellable, never
        /// implicitly selected by !drink, and only transfer by explicit serial.</summary>
        [BsonIgnore]
        public bool IsEmpty => emptiedAt.HasValue;
    }
}
