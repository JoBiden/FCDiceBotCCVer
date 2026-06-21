using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.Casual
{
    /// <summary>
    /// Processor for the boobhat interaction: the initiator rests their chest on the
    /// recipient's head. Directional casual — <c>boobhatgive</c> goes to the one providing
    /// the hat (the chest), <c>boobhattake</c> to the one wearing it. Modeled on
    /// <see cref="SpankProcessor"/> / <see cref="BullyProcessor"/>.
    /// </summary>
    public class BoobhatProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "boobhat";
        public override string InvestmentLevel => "casual";

        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public BoobhatProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public BoobhatProcessor() : base()
        {
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Increment counts with rate limiting (give/take variants).
            // Initiator provides the hat (boobhatgive); recipient wears it (boobhattake).
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, "boobhatgive", "boobhattake", RateLimit);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "boobhat";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            // Owner-provided descriptors.
            var boobhatDescriptors = new List<string>
            {
                "Nice hat!",
                "How heavy are they?",
                "Can you still see?",
                "Soft, warm...",
                "How forward!"
            };

            return $"{initiatorProfile.displayName} lets the full weight of their chest fall onto {recipientProfile.displayName}. {GetRandomDescriptor(boobhatDescriptors)}";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " wants to rest their chest atop " + recipientProfile.displayName + ". Do you !consent to the temporary headwear?";
        }
    }
}
