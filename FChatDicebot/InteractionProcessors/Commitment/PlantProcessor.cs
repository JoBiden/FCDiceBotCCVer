using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Numerics;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the plant interaction - transforms someone into a plant for 1 day
    /// </summary>
    public class PlantProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "plant";
        public override string InvestmentLevel => "commitment";

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
            identifier = Utils.AnOrA(identifier) + " " + identifier;
            return initiatorProfile.displayName + " has grown the garden by turning " + recipientProfile.displayName + " into " + identifier + "! They might stay planted for quite awhile... surely the gardeners will take good care of them.";
        }

        public override CooldownSpec CooldownRule => Cooldown;

        public static readonly CooldownSpec Cooldown = new CooldownSpec
        {
            Kind = CooldownKind.Cooldown,
            Binds = CooldownBinds.Recipient,
            PeriodDays = 1
        };

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string seriousness = ConsentWarningText.Block(
                ConsentWarningText.FrequencyRecipient("planted", Cooldown.PeriodDays));

            return initiatorProfile.displayName + " is going to turn " + recipientProfile.displayName
                + " into a " + identifier + "! " + seriousness
                + " Do you !consent to becoming plantlife?";
        }
    }
}
