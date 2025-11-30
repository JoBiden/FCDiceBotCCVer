using FChatDicebot.Database;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Processor for the feed interaction - an involved interaction with 30 minute rate limit
    /// </summary>
    public class FeedProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "feed";
        public override string InvestmentLevel => "involved";

        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public FeedProcessor(IChateauDatabase database) : base(database)
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:
                    return "fed";
                case VerbTense.Present:
                    return "feeds";
                case VerbTense.Future:
                    return "will feed";
                default:
                    return "feed";
            }
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public FeedProcessor() : base()
        {
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            // First do base validation (profiles exist)
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            // Check that identifier (substance) is provided
            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("substance"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Increment counts with rate limiting (give/take variants)
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, "feedgive", "feedtake", RateLimit);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "feed";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} has fed {recipientProfile.displayName} some {Utils.SubstanceToText(identifier)}! Was it yummy? I bet it was.";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " is going to feed " + recipientProfile.displayName + " some " + Utils.SubstanceToText(identifier) + "! Do you !consent to consuming that?";
        }
    }
}