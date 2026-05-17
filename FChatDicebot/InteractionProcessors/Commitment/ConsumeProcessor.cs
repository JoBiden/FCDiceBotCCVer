using FChatDicebot.Database;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the consume interaction
    /// </summary>
    public class ConsumeProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "consume";
        public override string InvestmentLevel => "consequence";

        public ConsumeProcessor(IChateauDatabase database) : base(database)
        {
        }

        public ConsumeProcessor() : base()
        {
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string recipient = command.pendingInteraction.recipient;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Get the recipient's profile
            Profile recipientProfile = Database.GetProfile(recipient);

            // Set cooldown timer (1 day)
            CoolDown consumeTimer = new CoolDown();
            consumeTimer.timerEnd = DateTime.UtcNow.Date.AddDays(1);
            recipientProfile.timers["consume"] = consumeTimer;

            // Save the updated profile
            Database.SetProfile(recipient, recipientProfile);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return "consume";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " consumes " + recipientProfile.displayName + ", and they were never heard from again... or at least, it will be quite some time before they manage to escape, reform, or otherwise recover their strength.";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " is going to consume " + recipientProfile.displayName + "! [b]This should not be taken lightly, and can not be done frequently.[/b] Do you !consent to being consumed, devoured, or otherwise feasted upon?";
        }
    }
}
