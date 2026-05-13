using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the rename interaction - changes someone's display name for 7 days
    /// </summary>
    public class RenameProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "rename";
        public override string InvestmentLevel => "consequence";

        public RenameProcessor(IChateauDatabase database) : base(database)
        {
        }

        public RenameProcessor() : base()
        {
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            // Check that a new name was provided
            // Note: The extraParameters check would need to be done in the command validation
            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Get the recipient's profile
            Profile recipientProfile = Database.GetProfile(recipient);

            // Create the new display name with strikethrough of old name
            string newName = command.pendingInteraction.extraParameters.FirstOrDefault().AsString;
            recipientProfile.displayName = "[s]" + recipient + "[/s] " + newName;

            // Set cooldown timer (7 days)
            CoolDown renameTimer = new CoolDown();
            renameTimer.timerEnd = DateTime.UtcNow.Date.AddDays(7);
            recipientProfile.timers["rename"] = renameTimer;

            // Save the updated profile
            Database.SetProfile(recipient, recipientProfile);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "rename";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string newName = identifier; // The new name should be passed as identifier in this context
            return initiatorProfile.displayName + " has made it known that " + recipientProfile.displayName + " is to be known as " + newName + " henceforth! All occurences of their name in our records will be changed to reflect their new identity.";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string newName = identifier;
            return initiatorProfile.displayName + " intends for " + recipientProfile.displayName + " to be known as " + newName + " from now on. [b]This should not be taken lightly, and can not be done frequently. The new name will show almost every time the Chateau mentions you.[/b] Do you !consent to this life-changing occasion?";
        }
    }
}
