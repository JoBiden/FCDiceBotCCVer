using FChatDicebot.Database;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the objectify interaction - transforms someone into an object. 7 day cool down for recipient.
    /// </summary>
    public class ObjectifyProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "objectify";
        public override string InvestmentLevel => "commitment";

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

        /// <summary>
        /// Profile-key under which the 1-day re-objectify lock is stored on the recipient.
        /// Exposed so the command's pre-check and the processor's recheck consult the same
        /// timer and can't drift.
        /// </summary>
        public const string CooldownTimerKey = "objectify";

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

            // Recipient already objectified too recently — rejection lives here (not in
            // the command) so a stray pending that races past the command-time check
            // still gets gated at consent time.
            Profile recipientProfile = Database.GetProfile(recipient);
            if (recipientProfile?.timers != null
                && recipientProfile.timers.TryGetValue(CooldownTimerKey, out var timer)
                && timer.timerEnd > DateTime.UtcNow)
            {
                string recipientName = recipientProfile.displayName ?? recipient;
                return ValidationResult.Failure(
                    "You're trying to objectify " + recipientName + " but they were recently objectified! "
                    + "Please respect that 'Commitment' interactions are meant to be just that - a commitment. Wait a little longer for them to recover before you objectify them again. \n\n"
                    + recipientName + " will be available to objectify no sooner than " + timer.timerEnd
                    + " (the current time is " + DateTime.UtcNow + ")");
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
            identifier = Utils.AnOrA(identifier) + " " + identifier;
            return initiatorProfile.displayName + " has made " + recipientProfile.displayName + " into some sort of " + identifier + "! Who knows what's in store for them, but they'll be stuck with their fate for quite awhile...";
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
                ConsentWarningText.FrequencyRecipient("objectified", Cooldown.PeriodDays));

            return initiatorProfile.displayName + " is going to turn " + recipientProfile.displayName
                + " into some sort of " + identifier + "! " + seriousness
                + " Do you !consent to becoming an object?";
        }
    }
}
