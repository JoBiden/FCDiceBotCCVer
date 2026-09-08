using FChatDicebot.BotCommands.Base;
using FChatDicebot.Database;
using FChatDicebot.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Private read-only view of everything the caller has collected — bottles with their
    /// serials, donors, corruption tags and sell values, the panties they're keeping, and
    /// whatever type is added next.
    ///
    /// <para>
    /// This was <c>!bottles</c>, and the rename is the point. Bottles stopped being the only
    /// collectible when panties shipped, so a listing that only knew about bottles left the rest
    /// of a resident's collection invisible: held, numbered, transferable, and impossible to
    /// look at. <c>!bottles</c> survives as an alias because residents learned it.
    /// </para>
    ///
    /// <para>
    /// The command owns no per-type knowledge. Each <see cref="CollectionSection"/> renders its
    /// own type and this assembles the sections that had something to say, which is why adding a
    /// collectible type doesn't touch this file.
    /// </para>
    ///
    /// <para>
    /// Argument shape deliberately still mirrors <see cref="ChateauSell"/> so the two read as a
    /// pair, and the ordering is the same newest-first contract, meaning the top bottle row here
    /// is exactly what an unfiltered <c>!sell</c> or <c>!drink</c> will act on next.
    /// </para>
    ///
    /// <para>
    /// Self-only. Another resident's collection shows up in summary form on their public
    /// <c>!dossier</c> with counts but no subject names, which is the privacy line: how much
    /// someone holds is public, whose it is isn't.
    /// </para>
    /// </summary>
    public class ChateauCollection : ChatBotCommand
    {
        public ChateauCollection()
        {
            Name = "collection";
            Aliases = new string[] { "bottles" };
            Category = "General";
            ShortDescription = "Look over everything you've collected.";
            LongDescription = "Review everything in your personal collection. Each item carries its own number and the resident it came from: bottles also show whether they're corrupt or pure and what the Chateau would pay, and empties keep their number too, so your collection remembers everything you've ever drunk. Name a kind of item to see only that, or filter bottles by substance and by the resident they came from.";
            Usage = "!collection\nor\n!collection {kind}\nor\n!collection {substance}\nor\n!collection {substance} [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "milk", "panties", "sell", "drink", "pay", "bank" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = "substance";
            // "bottles" and "panties" are argument words, not residents nobody recognises.
            ArgumentKeywords = CollectionSections.AllKeywords();
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

            var filter = new CollectionFilter
            {
                TypeToken = ParseTypeToken(terms),
                Substance = commandController.GetIdentifierFromCommandTerms(rawTerms, "substance"),
                Subject = commandController.GetUserNameFromCommandTerms(rawTerms),
            };

            bot.SendPrivateMessage(
                BuildCollectionText(MonDB.GetDatabase(), profile, filter), characterName);
        }

        /// <summary>
        /// The kind of item the resident asked for, or null. Reads the lowercased terms rather
        /// than the raw ones because it's matching a bare word, not a tagged span.
        /// </summary>
        public static string ParseTypeToken(string[] terms)
        {
            CollectionSection section = CollectionSections.ByKeywordIn(terms);
            return section?.FilterToken;
        }

        /// <summary>
        /// Pure renderer, factored out so the layout can be tested without a bot connection.
        /// Takes the database only to resolve subject userNames into displayNames.
        /// </summary>
        public static string BuildCollectionText(
            IChateauDatabase database, Profile profile, CollectionFilter filter)
        {
            if (filter == null) filter = new CollectionFilter();

            var shown = CollectionSections.InPrintOrder
                .Select(s => new { Section = s, Result = s.Build(database, profile, filter) })
                .Where(b => b.Result.Any)
                .ToList();

            if (shown.Count == 0)
            {
                return NothingToShowText(profile, filter);
            }

            var sb = new StringBuilder();
            sb.Append(ReadoutText.Title("Collection of " + profile.displayName)).Append('\n');
            sb.Append("Our records show you're holding ")
              .Append(JoinWithAnd(shown.SelectMany(b => b.Result.Holdings).ToList()))
              .Append(".\n");

            foreach (var block in shown)
            {
                // No spoiler summary: the opening line already reports the size of every section,
                // so repeating it beside a collapsed header would say it twice.
                sb.Append(ReadoutText.LineSection(
                    block.Section.Header, ReadoutDomain.Economy, null, block.Result.Rows));
            }

            var footers = shown.Select(b => b.Result.Footer)
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList();
            if (footers.Count > 0)
            {
                sb.Append(string.Join("\n", footers.Select(ReadoutText.Footer)));
            }

            return sb.ToString().TrimEnd('\n');
        }

        /// <summary>
        /// Nothing matched. Which of the three ways that happened decides what to point at, so
        /// the remedy offered is one the resident can actually act on.
        /// </summary>
        private static string NothingToShowText(Profile profile, CollectionFilter filter)
        {
            // They named a kind of item. That type says how one is come by, whether or not they
            // hold anything else — "you have no panties" is more use than "your filter matched
            // nothing" even to someone with forty bottles.
            CollectionSection named = CollectionSections.ByKeyword(filter.TypeToken);
            if (named != null && string.IsNullOrEmpty(filter.Substance) && string.IsNullOrEmpty(filter.Subject))
            {
                return named.EmptyText;
            }

            if (CollectionInventory.IsEmpty(profile)) return EmptyCollectionText;

            return filter.IsNarrowed ? FilterMissText : EmptyCollectionText;
        }

        /// <summary>Joins cells as a sentence list: "a", "a and b", "a, b and c".</summary>
        private static string JoinWithAnd(IList<string> cells)
        {
            if (cells == null || cells.Count == 0) return string.Empty;
            if (cells.Count == 1) return cells[0];
            return string.Join(", ", cells.Take(cells.Count - 1)) + " and " + cells[cells.Count - 1];
        }

        public const string EmptyCollectionText =
            "Nothing in your collection yet. Go !milk a willing resident, or ask someone for their !panties, to start one.";

        private const string FilterMissText =
            "We don't see anything like that in your collection. Use !collection on its own to see everything you're holding.";
    }
}
