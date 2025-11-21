using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Processor for the dressup interaction - an involved interaction with 30 minute rate limit
    /// </summary>
    public class DressupProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "dressup";
        public override string InvestmentLevel => "involved";

        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            // First do base validation (profiles exist)
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            // Check that identifier (attire) is provided
            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("attire"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string attire = command.pendingInteraction.identifier;

            // Save the interaction to history
            MonDB.addInteraction(command.pendingInteraction);

            // Get the recipient's profile and set their attire characteristic
            Profile recipientProfile = MonDB.getProfile(recipient);
            recipientProfile.characteristics["attire"] = attire;
            MonDB.setProfile(recipient, recipientProfile);

            // Increment counts with rate limiting (give/take variants)
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, "dressupgive", "dressuptake", RateLimit);

            // Remove pending interaction
            MonDB.removePendingInteraction(command.Id);

            return "dressup";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} has dressed up {recipientProfile.displayName} in {Utils.AttireToText(identifier)}! Do a spin for everyone, let them admire your new garb!";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} wants to dress you up in {Utils.AttireToText(identifier)}. Do you !consent to this makeover?";
        }
    }
}