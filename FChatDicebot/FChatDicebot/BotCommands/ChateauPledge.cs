using System;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.InteractionProcessors;

namespace FChatDicebot.BotCommands
{
    public class ChateauPledge : ChatBotCommand
    {
        public ChateauPledge()
        {
            Name = "pledge";
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
                errorMessage = "Please specify which interaction type you want to pledge. Usage: !pledge [user]Name[/user] interactiontype";
                valid = false;
            }

            // Validate that the interaction type exists and is pledgeable
            if (valid && !string.IsNullOrEmpty(interactionType))
            {
                var processor = InteractionProcessorRegistry.GetProcessor(interactionType);

                if (processor == null)
                {
                    errorMessage = $"The interaction type '{interactionType}' doesn't exist. Make sure you spelled it correctly!";
                    valid = false;
                }
                else
                {
                    // Check if interaction is pledgeable (not casual)
                    string investmentLevel = processor.InvestmentLevel.ToLower();
                    if (investmentLevel == "casual")
                    {
                        errorMessage = $"Casual interactions like '{interactionType}' aren't significant enough to pledge. Only involved, commitment, and consequence interactions can be pledged.";
                        valid = false;
                    }
                }
            }

            if (valid)
            {
                // Get the processor to determine investment level
                var processor = InteractionProcessorRegistry.GetProcessor(interactionType);

                // Create the pledge
                Pledge newPledge = new Pledge
                {
                    pledger = characterName,
                    pledgee = pledgee,
                    interactionType = interactionType,
                    identifier = null, // For now, we'll add identifier support later
                    investmentLevel = processor.InvestmentLevel,
                    pledgeTime = DateTime.UtcNow,
                    status = "active"
                };

                // Save the pledge
                MonDB.addPledge(newPledge);

                // Send confirmation message
                string message = $"{pledgerProfile.displayName} has pledged to {interactionType} {pledgeeProfile.displayName} when the time is right. The Chateau's records have been updated to reflect this promise!";
                bot.SendMessageInChannel(message, channel);
            }
            else
            {
                bot.SendPrivateMessage(errorMessage, characterName);
            }
        }
    }
}
