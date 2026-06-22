using FChatDicebot.Database;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the monsterize interaction - transforms someone into a monster for 7 days
    /// </summary>
    public class MonsterizeProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "monsterize";
        public override string InvestmentLevel => "consequence";

        public MonsterizeProcessor(IChateauDatabase database) : base(database)
        {
        }

        public MonsterizeProcessor() : base()
        {
        }

        /// <summary>
        /// Profile-key under which the 7-day re-monsterize lock is stored on the recipient.
        /// Exposed so the command's pre-check and the processor's recheck consult the same
        /// timer and can't drift.
        /// </summary>
        public const string CooldownTimerKey = "monsterize";

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("monster"));
            }

            // Recipient already monsterized too recently — rejection lives here (not in
            // the command) so a stray pending that races past the command-time check
            // still gets gated at consent time.
            Profile recipientProfile = Database.GetProfile(recipient);
            if (recipientProfile?.timers != null
                && recipientProfile.timers.TryGetValue(CooldownTimerKey, out var timer)
                && timer.timerEnd > DateTime.UtcNow)
            {
                string recipientName = recipientProfile.displayName ?? recipient;
                string remaining = Utils.GetTimeSpanPrint(timer.timerEnd - DateTime.UtcNow);
                return ValidationResult.Failure(
                    "You're trying to monsterize " + recipientName + " but they only recently changed to the monster they currently are! "
                    + "Please respect that 'Consequence' interactions are also a Commitment. Wait a little longer before you change their form again. \n\n"
                    + recipientName + " will be available to monsterize in " + remaining);
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string recipient = command.pendingInteraction.recipient;
            string monsterType = command.pendingInteraction.identifier;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Get the recipient's profile
            Profile recipientProfile = Database.GetProfile(recipient);

            // Set the monster characteristic
            recipientProfile.characteristics["monster"] = monsterType;

            // Set cooldown timer (7 days)
            CoolDown monsterizeTimer = new CoolDown();
            monsterizeTimer.timerEnd = DateTime.UtcNow.Date.AddDays(7);
            recipientProfile.timers["monsterize"] = monsterizeTimer;

            // Save the updated profile
            Database.SetProfile(recipient, recipientProfile);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "monsterize";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            Identifier monsterIdentifier = MonDB.getIdentifier(identifier);
            string monsterText = monsterIdentifier != null ? monsterIdentifier.description : identifier;

            identifier = Utils.AnOrA(identifier) + " " + identifier;
            return initiatorProfile.displayName + " has bolstered monsterkind by turning " + recipientProfile.displayName + " into " + identifier + "! We welcome all monsters to our Chateau, no matter what your origins. Enjoy your new life as " + identifier + "~";
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
                ConsentWarningText.FrequencyRecipient("monsterized", Cooldown.PeriodDays),
                "The capabilities of your new body might be referenced in flavor text and !work checks.");

            return initiatorProfile.displayName + " is going to transform " + recipientProfile.displayName
                + " into " + Utils.AnOrA(identifier) + " " + identifier + "! " + seriousness
                + " Do you !consent to your new form?";
        }
    }
}
