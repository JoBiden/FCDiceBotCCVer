using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
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
            RelatedCommands = new string[] { "identifiers", "list" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string returnText = string.Empty;
            if (terms.Length < 1)
            {
                returnText = "Use this command to read the description of an identifier. If you want to see a list of identifiers, use !category instead. Please note that only the first term provided will be described, and any additional terms will be ignored.";
            } else
            {
                Identifier identifier = MonDB.getIdentifier(terms.FirstOrDefault());
                if (identifier != null)
                {
                    returnText = "[b]" + identifier.type + "[/b]\n" + identifier.description;
                }
                else
                {
                    returnText = "That identifier was not found in our records. Are you sure you spelled it right?";
                }
                
                
            }
                
            bot.SendPrivateMessage(returnText, characterName);
            
        }
    }
}
