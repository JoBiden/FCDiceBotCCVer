using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Casual
{
    /// <summary>
    /// Processor for the pet interaction: the initiator pets the recipient. Directional
    /// casual — <c>petgive</c> goes to the one doing the petting, <c>pettake</c> to the one
    /// being petted. Modeled on <see cref="LickProcessor"/> / <see cref="SpankProcessor"/>.
    ///
    /// Its custom <c>!seteicon pet</c> icon is the <b>recipient's</b> (unlike most directional
    /// casuals, which show the initiator's): residents typically keep a "my character being
    /// petted" eicon, so the natural icon to surface is the pet's, not the petter's. See
    /// <see cref="GetEiconSubject"/> and <see cref="GetGroupEiconSuffix"/>.
    /// </summary>
    public class PetProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "pet";
        public override string InvestmentLevel => "casual";

        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        // Directional group model: initiator +R petgive, each recipient +1 pettake.
        public override GroupSpec GroupSpec => GroupSpec.Directional("petgive", "pettake");

        // {petgiver} = the one petting (initiator); {pettaker} = the one being petted
        // (recipient; for a multi-target pet this resolves to the first consenter — see
        // GetGroupCompletionMessage).
        private static readonly List<string> PetDescriptors = new List<string>
        {
            "Who's a good pet? {pettaker} is.",
            "Scritch scritch scritch.",
            "Pat, pat, pat.",
            "A few gentle strokes behind the ears.",
            "{pettaker} leans into it happily.",
            "There, there."
        };

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public PetProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public PetProcessor() : base()
        {
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Increment counts with rate limiting (give/take variants).
            // Initiator is the one petting (petgive); recipient is petted (pettake).
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, "petgive", "pettake", RateLimit);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "pet";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string descriptor = GetRandomDescriptor(PetDescriptors)
                .Replace("{petgiver}", initiatorProfile.displayName)
                .Replace("{pettaker}", recipientProfile.displayName);

            return $"{initiatorProfile.displayName} gently pets {recipientProfile.displayName}. {descriptor}";
        }

        public override string GetGroupCompletionMessage(Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder, string identifier)
        {
            if (consentersInOrder.Count == 1)
                return GetCompletionMessage(initiatorProfile, consentersInOrder[0], identifier);

            // {pettaker} in a multi-target pet resolves to the first consenter (deterministic).
            string descriptor = GetRandomDescriptor(PetDescriptors)
                .Replace("{petgiver}", initiatorProfile.displayName)
                .Replace("{pettaker}", consentersInOrder[0].displayName);

            // "Alice pets Bob, Carol, and Dave. {descriptor}".
            string names = JoinNamesSerial(consentersInOrder.Select(p => p.displayName).ToList());
            return $"{initiatorProfile.displayName} pets {names}. {descriptor}";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " wants to pet " + recipientProfile.displayName + ". Do you !consent to some scritches?";
        }

        /// <summary>
        /// The custom <c>!seteicon pet</c> icon belongs to the one being petted — the recipient —
        /// since residents keep a "being petted" eicon rather than a "petting someone" one.
        /// </summary>
        protected override Profile GetEiconSubject(string interactionVerb, Profile initiatorProfile, Profile recipientProfile)
        {
            return recipientProfile ?? initiatorProfile;
        }

        /// <summary>
        /// Group custom-eicon flourish: each petted recipient's own <c>pet</c> eicon (in consent
        /// order), not the initiator's. Mirrors the 1:1 <see cref="GetEiconSubject"/> redirect so
        /// a group pet surfaces every "being petted" icon rather than the petter's.
        /// </summary>
        public override string GetGroupEiconSuffix(string interactionVerb, Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder)
        {
            string verb = string.IsNullOrEmpty(interactionVerb) ? InteractionType : interactionVerb;
            if (InteractionEiconSupport.IsSelfRendered(verb)) return string.Empty;

            var eicons = new List<string>();
            if (consentersInOrder != null)
            {
                foreach (var consenter in consentersInOrder)
                {
                    AddInteractionEicon(eicons, consenter, verb);
                }
            }
            return JoinEiconSuffix(eicons);
        }
    }
}
