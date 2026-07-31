using FChatDicebot.BotCommands.Base;
using FChatDicebot.Database;
using FChatDicebot.Model;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Private read-only view of the caller's bottle collection: full bottles with their
    /// serials, donors, corruption tags and sell values, then the numbered empties.
    ///
    /// Argument shape deliberately mirrors <see cref="ChateauSell"/> so the two read as a pair,
    /// and the ordering is the same newest-first contract, meaning the top line of this output
    /// is exactly what an unfiltered <c>!sell</c> or <c>!drink</c> will act on next.
    ///
    /// Self-only. Another resident's collection shows up in summary form on their public
    /// <c>!dossier</c> with counts but no donor names, which is the privacy line: how many
    /// bottles someone has is public, who they milked for them is not.
    /// </summary>
    public class ChateauBottles : ChatBotCommand
    {
        public ChateauBottles()
        {
            Name = "bottles";
            Aliases = new string[] { "collection" };
            Category = "General";
            ShortDescription = "Look over the bottles you've collected.";
            LongDescription = "Review the bottled fluid in your personal collection. Each bottle carries its own number, the resident it came from, whether it's corrupt or pure, and what the Chateau would pay for it. Empties keep their number too, so your collection remembers everything you've ever drunk. Filter by substance and by donor to narrow the list.";
            Usage = "!bottles\nor\n!bottles {substance}\nor\n!bottles {substance} [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "milk", "sell", "drink", "pay", "bank" };
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
            Profile profile = MonDB.getProfile(characterName);
            if (profile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notRegisteredText(), characterName);
                return;
            }

            string substanceFilter = commandController.GetIdentifierFromCommandTerms(rawTerms, "substance");
            string sourceFilter = commandController.GetUserNameFromCommandTerms(rawTerms);

            bot.SendPrivateMessage(
                BuildCollectionText(MonDB.GetDatabase(), profile, substanceFilter, sourceFilter),
                characterName);
        }

        /// <summary>
        /// Pure renderer, factored out so the layout can be tested without a bot connection.
        /// Takes the database only to resolve donor userNames into displayNames.
        /// </summary>
        public static string BuildCollectionText(
            IChateauDatabase database, Profile profile, string substanceFilter, string sourceFilter)
        {
            bool filtered = !string.IsNullOrEmpty(substanceFilter) || !string.IsNullOrEmpty(sourceFilter);

            if (BottleInventory.IsCollectionEmpty(profile))
            {
                return EmptyCollectionText;
            }

            var full = BottleInventory.SelectFull(profile, substanceFilter, sourceFilter);
            var empties = BottleInventory.SelectEmpty(profile, substanceFilter, sourceFilter);

            if (full.Count == 0 && empties.Count == 0)
            {
                return filtered ? FilterMissText : EmptyCollectionText;
            }

            var lines = new List<string>();
            // "Bottle Collection" rather than bare "Collection" so it doesn't read as a
            // collection of residents.
            lines.Add(ReadoutText.Title("Bottle Collection of " + profile.displayName));

            // A resident who has drunk everything they own still has a collection worth
            // showing, but "you're holding 0 bottles" would be a strange way to open it.
            if (full.Count == 0)
            {
                string emptyWord = empties.Count == 1 ? "empty" : "empties";
                lines.Add("Nothing left to drink, but our records show you're keeping "
                    + ReadoutText.Num(empties.Count) + " " + emptyWord + ":");
                lines.Add(ReadoutText.RowIndent + BottleInventory.FormatSerials(
                    empties.Select(b => b.serial), ChateauCurrency.BottleSerialDisplayCap));
                lines.Add(ReadoutText.Footer("Go !milk a willing resident if you'd like something to drink."));
                return string.Join("\n", lines);
            }

            string bottleWord = full.Count == 1 ? "bottle" : "bottles";
            lines.Add("Our records show you're holding " + ReadoutText.Num(full.Count) + " " + bottleWord + ":");

            int totalValue = 0;
            foreach (var group in BottleInventory.Group(full))
            {
                int pricePer = ChateauCurrency.GetSellPricePerBottle(group.Substance, group.CorruptionTag);
                totalValue += pricePer * group.Count;
                lines.Add(ReadoutText.RowIndent + BuildGroupLine(database, group, pricePer));
            }

            if (empties.Count > 0)
            {
                lines.Add(ReadoutText.Section("Empties", ReadoutDomain.Economy) + " "
                    + BottleInventory.FormatSerials(
                        empties.Select(b => b.serial), ChateauCurrency.BottleSerialDisplayCap));
            }

            lines.Add(ReadoutText.Footer("That's " + ReadoutText.Num(totalValue) + " " + ChateauCurrency.SellPayoutCurrency
                + " if you choose to !sell the lot to the Chateau on the cheap, but someone might be"
                + " willing to let you !pay them with a few bottles... or you could always have a !drink."));

            return string.Join("\n", lines);
        }

        private static string BuildGroupLine(IChateauDatabase database, BottleInventory.BottleGroup group, int pricePer)
        {
            // Substance is the row's label, so it takes [u] like every other labelled row;
            // corrupt/pure stay bold because they're a state flag, not a heading.
            string line = ReadoutText.Label(ReadoutText.CapitalizePastTags(Utils.SubstanceToText(group.Substance))) + " from "
                + DonorText(database, group.SourceName);

            if (group.CorruptionTag == ChateauCurrency.CorruptTag)
            {
                line += ", [b]corrupt[/b]";
            }
            else if (group.CorruptionTag == ChateauCurrency.PurifiedTag)
            {
                line += ", [b]pure[/b]";
            }

            line += ReadoutText.InlineSeparator
                + BottleInventory.FormatSerials(group.Serials, ChateauCurrency.BottleSerialDisplayCap)
                + ReadoutText.InlineSeparator
                + ReadoutText.Num(pricePer) + " " + ChateauCurrency.SellPayoutCurrency + " each";
            return line;
        }

        /// <summary>
        /// Shared with <c>!drink</c> so the two commands describe an untouched collection the
        /// same way.
        /// </summary>
        public const string EmptyCollectionText =
            "No bottles to speak of yet. Go !milk a willing resident to start your collection!";

        private const string FilterMissText =
            "We don't see anything like that in your collection. Use !bottles on its own to see everything you're holding.";

        /// <summary>
        /// Donor names are stored as userNames and must be resolved before a resident reads
        /// them. Falls back to the stored name only if the donor has since vanished from the
        /// roster entirely.
        /// </summary>
        public static string DonorText(IChateauDatabase database, string sourceName)
        {
            if (string.IsNullOrEmpty(sourceName)) return "an unknown donor";
            string displayName = database?.GetDisplayName(sourceName);
            return string.IsNullOrEmpty(displayName) ? sourceName : displayName;
        }
    }
}
