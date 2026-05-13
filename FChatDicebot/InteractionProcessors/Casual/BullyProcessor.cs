using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.Casual
{
    /// <summary>
    /// Processor for the bully interaction
    /// </summary>
    public class BullyProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "bully";
        public override string InvestmentLevel => "casual";

        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public BullyProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public BullyProcessor() : base()
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:
                    return "bullied";
                case VerbTense.Present:
                    return "bullies";
                case VerbTense.Future:
                    return "will bully";
                default:
                    return "bully";
            }
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Increment counts with rate limiting (give/take variants)
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, "bullygive", "bullytake", RateLimit);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "bully";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            var random = new Random();
            var bullyDescriptors = new List<string>
            {
                "boolies them into submission!",
                "shows them whose boss!",
                "applies excessive force to their victim!",
                "spins them right round!",
                "establishes the pecking order!",
                "instills fear..."
            };

            return $"{initiatorProfile.displayName} takes {recipientProfile.displayName} by the collar and {GetRandomDescriptor(bullyDescriptors)}";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " is gearing up to bully " + recipientProfile.displayName + ". Do you !consent to whatever is coming?";
        }
    }
}
