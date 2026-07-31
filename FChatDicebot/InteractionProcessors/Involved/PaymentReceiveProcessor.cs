using FChatDicebot.Database;
using FChatDicebot.Model;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Processor for paymentReceive - transfers currency from recipient to initiator
    /// </summary>
    public class PaymentReceiveProcessor : PaymentProcessorBase
    {
        public override string InteractionType => "paymentReceive";

        protected override bool IsGive => false;
        protected override string InsufficientFundsSuffix => "to cover that bill.";

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

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            if (BottlePayment.IsBottlePayment(identifier))
            {
                return $"{initiatorProfile.displayName} accepts the bottles from {recipientProfile.displayName}. Is that a vintage?";
            }
            return $"{recipientProfile.displayName} pays {identifier} to {initiatorProfile.displayName}. Transaction complete!";
        }

        protected override string BuildConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            // identifier is "{amount} {currency}" (e.g. "100 gold") for a currency payment, or the
            // rendered bottle summary from BottlePayment.Describe for a bottle transfer — see
            // ChateauPay.Run / ChateauPay.RunBottlePayment.
            if (BottlePayment.DescribesBottles(identifier))
            {
                return $"{initiatorProfile.displayName} is asking {recipientProfile.displayName} for {identifier}! Do you !consent to handing them over?";
            }
            return $"{initiatorProfile.displayName} is requesting {recipientProfile.displayName} pay them {identifier}! Do you !consent to this transaction?";
        }
    }
}
