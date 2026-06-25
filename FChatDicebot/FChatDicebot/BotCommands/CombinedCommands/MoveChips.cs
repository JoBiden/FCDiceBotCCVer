using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands.Base
{
    public class MoveChips
    {
        public static void Run(BotMain bot, BotCommandController commandController,
            string[] rawTerms, string[] terms, MessageAddress originCharacterAddress,// string characterName, string channel, string guild,
            UserGeneratedCommand command, string originCommandName, string verbUsed, bool chipsMovingFromOriginUser, bool forceGiveWithoutName)// CardMoveType moveType)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(originCharacterAddress);

            bool characterIsAdmin = Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, originCharacterAddress.character);
            bool characterIsTrustedForChannel = Utils.IsCharacterTrusted(bot.AccountSettings.TrustedCharacters, originCharacterAddress);
            bool discordMessage = Utils.IsDiscordMessage(command);
            bool discordAuthorized = discordMessage && commandController.AuthorizedDiscordAdmin(command);

            if ((thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && command.ops == null) && !discordAuthorized)
            {
                bot.RequestChannelOpListAndQueueFurtherRequest(command);
            }
            else if (!thisChannel.AllowChips)
            {
                bot.SendMessageInChannel(originCommandName + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", originCharacterAddress);
            }
            else if ((forceGiveWithoutName || !chipsMovingFromOriginUser) && ((thisChannel.ChipsClearance == ChipsClearanceLevel.DicebotAdmin && !characterIsAdmin && !characterIsTrustedForChannel) ||
                (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && command.ops != null && !command.ops.Contains(originCharacterAddress.character) && !characterIsAdmin) ||
                (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && !discordAuthorized && discordMessage)))
            {
                bot.SendMessageInChannel(TextFormat.GetCharacterUserTags(originCharacterAddress.character) + " cannot perform \'" + originCommandName + "\' under the current chip settings for this channel.", originCharacterAddress);
            }

            //    bool characterIsAdmin = Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, originCharacterAddress.character);

            //if (!thisChannel.AllowChips)
            //{
            //    bot.SendMessageInChannel("Moving " + BotMain.CurrencyPlaceholder + "s is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", originCharacterAddress);
            //}
            //else if (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && command.ops == null)
            //{
            //    bot.RequestChannelOpListAndQueueFurtherRequest(command);
            //}
            //else if ((forceGiveWithoutName || !chipsMovingFromOriginUser) && ((thisChannel.ChipsClearance == ChipsClearanceLevel.DicebotAdmin && !characterIsAdmin) ||
            //    (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && !command.ops.Contains(originCharacterAddress.character) && !characterIsAdmin)))
            //{
            //    bot.SendMessageInChannel(TextFormat.GetCharacterUserTags(originCharacterAddress.character) + " cannot perform [" + originCommandName + "] under the current chip settings for this channel.", originCharacterAddress);
            //}
            else
            {
                string messageString = "";
                if (terms.Length < 2)
                {
                    messageString = "Failed: \'" + originCommandName + "\' requires a number (first) and a user name (second).";
                }
                else
                {
                    bool all = false;
                    int giveAmount = Utils.GetNumberFromInputs(terms);

                    if (giveAmount <= 0 && !all)
                    {
                        messageString = "Failed: You must input a number of " + BotMain.CurrencyPlaceholder + "s over 0 or 'all' to use \'" + originCommandName + "\'";
                    }
                    else
                    {
                        string[] rawTermsMost = Utils.GetRemainingTermsAfterRemovingOneTerm(rawTerms, giveAmount.ToString());

                        string targetUserName = Utils.GetUserNameFromFullInputs(rawTermsMost);

                        if(!chipsMovingFromOriginUser)
                        {
                            messageString = bot.DiceBot.TakeChips(originCharacterAddress, targetUserName, giveAmount, all);
                        }
                        else
                        {
                            if(forceGiveWithoutName)
                            {
                                messageString = bot.DiceBot.GiveChips(originCharacterAddress, targetUserName, giveAmount, all, true);
                            }
                            else
                            {
                                messageString = bot.DiceBot.GiveChips(originCharacterAddress, targetUserName, giveAmount, all, false);
                            }
                        }

                        commandController.SaveChipsToDisk(originCommandName);
                    }
                }

                bot.SendMessageInChannel(messageString, originCharacterAddress);
            }
        }
    }

}
