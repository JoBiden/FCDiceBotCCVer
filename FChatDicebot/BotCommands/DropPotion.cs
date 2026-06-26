using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;

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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {

            Potion p = bot.DiceBot.GetPotionHeld(address);

            if(p == null)
            {
                bot.SendMessageInChannel(TextFormat.GetCharacterUserTags(address.character) + " is not holding a potion.", address);
            }
            else
            {
                //string targetUserName = characterName;
                //decided this feature wouldn't make much sense, but can drop potion held by another player
                //if(rawTerms != null && rawTerms.Length > 0)
                //{
                //    targetUserName = Utils.GetFullStringOfInputs(rawTerms).Trim();
                //}

                bool success = bot.DiceBot.RemovePotionHeld(address);// targetUserName, channel);

                string output = "Removed potion held by " + TextFormat.GetCharacterIconTags(address.character) + ".";
                if(!success)
                {
                    output = "Failed to remove potion held by " + TextFormat.GetCharacterIconTags(address.character) + ".";
                }
                else
                {
                    bot.BotCommandController.SaveCharacterDataToDisk();
                }


                bot.SendMessageInChannel(output, address);
            }

        }
    }
}
