using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot
{
    /// <summary>
    /// The panties half of <c>!collection</c>: one row per resident they came from, with the
    /// numbers of every pair of theirs you're holding.
    ///
    /// <para>
    /// As thin as <see cref="Panties"/> itself. There is no substance, no price and no full/empty
    /// state to render, so the only thing a pair has to say for itself is whose it was and what
    /// number it wears — both of which are base-class fields, which is why this section reads
    /// entirely through <see cref="CollectionInventory"/> and touches nothing bottle-shaped.
    /// </para>
    /// </summary>
    public class PantiesCollectionSection : CollectionSection
    {
        public override Type ItemType => typeof(Panties);
        public override string Header => "Panties";
        public override string FilterToken => "panties";

        public override string EmptyText =>
            "No panties in your collection yet. Ask a willing resident for a pair with !panties.";

        public override string TransferNoun => "panties";
        public override string TransferGiveFlavor => "What a thoughtful gift!";
        public override string TransferTakeFlavor => "Sharing is caring!";
        public override string TransferGoneReason => "passed along to someone else";

        public override CollectionSectionResult Build(
            IChateauDatabase database, Profile profile, CollectionFilter filter)
        {
            var result = new CollectionSectionResult();
            if (filter == null) filter = new CollectionFilter();
            if (!MatchesTypeFilter(filter)) return result;

            // A substance filter is a bottle question. Rather than ignoring it and printing every
            // pair under "!collection cum", the section reports nothing — which is the honest
            // answer, and is what lets a substance filter narrow the readout to bottles without
            // any section having to know the others exist.
            if (!string.IsNullOrEmpty(filter.Substance)) return result;

            var pairs = CollectionInventory.Select<Panties>(profile, filter.Subject);
            if (pairs.Count == 0) return result;

            foreach (var group in CollectionInventory.GroupBySubject(pairs))
            {
                result.Rows.Add(
                    ReadoutText.Label(CollectionInventory.SubjectText(database, group.SubjectName))
                    + ReadoutText.InlineSeparator
                    + CollectionInventory.FormatSerials(group.Serials, ChateauCurrency.SerialDisplayCap));
            }

            result.Holdings.Add(ReadoutText.Num(pairs.Count)
                + (pairs.Count == 1 ? " pair of panties" : " pairs of panties"));
            result.Footer = "The Chateau won't buy panties back, but you can !pay a pair on to someone else.";

            return result;
        }

        /// <summary>
        /// Every pair, newest first. A pair has no equivalent of a bottle's full/empty state, so
        /// there is nothing to hold back — which is most of what makes this type a good test of
        /// whether the transfer path really generalized.
        /// </summary>
        public override List<Collectible> TransferCandidates(Profile profile, string substanceFilter)
        {
            // A substance is a bottle question, and a request that carries one isn't asking for
            // these. Same rule the readout follows.
            if (!string.IsNullOrEmpty(substanceFilter)) return new List<Collectible>();

            return CollectionInventory.Select<Panties>(profile, null)
                .Where(p => p.IsTransferable)
                .Cast<Collectible>()
                .ToList();
        }

        /// <summary>
        /// Whose they were is the whole of what a recipient needs to know, so that is all this
        /// says. No substance, no tag, and no full/empty note, because a pair has none of them.
        /// </summary>
        public override string DescribeParcel(IChateauDatabase database, List<Collectible> items)
        {
            var pairs = items == null ? new List<Panties>() : items.OfType<Panties>().ToList();
            if (pairs.Count == 0) return string.Empty;

            var pieces = new List<string>();
            foreach (var group in CollectionInventory.GroupBySubject(pairs))
            {
                pieces.Add(CountWord(group.Count) + " originally from "
                    + CollectionInventory.SubjectText(database, group.SubjectName));
            }

            string pairWord = pairs.Count == 1 ? "pair of panties" : "pairs of panties";
            return "[b]" + pairs.Count + " " + pairWord + "[/b]: " + JoinWithAnd(pieces);
        }
    }
}
