using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;

namespace FChatDicebot.BotCommands
{
    public class Uptime : ChatBotCommand
    {
        public Uptime()
        {
            Name = "uptime";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            int channelsNumber = bot.ChannelsJoined.Count();
            TimeSpan onlineTime = DateTime.UtcNow - bot.LoginTime;

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage("Chateau Contract has been online for " + Utils.GetTimeSpanPrint(onlineTime), characterName);
            }
            else
            {
                bot.SendMessageInChannel("Chateau Contract has been online for " + Utils.GetTimeSpanPrint(onlineTime), channel);
            }
        }
    }
}
