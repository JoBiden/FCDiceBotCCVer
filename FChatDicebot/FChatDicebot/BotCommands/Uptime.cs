using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            int channelsNumber = bot.ChannelsJoined.Count();
            double onlineTime = DoubleTime.GetCurrentTimestampSeconds() - bot.FListLoginTime;

            if (Utils.IsDiscordMessage(command))
            {
                onlineTime = DoubleTime.GetCurrentTimestampSeconds() - bot.DiscordLoginTime;
            }

            if (!commandController.MessageCameFromChannel(address))
            {
                bot.SendPrivateMessage("Dicebot has been online for " + DoubleTime.PrintTimeFromSeconds(onlineTime), address);
            }
            else
            {
                bot.SendMessageInChannel("Dicebot has been online for " + DoubleTime.PrintTimeFromSeconds(onlineTime), address);
            }
        }
    }
}
