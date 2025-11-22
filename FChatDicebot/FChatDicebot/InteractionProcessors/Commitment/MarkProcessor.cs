using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Commitment
{
    /// <summary>
    /// Processor for the mark interaction - a commitment interaction with cooldown
    /// </summary>
    public class MarkProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "mark";
        public override string InvestmentLevel => "commitment";

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public MarkProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public MarkProcessor() : base()
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

            // Check that initiator has set their mark
            Profile initiatorProfile = Database.GetProfile(initiator);
            if (!initiatorProfile.characteristics.ContainsKey("mark"))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.markNotSetText());
            }

            // Check that identifier (bodypart) is provided
            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("bodypart"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string bodypart = command.pendingInteraction.identifier;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Get the recipient's profile
            Profile recipientProfile = Database.GetProfile(recipient);

            // Add initiator to the mark list for this body part
            string listname = bodypart + "marks";
            List<string> markList = new List<string>();
            if (recipientProfile.lists.ContainsKey(listname))
            {
                markList = recipientProfile.lists[listname];
            }
            markList.Add(initiator);
            recipientProfile.lists[listname] = markList;

            // Special handling for Queen Contract's mark
            if (!recipientProfile.characteristics.ContainsKey("queenMark") && initiator == "Queen Contract")
            {
                recipientProfile.characteristics["queenMark"] = "[eicon]qcmark[/eicon]";
            }

            // Set cooldown timer (1 day)
            CoolDown markTimer = new CoolDown();
            markTimer.timerEnd = DateTime.UtcNow.Date.AddDays(1);
            recipientProfile.timers["mark"] = markTimer;

            // Save the updated profile
            Database.SetProfile(recipient, recipientProfile);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "mark";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string bodypartText = Utils.BodypartToText(identifier);
            string mark = initiatorProfile.characteristics["mark"];
            
            return $"{initiatorProfile.displayName} emblazons their mark upon {recipientProfile.displayName}'s {bodypartText}. Wear it with pride~ {mark}";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string bodypartText = Utils.BodypartToText(identifier);
            
            return $"{initiatorProfile.displayName} is going to mark {recipientProfile.displayName} on their {bodypartText}! [b]This should not be taken lightly, and can not be done frequently.[/b] Do you !consent to receiving their mark?";
        }
    }
}
