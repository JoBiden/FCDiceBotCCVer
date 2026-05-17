using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class DropPotion : ChatBotCommand
    {
        public DropPotion()
        {
            Name = "droppotion";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.CharacterInventories;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {

            Potion p = bot.DiceBot.GetPotionHeld(characterName, channel);

            if(p == null)
            {
                bot.SendMessageInChannel(Utils.GetCharacterUserTags(characterName) + " is not holding a potion.", channel);
            }
            else
            {
                string targetUserName = characterName;
                //decided this feature wouldn't make much sense, but can drop potion held by another player
                //if(rawTerms != null && rawTerms.Length > 0)
                //{
                //    targetUserName = Utils.GetFullStringOfInputs(rawTerms).Trim();
                //}

                bool success = bot.DiceBot.RemovePotionHeld(targetUserName, channel);

                string output = "Removed potion held by " + Utils.GetCharacterIconTags(targetUserName) + ".";
                if(!success)
                {
                    output = "Failed to remove potion held by " + Utils.GetCharacterIconTags(targetUserName) + ".";
                }
                else
                {
                    bot.BotCommandController.SaveCharacterDataToDisk();
                }


                bot.SendMessageInChannel(output, channel);
            }

        }
    }
}
