using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class ChateauList : ChatBotCommand
    {
        public ChateauList()
        {
            Name = "list";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !identifiers";
            LongDescription = "Shorthand for the !identifiers command. Lists all available identifiers for a given category. See !help identifiers for full details.";
            Usage = "!list {category}";
            RelatedCommands = new string[] { "identifiers", "identifier", "whatis" };
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
            ChateauIdentifiers category = new ChateauIdentifiers();
            category.Run(bot, commandController, rawTerms, terms, characterName, channel, command);
        }
    }
}
