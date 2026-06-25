using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands
{
    //NOTE: admin only used to manually edit settings for every channel at once
    public class AuditChannels : ChatBotCommand
    {
        public AuditChannels()
        {
            Name = "auditchannels";
            RequireBotAdmin = true;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedChannels;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {

            bot.BeginChannelAudit();

            string output = "starting channel audit. see bot log for details";
            bot.SendMessageInChannel(output, address);
        }
    }

}