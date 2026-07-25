using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;
using SharpCompress;

namespace FChatDicebot.BotCommands
{
    public class ChateauIdentifier : ChatBotCommand
    {
        public ChateauIdentifier()
        {
            Name = "identifier";
            Aliases = new string[] { "whatis" };
            Category = "General";
            ShortDescription = "Get information about a specific identifier";
            LongDescription = "Look up detailed information about a specific identifier (bodypart, substance, species, etc.). Shows the description and categories for that identifier.";
            Usage = "!identifier {identifier}\nor\n!whatis {identifier}";
            RelatedCommands = new string[] { "category", "list" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string returnText = string.Empty;
            if (terms.Length < 1)
            {
                returnText = "Use this command to read the description of an identifier. If you want to see a list of identifiers, use !category instead. Please note that only the first term provided will be described, and any additional terms will be ignored.";
            } else
            {
                Identifier identifier = MonDB.getIdentifier(terms.FirstOrDefault());
                returnText = BuildIdentifierMessage(identifier, characterName);
            }

            bot.SendPrivateMessage(returnText, characterName);

        }

        /// <summary>
        /// The DM body for one looked-up identifier: title line (carrying the Chateau's own
        /// eicon for it, when set), description, the monster gestation/brood line, and — for
        /// bodyparts — the asker's own pinned eicon.
        /// </summary>
        internal static string BuildIdentifierMessage(Identifier identifier, string characterName)
        {
            if (identifier == null)
            {
                return "That identifier was not found in our records. Are you sure you spelled it right?";
            }

            // The identifier's own bot-wide eicon (if the Chateau has one on file for it)
            // rides on the title line.
            string titleEicon = string.IsNullOrEmpty(identifier.eicon) ? string.Empty : " " + identifier.eicon;
            string returnText = "[b]" + identifier.type + "[/b]" + titleEicon + "\n" + identifier.description;

            if (identifier.categories != null && identifier.categories.Contains("monster", StringComparer.OrdinalIgnoreCase))
            {
                BreedProcessor.ResolveGestationAndBroodRange(identifier, out int gestationDays, out int broodSizeMin, out int broodSizeMax);
                string broodText = broodSizeMin == broodSizeMax ? broodSizeMin.ToString() : broodSizeMin + "-" + broodSizeMax;
                string dayWord = gestationDays == 1 ? "day" : "days";
                returnText += "\n[i]Gestation: " + gestationDays + " " + dayWord + ". Brood size: " + broodText + ".[/i]";
            }

            // For bodyparts, remind the asker what they've pinned to their own (this reply is
            // private, so it's only ever shown to its owner).
            return returnText + PersonalBodypartEiconLine(identifier, characterName);
        }

        /// <summary>
        /// The "[i]Your personal eicon: …[/i]" tail, when the identifier is a bodypart and the
        /// asking resident has pinned one of their own eicons to it with <c>!seteicon</c>.
        /// Empty for everything else.
        /// </summary>
        private static string PersonalBodypartEiconLine(Identifier identifier, string characterName)
        {
            if (identifier.categories == null
                || !identifier.categories.Contains("bodypart", StringComparer.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            Profile profile = MonDB.getProfile(characterName);
            if (profile == null) return string.Empty;

            string personalEicon = InteractionEiconSupport.GetBodypartEicon(profile, identifier.type);
            return string.IsNullOrEmpty(personalEicon)
                ? string.Empty
                : "\n[i]Your personal eicon: " + personalEicon + "[/i]";
        }
    }
}
