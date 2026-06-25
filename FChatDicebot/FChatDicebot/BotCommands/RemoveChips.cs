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
    public class RemoveChips : ChatBotCommand
    {
        public RemoveChips()
        {
            Name = "removechips";
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
                //string messageString = "";

                if (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && command.ops == null)
                {
                    bot.RequestChannelOpListAndQueueFurtherRequest(command);
                }
                else
                {
                    bool pot = false;
                    bool targetedName = false;

                    int chipAmount = Utils.GetNumberFromInputs(terms);
                    string appliedName = address.character;

                    if (terms != null && terms.Length > 1)
                    {
                        string[] rawTermsMost = Utils.GetRemainingTermsAfterRemovingOneTerm(rawTerms, chipAmount.ToString());

                        if (terms.Contains("pot") && terms.Length == 2 && chipAmount > 0)
                            pot = true;
                        else
                        {
                            appliedName = Utils.GetUserNameFromFullInputs(rawTermsMost);
                            targetedName = true;
                        }
                    }

                    bool characterIsAdmin = Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, address.character);
                    bool characterIsTrustedForChannel = Utils.IsCharacterTrusted(bot.AccountSettings.TrustedCharacters, address);

                    ChipPile targetPile = bot.DiceBot.GetChipPile(new MessageAddress() { character = appliedName, channel = address.channel, guild = address.guild }, false);
                    if (targetPile == null && targetedName)
                    {
                        bot.SendMessageInChannel("Failed: could not find a pile for user (" + TextFormat.GetCharacterUserTags(appliedName) + ").", address);
                    }
                    else if (targetedName && ((thisChannel.ChipsClearance == ChipsClearanceLevel.DicebotAdmin && !characterIsAdmin && !characterIsTrustedForChannel) ||
                        (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && command.ops != null && !command.ops.Contains(address.character) && !characterIsAdmin)))
                    {
                        bot.SendMessageInChannel(TextFormat.GetCharacterUserTags(address.character) + " cannot perform \'" + Name + "\' with another user's name under the current chip settings for this channel.", address);
                    }
                    else
                    {
                        string messageString = "";
                        if (chipAmount <= 0)
                        {
                            messageString = "Error: You must specify a number of " + BotMain.CurrencyPlaceholder + "s above 0 to add.";
                        }
                        else
                        {
                            messageString = bot.DiceBot.AddChips(new MessageAddress() { character = appliedName, channel = address.channel, guild = address.guild }, -1 * chipAmount, pot);

                            commandController.SaveChipsToDisk("AddChips");
                        }

                        bot.SendMessageInChannel(messageString, address);
                    }
                }


                //if (terms != null && terms.Length >= 1 && terms.Contains("pot"))
                //    pot = true;

                //messageString = "";
                //if (chipAmount <= 0)
                //{
                //    messageString = "Error: You must specify a number of " + BotMain.CurrencyPlaceholder + "s above 0 to remove.";
                //}
                //else
                //{
                //    messageString = bot.DiceBot.AddChips(address, -1 * chipAmount, pot);

                //    commandController.SaveChipsToDisk("RemoveChips");
                //}

                //bot.SendMessageInChannel(messageString, address);
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
        }
    }
}
