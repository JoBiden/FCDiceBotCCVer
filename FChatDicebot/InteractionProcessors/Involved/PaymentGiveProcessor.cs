using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Processor for paymentGive - transfers currency from initiator to recipient
    /// </summary>
    public class PaymentGiveProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "paymentGive";
        public override string InvestmentLevel => "involved";

        public PaymentGiveProcessor(IChateauDatabase database) : base(database)
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:
                    return "paid";
                case VerbTense.Present:
                    return "pays";
                case VerbTense.Future:
                    return "will pay";
                default:
                    return "pay";
            }
        }
        public PaymentGiveProcessor() : base()
        {
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("currency"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string currency = command.pendingInteraction.identifier;
            int amount = command.pendingInteraction.extraParameters.FirstOrDefault().ToInt32();
            int magnitude = Math.Abs(amount);

            // paymentGive: initiator pays recipient. Debit is atomic and guarded (>= magnitude
            // unless the currency allows negative balances), so this can never mint currency on
            // self-pay (initiator == recipient nets the same $inc twice back to zero) and never
            // overdraws a currency that isn't ious/nothing even if a second queued payment races
            // this one to consent.
            bool debited = Database.TryDebitCurrency(initiator, currency, magnitude, CurrencyRules.AllowsNegative(currency));
            if (!debited)
            {
                _lastInitiatorPrivateMessage = initiator + " no longer has enough " + currency + " for that transfer.";
                Database.DeletePendingCommand(command.Id);
                return "NoInteraction";
            }

            Database.ChangeCurrency(recipient, currency, magnitude);

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "paymentGive";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} hands over some {identifier} to {recipientProfile.displayName}. Transaction complete!";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} wants to give you some {identifier}. Do you !consent to receiving this payment?";
        }
    }
}
