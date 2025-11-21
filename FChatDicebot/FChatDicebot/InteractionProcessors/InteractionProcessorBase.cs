using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Base class for interaction processors that provides common functionality.
    /// Inherit from this to reduce boilerplate code.
    /// </summary>
    public abstract class InteractionProcessorBase : IInteractionProcessor
    {
        public abstract string InteractionType { get; }
        public abstract string InvestmentLevel { get; }
        protected string _lastRateLimitMessage = string.Empty;

        public string GetAndClearRateLimitMessage()
        {
            string msg = _lastRateLimitMessage;
            _lastRateLimitMessage = string.Empty;
            return msg;
        }

        /// <summary>
        /// Process the interaction. Override this to implement interaction-specific logic.
        /// </summary>
        public abstract string ProcessInteraction(PendingCommand command);

        /// <summary>
        /// Get the completion message. Override this to customize the message.
        /// </summary>
        public abstract string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier);

        /// <summary>
        /// Basic validation - checks that profiles exist.
        /// Override to add interaction-specific validation.
        /// </summary>
        public virtual ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            Profile initiatorProfile = MonDB.getProfile(initiator);
            Profile recipientProfile = MonDB.getProfile(recipient);

            if (initiatorProfile == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(initiator));
            }

            if (recipientProfile == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(recipient));
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Default consent warning. Override to provide interaction-specific warnings.
        /// </summary>
        public virtual string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} wants to {InteractionType} with {recipientProfile.displayName}. Do you !consent?";
        }

        /// <summary>
        /// Helper method to get a random descriptor from a list
        /// </summary>
        protected string GetRandomDescriptor(List<string> descriptors)
        {
            var random = new Random();
            return descriptors[random.Next(descriptors.Count)];
        }

        /// <summary>
        /// Helper method to increment counts for both participants
        /// </summary>
        protected void IncrementBothCounts(string initiator, string recipient, string countLabel)
        {
            MonDB.incrementCount(initiator, countLabel);
            MonDB.incrementCount(recipient, countLabel);
        }

        /// <summary>
        /// Helper method to increment counts for both participants with rate limiting.
        /// Returns a rate limit message if either participant was rate limited.
        /// </summary>
        protected string IncrementBothCountsWithRateLimit(string initiator, string recipient, string countLabel, TimeSpan rateLimit)
        {
            bool initiatorCounted = MonDB.IncrementCountWithRateLimit(initiator, countLabel, rateLimit);
            bool recipientCounted = MonDB.IncrementCountWithRateLimit(recipient, countLabel, rateLimit);

            return GetRateLimitMessage(initiator, recipient, initiatorCounted, recipientCounted);
        }

        /// <summary>
        /// Helper method to increment different counts for initiator and recipient with rate limiting.
        /// Returns a rate limit message if either participant was rate limited.
        /// </summary>
        protected string IncrementDifferentCountsWithRateLimit(string initiator, string recipient,
            string initiatorLabel, string recipientLabel, TimeSpan rateLimit)
        {
            bool initiatorCounted = MonDB.IncrementCountWithRateLimit(initiator, initiatorLabel, rateLimit);
            bool recipientCounted = MonDB.IncrementCountWithRateLimit(recipient, recipientLabel, rateLimit);

            return GetRateLimitMessage(initiator, recipient, initiatorCounted, recipientCounted);
        }

        /// <summary>
        /// Generate the rate limit message based on who was rate limited
        /// </summary>
        private string GetRateLimitMessage(string initiator, string recipient, bool initiatorCounted, bool recipientCounted)
        {
            Profile initiatorProfile = MonDB.getProfile(initiator);
            Profile recipientProfile = MonDB.getProfile(recipient);

            if (!initiatorCounted && !recipientCounted)
            {
                // Both rate limited
                return $"\n\n[sub]Looks like that didn't make it into either dossier though... The clerks were probably still busy processing their last {InteractionType}s.[/sub]";
            }
            else if (!initiatorCounted)
            {
                // Only initiator rate limited
                return $"\n\n[sub]Looks like that didn't make it into {initiatorProfile.displayName}'s dossier though... The clerks were probably still busy processing their last {InteractionType}.[/sub]";
            }
            else if (!recipientCounted)
            {
                // Only recipient rate limited
                return $"\n\n[sub]Looks like that didn't make it into {recipientProfile.displayName}'s dossier though... The clerks were probably still busy processing their last {InteractionType}.[/sub]";
            }

            // Both counted successfully
            return string.Empty;
        }

        /// <summary>
        /// Helper method to increment different counts for initiator and recipient
        /// </summary>
        protected void IncrementDifferentCounts(string initiator, string recipient, string initiatorLabel, string recipientLabel)
        {
            MonDB.incrementCount(initiator, initiatorLabel);
            MonDB.incrementCount(recipient, recipientLabel);
        }
    }
}
