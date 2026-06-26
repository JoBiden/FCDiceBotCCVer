using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class ChateauWhatis : ChatBotCommand
    {
        public ChateauWhatis()
        {
            Name = "whatis";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !identifier";
            LongDescription = "Shorthand for the !identifier command. See !help identifier for full details.";
            Usage = "!whatis {identifier}";
            RelatedCommands = new string[] { "identifier", "category", "list" };
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
            ChateauIdentifier identifier = new ChateauIdentifier();
            identifier.Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
