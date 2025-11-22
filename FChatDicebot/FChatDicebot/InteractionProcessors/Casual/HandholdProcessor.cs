using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.Casual
{
    public class HandholdProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "handhold";
        public override string InvestmentLevel => "casual";
        //consideration: Make this a variable that mods can set per interaction type?
        private static readonly TimeSpan RateLimit = TimeSpan.FromHours(1);

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public HandholdProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public HandholdProcessor() : base()
        {
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string rateLimitMessage = IncrementBothCountsWithRateLimit(initiator, recipient, "handhold", RateLimit);
            Database.DeletePendingCommand(command.Id);

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
