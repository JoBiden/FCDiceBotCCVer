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
                onlineTime = DoubleTime.GetCurrentTimestampSeconds() -  bot.DiscordLoginTime;
            }

            string resultMessageString = "Dice Bot is developed and hosted by [user]Ambitious Syndra[/user]"
                + "\nCurrent version " + BotMain.Version
                + "\nCurrently operating in " + channelsNumber + " channels."
                + "\nOnline for " + DoubleTime.PrintTimeFromSeconds(onlineTime)
                + "\nCurrent Game Sessions Active: " + (bot.DiceBot.GameSessions != null? bot.DiceBot.GameSessions.Count() : 0)
                + "\nCurrent Chip Piles Recorded: " + (bot.DiceBot.ChipPiles != null ? bot.DiceBot.ChipPiles.Count() : 0)
                + "\nCurrent Tables Recorded: " + (bot.SavedTables != null ? bot.SavedTables.Count() : 0)
                + "\nFor a list of commands, use !help. See the profile [user]Dice Bot[/user] for more detailed information.";

            if (terms.Contains("history") || terms.Contains("h") || terms.Contains("s"))
            {
                resultMessageString = "Dice Bot was developed by [user]Ambitious Syndra[/user] on 10/12/2020"
                    + "\nCurrent version " + BotMain.Version
                    + "\nFirst Discord Version " + "1.50 on 7/13/2024"
                    + "\nMaximum F-Chat channels joined (75) first reached 3/31/2025"
                    + "\nCurrently operating in " + channelsNumber + " channels."
                    + "\nOnline for " + DoubleTime.PrintTimeFromSeconds(onlineTime)
                    + "\nFor a list of commands, use !help. See the profile [user]Dice Bot[/user] for more detailed information.";
            }

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
