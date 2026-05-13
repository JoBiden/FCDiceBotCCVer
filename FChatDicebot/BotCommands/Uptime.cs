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
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Check how long the bot has been online";
            LongDescription = "Displays how long the Chateau Contract Bot has been running since its last restart, formatted in days, hours, minutes, and seconds.";
            Usage = "!uptime";
            RelatedCommands = new string[] { "botinfo" };
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
            int channelsNumber = bot.ChannelsJoined.Count();
            double onlineTime = Utils.GetCurrentTimestampSeconds() - bot.LoginTime;

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage("Chateau Contract has been online for " + Utils.PrintTimeFromSeconds(onlineTime), characterName);
            }
            else
            {
                bot.SendMessageInChannel("Chateau Contract has been online for " + Utils.PrintTimeFromSeconds(onlineTime), channel);
            }
        }
    }
}
