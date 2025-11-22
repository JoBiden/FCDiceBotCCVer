using System;
using System.Linq;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.InteractionProcessors;

namespace FChatDicebot.BotCommands
{
    public class ChateauFulfill : ChatBotCommand
    {
        public ChateauFulfill()
        {
            Name = "fulfill";
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
                errorMessage = "Please specify which pledged interaction you want to fulfill. Usage: !fulfill [user]Name[/user] interactiontype";
                valid = false;
            }

            Pledge pledgeToFulfill = null;

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
                    pledgeToFulfill = matchingPledges.OrderBy(p => p.pledgeTime).First();
                }
            }

            // Validate the interaction processor exists
            if (valid)
            {
                var processor = InteractionProcessorRegistry.GetProcessor(interactionType);
                if (processor == null)
                {
                    errorMessage = $"The interaction type '{interactionType}' no longer exists. This shouldn't happen!";
                    valid = false;
                }
            }

            if (valid && pledgeToFulfill != null)
            {
                var processor = InteractionProcessorRegistry.GetProcessor(interactionType);

                // Create the interaction
                Interaction interaction = new Interaction
                {
                    initiator = characterName,
                    recipient = pledgee,
                    type = interactionType,
                    identifier = pledgeToFulfill.identifier,
                    investmentLevel = pledgeToFulfill.investmentLevel,
                    interactionTime = DateTime.UtcNow
                };

                // Create pending command (requires consent)
                PendingCommand pendingCommand = new PendingCommand
                {
                    pendingInteraction = interaction,
                    awaitingConsentFrom = pledgee
                };

                // Store pledge ID in extra parameters so we can mark it fulfilled after consent
                pendingCommand.pendingInteraction.extraParameters = new MongoDB.Bson.BsonArray
                {
                    new MongoDB.Bson.BsonDocument
                    {
                        { "pledgeId", pledgeToFulfill.Id.ToString() }
                    }
                };

                MonDB.addPendingCommand(pendingCommand);

                // Get consent warning from processor
                string consentWarning = processor.GetConsentWarning(pledgerProfile, pledgeeProfile, pledgeToFulfill.identifier);

                // Add note that this is fulfilling a pledge
                string timeAgo = Utils.TimeDifferenceText(pledgeToFulfill.pledgeTime, DateTime.UtcNow);
                string pledgeNote = $"\n\n[sub]This is fulfilling a pledge made {timeAgo} ago.[/sub]";

                bot.SendMessageInChannel(consentWarning + pledgeNote, channel);
            }
            else if (!valid)
            {
                bot.SendPrivateMessage(errorMessage, characterName);
            }
        }
    }
}
