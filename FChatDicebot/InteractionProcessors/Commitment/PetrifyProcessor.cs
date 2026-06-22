using FChatDicebot.Database;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the petrify interaction - turns someone into a statue at a specified location
    /// </summary>
    public class PetrifyProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "petrify";
        public override string InvestmentLevel => "commitment";

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
            petrifyTimer.timerEnd = DateTime.UtcNow.Date.AddDays(7);
            recipientProfile.timers["petrify"] = petrifyTimer;

            // Save the updated profile
            Database.SetProfile(recipient, recipientProfile);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "petrify";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " has petrified " + recipientProfile.displayName + " " + Utils.LocationToText(identifier, initiatorProfile.userName, recipientProfile.userName) + "! They might be stuck there for quite awhile... hopefully visitors enjoy the pose they're stuck in.";
        }

        public override CooldownSpec CooldownRule => Cooldown;

        public static readonly CooldownSpec Cooldown = new CooldownSpec
        {
            Kind = CooldownKind.Cooldown,
            Binds = CooldownBinds.Recipient,
            PeriodDays = 7
        };

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string seriousness = ConsentWarningText.Block(
                ConsentWarningText.FrequencyRecipient("petrified", Cooldown.PeriodDays));

            return initiatorProfile.displayName + " is going to petrify " + recipientProfile.displayName + " "
                + Utils.LocationToText(identifier, initiatorProfile.displayName, recipientProfile.displayName) + "! " + seriousness
                + " Do you !consent to becoming still as a statue?";
        }
    }
}
