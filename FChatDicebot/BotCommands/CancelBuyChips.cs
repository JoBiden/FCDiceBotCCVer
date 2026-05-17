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
    public class CancelBuyChips : ChatBotCommand
    {
        public CancelBuyChips()
        {
            Name = "cancelbuychips";
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
                VcChipOrder existingOrder = bot.DiceBot.GetVcChipOrder(characterName, channel);

                if(existingOrder == null)
                {
                    responseMessage = "Failed: An outstanding chips order does not exist for " + Utils.GetCharacterUserTags(characterName);
                }
                else
                {
                    bool success = bot.VelvetcuffConnection.DeleteVcTransaction(existingOrder.TransactionId);

                    
                    if(success)
                    {
                        existingOrder.OrderStatus = 3;
                        bot.DiceBot.VcChipOrders.Remove(existingOrder);

                        responseMessage = Utils.GetCharacterUserTags(characterName) + ", your order for Dice Bot chips from VC was cancelled successfully.";

                        bot.BotCommandController.SaveVcChipOrdersToDisk();
                    }
                    else
                    {
                        responseMessage = "Error: Failed to cancel chips order. [sub]Probably a web connection issue.[/sub] Try again later and contact Ambitious Syndra is the issue persists.";
                    }
                    
                }
            }

            bot.SendMessageInChannel(responseMessage, channel);
        }
    }
}
