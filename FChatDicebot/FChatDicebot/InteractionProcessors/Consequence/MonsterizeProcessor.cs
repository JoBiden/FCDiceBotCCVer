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

            return $"With a flash of eldritch energy, {initiatorProfile.displayName} transforms {recipientProfile.displayName} into {monsterText}! They'll remain in this monstrous form for the next week...";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            Identifier monsterIdentifier = MonDB.getIdentifier(identifier);
            string monsterText = monsterIdentifier != null ? monsterIdentifier.description : identifier;

            return $"{initiatorProfile.displayName} wants to monsterize {recipientProfile.displayName} into {monsterText} for 7 days. [b]This is a major transformation![/b] Do you !consent?";
        }
    }
}
