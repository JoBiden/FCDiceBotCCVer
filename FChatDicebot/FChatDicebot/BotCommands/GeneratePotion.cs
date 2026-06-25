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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string privateMessage = "";
            CharacterData dat = bot.DiceBot.GetCharacterData(address);
            ChannelSettings settings = bot.GetChannelSettings(address);
            string generatedItem = "a random " + commandController.GetChannelPotionName(settings, false);

            GeneratePotionResult rollResult = bot.DiceBot.GeneratePotion(terms, settings, address, true);//, out privateMessage);

            if (rollResult == null)
            {
                bot.SendMessageInChannel("Error: Rollresult was null on GeneratePotion.", address);
            }
            else if (rollResult.Result == null)
            {
                if(!string.IsNullOrEmpty(rollResult.OutputString))
                    bot.SendMessageInChannel(rollResult.OutputString, address);
                else
                    bot.SendMessageInChannel("Error: Failed to generate " + commandController.GetChannelPotionName(settings, false) + " item in GeneratePotion (43).", address);
            }
            else
            {
                privateMessage = rollResult.PrivateMessage;

                ChipPile pile = bot.DiceBot.GetChipPile(address);
                string transactionString = "";
                bool canAffordPotion = false;
                int potionCost = settings.PotionChipsCost;
                if (settings.PotionChipsCost > 0 && settings.AllowChips)
                {
                    bool randomPotion = true;
                    if (rollResult.Result != null && rollResult.SpecificPotionGenerated)
                    {
                        potionCost = rollResult.Result.GetValue();
                        randomPotion = false;
                    }

                    if (randomPotion)
                        generatedItem = "a random " + commandController.GetChannelPotionName(settings, false);
                    else
                        generatedItem = rollResult.Result.GetName();

                    if (pile.Chips >= potionCost)//settings.PotionChipsCost)
                    {
                        pile.Chips -= potionCost;

                        transactionString = "[sub]Paying " + potionCost + " " + BotMain.CurrencyPlaceholder + "s and buying " + generatedItem + "...[/sub]\n";
                        commandController.SaveChipsToDisk("generatepotion");

                        canAffordPotion = true;
                    }
                    else
                    {
                        canAffordPotion = false;
                    }
                }
                else
                {
                    canAffordPotion = true;
                }

                if (canAffordPotion)
                {
                    bot.SendMessageInChannel(transactionString + rollResult.OutputString, address);
                    if (!string.IsNullOrEmpty(privateMessage))
                    {
                        bot.SendPrivateMessage(privateMessage, address);
                    }

                    dat.TimesPotionGenerated += 1;
                    commandController.SaveCharacterDataToDisk();
                }
                else
                {
                    bot.SendMessageInChannel("Failed: You could not afford to buy " + generatedItem + " for " + potionCost + " " + BotMain.CurrencyPlaceholder + "s (" + pile.Chips + " held)", address);
                }
            }
            ////
        }
    }
}
