using FChatDicebot.Database;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Processor for the dressup interaction - an involved interaction with 30 minute rate limit
    /// </summary>
    public class DressupProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "dressup";
        public override string InvestmentLevel => "involved";

        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public DressupProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public DressupProcessor() : base()
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:
                    return "dressed up";
                case VerbTense.Present:
                    return "dresses up";
                case VerbTense.Future:
                    return "will dress up";
                default:
                    return "dress up";
            }
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            // First do base validation (profiles exist)
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            // Check that identifier (attire) is provided
            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("attire"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string attire = command.pendingInteraction.identifier;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Get the recipient's profile and set their attire characteristic
            Profile recipientProfile = Database.GetProfile(recipient);
            recipientProfile.characteristics["attire"] = attire;
            Database.SetProfile(recipient, recipientProfile);

            // Increment counts with rate limiting (give/take variants)
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, "dressupgive", "dressuptake", RateLimit);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "dressup";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} has dressed up {recipientProfile.displayName} in {Utils.AttireToText(identifier)}! Do a spin for everyone, let them admire your new garb!";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " is going to dress up " + recipientProfile.displayName + " in " + Utils.AttireToText(identifier) + "! Do you !consent to the change of attire?";
        }
    }
}