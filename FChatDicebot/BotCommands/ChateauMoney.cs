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
    public class ChateauMoney : ChatBotCommand
    {
        public ChateauMoney()
        {
            Name = "money";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !bank";
            LongDescription = "Alternative wording for the !bank command. See !help bank for full details.";
            Usage = "!money\nor\n!money [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "bank", "work", "volunteer" };
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
            ChateauBank bank = new ChateauBank();
            bank.Run(bot, commandController, rawTerms, terms, characterName, channel, command);
        }
    }
}
