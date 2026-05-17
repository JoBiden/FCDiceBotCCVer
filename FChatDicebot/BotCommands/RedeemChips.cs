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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            if (thisChannel.AllowChips)
            {
                if(thisChannel.ChipsClearance != ChipsClearanceLevel.DicebotAdmin)
                {
                    bot.SendMessageInChannel("Redeemchips is only functional in channels that have [b]restricted[/b] chips pools. Please try using '!addchips' instead.", channel);
                }
                else
                {
                    string messageString = "";

                    if(rawTerms.Count() < 1)
                    {
                        messageString = "Error: This command requires a chips code.";
                    }
                    else
                    {
                        string chipsCode = rawTerms[0];

                        //find chips coupon 
                        ChipsCoupon coupon = bot.ChipsCoupons.FirstOrDefault(a => a.Code == chipsCode);

                        if (coupon == null)
                        {
                            messageString = "chips code not found (" + chipsCode + ")";
                        }
                        else if (coupon.Redeemed)
                        {
                            messageString = "This chips code (" + chipsCode + ") has already been redeemed.";
                        }
                        else
                        {
                            //mark redeemed and add chips to the redeeming character
                            coupon.Redeemed = true;
                            coupon.RedeemedBy = characterName;

                            messageString = "Chips code redeemed. " + bot.DiceBot.AddChips(characterName, channel, coupon.ChipsAmount, false) +
                                " [sub]Thank you for purchasing Dice Bot chips![/sub]";

                            commandController.SaveChipsToDisk("RedeemChips");
                            commandController.SaveCouponsToDisk();
                        }
                    }
                    

                    bot.SendMessageInChannel(messageString, channel);
                }
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", channel);
            }
        }
    }
}
