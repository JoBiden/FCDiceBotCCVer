using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.Casual
{
    /// <summary>
    /// Processor for the cuddle interaction - a casual, unlimited interaction
    /// </summary>
    public class CuddleProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "cuddle";
        public override string InvestmentLevel => "casual";
        //consideration: Make this a variable that mods can set per interaction type?
        private static readonly TimeSpan RateLimit = TimeSpan.FromHours(1);

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public CuddleProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public CuddleProcessor() : base()
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:
                    return "cuddled";
                case VerbTense.Present:
                    return "cuddles";
                case VerbTense.Future:
                    return "will cuddle";
                default:
                    return "cuddle";
            }
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            TimeSpan rateLimit = TimeSpan.FromMinutes(60);

            string rateLimitMessage = IncrementBothCountsWithRateLimit(initiator, recipient, "cuddle", RateLimit);
            Database.DeletePendingCommand(command.Id);

            return "cuddle";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            var cuddleDescriptors = new List<string> 
            { 
                "Cute.", 
                "That's kind of lewd...", 
                "So salatious.", 
                "Hot!", 
                "Looks cozy!", 
                "Is there room for one more?", 
                $"{initiatorProfile.displayName} is definitely the big spoon.", 
                $"{recipientProfile.displayName} is definitely the little spoon." 
            };

            string message = $"{initiatorProfile.displayName} and {recipientProfile.displayName} cuddle up together. {GetRandomDescriptor(cuddleDescriptors)}";

            // Special handling for Queen Contract and The Corrupted Rin
            if (initiatorProfile.userName == "Queen Contract" || recipientProfile.userName == "Queen Contract")
            {
                if (initiatorProfile.userName == "The Corrupted Rin" || recipientProfile.userName == "The Corrupted Rin")
                {
                    message += " [eicon]rin_lap[/eicon]";
                }
                else
                {
                    message += " [eicon]qchug[/eicon]";
                }
            }

            return message;
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} wants to cuddle with {recipientProfile.displayName}! Do you !consent?";
        }
    }
}
