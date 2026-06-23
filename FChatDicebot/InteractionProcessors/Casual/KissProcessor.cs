using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Casual
{
    /// <summary>
    /// Processor for the kiss interaction - a casual, unlimited interaction
    /// Refactored to support dependency injection for testability.
    /// </summary>
    public class KissProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "kiss";
        public override string InvestmentLevel => "casual";
        //consideration: Make this a variable that mods can set per interaction type?
        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        // Symmetric group model: every participant gets +(M-1) "kiss".
        public override GroupSpec GroupSpec => GroupSpec.Symmetric("kiss");

        private static readonly List<string> KissDescriptors = new List<string>
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

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public KissProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public KissProcessor() : base()
        {
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Increment counts for both participants
            IncrementBothCountsWithRateLimit(initiator, recipient, "kiss", RateLimit);

            // Remove the pending interaction
            Database.DeletePendingCommand(command.Id);

            return "kiss";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string message = $"Mwah! {initiatorProfile.displayName} and {recipientProfile.displayName} share a kiss, {GetRandomDescriptor(KissDescriptors)}";

            // Special handling for Queen Contract (keeping your existing special case)
            if (initiatorProfile.userName == "Queen Contract")
            {
                message += "[eicon]qckiss[/eicon]";
            }

            return message;
        }

        public override string GetGroupCompletionMessage(Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder, string identifier)
        {
            if (consentersInOrder.Count == 1)
                return GetCompletionMessage(initiatorProfile, consentersInOrder[0], identifier);

            var names = new List<string> { initiatorProfile.displayName };
            names.AddRange(consentersInOrder.Select(p => p.displayName));
            string message = $"Mwah! {JoinNamesSerial(names)} share a flurry of kisses, {GetRandomDescriptor(KissDescriptors)}";

            if (initiatorProfile.userName == "Queen Contract")
            {
                message += "[eicon]qckiss[/eicon]";
            }

            return message;
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " wants to give you a kiss, " + recipientProfile.displayName + ". Do you !consent to a smooch?";
        }
    }
}