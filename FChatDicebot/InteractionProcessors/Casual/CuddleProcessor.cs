using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Casual
{
    /// <summary>
    /// Processor for the cuddle interaction
    /// </summary>
    public class CuddleProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "cuddle";
        public override string InvestmentLevel => "casual";
        //consideration: Make this a variable that mods can set per interaction type?
        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        // Symmetric group model: every participant gets +(M-1) "cuddle".
        public override GroupSpec GroupSpec => GroupSpec.Symmetric("cuddle");

        // Mutual interaction: both cuddlers' custom !seteicon eicons show on the completion.
        public override bool EiconAppliesToBothParties => true;

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

            _lastRateLimitMessage = IncrementBothCountsWithRateLimit(initiator, recipient, "cuddle", RateLimit);
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

        public override string GetGroupCompletionMessage(Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder, string identifier)
        {
            if (consentersInOrder.Count == 1)
                return GetCompletionMessage(initiatorProfile, consentersInOrder[0], identifier);

            // The big/little-spoon descriptors don't make sense for a pile, so the group
            // pool drops them and adds a couple of cuddle-puddle-flavored lines instead.
            var groupDescriptors = new List<string>
            {
                "Cute.",
                "That's kind of lewd...",
                "So salatious.",
                "Hot!",
                "Looks cozy!",
                "Is there room for one more?",
                "A proper cuddle puddle!",
                "Everyone's invited."
            };

            string message = BuildSymmetricGroupMessage(
                initiatorProfile, consentersInOrder, "cuddle up together", GetRandomDescriptor(groupDescriptors));

            bool anyQueen = initiatorProfile.userName == "Queen Contract"
                || consentersInOrder.Any(p => p.userName == "Queen Contract");
            bool anyRin = initiatorProfile.userName == "The Corrupted Rin"
                || consentersInOrder.Any(p => p.userName == "The Corrupted Rin");
            if (anyQueen)
            {
                message += anyRin ? " [eicon]rin_lap[/eicon]" : " [eicon]qchug[/eicon]";
            }

            return message;
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " wants to cuddle you, " + recipientProfile.displayName + ". Do you !consent to some cuddles?";
        }
    }
}
