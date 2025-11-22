using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.Casual
{
    /// <summary>
    /// Processor for the spank interaction - a casual interaction with 1 hour rate limit
    /// </summary>
    public class SpankProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "spank";
        public override string InvestmentLevel => "casual";

        private static readonly TimeSpan RateLimit = TimeSpan.FromHours(1);

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public SpankProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public SpankProcessor() : base()
        {
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Increment counts with rate limiting (give/take variants)
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, "spankgive", "spanktake", RateLimit);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "spank";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            var random = new Random();
            var spankDescriptors = new List<string>
            {
                "a sharp spank!",
                "a love tap to the ass.",
                "a smack that will leave a mark.",
                "a red imprint on their derriere.",
                "a surprisingly loving booty grope.",
                "an impact with enough force to cook a lesser being."
            };

            string message = $"{initiatorProfile.displayName} winds up and gives {recipientProfile.displayName} {GetRandomDescriptor(spankDescriptors)}";

            if (recipientProfile.userName == "Queen Contract")
            {
                message += " [eicon]qcass[/eicon]";
            }

            return message;
        }
    }
}