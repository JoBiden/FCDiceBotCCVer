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
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Display bot version and developer information";
            LongDescription = "Shows information about the Chateau Contract Bot including current version, developer credits, and how long the bot has been online. Also references the Dice Bot that this was adapted from.";
            Usage = "!botinfo";
            RelatedCommands = new string[] { "uptime", "help" };
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

            string resultMessageString = "Chateau Contract Bot was developed by [user]Queen Contract[/user] as an adaptation of Dice Bot, developed by [user]Ambitious Syndra[/user]"
                + "\nCurrent version " + BotMain.Version
                + "\nCurrently operating in " + channelsNumber + " channels."
                + "\nOnline for " + Utils.PrintTimeFromSeconds(onlineTime)
                + "\nFor a list of dice commands, use !dicehelp. See the profiles [user]Chateau Contract[/user] and [user]Dice Bot[/user] for more detailed information (this bot may not be up to date with Dice Bot)";
                
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
