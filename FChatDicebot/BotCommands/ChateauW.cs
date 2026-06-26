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

namespace FChatDicebot.BotCommands
{
    public class ChateauW : ChatBotCommand
    {
        public ChateauW()
        {
            Name = "w";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !work";
            LongDescription = "Shorthand for the !work command. See !help work for full details.";
            Usage = "!w\n!w [choice number]";
            RelatedCommands = new string[] { "work", "volunteer", "bank" };
            CooldownDuration = "1 day";
            CooldownAppliesTo = "initiator";
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
            ChateauWork work = new ChateauWork();
            work.Run(bot, commandController, rawTerms, terms, address, command);
           
        }
    }
}
