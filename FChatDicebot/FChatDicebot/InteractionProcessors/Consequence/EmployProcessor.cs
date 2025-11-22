using FChatDicebot.Database;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the employ interaction - assigns someone a job for 1 day
    /// </summary>
    public class EmployProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "employ";
        public override string InvestmentLevel => "consequence";

        public EmployProcessor(IChateauDatabase database) : base(database)
        {
        }

        public EmployProcessor() : base()
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
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("job"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string job = command.pendingInteraction.identifier;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Get the recipient's profile
            Profile recipientProfile = Database.GetProfile(recipient);

            // Set the job and employer
            recipientProfile.characteristics["job"] = job;
            recipientProfile.characteristics["employer"] = initiator;

            // Set cooldown timer (1 day)
            CoolDown employTimer = new CoolDown();
            employTimer.timerEnd = DateTime.UtcNow.Date.AddDays(1);
            recipientProfile.timers["employ"] = employTimer;

            // Save the updated profile
            Database.SetProfile(recipient, recipientProfile);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "employ";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            Identifier jobIdentifier = MonDB.getIdentifier(identifier);
            string jobText = jobIdentifier != null ? jobIdentifier.description : identifier;

            return $"{initiatorProfile.displayName} has employed {recipientProfile.displayName} as {jobText} for the day! Time to get to work~";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            Identifier jobIdentifier = MonDB.getIdentifier(identifier);
            string jobText = jobIdentifier != null ? jobIdentifier.description : identifier;

            return $"{initiatorProfile.displayName} wants to employ {recipientProfile.displayName} as {jobText} for 1 day. Do you !consent to this employment?";
        }
    }
}
