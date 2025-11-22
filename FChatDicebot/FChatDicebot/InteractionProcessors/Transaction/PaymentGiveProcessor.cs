using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Transaction
{
    /// <summary>
    /// Processor for paymentGive - transfers currency from initiator to recipient
    /// </summary>
    public class PaymentGiveProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "paymentGive";
        public override string InvestmentLevel => "transaction";

        public PaymentGiveProcessor(IChateauDatabase database) : base(database)
        {
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

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Get both profiles
            Profile initiatorProfile = Database.GetProfile(initiator);
            Profile recipientProfile = Database.GetProfile(recipient);

            // Ensure both have currency field
            if (!initiatorProfile.currencies.ContainsKey(currency))
            {
                initiatorProfile.currencies[currency] = 0;
            }
            if (!recipientProfile.currencies.ContainsKey(currency))
            {
                recipientProfile.currencies[currency] = 0;
            }

            // Transfer: subtract from initiator, add to recipient
            initiatorProfile.currencies[currency] -= amount;
            recipientProfile.currencies[currency] += amount;

            // Save both profiles
            Database.SetProfile(initiator, initiatorProfile);
            Database.SetProfile(recipient, recipientProfile);

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
            return $"{initiatorProfile.displayName} wants to give you some {identifier}. Do you !consent to receive this payment?";
        }
    }
}
