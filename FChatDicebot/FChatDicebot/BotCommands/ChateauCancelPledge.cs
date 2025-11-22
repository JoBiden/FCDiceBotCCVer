using System;
using System.Linq;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauCancelPledge : ChatBotCommand
    {
        private const int CANCELLATION_COST = 10; // Cost in "favor" currency to cancel a pledge

        public ChateauCancelPledge()
        {
            Name = "cancelpledge";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            // Get the pledgee (recipient) from command terms
            string pledgee = commandController.GetUserNameFromCommandTerms(rawTerms);

            // Get the interaction type from command terms
            string interactionType = null;
            if (terms.Length >= 3)
            {
                interactionType = terms[2].ToLower();
            }

            Profile pledgerProfile = MonDB.getProfile(characterName);
            Profile pledgeeProfile = MonDB.getProfile(pledgee);

            bool valid = true;
            string errorMessage = "";

            // Validate profiles exist
            if (pledgerProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notRegisteredText(), characterName);
                return;
            }

            if (pledgeeProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(pledgee), characterName);
                valid = false;
            }

            // Validate interaction type was provided
            if (string.IsNullOrEmpty(interactionType))
            {
                errorMessage = "Please specify which pledged interaction you want to cancel. Usage: !cancelpledge [user]Name[/user] interactiontype";
                valid = false;
            }

            Pledge pledgeToCancel = null;

            // Find the matching active pledge
            if (valid)
            {
                var matchingPledges = MonDB.getActivePledges(characterName, pledgee, interactionType);

                if (matchingPledges.Count == 0)
                {
                    errorMessage = $"You don't have an active pledge to {interactionType} {pledgeeProfile.displayName}. Use !viewpledges to see your active pledges.";
                    valid = false;
                }
                else
                {
                    // If multiple pledges exist (shouldn't normally happen), use the oldest one
                    pledgeToCancel = matchingPledges.OrderBy(p => p.pledgeTime).First();
                }
            }

            // Check if user has enough currency to pay the cancellation cost
            if (valid && pledgeToCancel != null)
            {
                var currencies = MonDB.getCurrencies(characterName);
                int currentFavor = currencies.ContainsKey("favor") ? currencies["favor"] : 0;

                if (currentFavor < CANCELLATION_COST)
                {
                    errorMessage = $"Breaking a pledge requires {CANCELLATION_COST} favor, but you only have {currentFavor}. Pledges should not be made lightly!";
                    valid = false;
                }
            }

            if (valid && pledgeToCancel != null)
            {
                // Charge the cancellation cost
                pledgerProfile.currencies["favor"] = pledgerProfile.currencies.ContainsKey("favor") ? pledgerProfile.currencies["favor"] - CANCELLATION_COST : -CANCELLATION_COST;
                MonDB.setProfile(characterName, pledgerProfile);

                // Mark pledge as cancelled
                pledgeToCancel.status = "cancelled";
                MonDB.updatePledge(pledgeToCancel);

                // Send confirmation message
                string message = $"{pledgerProfile.displayName} has broken their pledge to {pledgeToCancel.interactionType} {pledgeeProfile.displayName}. The act of turning one's back on a promise has cost them {CANCELLATION_COST} favor. The Chateau remembers such moments...";
                bot.SendMessageInChannel(message, channel);
            }
            else if (!valid)
            {
                bot.SendPrivateMessage(errorMessage, characterName);
            }
        }
    }
}
