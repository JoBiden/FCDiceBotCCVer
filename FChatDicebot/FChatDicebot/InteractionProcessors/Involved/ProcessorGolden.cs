using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Processor for the golden interaction - an involved interaction with 30 minute rate limit
    /// </summary>
    public class GoldenProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "golden";
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

            // Check that identifier (bodypart) is provided
            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("bodypart"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Save the interaction to history
            MonDB.addInteraction(command.pendingInteraction);

            // Increment counts with rate limiting (give/take variants)
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, "goldengive", "goldentake", RateLimit);

            // Remove pending interaction
            MonDB.removePendingInteraction(command.Id);

            return "golden";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} breathes a sigh of relief as a golden fluid pours over {recipientProfile.displayName}'s {Utils.BodypartToText(identifier)}.";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} wants to give {recipientProfile.displayName} a golden shower on their {Utils.BodypartToText(identifier)}. Do you !consent to this?";
        }
    }
}
