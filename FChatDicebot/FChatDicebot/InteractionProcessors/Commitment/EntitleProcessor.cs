using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Commitment
{
    /// <summary>
    /// Processor for the entitle interaction - a commitment interaction with cooldown on receiving
    /// </summary>
    public class EntitleProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "entitle";
        public override string InvestmentLevel => "commitment";

        private const int MAX_TITLE_LENGTH = 100;
        private const char SYSTEM_MARKER = '·';

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public EntitleProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public EntitleProcessor() : base()
        {
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            // First do base validation (profiles exist)
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            // Check that identifier (title text) is provided
            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure("You must specify a title to give. Usage: !entitle {recipient} {\"title in quotes\"}");
            }

            // Check title length
            if (identifier.Length > MAX_TITLE_LENGTH)
            {
                return ValidationResult.Failure($"Title is too long! Maximum length is {MAX_TITLE_LENGTH} characters. Your title is {identifier.Length} characters.");
            }

            // Check for forbidden system marker character
            if (identifier.Contains(SYSTEM_MARKER))
            {
                return ValidationResult.Failure($"The character '{SYSTEM_MARKER}' is reserved for system-granted titles and cannot be used in custom titles.");
            }

            // Check if recipient already has this exact title
            Profile recipientProfile = Database.GetProfile(recipient);
            if (recipientProfile.titles != null && recipientProfile.titles.Any(t => t.titleText.Equals(identifier, StringComparison.OrdinalIgnoreCase)))
            {
                return ValidationResult.Failure($"{recipientProfile.displayName} already has the title \"{identifier}\".");
            }

            // Check cooldown on receiving titles
            if (recipientProfile.timers != null && recipientProfile.timers.ContainsKey("entitle"))
            {
                CoolDown entitleTimer = recipientProfile.timers["entitle"];
                if (DateTime.UtcNow < entitleTimer.timerEnd)
                {
                    TimeSpan remaining = entitleTimer.timerEnd - DateTime.UtcNow;
                    return ValidationResult.Failure($"{recipientProfile.displayName} cannot receive another title for {Utils.GetTimeSpanPrint(remaining)}.");
                }
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string titleText = command.pendingInteraction.identifier;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Get the recipient's profile
            Profile recipientProfile = Database.GetProfile(recipient);

            // Initialize titles list if null
            if (recipientProfile.titles == null)
            {
                recipientProfile.titles = new List<Title>();
            }

            // Create and add the new title
            Title newTitle = new Title
            {
                titleText = titleText,
                givenBy = initiator,
                grantedTime = DateTime.UtcNow
            };
            recipientProfile.titles.Add(newTitle);

            // Set cooldown timer (1 day) on receiving titles
            CoolDown entitleTimer = new CoolDown();
            entitleTimer.timerEnd = DateTime.UtcNow.Date.AddDays(1);
            recipientProfile.timers["entitle"] = entitleTimer;

            // Save the updated profile
            Database.SetProfile(recipient, recipientProfile);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "entitle";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} has bestowed the title \"{identifier}\" upon {recipientProfile.displayName}! May they wear it with pride!";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} wants to grant you the title \"{identifier}\". [b]This is a meaningful commitment and cannot be done frequently.[/b] Do you !consent to receiving this title?";
        }
    }
}






