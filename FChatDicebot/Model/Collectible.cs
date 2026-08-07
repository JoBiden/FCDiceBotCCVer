using MongoDB.Bson.Serialization.Attributes;
using System;

namespace FChatDicebot.Model
{
    /// <summary>
    /// One individually-identified thing a resident holds, remembered for life by its
    /// <see cref="serial"/> and by whose it is.
    ///
    /// <para>
    /// This is the counterpart to <see cref="Profile.currencies"/>. A currency is fungible — a
    /// name and a count, no identity, one copper indistinguishable from the next. A collectible
    /// is the opposite: every one is a separate document with its own number and its own person
    /// attached, so "Alice's bottle #12" is a different object from "Alice's bottle #40" and
    /// stays so through every hand it passes through.
    /// </para>
    ///
    /// <para>
    /// Stored polymorphically in <see cref="Profile.collectibles"/>. The Mongo driver writes a
    /// <c>_t</c> discriminator on every element (see <see cref="BsonKnownTypesAttribute"/>
    /// below); an element without one cannot deserialize into an abstract declared type at all,
    /// which is why <c>scripts/backfill-collectibles.js</c> is mandatory rather than tidying.
    /// </para>
    /// </summary>
    [BsonKnownTypes(typeof(MilkBottle), typeof(Panties))]
    public abstract class Collectible
    {
        /// <summary>
        /// Globally unique item number, issued from the shared counter at acquisition time and
        /// never reused. Shared across <b>all</b> collectible types on purpose: serial #1 is the
        /// oldest thing anyone in the Chateau holds, not the oldest bottle. Per-type counters
        /// would let two items both be "#1" and would cost the number its meaning.
        ///
        /// Zero only on bottle documents predating the original serial backfill.
        /// </summary>
        public int serial { get; set; }

        /// <summary>
        /// Whose item this is — the donor of a bottle, the previous owner of a pair of panties.
        /// Stores the <c>userName</c> at acquisition time and is <b>frozen</b>: it does not
        /// follow renames, so it must be resolved through <c>GetDisplayName</c> before reaching
        /// any player-facing string.
        /// </summary>
        public string subjectName { get; set; }

        /// <summary>
        /// When this item came into being. Drives newest-first ordering across the whole
        /// collection, which is why it lives on the base rather than each type carrying its own
        /// timestamp.
        /// </summary>
        public DateTime acquiredAt { get; set; }

        /// <summary>
        /// What to call one of these in a listing — "bottle", "pair of panties". Used for
        /// grouping and for the type filter; not stored.
        /// </summary>
        [BsonIgnore]
        public abstract string TypeLabel { get; }

        /// <summary>
        /// Whether the Chateau will buy this item. Types that are keepsakes rather than
        /// commodities override this to false, and <c>!sell</c> reads it rather than
        /// re-deriving the rule per caller.
        /// </summary>
        [BsonIgnore]
        public virtual bool IsSellable => true;

        /// <summary>
        /// Whether this item can change hands through <c>!pay</c>. Read by
        /// <see cref="BottlePayment"/> so a type that should never move says so once, on itself.
        /// </summary>
        [BsonIgnore]
        public virtual bool IsTransferable => true;
    }
}
