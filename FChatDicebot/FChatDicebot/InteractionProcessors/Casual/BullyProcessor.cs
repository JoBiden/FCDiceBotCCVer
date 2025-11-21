using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.Casual
{
    /// <summary>
    /// Processor for the bully interaction - a casual interaction with 1 hour rate limit
    /// </summary>
    public class BullyProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "bully";
        public override string InvestmentLevel => "casual";

        private static readonly TimeSpan RateLimit = TimeSpan.FromHours(1);

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Save the interaction to history
            MonDB.addInteraction(command.pendingInteraction);

            // Increment counts with rate limiting (give/take variants)
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, "bullygive", "bullytake", RateLimit);

            // Remove pending interaction
            MonDB.removePendingInteraction(command.Id);

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
    }
}
