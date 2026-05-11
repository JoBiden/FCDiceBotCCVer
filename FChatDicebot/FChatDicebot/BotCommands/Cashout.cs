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
    public class Cashout : ChatBotCommand
    {
        public Cashout()
        {
            Name = "cashout";
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
            else if (channel.ToLower() != BotMain.CasinoChannelId)
            {
                responseMessage = "Only chip piles in Velvetcuff Casino channel are eligible for the !cashout command.";
            }
            else
            {
                ChipPile pile = bot.DiceBot.GetChipPile(characterName, channel);
                
                bool timerFinished = bot.DiceBot.CountdownFinishedOrNotStarted(channel, characterName + DiceBot.PlayerCashoutSuffix);

                if(pile == null)
                {
                    responseMessage = "Error: Chips pile not found for " + Utils.GetCharacterUserTags(characterName);
                }
                else if(pile.Chips < DiceBot.MinimumChipCashoutSize)
                {
                    responseMessage = "Failed: You do not have enough chips to cash out, " + Utils.GetCharacterUserTags(characterName) + ". (held: " + pile.Chips + ") (minimum: " + DiceBot.MinimumChipCashoutSize + ")";
                }
                else if(!timerFinished)
                {
                    CountdownTimer redeemTimer = bot.DiceBot.GetCountdownTimer(channel, characterName + DiceBot.PlayerCashoutSuffix);

                    responseMessage = "Failed: You must wait longer cash out again, " + Utils.GetCharacterUserTags(characterName) + ". (remaining: " + redeemTimer.GetMinutesRemaining().ToString("F0") + " minutes)";
                }
                else
                {
                    int cashoutAmount = pile.Chips;

                    if(terms.Length > 0)
                    {
                        cashoutAmount = Utils.GetNumberFromInputs(terms);
                        if(cashoutAmount <= 0 || cashoutAmount > pile.Chips)
                        {
                            cashoutAmount = pile.Chips;
                        }
                    }
                    if (cashoutAmount > DiceBot.MaximumChipCashoutSize)
                        cashoutAmount = DiceBot.MaximumChipCashoutSize;

                    bot.SendMessageInChannel("Starting cash out from chips to VC$...", channel);

                    bool success = bot.VelvetcuffConnection.CreateNewVcTransaction(cashoutAmount, characterName, DiceBot.DiceBotCharacter, true, "Cashing out with " + cashoutAmount + " chips from the Casino.");
                    
                    if(success)
                    {
                        string giveChips = bot.DiceBot.GiveChips(characterName, DiceBot.HousePlayerAlias, channel, cashoutAmount, cashoutAmount == pile.Chips);

                        bot.BotCommandController.SaveChipsToDisk("Cashout");
                        bot.DiceBot.StartCountdownTimer(channel, characterName + DiceBot.PlayerCashoutSuffix, characterName, DiceBot.CashoutCooldownMs);

                        responseMessage = giveChips + "\n" + Utils.GetCharacterUserTags(characterName) + " has cashed out their chips for [color=green]VC$" + cashoutAmount + "[/color]\n[sub]Please accept the VC invoice as soon as possible.[/sub]";
                    }
                    else
                    {
                        responseMessage = "Error: Failed to cash out. [sub]Probably a web connection issue.[/sub] Try again later and contact Ambitious Syndra is the issue persists.";
                    }
                    
                }
            }

            bot.SendMessageInChannel(responseMessage, channel);
        }
    }
}
