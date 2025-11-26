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
using System.ComponentModel;

namespace FChatDicebot.BotCommands
{
    public class ChateauPay : ChatBotCommand
    {
        public ChateauPay()
        {
            Name = "pay";
            Aliases = new string[] { };
            Category = "Involved Interaction";
            ShortDescription = "Pay currency to another character";
            LongDescription = "Pay a specified amount of currency to another character. This transfers currency from your bank account to theirs. Both parties must have sufficient currency and consent.";
            Usage = "!pay [user]CharacterName[/user] [amount] [currency]";
            RelatedCommands = new string[] { "bank", "work", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string identifierType = "currency";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string currency = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            string[] rawInts = commandController.GetIntsFromCommandTermsAsStrings(rawTerms);
            int paymentAmount = 1; //this is the default if no int was found in the terms
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (currency == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                valid = false;
            }
            else //check that the payer has enough of the currency to pay
            {
                if (rawInts != null && rawInts.Length > 0)
                {
                    try
                    {
                        paymentAmount = Convert.ToInt32(rawInts[0]);
                    }
                    catch (Exception)
                    {
                        bot.SendPrivateMessage("The payment amount must be a valid whole number.", characterName);
                        valid = false;
                    }
                }
                if (paymentAmount == 0)
                {
                    bot.SendPrivateMessage("We won't waste our time with empty transfers. If you want to transfer nothing, at least specify a non zero amount of nothing!", characterName);
                    valid = false;
                }
                else if (paymentAmount > 0) //normal payment
                {
                    if (!initiatorProfile.currencies.ContainsKey(currency) || initiatorProfile.currencies[currency] < paymentAmount)
                    {
                        if (currency != "nothing")
                        {
                            bot.SendPrivateMessage("You don't have enough " + currency + " to make that payment.", characterName);
                            valid = false;
                        }
                        else if (paymentAmount > 100) //&& currency is "nothing" implied by logic
                        {
                            bot.SendPrivateMessage("You can't pay 'nothing' amounts over 100! Just think of the inflation...", characterName);
                            valid = false;
                        }
                    }
                }
                else //negative payment, requesting funds
                {
                    if (!recipientProfile.currencies.ContainsKey(currency) || recipientProfile.currencies[currency] < paymentAmount)
                    {
                        if (currency != "nothing")
                        {
                            bot.SendPrivateMessage("They don't have enough " + currency + " to make that payment.", characterName);
                            valid = false;
                        }
                        else if (paymentAmount > 100) //&& currency is "nothing" implied by logic
                        {
                            bot.SendPrivateMessage("You can't ask for 'nothing' amounts over 100! Just think of the inflation...", characterName);
                            valid = false;
                        }
                    }
                }
                if (valid)
                {
                    //payment amount is guaranteed to be non zero here
                    string message = paymentAmount > 0 ? initiatorProfile.displayName + " is going to pay " + recipientProfile.displayName + " " + paymentAmount + " " + currency + "! Do you !consent to this transaction?" :
                        initiatorProfile.displayName + " is requesting " + recipientProfile.displayName + " pay them " + -paymentAmount + " " + currency + "! Do you !consent to this transaction?";

                    Interaction payInteraction = new Interaction();
                    payInteraction.initiator = characterName;
                    payInteraction.recipient = recipient;
                    payInteraction.identifier = currency;
                    payInteraction.type = paymentAmount > 0 ? "paymentGive" : "paymentReceive";
                    payInteraction.investmentLevel = "involved";
                    payInteraction.interactionTime = DateTime.UtcNow;
                    payInteraction.extraParameters = new MongoDB.Bson.BsonArray
                    {
                        paymentAmount
                    };

                    PendingCommand pendingPay = new PendingCommand();
                    pendingPay.pendingInteraction = payInteraction;
                    pendingPay.awaitingConsentFrom = recipient;

                    MonDB.addPendingCommand(pendingPay);

                    bot.SendMessageInChannel(message, channel);
                }
            }
        }
    }
}
