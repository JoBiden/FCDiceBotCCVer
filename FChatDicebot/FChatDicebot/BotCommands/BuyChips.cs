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
    public class BuyChips : ChatBotCommand
    {
        public BuyChips()
        {
            Name = "buychips";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings chan = bot.GetChannelSettings(channel);
            string responseMessage = "";
            if(!chan.AllowChips)
            {
                responseMessage = "Chips are not allowed under the settings for this channel.";
            }
            else
            {
                ChipPile pile = bot.DiceBot.GetChipPile(characterName, channel);
                
                VcChipOrder existingOrder = bot.DiceBot.GetVcChipOrder(characterName, channel);

                if(pile == null)
                {
                    responseMessage = "Error: Chips pile not found for " + Utils.GetCharacterUserTags(characterName);
                }
                else if(existingOrder != null)
                {
                    responseMessage = "Failed: An outstanding chips order already exists for " + Utils.GetCharacterUserTags(characterName);
                }
                else
                {
                    int buyAmount = 2000;

                    if(terms.Length > 0)
                    {
                        buyAmount = Utils.GetNumberFromInputs(terms);
                    }

                    if (buyAmount <= 1000)
                        buyAmount = 1000;
                    if (buyAmount > DiceBot.MaximumChipBuySize)
                        buyAmount = DiceBot.MaximumChipBuySize;
                    

                    bot.SendMessageInChannel("Starting buy chips with VC$...", channel);

                    bool success = bot.VelvetcuffConnection.CreateNewVcTransaction(buyAmount, characterName, DiceBot.DiceBotCharacter, false, "Order for " + buyAmount + " VC - Casino chips.");
                    if(success)
                    {
                        string vcTransactionId = bot.VelvetcuffConnection.CurrentPaymentId;
                        bool added = bot.DiceBot.AddVcChipOrder(buyAmount, characterName, channel, vcTransactionId);

                        responseMessage = Utils.GetCharacterUserTags(characterName) + " has created an order for [color=yellow]" + buyAmount + "[/color] Dice Bot chips.\n[sub]Please accept the VC invoice as soon as possible.[/sub]";

                        bot.BotCommandController.SaveVcChipOrdersToDisk();
                    }
                    else
                    {
                        responseMessage = "Error: Failed to order chips. [sub]Probably a web connection issue.[/sub] Try again later and contact Ambitious Syndra is the issue persists.";
                    }
                    
                }
            }

            bot.SendMessageInChannel(responseMessage, channel);
        }
    }
}
