using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

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

        // Directional group model: initiator +R bullygive, each recipient +1 bullytake.
        public override GroupSpec GroupSpec => GroupSpec.Directional("bullygive", "bullytake");

        private static readonly List<string> BullyDescriptors = new List<string>
        {
            "boolies them into submission!",
            "shows them whose boss!",
            "applies excessive force to their victim!",
            "spins them right round!",
            "establishes the pecking order!",
            "instills fear..."
        };

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
            return $"{initiatorProfile.displayName} takes {recipientProfile.displayName} by the collar and {GetRandomDescriptor(BullyDescriptors)}";
        }

        public override string GetGroupCompletionMessage(Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder, string identifier)
        {
            if (consentersInOrder.Count == 1)
                return GetCompletionMessage(initiatorProfile, consentersInOrder[0], identifier);

            string names = JoinNamesSerial(consentersInOrder.Select(p => p.displayName).ToList());
            return $"{initiatorProfile.displayName} rounds up {names} and {GetRandomDescriptor(BullyDescriptors)}";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " is gearing up to bully " + recipientProfile.displayName + ". Do you !consent to whatever is coming?";
        }
    }
}
