using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot
{
    /// <summary>
    /// The bottle half of <c>!collection</c>: full bottles collapsed into one row per
    /// substance/donor/tag with their serials and sell price, then the numbered empties.
    ///
    /// <para>
    /// The ordering is the same newest-first contract <see cref="BottleInventory"/> serves to
    /// <c>!sell</c> and <c>!drink</c>, which is what makes the top row of this section exactly
    /// what an unfiltered <c>!sell</c> or <c>!drink</c> acts on next.
    /// </para>
    /// </summary>
    public class BottleCollectionSection : CollectionSection
    {
        public override Type ItemType => typeof(MilkBottle);
        public override string Header => "Bottles";
        public override string FilterToken => "bottles";

        /// <summary>Residents type the singular as often as the plural.</summary>
        public override string[] Keywords => new[] { "bottles", "bottle" };

        public override string TransferNoun => "bottles";
        public override string TransferGiveFlavor => "Is that a vintage?";
        public override string TransferGoneReason => "sold or enjoyed";

        /// <summary>
        /// Shared with <c>!drink</c> so the two commands describe an untouched bottle collection
        /// the same way.
        /// </summary>
        public const string NoBottlesText =
            "No bottles to speak of yet. Go !milk a willing resident to start your collection!";

        public override string EmptyText => NoBottlesText;

        public override CollectionSectionResult Build(
            IChateauDatabase database, Profile profile, CollectionFilter filter)
        {
            var result = new CollectionSectionResult();
            if (filter == null) filter = new CollectionFilter();
            if (!MatchesTypeFilter(filter)) return result;

            var full = BottleInventory.SelectFull(profile, filter.Substance, filter.Subject);
            var empties = BottleInventory.SelectEmpty(profile, filter.Substance, filter.Subject);
            if (full.Count == 0 && empties.Count == 0) return result;

            int totalValue = 0;
            foreach (var group in BottleInventory.Group(full))
            {
                int pricePer = ChateauCurrency.GetSellPricePerBottle(group.Substance, group.CorruptionTag);
                totalValue += pricePer * group.Count;
                result.Rows.Add(BuildGroupLine(database, group, pricePer));
            }

            if (empties.Count > 0)
            {
                // An empty is a keepsake rather than a holding, so it sits as its own row under
                // the bottles rather than earning a section of its own the way it did when
                // bottles were the whole readout.
                result.Rows.Add(ReadoutText.Row("Empties", CollectionInventory.FormatSerials(
                    empties.Select(b => b.serial), ChateauCurrency.SerialDisplayCap)));
            }

            if (full.Count > 0)
            {
                result.Holdings.Add(ReadoutText.Num(full.Count) + " bottle" + (full.Count == 1 ? "" : "s"));
            }
            if (empties.Count > 0)
            {
                result.Holdings.Add(ReadoutText.Num(empties.Count) + " empt" + (empties.Count == 1 ? "y" : "ies"));
            }

            // Only worth saying when there is something the Chateau would actually buy: a
            // resident holding nothing but empties is being told about a sale of zero.
            if (full.Count > 0)
            {
                result.Footer = "That's " + ReadoutText.Num(totalValue) + " " + ChateauCurrency.SellPayoutCurrency
                    + " if you choose to !sell the lot to the Chateau on the cheap, but someone might be"
                    + " willing to let you !pay them with a few bottles... or you could always have a !drink.";
            }

            return result;
        }

        /// <summary>
        /// Full bottles only. Someone asking for "3 bottles" means three with something in them,
        /// and the friction of naming a number is the right price for moving an empty.
        /// </summary>
        public override List<Collectible> TransferCandidates(Profile profile, string substanceFilter)
        {
            return BottleInventory.SelectFull(profile, substanceFilter, null)
                .Where(b => b.IsTransferable)
                .Cast<Collectible>()
                .ToList();
        }

        /// <summary>
        /// Names the substance, the donor, and any corruption tag, because a recipient deserves
        /// to know they're being handed corrupt goods before they agree to take them.
        /// </summary>
        public override string DescribeParcel(IChateauDatabase database, List<Collectible> items)
        {
            var bottles = items == null ? new List<MilkBottle>() : items.OfType<MilkBottle>().ToList();
            if (bottles.Count == 0) return string.Empty;

            var pieces = new List<string>();
            foreach (var group in BottleInventory.Group(bottles))
            {
                string piece = CountWord(group.Count) + " of the "
                    + Utils.SubstanceToText(group.Substance)
                    + " from " + CollectionInventory.SubjectText(database, group.SourceName);

                if (group.CorruptionTag == ChateauCurrency.CorruptTag) piece += " ([b]corrupt[/b])";
                else if (group.CorruptionTag == ChateauCurrency.PurifiedTag) piece += " ([b]pure[/b])";

                pieces.Add(piece);
            }

            string bottleWord = bottles.Count == 1 ? "bottle" : "bottles";
            string described = "[b]" + bottles.Count + " " + bottleWord + "[/b]: " + JoinWithAnd(pieces);

            // Whether the contents are still in there is the single most important thing the
            // recipient needs to know before agreeing, so it goes on the whole parcel rather than
            // per line. Mixed parcels say so instead of implying either.
            int emptyCount = bottles.Count(b => b.IsEmpty);
            if (emptyCount == bottles.Count)
            {
                described += " [sub](already emptied)[/sub]";
            }
            else if (emptyCount > 0)
            {
                described += " [sub](" + emptyCount + " of them already emptied)[/sub]";
            }
            return described;
        }

        private static string BuildGroupLine(IChateauDatabase database, BottleInventory.BottleGroup group, int pricePer)
        {
            // Substance is the row's label, so it takes [u] like every other labelled row;
            // corrupt/pure stay bold because they're a state flag, not a heading.
            string line = ReadoutText.Label(ReadoutText.CapitalizePastTags(Utils.SubstanceToText(group.Substance))) + " from "
                + CollectionInventory.SubjectText(database, group.SourceName);

            if (group.CorruptionTag == ChateauCurrency.CorruptTag)
            {
                line += ", [b]corrupt[/b]";
            }
            else if (group.CorruptionTag == ChateauCurrency.PurifiedTag)
            {
                line += ", [b]pure[/b]";
            }

            line += ReadoutText.InlineSeparator
                + CollectionInventory.FormatSerials(group.Serials, ChateauCurrency.SerialDisplayCap)
                + ReadoutText.InlineSeparator
                + ReadoutText.Num(pricePer) + " " + ChateauCurrency.SellPayoutCurrency + " each";
            return line;
        }
    }
}
