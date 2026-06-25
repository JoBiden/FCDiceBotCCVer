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
    public class RedeemChips : ChatBotCommand
    {
        public RedeemChips()
        {
            Name = "redeemchips";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            if (thisChannel.AllowChips)
            {
                if(thisChannel.ChipsClearance != ChipsClearanceLevel.DicebotAdmin)
                {
                    bot.SendMessageInChannel("Redeemchips is only functional in channels that have [b]restricted[/b] " + BotMain.CurrencyPlaceholder + "s pools. Please try using '!addchips' instead.", address);
                }
                else
                {
                    string messageString = "";

                    if(rawTerms.Count() < 1)
                    {
                        messageString = "Error: This command requires a " + BotMain.CurrencyPlaceholder + "s code.";
                    }
                    else
                    {
                        string chipsCode = rawTerms[0];

                        //find chips coupon 
                        ChipsCoupon coupon = bot.ChipsCoupons.FirstOrDefault(a => a.Code == chipsCode);

                        if (coupon == null)
                        {
                            messageString = "" + BotMain.CurrencyPlaceholderCapital + "s code not found (" + chipsCode + ")";
                        }
                        else if (coupon.Redeemed)
                        {
                            messageString = "This " + BotMain.CurrencyPlaceholder + "s code (" + chipsCode + ") has already been redeemed.";
                        }
                        else
                        {
                            //mark redeemed and add chips to the redeeming character
                            coupon.Redeemed = true;
                            coupon.RedeemedBy = address.character;

                            messageString = "" + BotMain.CurrencyPlaceholderCapital + "s code redeemed. " + bot.DiceBot.AddChips(address, coupon.ChipsAmount, false) +
                                " [sub]Thank you for purchasing " + DiceBot.DiceBotCharacter + " " + BotMain.CurrencyPlaceholder + "s![/sub]";

                            commandController.SaveChipsToDisk("RedeemChips");
                            commandController.SaveCouponsToDisk();
                        }
                    }


                    bot.SendMessageInChannel(messageString, address);
                }
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
        }
    }
}
