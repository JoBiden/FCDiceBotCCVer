using FChatDicebot.Database;
using FChatDicebot.Model;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Processor for paymentGive - transfers currency from initiator to recipient
    /// </summary>
    public class PaymentGiveProcessor : PaymentProcessorBase
    {
        public override string InteractionType => "paymentGive";

        protected override bool IsGive => true;
        protected override string InsufficientFundsSuffix => "for that transfer.";

        public PaymentGiveProcessor(IChateauDatabase database) : base(database)
        {
        }

        public PaymentGiveProcessor() : base()
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

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            if (BottlePayment.IsBottlePayment(identifier))
            {
                return $"{initiatorProfile.displayName} hands the bottles over to {recipientProfile.displayName}. Is that a vintage?";
            }
            return $"{initiatorProfile.displayName} hands over some {identifier} to {recipientProfile.displayName}. Transaction complete!";
        }

        protected override string BuildConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            // identifier is "{amount} {currency}" (e.g. "100 gold") for a currency payment, or the
            // rendered bottle summary from BottlePayment.Describe for a bottle transfer — see
            // ChateauPay.Run / ChateauPay.RunBottlePayment.
            if (BottlePayment.DescribesBottles(identifier))
            {
                return $"{initiatorProfile.displayName} is going to pass {recipientProfile.displayName} {identifier}! Do you !consent to receiving them?";
            }
            return $"{initiatorProfile.displayName} is going to pay {recipientProfile.displayName} {identifier}! Do you !consent to this transaction?";
        }
    }
}
