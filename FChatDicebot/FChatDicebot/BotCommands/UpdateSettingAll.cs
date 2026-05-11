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
    //NOTE: admin only used to manually edit settings for every channel at once
    public class UpdateSettingAll : ChatBotCommand
    {
        public UpdateSettingAll()
        {
            Name = "updatesettingall";
            RequireBotAdmin = true;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedChannels;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            foreach(var character in bot.DiceBot.CharacterDatas)
            {
                character.LastSlotsSpin = 1;
            }
            commandController.SaveCharacterDataToDisk();

            string output = "(All character data updated) set last slots spin to 1";
            
            //foreach (var settings in bot.SavedChannelSettings)
            //{
            //    settings.UseDefaultPotions = true;

            //    //settings.WorkTierRange = 5;
            //    //settings.WorkMultiplier = 100;

            //    //settings.SlotsMultiplierLimit = 1000;
            //    //if (settings.StartWith500Chips)
            //    //    settings.StartingChips = 500;
            //    //else
            //    //    settings.StartingChips = 0;
            //}

            //commandController.SaveChannelSettingsToDisk();

            bot.SendMessageInChannel(output, channel);
        }
    }

}