using FChatDicebot.Database;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the objectify interaction - transforms someone into an object for 1 day
    /// </summary>
    public class ObjectifyProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "objectify";
        public override string InvestmentLevel => "consequence";

        public ObjectifyProcessor(IChateauDatabase database) : base(database)
        {
        }

        public ObjectifyProcessor() : base()
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:
                    return "objectified";
                case VerbTense.Present:
                    return "objectifies";
                case VerbTense.Future:
                    return "will objectify";
                default:
                    return "objectify";
            }
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
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("object"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string recipient = command.pendingInteraction.recipient;
            string objectType = command.pendingInteraction.identifier;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Get the recipient's profile
            Profile recipientProfile = Database.GetProfile(recipient);

            // Set the object type
            recipientProfile.characteristics["objectType"] = objectType;

            // Set cooldown timer (1 day)
            CoolDown objectifyTimer = new CoolDown();
            objectifyTimer.timerEnd = DateTime.UtcNow.Date.AddDays(1);
            recipientProfile.timers["objectify"] = objectifyTimer;

            // Save the updated profile
            Database.SetProfile(recipient, recipientProfile);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "objectify";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            Identifier objectIdentifier = MonDB.getIdentifier(identifier);
            string objectText = objectIdentifier != null ? objectIdentifier.description : identifier;

            return $"{initiatorProfile.displayName} waves their hand and {recipientProfile.displayName} shimmers, transforming into {objectText}! They'll be an object until tomorrow.";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            Identifier objectIdentifier = MonDB.getIdentifier(identifier);
            string objectText = objectIdentifier != null ? objectIdentifier.description : identifier;

            return $"{initiatorProfile.displayName} wants to objectify {recipientProfile.displayName} into {objectText} for 1 day. [b]You'll become an inanimate object![/b] Do you !consent?";
        }
    }
}
