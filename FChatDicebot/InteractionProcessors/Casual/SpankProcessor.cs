using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Casual
{
    /// <summary>
    /// Processor for the spank interaction
    /// </summary>
    public class SpankProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "spank";
        public override string InvestmentLevel => "casual";

        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        // Directional group model: initiator +R spankgive, each recipient +1 spanktake.
        public override GroupSpec GroupSpec => GroupSpec.Directional("spankgive", "spanktake");

        // The spanked party's rear is what takes the hit, so their ass eicon shows. This is
        // the general form of what used to be a hardcoded Queen Contract easter egg — she
        // now carries [eicon]qcass[/eicon] as her own !seteicon ass, like anyone else.
        public override BodypartEiconRule BodypartEiconRule => BodypartEiconRule.Part("ass", BodypartEiconOwner.Recipient);

        private static readonly List<string> SpankDescriptors = new List<string>
        {
            "a sharp spank!",
            "a love tap to the ass.",
            "a smack that will leave a mark.",
            "a red imprint on their derriere.",
            "a surprisingly loving booty grope.",
            "an impact with enough force to cook a lesser being."
        };

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public SpankProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public SpankProcessor() : base()
        {
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Increment counts with rate limiting (give/take variants)
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, "spankgive", "spanktake", RateLimit);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "spank";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} winds up and gives {recipientProfile.displayName} {GetRandomDescriptor(SpankDescriptors)}";
        }

        public override string GetGroupCompletionMessage(Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder, string identifier)
        {
            if (consentersInOrder.Count == 1)
                return GetCompletionMessage(initiatorProfile, consentersInOrder[0], identifier);

            string names = JoinNamesSerial(consentersInOrder.Select(p => p.displayName).ToList());
            return $"{initiatorProfile.displayName} winds up and gives {names} {GetRandomDescriptor(SpankDescriptors)}";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " is about to give " + recipientProfile.displayName + " a spank. Do you !consent to that sting?";
        }
    }
}