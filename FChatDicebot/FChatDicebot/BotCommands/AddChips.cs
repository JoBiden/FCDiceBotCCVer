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
    public class AddChips : ChatBotCommand
    {
        public AddChips()
        {
            Name = "addchips";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            bool characterIsAdmin = Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, address.character);
            bool characterIsTrustedForChannel = Utils.IsCharacterTrusted(bot.AccountSettings.TrustedCharacters, address);
            bool discordMessage = Utils.IsDiscordMessage(command);
            bool discordAuthorized = discordMessage && commandController.AuthorizedDiscordAdmin(command);

            if ((thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && command.ops == null) && !discordAuthorized)
            {
                bot.RequestChannelOpListAndQueueFurtherRequest(command);
            }
            else if (!thisChannel.AllowChips)
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
            else if ((thisChannel.ChipsClearance == ChipsClearanceLevel.DicebotAdmin && !characterIsAdmin && !characterIsTrustedForChannel) ||
                (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && command.ops != null && !command.ops.Contains(address.character) && !characterIsAdmin) ||
                (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && !discordAuthorized && discordMessage))
            {
                bot.SendMessageInChannel(TextFormat.GetCharacterUserTags(address.character) + " cannot perform \'" + Name + "\' under the current chip settings for this channel.", address);
            }
            else
            {
                bool pot = false;
                bool targetedName = false;

                int chipAmount = Utils.GetNumberFromInputs(terms);
                string appliedName = address.character;

                if(terms != null && terms.Length > 1)
                {
                    string[] rawTermsMost = Utils.GetRemainingTermsAfterRemovingOneTerm(rawTerms, chipAmount.ToString());

                    if(terms.Contains("pot") && terms.Length == 2 && chipAmount > 0)
                        pot = true;
                    else
                    {
                        appliedName = Utils.GetUserNameFromFullInputs(rawTermsMost);
                        targetedName = true;
                    }
                }

                ChipPile targetPile = bot.DiceBot.GetChipPile(new MessageAddress() { character = appliedName, channel = address.channel, guild = address.guild }, false);
                if(targetPile == null && targetedName)
                {
                    bot.SendMessageInChannel("Failed: could not find a pile for user (" + TextFormat.GetCharacterUserTags(appliedName) + ").", address);
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
                        messageString = bot.DiceBot.AddChips(new MessageAddress() { character = appliedName, channel = address.channel, guild = address.guild }, chipAmount, pot);

                        commandController.SaveChipsToDisk("AddChips");
                    }

                    bot.SendMessageInChannel(messageString, address);
                }
            }
        }
    }
}
