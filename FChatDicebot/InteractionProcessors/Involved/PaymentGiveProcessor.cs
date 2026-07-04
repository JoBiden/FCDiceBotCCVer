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
            return $"{initiatorProfile.displayName} hands over some {identifier} to {recipientProfile.displayName}. Transaction complete!";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            // identifier is "{amount} {currency}" (e.g. "100 gold") — see ChateauPay.Run.
            return $"{initiatorProfile.displayName} is going to pay {recipientProfile.displayName} {identifier}! Do you !consent to this transaction?";
        }
    }
}
