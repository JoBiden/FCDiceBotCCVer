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
    public class ViewStartupChannels : ChatBotCommand
    {
        public ViewStartupChannels()
        {
            Name = "viewstartupchannels";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedChannels;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            List<ChannelSettings> chans = bot.SavedChannelSettings.Where(a => a.StartupChannel).ToList();

            string totalList = "";
            List<int> numbers = Utils.GetAllNumbersFromInputs(rawTerms);
            int startCount = 0;
            if (numbers.Count > 0)
                startCount = numbers[0];

            int counter = 0;
            foreach (ChannelSettings chanset in chans)
            {
                counter++;
                List<CharacterData> relevantDatas = bot.DiceBot.CharacterDatas.Where(a => a.Channel != null && chanset.Name != null && a.Channel.ToLower() == chanset.Name.ToLower()).ToList();
                int totalSpins = 0;
                int totalWorked = 0;
                int totalPotions = 0;
                if (relevantDatas != null && relevantDatas.Count > 0)
                {
                    totalSpins = relevantDatas.Sum(a => a.TimesSlotsSpun);
                    totalWorked = relevantDatas.Sum(a => a.TimesWorked);
                    totalPotions = relevantDatas.Sum(a => a.TimesPotionGenerated);
                }

                int customPotionsListCount = bot.SavedPotions.Count(a => a.Channel == chanset.Name);
                bool customJobs = bot.SavedJobsLists.Count(a => a.Channel == chanset.Name) > 0;
                //bot.DiceBot.

                string thisChannelPrintout = "(" + counter + ") [session]" + chanset.Name + "[/session]" + " " + chanset.Name + ": characters saved " + relevantDatas.Count + " | Last Message " + DoubleTime.ConvertFromSecondsTimestamp(chanset.LastBotmessageToChannel).ToShortDateString() + " | Total Messages " + chanset.TotalBotMessages +
                    ", total slots & works & potions " + totalSpins + ", " + totalWorked + ", " + totalPotions + " | roulette & blackjack time " + chanset.LastRouletteSpinTime + ", " + chanset.LastBlackjackGameTime + " | custom potions created " + customPotionsListCount + " | custom jobs? " + customJobs + "\n";
                if (counter >= startCount)
                    totalList += thisChannelPrintout;
            }

            string output = "List of flist channels with startup enabled in order:\n" + totalList;

            if (commandController.MessageCameFromChannel(address))
                bot.SendMessageInChannel(output, address);
            else
                bot.SendPrivateMessage(output, address);
        }
    }
}
