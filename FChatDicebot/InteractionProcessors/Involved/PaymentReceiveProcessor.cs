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
            return $"{recipientProfile.displayName} pays {identifier} to {initiatorProfile.displayName}. Transaction complete!";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            // identifier is "{amount} {currency}" (e.g. "100 gold") — see ChateauPay.Run.
            return $"{initiatorProfile.displayName} is requesting {recipientProfile.displayName} pay them {identifier}! Do you !consent to this transaction?";
        }
    }
}
