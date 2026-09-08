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
            ShortDescription = "Transfer currency or collected items to/from another resident.";
            LongDescription = "Pay an amount of currency to another character (defaulting to 1 if no amount is specified). Pay a negative amount to instead 'bill' someone else and ask them to pay you. Currency isn't inherently valuable in the Chateau where everyone is cared for one way or another, but people still like to exchange it for goods and services. You can also pass along things from your collection by naming the kind of item: bottles, full or empty, or panties. Name them by number, or give a kind and an amount. Every item keeps its number and the resident it came from when it changes hands.";
            Usage = "!pay [noparse][user]NameInUserTag[/user][/noparse] {amount} {currency}\nor\n!pay {currency}\nor\n!pay [noparse][user]NameInUserTag[/user][/noparse] {amount} bottles {substance}\nor\n!pay [noparse][user]NameInUserTag[/user][/noparse] bottles #12 #13\nor\n!pay [noparse][user]NameInUserTag[/user][/noparse] panties #43";
            RelatedCommands = new string[] { "bank", "work", "volunteer", "collection" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = "currency";
            // "bottles" / "panties" are argument words. Only matters for a bare-name call
            // ("!pay bob panties #43") — an explicit [user] tag short-circuits the resolver.
            ArgumentKeywords = CollectionSections.AllKeywords();
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
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
            else if (string.Equals(recipient, characterName, StringComparison.OrdinalIgnoreCase))
            {
                bot.SendPrivateMessage("You can't pay yourself. You can !volunteer and even !employ yourself or others, then !work for money.", characterName);
                valid = false;
            }
            // Collectibles are not a currency bucket, so they branch off before the
            // currency-identifier lookup that would reject the keyword. Checked after the
            // not-found/self-pay guards so those messages stay the same for both kinds of payment.
            else if (CollectiblePayment.SectionFor(rawTerms) != null)
            {
                RunCollectiblePayment(bot, commandController, rawTerms, address, characterName, recipient, initiatorProfile, recipientProfile, channel);
                return;
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
                    int billMagnitude = -paymentAmount;
                    if (!recipientProfile.currencies.ContainsKey(currency) || recipientProfile.currencies[currency] < billMagnitude)
                    {
                        if (currency != "nothing")
                        {
                            bot.SendPrivateMessage("They don't have enough " + currency + " to make that payment.", characterName);
                            valid = false;
                        }
                        else if (billMagnitude > 100) //&& currency is "nothing" implied by logic
                        {
                            bot.SendPrivateMessage("You can't ask for 'nothing' amounts over 100! Just think of the inflation...", characterName);
                            valid = false;
                        }
                    }
                }
                if (valid)
                {
                    //payment amount is guaranteed to be non zero here
                    // Delegate consent wording to the processor so it stays in one place
                    // (previously hand-rolled here and had drifted from PaymentGiveProcessor/
                    // PaymentReceiveProcessor's own GetConsentWarning). GetConsentWarning's
                    // identifier slot only carries one string, so fold the magnitude into it
                    // ("100 gold") rather than dropping the amount from the announcement.
                    var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor(
                        paymentAmount > 0 ? "paymentGive" : "paymentReceive");
                    string amountAndCurrency = Math.Abs(paymentAmount) + " " + currency;
                    string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, amountAndCurrency);

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

        /// <summary>
        /// Goods-transfer variant of a payment. Resolves the request down to concrete serial
        /// numbers up front and stores those on the pending command, so the consent-time recheck
        /// asks an exact question ("does the payer still hold #142?") rather than re-running a
        /// filter that could quietly select different items than the recipient agreed to.
        ///
        /// The typed keyword picks the type, and the whole parcel is that one type: the stored
        /// identifier holds a single token and the completion message names the goods from it.
        /// </summary>
        private void RunCollectiblePayment(
            BotMain bot, BotCommandController commandController, string[] rawTerms, MessageAddress address,
            string characterName, string recipient, Profile initiatorProfile, Profile recipientProfile, string channel)
        {
            CollectionSection section = CollectiblePayment.SectionFor(rawTerms);
            string substanceFilter = commandController.GetIdentifierFromCommandTerms(rawTerms, "substance");
            List<int> requestedSerials = CollectiblePayment.ParseSerials(rawTerms);

            int amount = 1;
            string[] rawInts = commandController.GetIntsFromCommandTermsAsStrings(rawTerms);
            if (rawInts != null && rawInts.Length > 0)
            {
                if (!int.TryParse(rawInts[0], out amount))
                {
                    bot.SendPrivateMessage("The payment amount must be a valid whole number.", characterName);
                    return;
                }
            }
            else if (requestedSerials.Count > 0)
            {
                // Naming items by number states the count implicitly.
                amount = requestedSerials.Count;
            }

            if (amount == 0)
            {
                bot.SendPrivateMessage("We won't waste our time with empty transfers. Name at least one item!", characterName);
                return;
            }

            bool isGive = amount > 0;
            Profile payerProfile = isGive ? initiatorProfile : recipientProfile;

            var selection = CollectiblePayment.Select(payerProfile, section, requestedSerials, substanceFilter, Math.Abs(amount));
            if (!selection.IsValid)
            {
                bot.SendPrivateMessage(
                    isGive ? selection.PayerFacingError : selection.ThirdPartyError(recipientProfile.displayName),
                    characterName);
                return;
            }

            var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor(
                isGive ? "paymentGive" : "paymentReceive");

            string describedGoods = CollectiblePayment.Describe(MonDB.GetDatabase(), section, selection.Items);
            string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, describedGoods);

            Interaction payInteraction = new Interaction();
            payInteraction.initiator = characterName;
            payInteraction.recipient = recipient;
            // The type token, not a generic marker: the completion messages read it back, and
            // every payment completed before panties existed carries "bottles" and still resolves.
            payInteraction.identifier = section.FilterToken;
            payInteraction.type = isGive ? "paymentGive" : "paymentReceive";
            payInteraction.investmentLevel = "involved";
            payInteraction.interactionTime = DateTime.UtcNow;
            payInteraction.extraParameters = new MongoDB.Bson.BsonArray { amount };
            // The exact items promised, each carrying whether it was spent at promise time.
            // Everything after this point works from these, not from the filter that found them.
            foreach (var item in selection.Items)
            {
                payInteraction.extraParameters.Add(CollectiblePayment.EncodePromise(item));
            }

            PendingCommand pendingPay = new PendingCommand();
            pendingPay.pendingInteraction = payInteraction;
            pendingPay.awaitingConsentFrom = recipient;

            MonDB.addPendingCommand(pendingPay);

            bot.SendMessageInChannel(message, channel);
        }
    }
}
