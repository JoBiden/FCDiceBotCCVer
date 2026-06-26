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
    public class Directory : ChatBotCommand
    {
        public Directory()
        {
            Name = "directory";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedChannels;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            if (commandController.MessageCameFromChannel(address))
            {
                bot.SendMessageInChannel("Failed: The '!directory' command can only be done in private message [sub](to prevent spam)[/sub].", address);
                return;
            }

            List<ChannelSettings> chans = bot.SavedChannelSettings.Where(a => a.StartupChannel && a.ShowInDirectory).ToList();
            int unlistedChans = bot.SavedChannelSettings.Count(a => a.StartupChannel && !a.ShowInDirectory);            //List<string> channelInfos = new List<string>();

            int counter = 0;
            string totalList = "[icon]" + bot.AccountSettings.CharacterName + "[/icon] [b][color=yellow]" + bot.AccountSettings.CharacterName + " Directory[/color][/b] [icon]" + bot.AccountSettings.CharacterName + "[/icon]\n";
            foreach (ChannelSettings chanset in chans)
            {
                counter++;

                int customPotionsListCount = bot.SavedPotions.Count(a => a.Channel != null && a.Channel.ToLower() == chanset.Name.ToLower());
                bool customJobs = bot.SavedJobsLists.Count(a => a.Channel != null && a.Channel.ToLower() == chanset.Name.ToLower()) > 0;

                int characterDatas = bot.DiceBot.CharacterDatas.Count(a => a.Channel != null && chanset != null && chanset.Name != null && a.Channel.ToLower() == chanset.Name.ToLower());
                double currentTime = DoubleTime.GetCurrentTimestampSeconds();
                string timeDiff = DoubleTime.PrintTimeFromSeconds(currentTime - chanset.LastBotmessageToChannel);
                string thisChannelPrintout = "(" + counter + ") " + chanset.ChannelDisplayName + " [session=Join]" + chanset.Name + "[/session]: " + chanset.DirectoryListing + "\n" +
                    "      [b]Start Date:[/b] " + DoubleTime.ConvertFromSecondsTimestamp(chanset.CreationDate) + ", [b]Characters Registered:[/b] " + characterDatas + ", [b]Last Command Sent:[/b] " + timeDiff + " ago \n"
                    + "      [b]work enabled?[/b] " + Utils.GetYesNo(chanset.AllowWork) + " [b]slots?[/b] " + Utils.GetYesNo(chanset.AllowSlots) + " [b]games?[/b] " + Utils.GetYesNo(chanset.AllowGames) + " [b]tables?[/b] " + Utils.GetYesNo(chanset.AllowTableRolls)
                    + " [b]delves?[/b] " + Utils.GetYesNo(chanset.AllowRPG) + " [b]custom potions?[/b] " + Utils.GetYesNo(customPotionsListCount > 0) + " [b]custom jobs?[/b] " + Utils.GetYesNo(customJobs) + "\n\n";
                totalList += thisChannelPrintout;
            }

            string output = totalList;// "List of flist channels with startup enabled in order\n: " + channels;

            output += "\nThere are also " + unlistedChans + " unlisted channel" + TextFormat.SIfPlural(unlistedChans) + " that have ShowInDirectory set to false.";

            if (commandController.MessageCameFromChannel(address))
                bot.SendMessageInChannel(output, address);
            else
                bot.SendPrivateMessage(output, address);
        }
    }
}
