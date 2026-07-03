using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Processor for paymentReceive - transfers currency from recipient to initiator
    /// </summary>
    public class PaymentReceiveProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "paymentReceive";
        public override string InvestmentLevel => "involved";

        public PaymentReceiveProcessor(IChateauDatabase database) : base(database)
        {
        }

        public PaymentReceiveProcessor() : base()
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:
                    return "billed";
                case VerbTense.Present:
                    return "bills";
                case VerbTense.Future:
                    return "will bill";
                default:
                    return "bill";
            }
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

            // paymentReceive: initiator is billing recipient, i.e. recipient pays initiator.
            // Same atomic guarded-debit-then-credit as PaymentGiveProcessor, just with payer/payee
            // swapped, so it can never mint currency on self-target and never overdraws a
            // currency that isn't ious/nothing.
            bool debited = Database.TryDebitCurrency(recipient, currency, magnitude, CurrencyRules.AllowsNegative(currency));
            if (!debited)
            {
                _lastInitiatorPrivateMessage = recipient + " no longer has enough " + currency + " to cover that bill.";
                Database.DeletePendingCommand(command.Id);
                return "NoInteraction";
            }

            Database.ChangeCurrency(initiator, currency, magnitude);

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "paymentReceive";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{recipientProfile.displayName} pays {identifier} to {initiatorProfile.displayName}. Transaction complete!";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} wants to receive {identifier} from you. Do you !consent to making this payment?";
        }
    }
}
