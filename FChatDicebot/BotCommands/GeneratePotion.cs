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
    public class GeneratePotion : ChatBotCommand
    {
        public GeneratePotion()
        {
            Name = "generatepotion";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.CharacterInventories;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {

            string privateMessage = "";

            CharacterData dat = bot.DiceBot.GetCharacterData(characterName, channel);

            string rollResult = bot.DiceBot.GeneratePotion(terms, characterName, channel, true, out privateMessage);

            ChannelSettings settings = bot.GetChannelSettings(channel);
            ChipPile pile = bot.DiceBot.GetChipPile(characterName, channel);
            string transactionString = "";
            bool generate = false;
            if(settings.PotionChipsCost > 0 && settings.AllowChips)
            {

                if(pile.Chips >= settings.PotionChipsCost)
                {
                    pile.Chips -= settings.PotionChipsCost;

                    transactionString = "[sub]Paying " + settings.PotionChipsCost + " chips and buying a potion...[/sub]\n";

                    commandController.SaveChipsToDisk("generatepotion");

                    generate = true;
                }
                else
                {
                    generate = false;
                }
            }
            else
            {
                generate = true;
            }

            if(generate)
            {
                bot.SendMessageInChannel(transactionString + rollResult, channel);
                if (!string.IsNullOrEmpty(privateMessage))
                {
                    bot.SendPrivateMessage(privateMessage, characterName);
                }

                dat.TimesPotionGenerated += 1;
                commandController.SaveCharacterDataToDisk();
            }
            else
            {
                bot.SendMessageInChannel("Failed: You could not afford to buy a potion for " + settings.PotionChipsCost + " chips (" + pile.Chips +" held)", channel);
            }
        }
    }
}
