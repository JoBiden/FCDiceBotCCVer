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
    public class ChateauBalance : ChatBotCommand
    {
        public ChateauBalance()
        {
            Name = "balance";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !bank";
            LongDescription = "Alternative wording for the !bank command. See !help bank for full details.";
            Usage = "!balance\nor\n!balance [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "bank", "work", "volunteer" };
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
            ChateauBank bank = new ChateauBank();
            bank.Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
