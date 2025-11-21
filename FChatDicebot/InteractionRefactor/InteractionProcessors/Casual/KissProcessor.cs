using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Processor for the kiss interaction - a casual, unlimited interaction
    /// </summary>
    public class KissProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "kiss";
        public override string InvestmentLevel => "casual";

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Increment counts for both participants
            IncrementBothCounts(initiator, recipient, "kiss");

            // Remove the pending interaction
            MonDB.removePendingInteraction(command.Id);

            return "kiss";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            var random = new Random();
            var kissDescriptors = new List<string> 
            { 
                "cute.", 
                "that's kind of lewd...", 
                "so salatious.", 
                "hot!", 
                "it sounded quite wet.", 
                "short and sweet.", 
                "slow and sensual.", 
                "just a casual peck.", 
                "doki..." 
            };

            string message = $"Mwah! {initiatorProfile.displayName} and {recipientProfile.displayName} share a kiss, {GetRandomDescriptor(kissDescriptors)}";

            // Special handling for Queen Contract (keeping your existing special case)
            if (initiatorProfile.userName == "Queen Contract")
            {
                message += "[eicon]qckiss[eicon]";
            }

            return message;
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} wants to give {recipientProfile.displayName} a smooch! Do you !consent?";
        }
    }
}
