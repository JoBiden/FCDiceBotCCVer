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
    public class ChateauHug : ChatBotCommand
    {
        public ChateauHug()
        {
            Name = "hug";
            Aliases = new string[] { };
            Category = "Casual Interaction";
            ShortDescription = "Alias for !cuddle";
            LongDescription = "Alternative wording for the !cuddle command. See !help cuddle for full details.";
            Usage = "!hug [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "cuddle", "kiss", "handhold" };
            CooldownDuration = "30 Minutes";
            CooldownAppliesTo = "both initiator and recipient";
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            ChateauCuddle cuddle = new ChateauCuddle();
            cuddle.Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
