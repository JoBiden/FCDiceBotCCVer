using MongoDB.Bson.Serialization.Attributes;

namespace FChatDicebot.Model
{
    /// <summary>
    /// One pair of panties in a resident's collection, kept for life under its
    /// <see cref="Collectible.serial"/> and remembering whose they were.
    ///
    /// The first non-bottle <see cref="Collectible"/>, and deliberately a thin one: it adds no
    /// fields of its own. Everything that makes it a real item — the number, the frozen subject
    /// name, the acquisition time, the listing, the transfer, the privacy rule — comes from the
    /// base class. That is the whole argument for having promoted bottles into a framework.
    ///
    /// Unlike a bottle, panties are a keepsake rather than a commodity: the Chateau will not buy
    /// them back, so they can be given away but never sold.
    /// </summary>
    public class Panties : Collectible
    {
        /// <inheritdoc/>
        [BsonIgnore]
        public override string TypeLabel => "pair of panties";

        /// <summary>
        /// The Chateau doesn't deal in these. They change hands between residents or not at all,
        /// which is what makes holding someone's pair mean something.
        /// </summary>
        [BsonIgnore]
        public override bool IsSellable => false;
    }
}
