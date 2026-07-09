using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Casual
{
    public class HandholdProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "handhold";
        public override string InvestmentLevel => "casual";
        //consideration: Make this a variable that mods can set per interaction type?
        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        // Symmetric group model: every participant gets +(M-1) "handhold".
        public override GroupSpec GroupSpec => GroupSpec.Symmetric("handhold");

        // Mutual interaction: both parties' custom !seteicon eicons show on the completion.
        public override bool EiconAppliesToBothParties => true;

        private static readonly List<string> HandholdDescriptors = new List<string>
        {
            "Cute.",
            "That's kind of lewd...",
            "So salatious.",
            "Hot!",
            "When's the wedding?",
            "The forbidden act, out in the open..."
        };

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

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:
                    return "held hands";
                case VerbTense.Present:
                    return "holds hands";
                case VerbTense.Future:
                    return "will hold hands";
                default:
                    return "hold hands";
            }
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            _lastRateLimitMessage = IncrementBothCountsWithRateLimit(initiator, recipient, "handhold", RateLimit);
            Database.DeletePendingCommand(command.Id);

            return "handhold";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"Ooh, {initiatorProfile.displayName} and {recipientProfile.displayName} hold hands! {GetRandomDescriptor(HandholdDescriptors)}";
        }

        public override string GetGroupCompletionMessage(Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder, string identifier)
        {
            if (consentersInOrder.Count == 1)
                return GetCompletionMessage(initiatorProfile, consentersInOrder[0], identifier);

            var names = new List<string> { initiatorProfile.displayName };
            names.AddRange(consentersInOrder.Select(p => p.displayName));
            return $"Ooh, {JoinNamesSerial(names)} hold hands in a chain! {GetRandomDescriptor(HandholdDescriptors)}";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " wants to hold your hand (or equivalent appendage) " + recipientProfile.displayName + ". Do you !consent to some handholding?";
        }
    }
}
