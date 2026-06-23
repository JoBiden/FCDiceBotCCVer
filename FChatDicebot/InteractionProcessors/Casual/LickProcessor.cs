using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

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

        // Directional group model: initiator +R lickgive, each recipient +1 licktake.
        public override GroupSpec GroupSpec => GroupSpec.Directional("lickgive", "licktake");

        // {lickgiver} = the licker (initiator); {licktaker} = the licked (recipient; for a
        // multi-target lick this resolves to the first consenter — see GetGroupCompletionMessage).
        private static readonly List<string> LickDescriptors = new List<string>
        {
            "How many more licks to get to the center?",
            "I bet they taste good.",
            "In cat culture, that means {lickgiver} is in charge.",
            "In bunny culture, that means {licktaker} is in charge.",
            "Is this what it means to be groomed?",
            "Mlem!"
        };

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
            string descriptor = GetRandomDescriptor(LickDescriptors)
                .Replace("{lickgiver}", initiatorProfile.displayName)
                .Replace("{licktaker}", recipientProfile.displayName);

            if (initiatorProfile.userName == "Queen Contract")
            {
                descriptor += "[eicon]qctongue[/eicon]";
            }

            return $"{initiatorProfile.displayName} gives {recipientProfile.displayName} a slow lick. {descriptor}";
        }

        public override string GetGroupCompletionMessage(Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder, string identifier)
        {
            if (consentersInOrder.Count == 1)
                return GetCompletionMessage(initiatorProfile, consentersInOrder[0], identifier);

            // {licktaker} in a multi-target lick resolves to the first consenter (B7 open
            // item — pick first, deterministic).
            string descriptor = GetRandomDescriptor(LickDescriptors)
                .Replace("{lickgiver}", initiatorProfile.displayName)
                .Replace("{licktaker}", consentersInOrder[0].displayName);

            if (initiatorProfile.userName == "Queen Contract")
            {
                descriptor += "[eicon]qctongue[/eicon]";
            }

            // Spec's directional example: "Alice licks Bob, Carol, and Dave. {descriptor}".
            string names = JoinNamesSerial(consentersInOrder.Select(p => p.displayName).ToList());
            return $"{initiatorProfile.displayName} licks {names}. {descriptor}";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " wants to give " + recipientProfile.displayName + " a lick. Do you !consent to being lapped at?";
        }
    }
}
