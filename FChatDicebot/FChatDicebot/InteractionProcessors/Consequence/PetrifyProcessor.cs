using FChatDicebot.Database;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the petrify interaction - turns someone to stone for 1 day
    /// </summary>
    public class PetrifyProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "petrify";
        public override string InvestmentLevel => "consequence";

        public PetrifyProcessor(IChateauDatabase database) : base(database)
        {
        }

        public PetrifyProcessor() : base()
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:
                    return "petrified";
                case VerbTense.Present:
                    return "petrifies";
                case VerbTense.Future:
                    return "will petrify";
                default:
                    return "petrify";
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
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("location"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string recipient = command.pendingInteraction.recipient;
            string location = command.pendingInteraction.identifier;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Get the recipient's profile
            Profile recipientProfile = Database.GetProfile(recipient);

            // Set the petrify location
            recipientProfile.characteristics["petrifylocation"] = location;

            // Set cooldown timer (1 day)
            CoolDown petrifyTimer = new CoolDown();
            petrifyTimer.timerEnd = DateTime.UtcNow.Date.AddDays(1);
            recipientProfile.timers["petrify"] = petrifyTimer;

            // Save the updated profile
            Database.SetProfile(recipient, recipientProfile);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "petrify";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            Identifier locationIdentifier = MonDB.getIdentifier(identifier);
            string locationText = locationIdentifier != null ? locationIdentifier.description : identifier;

            return $"{initiatorProfile.displayName} casts a petrifying spell! {recipientProfile.displayName} slowly turns to stone, becoming a statue {locationText}. They'll remain frozen until tomorrow...";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            Identifier locationIdentifier = MonDB.getIdentifier(identifier);
            string locationText = locationIdentifier != null ? locationIdentifier.description : identifier;

            return $"{initiatorProfile.displayName} wants to petrify {recipientProfile.displayName} {locationText} for 1 day. [b]You'll be turned to stone![/b] Do you !consent?";
        }
    }
}
