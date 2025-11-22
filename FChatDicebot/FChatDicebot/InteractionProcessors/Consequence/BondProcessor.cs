using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the bond interaction - creates a bond between two characters for 1 day
    /// </summary>
    public class BondProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "bond";
        public override string InvestmentLevel => "consequence";

        public BondProcessor(IChateauDatabase database) : base(database)
        {
        }

        public BondProcessor() : base()
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
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("bond"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string bondType = command.pendingInteraction.identifier;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Get both profiles
            Profile initiatorProfile = Database.GetProfile(initiator);
            Profile recipientProfile = Database.GetProfile(recipient);

            // Determine list names based on bond type
            string bondInitiatorListName = "bond" + bondType + "initiated";
            string bondRecipientListName = "bond" + bondType + "received";

            // Get or create lists
            List<string> bondInitiatedList = new List<string>();
            List<string> bondReceivedList = new List<string>();

            if (initiatorProfile.lists.ContainsKey(bondInitiatorListName))
            {
                bondInitiatedList = initiatorProfile.lists[bondInitiatorListName];
            }
            if (recipientProfile.lists.ContainsKey(bondRecipientListName))
            {
                bondReceivedList = recipientProfile.lists[bondRecipientListName];
            }

            // Add to lists
            bondInitiatedList.Add(recipient);
            bondReceivedList.Add(initiator);

            initiatorProfile.lists[bondInitiatorListName] = bondInitiatedList;
            recipientProfile.lists[bondRecipientListName] = bondReceivedList;

            // Set cooldown timers for both (1 day)
            CoolDown bondTimer = new CoolDown();
            bondTimer.timerEnd = DateTime.UtcNow.Date.AddDays(1);
            initiatorProfile.timers["bond"] = bondTimer;
            recipientProfile.timers["bond"] = bondTimer;

            // Save both profiles
            Database.SetProfile(initiator, initiatorProfile);
            Database.SetProfile(recipient, recipientProfile);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "bond";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            Identifier bondIdentifier = MonDB.getIdentifier(identifier);
            string bondText = bondIdentifier != null ? bondIdentifier.text : identifier;

            return $"{initiatorProfile.displayName} and {recipientProfile.displayName} form a {bondText} bond! This connection will last for the next day.";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            Identifier bondIdentifier = MonDB.getIdentifier(identifier);
            string bondText = bondIdentifier != null ? bondIdentifier.text : identifier;

            return $"{initiatorProfile.displayName} wants to form a {bondText} bond with {recipientProfile.displayName} for 1 day. [b]This creates a connection between you![/b] Do you !consent?";
        }
    }
}
