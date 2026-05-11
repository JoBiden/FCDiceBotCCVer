using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;

namespace FChatDicebot.BotCommands
{
    public class GenerateChipsCode : ChatBotCommand
    {
        public GenerateChipsCode()
        {
            Name = "generatechipscode";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string outputString = "";
            if(rawTerms.Length != 1)
            {
                outputString = "Error: This command requires 1 term. (Amount)";
            }
            else
            {
                int chipAmount = Utils.GetNumberFromInputs(terms);

                string chipCode = chipAmount + "_" + Utils.RandomString(bot.DiceBot.random, 8);

                if (chipAmount > 0 && !string.IsNullOrEmpty(chipCode))
                {
                    while(bot.ChipsCoupons.FirstOrDefault(a => a.Code == chipCode) != null)
                    {
                        chipCode = chipAmount + "_" + Utils.RandomString(bot.DiceBot.random, 8);
                    }
                    
                    bot.ChipsCoupons.Add(new ChipsCoupon()
                    {
                        ChipsAmount = chipAmount,
                        Code = chipCode
                    });
                    commandController.SaveCouponsToDisk();

                    outputString = "Added code " + chipCode + " for " + chipAmount + " chips.";
                }
                else
                {
                    outputString = "Error on inputs for chips code.";
                }
            }

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(outputString, characterName);
            }
            else
            {
                bot.SendMessageInChannel(outputString, channel);
            }
        }
    }
}
