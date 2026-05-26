using FChatDicebot.Database;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Processor for the golden interaction - an involved interaction with 30 minute rate limit
    /// </summary>
    public class GoldenProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "golden";
        public override string InvestmentLevel => "involved";

        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public GoldenProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public GoldenProcessor() : base()
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:
                    return "relieved themselves";
                case VerbTense.Present:
                    return "relieves themself";
                case VerbTense.Future:
                    return "will relieve themself";
                default:
                    return "relieve themself";
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

            // Check that identifier (bodypart) is provided
            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("bodypart"));
            }

            // Break gating: if the targeted bodypart is broken on the recipient, refuse.
            // BreakStatusContributor's signature can't see the targeted bodypart, so this
            // dynamic-target check lives here in the processor.
            Profile recipientProfile = Database.GetProfile(recipient);
            var recipientBreaks = BreakInstance.LoadAllWithTick(recipientProfile);
            foreach (var entry in recipientBreaks)
            {
                if (string.Equals(entry.Part, identifier, StringComparison.OrdinalIgnoreCase))
                {
                    string recipientName = string.IsNullOrEmpty(recipientProfile?.displayName) ? recipient : recipientProfile.displayName;
                    return ValidationResult.Failure(
                        recipientName + "'s " + identifier + " is too broken for a golden shower.");
                }
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Increment counts with rate limiting (give/take variants)
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, "goldengive", "goldentake", RateLimit);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "golden";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} breathes a sigh of relief as a golden fluid pours over {recipientProfile.displayName}'s {Utils.BodypartToText(identifier)}.";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " is going to relieve themselves using " + recipientProfile.displayName + "'s " + Utils.BodypartToText(identifier) + "! Do you !consent to being used like that?";
        }
    }
}
