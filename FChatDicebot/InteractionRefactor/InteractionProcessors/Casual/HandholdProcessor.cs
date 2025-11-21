using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors
{
    public class HandholdProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "handhold";
        public override string InvestmentLevel => "casual";

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            IncrementBothCounts(initiator, recipient, "handhold");
            MonDB.removePendingInteraction(command.Id);

            return "handhold";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            var handholdDescriptors = new List<string> 
            { 
                "Cute.", 
                "That's kind of lewd...", 
                "So salatious.", 
                "Hot!", 
                "When's the wedding?", 
                "The forbidden act, out in the open..."
            };

            return $"Ooh, {initiatorProfile.displayName} and {recipientProfile.displayName} hold hands! {GetRandomDescriptor(handholdDescriptors)}";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} wants to hold hands with {recipientProfile.displayName}! Do you !consent?";
        }
    }
}
