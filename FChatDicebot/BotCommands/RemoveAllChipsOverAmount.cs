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
    public class RemoveAllChipsOverAmount : ChatBotCommand
    {
        public RemoveAllChipsOverAmount()
        {
            Name = "removeallchipsoveramount";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {

            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            if (thisChannel.AllowChips)
            {
                if (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && command.ops == null)
                {
                    bot.RequestChannelOpListAndQueueFurtherRequest(command);
                }
                else
                {
                    int chipAmount = Utils.GetNumberFromInputs(terms);

                    List<ChipPile> allChannelPiles = bot.DiceBot.GetChannelChipPiles(address);

                    if (chipAmount <= 0)
                    {
                        bot.SendMessageInChannel("Error: You must specify a number of " + BotMain.CurrencyPlaceholder + "s above 0 to use as the limit.", address);
                        return;
                    }
                    if (allChannelPiles == null || allChannelPiles.Count < 1)
                    {
                        bot.SendMessageInChannel("Failed: Did not find any chip piles for this channel.", address);
                        return;
                    }

                    string allOutputs = "";
                    foreach (ChipPile p in allChannelPiles)
                    {
                        if (p != null && p.Chips > chipAmount)
                        {
                            if (!string.IsNullOrEmpty(allOutputs))
                                allOutputs += ", ";

                            allOutputs += TextFormat.GetCharacterUserTags(p.Character) + " had " + p.Chips;
                            p.Chips = chipAmount;
                        }
                    }

                    if (string.IsNullOrEmpty(allOutputs))
                    {
                        allOutputs += "No piles were found above the limit of " + chipAmount + " " + BotMain.CurrencyPlaceholder + "s";
                    }
                    else
                    {
                        allOutputs += "\nAll piles reduced to a maximum of " + chipAmount + " " + BotMain.CurrencyPlaceholder + "s.";
                    }

                    string appliedName = address.character;

                    if (terms != null && terms.Length > 1)
                    {
                        string[] rawTermsMost = Utils.GetRemainingTermsAfterRemovingOneTerm(rawTerms, chipAmount.ToString());

                        if (!(terms.Contains("pot") && terms.Length == 2 && chipAmount > 0))
                        {
                            appliedName = Utils.GetUserNameFromFullInputs(rawTermsMost);
                        }
                    }

                    bot.SendMessageInChannel(allOutputs, address);
                    commandController.SaveChipsToDisk("removeallchipsoveramount");
                }
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
        }
    }
}
