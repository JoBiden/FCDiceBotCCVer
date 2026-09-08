using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot
{
    /// <summary>
    /// What <c>!collection</c> was asked to narrow to. Every field is optional and null means
    /// "any", so an unfiltered call is an instance with nothing set.
    /// </summary>
    public class CollectionFilter
    {
        /// <summary>
        /// A <see cref="CollectionSection.FilterToken"/> the resident typed ("bottles",
        /// "panties"), or null for every type.
        /// </summary>
        public string TypeToken;

        /// <summary>
        /// Substance identifier. Only bottles carry one, so setting this narrows the readout to
        /// bottles by consequence rather than by rule — a section with no substance to match on
        /// reports nothing.
        /// </summary>
        public string Substance;

        /// <summary>Whose items to show — a userName, matched against <c>subjectName</c>.</summary>
        public string Subject;

        /// <summary>
        /// True when the resident narrowed the view at all. Drives the difference between "you
        /// own nothing" and "nothing matched that", which are different problems with different
        /// remedies.
        /// </summary>
        public bool IsNarrowed =>
            !string.IsNullOrEmpty(TypeToken)
            || !string.IsNullOrEmpty(Substance)
            || !string.IsNullOrEmpty(Subject);
    }

    /// <summary>What one section has to say about a resident's holdings.</summary>
    public class CollectionSectionResult
    {
        /// <summary>
        /// This section's contribution to the opening summary line, one atomic phrase per cell
        /// ("[b]3[/b] bottles", "[b]1[/b] empty"). Kept as separate cells rather than one joined
        /// string so the readout can punctuate the whole list once — a section that pre-joined
        /// its own with "and" would leave a sentence carrying two of them.
        /// </summary>
        public List<string> Holdings = new List<string>();

        /// <summary>Rows printed under the section header, already formatted.</summary>
        public List<string> Rows = new List<string>();

        /// <summary>Optional closing small-print note, or null for none.</summary>
        public string Footer;

        public bool Any => Rows.Count > 0;
    }

    /// <summary>
    /// How one <see cref="Collectible"/> type renders inside <c>!collection</c>.
    ///
    /// <para>
    /// The cross-type view the Collectibles spec deferred needs each type to describe itself, and
    /// the spec's own note on the subject rules out the obvious shape: an abstract
    /// <c>DescribeFor</c> on <see cref="Collectible"/> would produce one line per item, and
    /// bottles are only legible because separate milkings of the same kind collapse into one row.
    /// So the unit of description is a <b>section</b>, not an item.
    /// </para>
    ///
    /// <para>
    /// Everything that reads a type's own fields lives in its section — bottles group on
    /// substance and corruption and price themselves, panties group on nothing but whose they
    /// were. That is the same boundary <see cref="CollectionInventory"/> and
    /// <see cref="BottleInventory"/> already draw, applied to display: if it compiles against
    /// <see cref="Collectible"/> it belongs in the shared helper, and if it doesn't it belongs
    /// here.
    /// </para>
    ///
    /// <para>
    /// Adding a collectible type is a model class, an acquisition path, one section, and one line
    /// in <see cref="CollectionSections"/>. A type with no section would be held, transferable and
    /// completely invisible, so a test fails on one rather than leaving it to be noticed.
    /// </para>
    /// </summary>
    public abstract class CollectionSection
    {
        /// <summary>The concrete <see cref="Collectible"/> subclass this section renders.</summary>
        public abstract Type ItemType { get; }

        /// <summary>Section header, without punctuation — "Bottles", "Panties".</summary>
        public abstract string Header { get; }

        /// <summary>
        /// The canonical word for this type: what <c>!collection {token}</c> and
        /// <c>!pay … {token} #12</c> are documented with, and what a payment stores in
        /// <see cref="Model.Interaction.identifier"/>.
        ///
        /// <b>Do not change a shipped token.</b> It is persisted on every completed payment of
        /// this type, and the completion messages read it back — a rename would leave old
        /// interactions describing a type that no longer answers to that name.
        /// </summary>
        public abstract string FilterToken { get; }

        /// <summary>
        /// Every word that means this type, canonical first. Residents type "bottle" as often as
        /// "bottles" and shouldn't have to care; a type whose name has only one form (panties)
        /// inherits the default and declares nothing.
        ///
        /// Drives the <c>!collection</c> filter, the <c>!pay</c> keyword, and the
        /// <c>ArgumentKeywords</c> both commands declare so bare-name resolution accounts for
        /// them instead of reporting a resident it can't place.
        /// </summary>
        public virtual string[] Keywords => new[] { FilterToken };

        /// <summary>
        /// What to say when this type is the only one asked for and the resident holds none of
        /// it. Per-type because the remedy is per-type: bottles come from <c>!milk</c>, panties
        /// from asking.
        /// </summary>
        public abstract string EmptyText { get; }

        /// <summary>
        /// This section's holdings summary and rows for one resident under one filter. Returns an
        /// empty result rather than null when nothing matches, so the caller can concatenate
        /// without checking.
        /// </summary>
        public abstract CollectionSectionResult Build(
            IChateauDatabase database, Profile profile, CollectionFilter filter);

        /// <summary>Whether a type filter (if any) names this section.</summary>
        public bool MatchesTypeFilter(CollectionFilter filter)
        {
            if (filter == null || string.IsNullOrEmpty(filter.TypeToken)) return true;
            return Answers(filter.TypeToken);
        }

        /// <summary>Whether a typed word names this type.</summary>
        public bool Answers(string word)
        {
            if (string.IsNullOrEmpty(word) || Keywords == null) return false;
            return Keywords.Any(k => string.Equals(k, word, StringComparison.OrdinalIgnoreCase));
        }

        // -------------------------------------------------------------------
        // Transfer — how a parcel of this type moves through !pay
        // -------------------------------------------------------------------

        /// <summary>
        /// The plural noun a completion message uses for a parcel of these ("bottles",
        /// "panties"). Per-type, like the flavor sentences below.
        /// </summary>
        public abstract string TransferNoun { get; }

        /// <summary>
        /// The flavor sentence closing a completion message, one per direction: giving a pair of
        /// panties away and being handed one are different moments and read differently.
        ///
        /// A type whose flavor doesn't care which way the goods went overrides only
        /// <see cref="TransferGiveFlavor"/> and inherits the other — "Is that a vintage?" is a
        /// joke about the bottle, not about who ended up with it.
        /// </summary>
        public abstract string TransferGiveFlavor { get; }

        /// <inheritdoc cref="TransferGiveFlavor"/>
        public virtual string TransferTakeFlavor => TransferGiveFlavor;

        /// <summary>
        /// How an item of this type plausibly left the payer's hands during the consent gap, as a
        /// past participle phrase ("sold or enjoyed"). Per-type for the same reason: the Chateau
        /// will not buy panties and nobody drinks them, so the bottle story doesn't transfer.
        /// </summary>
        public abstract string TransferGoneReason { get; }

        /// <summary>
        /// Items of this type eligible for an unfiltered <c>!pay … {amount} {token}</c>, newest
        /// first. Per-type because "which of these would someone mean by 'three'" is a type's own
        /// question: a bottle has to still have something in it, and a pair of panties has no
        /// equivalent condition.
        ///
        /// Named serials do <b>not</b> come through here — naming a number is the friction that
        /// buys the right to move something this wouldn't offer, which is how a numbered empty
        /// still changes hands.
        /// </summary>
        public abstract List<Collectible> TransferCandidates(Profile profile, string substanceFilter);

        /// <summary>
        /// Player-facing summary of a parcel of this type changing hands, for the consent prompt.
        /// Must open with a bolded count so <see cref="CollectiblePayment.DescribesCollectibles"/>
        /// can tell it apart from the currency path's bare "{amount} {currency}".
        ///
        /// This is where a type says what a recipient needs to know before agreeing — for bottles
        /// that's the substance, the donor, the corruption tag and whether it's already been
        /// drunk; for panties it's whose they were.
        /// </summary>
        public abstract string DescribeParcel(IChateauDatabase database, List<Collectible> items);

        // -------------------------------------------------------------------
        // Shared parcel grammar
        // -------------------------------------------------------------------

        /// <summary>Small counts read better spelled out: "two of the milk from Carol".</summary>
        protected static string CountWord(int count)
        {
            switch (count)
            {
                case 1: return "one";
                case 2: return "two";
                case 3: return "three";
                default: return count.ToString();
            }
        }

        /// <summary>
        /// Sentence list for parcel pieces: "a", "a and b", "a, b, and c". Deliberately keeps the
        /// Oxford comma the shipped bottle wording already used.
        /// </summary>
        protected static string JoinWithAnd(List<string> pieces)
        {
            if (pieces == null || pieces.Count == 0) return string.Empty;
            if (pieces.Count == 1) return pieces[0];
            if (pieces.Count == 2) return pieces[0] + " and " + pieces[1];
            return string.Join(", ", pieces.Take(pieces.Count - 1)) + ", and " + pieces[pieces.Count - 1];
        }
    }
}
