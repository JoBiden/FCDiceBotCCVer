using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings chan = bot.GetChannelSettings(address);
            string responseMessage = "";
            if(!chan.AllowChips)
            {
                responseMessage = "Failed: " + BotMain.CurrencyPlaceholder + "s are not allowed under the settings for this channel.";
            }
            else if (address.GetChannelKey().ToLower() != BotMain.CasinoChannelId)
            {
                responseMessage = "Failed: Only " + BotMain.CurrencyPlaceholder + " piles in Velvetcuff Casino channel are eligible for the !cashout command.";
            }
            else
            {
                ChipPile pile = bot.DiceBot.GetChipPile(address);

                bool timerFinished = bot.DiceBot.CountdownFinishedOrNotStarted(address, address.character + DiceBot.PlayerCashoutSuffix);

                if(pile == null)
                {
                    responseMessage = "Error: " + BotMain.CurrencyPlaceholder + "s pile not found for " + TextFormat.GetCharacterUserTags(address.character);
                }
                else if(pile.Chips < DiceBot.MinimumChipCashoutSize)
                {
                    responseMessage = "Failed: You do not have enough " + BotMain.CurrencyPlaceholder + "s to cash out, " + TextFormat.GetCharacterUserTags(address.character) + ". (held: " + pile.Chips + ") (minimum: " + DiceBot.MinimumChipCashoutSize + ")";
                }
                else if(!timerFinished)
                {
                    CountdownTimer redeemTimer = bot.DiceBot.GetCountdownTimer(address, address.character + DiceBot.PlayerCashoutSuffix);

                    responseMessage = "Failed: You must wait longer cash out again, " + TextFormat.GetCharacterUserTags(address.character) + ". (remaining: " + redeemTimer.GetMinutesRemaining().ToString("F0") + " minutes)";
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

                    bot.SendMessageInChannel("Starting cash out from " + BotMain.CurrencyPlaceholder + "s to VC$...", address);

                    string invoiceMessage = "Cashing out with " + cashoutAmount + " " + BotMain.CurrencyPlaceholder + "s from the Casino.";
                    invoiceMessage = TextFormat.SubstituteInCurrencyName(invoiceMessage, chan);
                    bool success = bot.VelvetcuffConnection.CreateNewVcTransaction(cashoutAmount, address.character, DiceBot.DiceBotCharacter, true, invoiceMessage);

                    if(success)
                    {
                        string giveChips = bot.DiceBot.GiveChips(address, DiceBot.HousePlayerAlias, cashoutAmount, cashoutAmount == pile.Chips);

                        bot.BotCommandController.SaveChipsToDisk("Cashout");
                        bot.DiceBot.StartCountdownTimer(address, address.character + DiceBot.PlayerCashoutSuffix, DiceBot.CashoutCooldownMs);

                        responseMessage = giveChips + "\n" + TextFormat.GetCharacterUserTags(address.character) + " has cashed out their " + BotMain.CurrencyPlaceholder + "s for [color=green]VC$" + cashoutAmount + "[/color]\n[sub]Please accept the VC invoice as soon as possible.[/sub]";
                    }
                    else
                    {
                        responseMessage = "Error: Failed to cash out. [sub]Probably a web connection issue.[/sub] Try again later and contact Ambitious Syndra is the issue persists.";
                    }

                }
            }

            bot.SendMessageInChannel(responseMessage, address);
        }
    }
}
