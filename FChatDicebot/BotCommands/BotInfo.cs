using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            int channelsNumber = bot.ChannelsJoined.Count();

            double onlineTime = DoubleTime.GetCurrentTimestampSeconds() - bot.FListLoginTime;

            if (Utils.IsDiscordMessage(command))
            {
                onlineTime = DoubleTime.GetCurrentTimestampSeconds() - bot.DiscordLoginTime;
            }

            string resultMessageString = "Chateau Contract Bot was developed by [user]Queen Contract[/user] as an adaptation of Dice Bot, developed by [user]Ambitious Syndra[/user]"
                + "\nCurrent version " + BotMain.Version
                + "\nCurrently operating in " + channelsNumber + " channels."
                + "\nOnline for " + DoubleTime.PrintTimeFromSeconds(onlineTime)
                + "\nCurrent Game Sessions Active: " + (bot.DiceBot.GameSessions != null? bot.DiceBot.GameSessions.Count() : 0)
                + "\nCurrent Chip Piles Recorded: " + (bot.DiceBot.ChipPiles != null ? bot.DiceBot.ChipPiles.Count() : 0)
                + "\nCurrent Tables Recorded: " + (bot.SavedTables != null ? bot.SavedTables.Count() : 0)
                + "\nFor a list of dice commands, use !dicehelp. See the profiles [user]Chateau Contract[/user] and [user]Dice Bot[/user] for more detailed information (this bot may not be up to date with Dice Bot)";

            if (!commandController.MessageCameFromChannel(address))
            {
                bot.SendPrivateMessage(resultMessageString, address);
            }
            else
            {
                bot.SendMessageInChannel(resultMessageString, address);
            }
        }
    }
}
