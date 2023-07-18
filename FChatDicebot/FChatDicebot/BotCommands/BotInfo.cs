using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;

namespace FChatDicebot.BotCommands
{
    public class BotInfo : ChatBotCommand
    {
        public BotInfo()
        {
            Name = "botinfo";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            int channelsNumber = bot.ChannelsJoined.Count();
            TimeSpan onlineTime = DateTime.UtcNow - bot.LoginTime;

            string resultMessageString = "Chateau Contract Bot was developed by [user]Queen Contract[/user] as an adaptation of Dice Bot, developed by [user]Ambitious Syndra[/user]"
                + "\nCurrent version " + BotMain.Version
                + "\nOnline for " + Utils.GetTimeSpanPrint(onlineTime)
                + "\nFor a list of commands, use !help. See the profiles [user]Chateau Contract[/user] and [user]Dice Bot[/user] for more detailed information (this bot may not be up to date with Dice Bot)";
                
            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(resultMessageString, characterName);
            }
            else
            {
                bot.SendMessageInChannel(resultMessageString, channel);
            }
        }
    }
}
