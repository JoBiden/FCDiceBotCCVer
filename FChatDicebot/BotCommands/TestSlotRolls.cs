using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string slotsName = commandController.GetNonNumberWordFromCommandTerms(terms);

            SavedSlotsSetting savedSlots = Utils.GetSlotsFromId(bot.SavedSlots, slotsName);

            CharacterData thisCharacterData = bot.DiceBot.GetCharacterData(characterName, channel);
            
            string errorMessage = "";
            
            SlotsSetting usedSlots = null;
            //find possibilities from channel settings for slots
            if (savedSlots != null)
                usedSlots = savedSlots.SlotsSetting;
            else
                usedSlots = commandController.GetDefaultSlotsSetting(false);

            Dictionary<int, int> winnings = new Dictionary<int, int>();
            for(int i = 0; i < 10000; i++)
            {
                SlotsSpinResult result = usedSlots.GetSpinResult(bot.DiceBot.random, 500, 1, 25000, null);
                if (winnings.ContainsKey(result.Winnings))
                {
                    winnings[result.Winnings]++;
                }
                else
                    winnings.Add(result.Winnings, 1);
            }

            string sendMessage = "";
            foreach(KeyValuePair<int,int> winrow in winnings)
            {
                sendMessage += "\n" + winrow.Key + " #" + winrow.Value;
            }

            bot.SendMessageInChannel(sendMessage, channel);
        }
    }
}
