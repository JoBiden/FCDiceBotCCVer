using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.Casual
{
    /// <summary>
    /// Processor for the lick interaction: the initiator licks the recipient. Directional
    /// casual — <c>lickgive</c> goes to the licker, <c>licktake</c> to the licked. Modeled
    /// on <see cref="SpankProcessor"/> / <see cref="BullyProcessor"/>.
    /// </summary>
    public class LickProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "lick";
        public override string InvestmentLevel => "casual";

        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public LickProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public LickProcessor() : base()
        {
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Increment counts with rate limiting (give/take variants).
            // Initiator is the licker (lickgive); recipient is the licked (licktake).
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, "lickgive", "licktake", RateLimit);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "lick";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            // Owner-provided descriptors. {lickgiver} = the licker (initiator); {licktaker} =
            // the licked (recipient). Tokens are resolved against display names here.
            var lickDescriptors = new List<string>
            {
                "How many more licks to get to the center?",
                "I bet they taste good.",
                "In cat culture, that means {lickgiver} is in charge.",
                "In bunny culture, that means {licktaker} is in charge.",
                "Is this what it means to be groomed?",
                "Mlem!"
            };

            string descriptor = GetRandomDescriptor(lickDescriptors)
                .Replace("{lickgiver}", initiatorProfile.displayName)
                .Replace("{licktaker}", recipientProfile.displayName);

            return $"{initiatorProfile.displayName} gives {recipientProfile.displayName} a slow lick. {descriptor}";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " wants to give " + recipientProfile.displayName + " a lick. Do you !consent to being lapped at?";
        }
    }
}
