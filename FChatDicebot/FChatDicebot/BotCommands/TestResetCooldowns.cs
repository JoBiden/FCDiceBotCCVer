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
    public class TestResetCooldowns : ChatBotCommand
    {
        public TestResetCooldowns()
        {
            Name = "testresetcooldowns";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            CharacterData thisCharacterData = bot.DiceBot.GetCharacterData(address);

            if(thisCharacterData != null)
            {
                thisCharacterData.LastLuckForecastTime = 0;
                thisCharacterData.LastSlotsSpin = 0;
                thisCharacterData.LastWorkedTime = 0;
                thisCharacterData.LastDungeonDelve = 0;

                commandController.SaveCharacterDataToDisk();

                bot.SendMessageInChannel("[i]Reset luck forecast, slots, work, dungeondelve cooldowns for: [/i]" + TextFormat.GetCharacterUserTags(address.character), address);
            }
        }
    }
}
