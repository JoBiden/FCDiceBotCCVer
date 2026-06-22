using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the bond interaction - creates a permanent bond between two characters
    /// </summary>
    public class BondProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "bond";
        public override string InvestmentLevel => "commitment";

        public override CooldownSpec CooldownRule => Cooldown;

        public static readonly CooldownSpec Cooldown = new CooldownSpec
        {
            Kind = CooldownKind.Cooldown,
            Binds = CooldownBinds.Both,
            PeriodDays = 1
        };

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
            string bondText = bondIdentifier != null ? bondIdentifier.description : identifier;

            return initiatorProfile.displayName + " is now " + recipientProfile.displayName + "'s " + Utils.BondToText(identifier, false) + ", and " + recipientProfile.displayName + " is now their " + Utils.BondToText(identifier, true) + "! May you enjoy a bright future together."; ;
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            // A bond puts a global cooldown on BOTH parties (not a per-pair limit), so the
            // recipient-framed clause speaks to the act of declaring a bond at all.
            string seriousness = ConsentWarningText.Block(
                "You can only declare a bond once per " + ConsentWarningText.PeriodWord(Cooldown.PeriodDays) + ".");

            return initiatorProfile.displayName + " would like to declare that " + recipientProfile.displayName
                + " is their " + Utils.BondToText(identifier, true) + "! " + seriousness
                + " Do you !consent to this new bond?";
        }
    }
}
