using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands.Base
{
    public class MoveChips
    {
        public static void Run(BotMain bot, BotCommandController commandController, 
            string[] rawTerms, string[] terms, string characterName, string channel, 
            UserGeneratedCommand command, string originCommandName, string verbUsed, bool chipsMovingFromOriginUser, bool forceGiveWithoutName)// CardMoveType moveType)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            bool characterIsAdmin = Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, characterName);

            if (!thisChannel.AllowChips)
            {
                bot.SendMessageInChannel("Moving chips is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", channel);
            }
            else if (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && command.ops == null)
            {
                bot.RequestChannelOpListAndQueueFurtherRequest(command);
            }
            else if ((forceGiveWithoutName || !chipsMovingFromOriginUser) && ((thisChannel.ChipsClearance == ChipsClearanceLevel.DicebotAdmin && !characterIsAdmin) ||
                (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && !command.ops.Contains(characterName) && !characterIsAdmin)))
            {
                bot.SendMessageInChannel(Utils.GetCharacterUserTags(characterName) + " cannot perform [" + originCommandName + "] under the current chip settings for this channel.", channel);
            }
            else
            {
                string messageString = "";
                if (terms.Length < 2)
                {
                    messageString = "Error: This command requires a number (first) and a user name (second).";
                }
                else
                {
                    bool all = false;
                    int giveAmount = Utils.GetNumberFromInputs(terms);

                    if (giveAmount <= 0 && !all)
                    {
                        messageString = "Error: You must input a number to take an amount of chips.";
                    }
                    else
                    {
                        string[] rawTermsMost = Utils.GetRemainingTermsAfterRemovingOneTerm(rawTerms, giveAmount.ToString());

                        string targetUserName = Utils.GetUserNameFromFullInputs(rawTermsMost);

                        if(!chipsMovingFromOriginUser)
                        {
                            messageString = bot.DiceBot.TakeChips(characterName, targetUserName, channel, giveAmount, all);
                        }
                        else
                        {
                            if(forceGiveWithoutName)
                            {
                                messageString = bot.DiceBot.GiveChips(characterName, targetUserName, channel, giveAmount, all, true);
                            }
                            else
                            {
                                messageString = bot.DiceBot.GiveChips(characterName, targetUserName, channel, giveAmount, all, false);
                            }
                        }

                        commandController.SaveChipsToDisk(originCommandName);
                    }
                }

                bot.SendMessageInChannel(messageString, channel);
            }
        }
    }

}
