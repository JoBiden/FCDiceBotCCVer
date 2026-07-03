using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Sells milked bottles from the initiator's inventory to the Chateau in reverse
    /// acquisition order (newest first). Returns one bottle-currency unit per sold
    /// bottle (the Chateau takes the empty back) alongside the substance-value copper
    /// payout. No consent flow — self-targeted system command.
    ///
    /// Syntax:
    ///   !sell                              — sell the single most recent bottle
    ///   !sell {amount}                     — sell N newest bottles
    ///   !sell {amount} {substance}         — N newest bottles of substance
    ///   !sell {amount} {substance} [user]X[/user]
    ///                                      — N newest of substance, sourced from X
    ///
    /// A single <see cref="MilkBottle"/> entry can hold multiple bottles
    /// (<c>quantity &gt;= 1</c>); selling can partially consume an entry, leaving
    /// the remaining quantity in the inventory under the same milkedAt timestamp.
    /// </summary>
    public class ChateauSell : ChatBotCommand
    {
        public ChateauSell()
        {
            Name = "sell";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Sell bottled substances, obtained when you !milk others.";
            LongDescription = "Sell bottled fluid from your personal inventory to the Chateau. Pricing depends on the substance's rarity and whether it is corrupt or pure. One empty bottle is returned per bottle sold. With no arguments, sells most recent bottle. With an amount, sells that many bottles in reverse of the order acquired, optionally filtered by substance and/or original source.";
            Usage = "!sell\nor\n!sell {amount}\nor\n!sell {amount} {substance}\nor\n!sell {amount} {substance} [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "milk", "bank" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = "substance";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            Profile profile = MonDB.getProfile(characterName);
            if (profile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notRegisteredText(), characterName);
                return;
            }
            if (profile.milkInventory == null || profile.milkInventory.Count == 0)
            {
                bot.SendPrivateMessage("You don't have any bottles in your inventory to sell.", characterName);
                return;
            }

            // Optional substance filter. null means "any substance".
            string substanceFilter = commandController.GetIdentifierFromCommandTerms(rawTerms, "substance");

            // Optional source filter. null means "any source".
            string sourceFilter = commandController.GetUserNameFromCommandTerms(rawTerms);

            // Optional amount. Missing means "sell the single most recent bottle".
            string[] rawInts = commandController.GetIntsFromCommandTermsAsStrings(rawTerms);
            int requestedAmount;
            if (rawInts != null && rawInts.Length > 0)
            {
                if (!int.TryParse(rawInts[0], out int parsed) || parsed <= 0)
                {
                    bot.SendPrivateMessage("The amount of bottles to sell must be a positive whole number.", characterName);
                    return;
                }
                requestedAmount = parsed;
            }
            else
            {
                requestedAmount = 1;
            }

            var sellResult = SellBottles(profile, substanceFilter, sourceFilter, requestedAmount);

            if (sellResult.BottlesSold == 0)
            {
                bot.SendPrivateMessage(
                    "No bottles in your inventory matched that filter. Use !bank or your !dossier to review what you've got bottled up.",
                    characterName);
                return;
            }

            // Persist inventory changes via the targeted milkInventory write (re-fetches
            // fresh at call time rather than reusing the profile loaded at the top of Run,
            // so it can't revert a concurrent currency/count/timer change), then credit
            // copper (substance value) and the bottle-currency (the Chateau handing the
            // empty back) via atomic $inc.
            MonDB.GetDatabase().SetMilkInventory(characterName, profile.milkInventory);
            MonDB.GetDatabase().ChangeCurrency(characterName, ChateauCurrency.SellPayoutCurrency, sellResult.PayoutCopper);
            MonDB.GetDatabase().ChangeCurrency(characterName, ChateauCurrency.BottleCurrency, sellResult.BottlesReturned);

            bot.SendMessageInChannel(BuildResultMessage(profile.displayName, sellResult), channel);
        }

        /// <summary>
        /// Result of a sell operation. Pure summary — does not include the rebuilt
        /// inventory because the caller mutates the profile in place.
        /// </summary>
        public class SellResult
        {
            public int BottlesSold;
            public int PayoutCopper;
            /// <summary>
            /// Bottle-currency units returned to the seller (one per sold bottle, the
            /// Chateau handing the empty back). Equal to <see cref="BottlesSold"/> in
            /// the current model — kept as a separate field so the line items in the
            /// channel message can refer to it without re-deriving.
            /// </summary>
            public int BottlesReturned;
            // Distinct (substance, source) pairs touched, in sell order (newest first).
            // Used to build a readable summary line for the channel message.
            public List<SellLineItem> LineItems = new List<SellLineItem>();
        }

        public class SellLineItem
        {
            public string Substance;
            public string SourceName;
            public int Bottles;
            public int Payout;
        }

        /// <summary>
        /// Mutates <paramref name="profile"/>: removes/decrements matching bottles
        /// from <c>milkInventory</c> in reverse acquisition order (newest first).
        /// Currency is returned in the result for the caller to persist separately.
        ///
        /// Pulled out as a pure method so it can be unit-tested without spinning
        /// up a database — the command's <c>Run</c> persists the result.
        /// </summary>
        public static SellResult SellBottles(Profile profile, string substanceFilter, string sourceFilter, int requestedAmount)
        {
            var result = new SellResult();
            if (profile?.milkInventory == null || profile.milkInventory.Count == 0 || requestedAmount <= 0)
            {
                return result;
            }

            // LIFO: newest first. We pull from the front of this ordering; entries are
            // looked at one at a time and either consumed whole or partially.
            var ordered = profile.milkInventory
                .Where(b => MatchesFilters(b, substanceFilter, sourceFilter))
                .OrderByDescending(b => b.milkedAt)
                .ToList();

            // Index payout summary by (substance, source) so the result message can
            // collapse multiple sessions of the same kind into one line.
            var lineItems = new Dictionary<string, SellLineItem>(StringComparer.Ordinal);

            int remainingToSell = requestedAmount;

            foreach (var bottle in ordered)
            {
                if (remainingToSell <= 0) break;

                int take = Math.Min(bottle.quantity, remainingToSell);
                if (take <= 0) continue;

                int pricePer = ChateauCurrency.GetSellPricePerBottle(bottle.substance, bottle.corruptionTag);
                int payout = pricePer * take;

                bottle.quantity -= take;
                remainingToSell -= take;
                result.BottlesSold += take;
                result.PayoutCopper += payout;
                result.BottlesReturned += take;

                string key = (bottle.substance ?? "") + "|" + (bottle.sourceName ?? "");
                if (!lineItems.TryGetValue(key, out var line))
                {
                    line = new SellLineItem
                    {
                        Substance = bottle.substance,
                        SourceName = bottle.sourceName,
                    };
                    lineItems[key] = line;
                }
                line.Bottles += take;
                line.Payout += payout;
            }

            // Prune emptied entries. Done in a second pass so we don't mutate the
            // list while iterating ordered above (ordered points into the same
            // List<MilkBottle> objects).
            profile.milkInventory.RemoveAll(b => b.quantity <= 0);

            // Preserve order-of-first-appearance (newest first) in the result list.
            foreach (var bottle in ordered)
            {
                string key = (bottle.substance ?? "") + "|" + (bottle.sourceName ?? "");
                if (lineItems.TryGetValue(key, out var line) && !result.LineItems.Contains(line))
                {
                    result.LineItems.Add(line);
                }
            }

            return result;
        }

        private static bool MatchesFilters(MilkBottle bottle, string substanceFilter, string sourceFilter)
        {
            if (!string.IsNullOrEmpty(substanceFilter)
                && !string.Equals(bottle.substance, substanceFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(sourceFilter)
                && !string.Equals(bottle.sourceName, sourceFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        private static string BuildResultMessage(string sellerDisplayName, SellResult result)
        {
            string lineSummary;
            if (result.LineItems.Count == 1)
            {
                var line = result.LineItems[0];
                lineSummary = result.BottlesSold + " bottle" + (result.BottlesSold == 1 ? "" : "s")
                    + " of " + Utils.SubstanceToText(line.Substance)
                    + " (sourced from " + (line.SourceName ?? "an unknown donor") + ")";
            }
            else
            {
                var pieces = new List<string>();
                foreach (var line in result.LineItems)
                {
                    pieces.Add(line.Bottles + " of " + Utils.SubstanceToText(line.Substance)
                        + " from " + (line.SourceName ?? "an unknown donor"));
                }
                lineSummary = result.BottlesSold + " bottles ("
                    + string.Join(", ", pieces) + ")";
            }

            return sellerDisplayName + " sold " + lineSummary + " for [b]" + result.PayoutCopper
                + " " + ChateauCurrency.SellPayoutCurrency + "[/b]. Empty bottles returned: "
                + result.BottlesReturned + ".";
        }
    }
}
