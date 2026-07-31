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

            string resultMessageString = Model.ReadoutText.Title("Chateau Contract Bot")
                + "\nDeveloped by [user]Queen Contract[/user] as an adaptation of Dice Bot, developed by [user]Ambitious Syndra[/user]"
                + "\n" + Model.ReadoutText.Section("Status", Model.ReadoutDomain.Reference)
                + "\n" + Model.ReadoutText.RowIndent + Model.ReadoutText.Row("Version", Model.ReadoutText.Num(BotMain.Version))
                + "\n" + Model.ReadoutText.RowIndent + Model.ReadoutText.Row("Channels", Model.ReadoutText.Num(channelsNumber))
                + "\n" + Model.ReadoutText.RowIndent + Model.ReadoutText.Row("Online For", Model.ReadoutText.Num(DoubleTime.PrintTimeFromSeconds(onlineTime)))
                + "\n" + Model.ReadoutText.RowIndent + Model.ReadoutText.Row("Game Sessions Active", Model.ReadoutText.Num(bot.DiceBot.GameSessions != null ? bot.DiceBot.GameSessions.Count() : 0))
                + "\n" + Model.ReadoutText.RowIndent + Model.ReadoutText.Row("Chip Piles Recorded", Model.ReadoutText.Num(bot.DiceBot.ChipPiles != null ? bot.DiceBot.ChipPiles.Count() : 0))
                + "\n" + Model.ReadoutText.RowIndent + Model.ReadoutText.Row("Tables Recorded", Model.ReadoutText.Num(bot.SavedTables != null ? bot.SavedTables.Count() : 0))
                + "\n" + Model.ReadoutText.Footer("For a list of dice commands, use !dicehelp. See the profiles [user]Chateau Contract[/user] and [user]Dice Bot[/user] for more detailed information (this bot may not be up to date with Dice Bot)");

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
