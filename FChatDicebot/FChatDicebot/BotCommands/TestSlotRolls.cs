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
    public class TestSlotRolls : ChatBotCommand
    {
        public TestSlotRolls()
        {
            Name = "testslotrolls";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string slotsName = commandController.GetNonNumberWordFromCommandTerms(terms);

            SavedSlotsSetting savedSlots = Utils.GetSlotsFromId(bot.SavedSlots, slotsName);

            //CharacterData thisCharacterData = bot.DiceBot.GetCharacterData(address);

            //string errorMessage = "";

            SlotsSetting usedSlots = null;
            //find possibilities from channel settings for slots
            if (savedSlots != null)
                usedSlots = savedSlots.SlotsSetting;
            else
                usedSlots = commandController.GetDefaultSlotsSetting(SlotsType.Default);

            Dictionary<int, int> winnings = new Dictionary<int, int>();
            int totalspent = 0;
            int totalwon = 0;
            int chipsBet = 500;
            int jackpot = 25000;
            int largestJackpot = 0;
            int spinAmount = 20000;
            for(int i = 0; i < spinAmount; i++)
            {
                SlotsSpinResult result = usedSlots.GetSpinResult(bot.DiceBot.random, chipsBet, 1, jackpot, null);
                if (winnings.ContainsKey(result.Winnings))
                {
                    winnings[result.Winnings]++;
                }
                else
                    winnings.Add(result.Winnings, 1);
                jackpot = result.NewJackpotAmount;
                totalspent += chipsBet;
                totalwon += result.Winnings;
                if (result.WonJackpot && result.Winnings > largestJackpot)
                    largestJackpot = result.Winnings;
            }

            string sendMessage = spinAmount + " spins: spent " + totalspent + " won " + totalwon + ", largest jackpot " + largestJackpot;

            foreach(KeyValuePair<int,int> winrow in winnings)
            {
                sendMessage += "\n" + winrow.Key + " #" + winrow.Value;
            }

            bot.SendMessageInChannel(sendMessage, address);
        }
    }
}
