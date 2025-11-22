using FChatDicebot.Database;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the plant interaction - transforms someone into a plant for 1 day
    /// </summary>
    public class PlantProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "plant";
        public override string InvestmentLevel => "consequence";

        public PlantProcessor(IChateauDatabase database) : base(database)
        {
        }

        public PlantProcessor() : base()
        {
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("plant"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string recipient = command.pendingInteraction.recipient;
            string plantType = command.pendingInteraction.identifier;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Get the recipient's profile
            Profile recipientProfile = Database.GetProfile(recipient);

            // Set the plant type
            recipientProfile.characteristics["plantType"] = plantType;

            // Set cooldown timer (1 day)
            CoolDown plantTimer = new CoolDown();
            plantTimer.timerEnd = DateTime.UtcNow.Date.AddDays(1);
            recipientProfile.timers["plant"] = plantTimer;

            // Save the updated profile
            Database.SetProfile(recipient, recipientProfile);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "plant";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            Identifier plantIdentifier = MonDB.getIdentifier(identifier);
            string plantText = plantIdentifier != null ? plantIdentifier.text : identifier;

            return $"{initiatorProfile.displayName} uses botanical magic to transform {recipientProfile.displayName} into {plantText}! They'll be rooted in place until tomorrow.";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            Identifier plantIdentifier = MonDB.getIdentifier(identifier);
            string plantText = plantIdentifier != null ? plantIdentifier.text : identifier;

            return $"{initiatorProfile.displayName} wants to transform {recipientProfile.displayName} into {plantText} for 1 day. [b]You'll become a plant![/b] Do you !consent?";
        }
    }
}
