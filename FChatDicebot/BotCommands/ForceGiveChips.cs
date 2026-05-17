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
    public class ForceGiveChips : ChatBotCommand
    {
        public ForceGiveChips()
        {
            Name = "forcegivechips";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            MoveChips.Run(bot, commandController, rawTerms, terms, characterName, channel, command, "ForceGiveChips", "give", true, true);
        }
    }
}
