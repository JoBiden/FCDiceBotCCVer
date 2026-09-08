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
            // The identifier is a collectible type token ("bottles", "panties") for a goods
            // transfer; the type owns both the noun and the closing flavor, so "Is that a
            // vintage?" stays a joke about bottles.
            CollectionSection section = CollectionSections.ByKeyword(identifier);
            if (section != null)
            {
                return $"{initiatorProfile.displayName} hands the {section.TransferNoun} over to {recipientProfile.displayName}. {section.TransferGiveFlavor}";
            }
            return $"{initiatorProfile.displayName} hands over some {identifier} to {recipientProfile.displayName}. Transaction complete!";
        }

        protected override string BuildConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            // identifier is "{amount} {currency}" (e.g. "100 gold") for a currency payment, or the
            // rendered parcel summary from CollectionSection.DescribeParcel for a goods transfer —
            // see ChateauPay.Run / ChateauPay.RunCollectiblePayment.
            if (CollectiblePayment.DescribesCollectibles(identifier))
            {
                return $"{initiatorProfile.displayName} is going to pass {recipientProfile.displayName} {identifier}! Do you !consent to receiving them?";
            }
            return $"{initiatorProfile.displayName} is going to pay {recipientProfile.displayName} {identifier}! Do you !consent to this transaction?";
        }
    }
}
