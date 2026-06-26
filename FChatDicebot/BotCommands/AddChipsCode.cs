using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.SavedData;
using Newtonsoft.Json;

namespace FChatDicebot.BotCommands
{
    public class AddChipsCode : ChatBotCommand
    {
        public AddChipsCode()
        {
            Name = "addchipscode";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            if(rawTerms.Length < 2)
            {
                bot.SendMessageInChannel("Error: This command requires 2 terms. (Amount and " + BotMain.CurrencyPlaceholder + " code)", address);
            }
            else
            {
                
                int chipAmount = Utils.GetNumberFromInputs(terms);

                string chipCode = rawTerms[1];

                if (chipAmount > 0 && !string.IsNullOrEmpty(chipCode))
                {

                    bot.ChipsCoupons.Add(new ChipsCoupon()
                    {
                        ChipsAmount = chipAmount,
                        Code = chipCode
                    });
                    commandController.SaveCouponsToDisk();

                    bot.SendMessageInChannel("Added code " + chipCode + " for " + chipAmount + " " + BotMain.CurrencyPlaceholder + TextFormat.SIfPlural(chipAmount) + ".", address);
                }
                else
                {
                    bot.SendMessageInChannel("Error on inputs for " + BotMain.CurrencyPlaceholder + "s code.", address);
                }


            }
        }
    }
}
