using System;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.InteractionProcessors;
using System.ComponentModel.Design;

namespace FChatDicebot.BotCommands
{
    public class ChateauPledge : ChatBotCommand
    {
        public ChateauPledge()
        {
            Name = "pledge";
            Aliases = new string[] { };
            Category = "Interaction";
            ShortDescription = "Pledge to perform an interaction in the future";
            LongDescription = "Make a pledge that some day soon you will do something with another resident. These pledges do not require consent from the other party to make, but do require consent to fulfill. The ratio of pledges still outstanding, pledges fulfilled, and pledges abandoned will affect how honorably you are presented when making a new pledge. Pledges are meant to be made days in advance of their fulfillment, and do not impact honor if fulfilled rapidly after creation.";
            Usage = "!pledge [noparse][user]NameInUserTag[/user][/noparse] {interactiontype}";
            RelatedCommands = new string[] { "pledges", "fulfill", "abandonpledge" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null; //replace with list of interactions once we have a way to output that
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
            string interactionType = commandController.GetInteractionTypeFromCommandTerms(rawTerms);

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
                errorMessage = "Please specify which interaction type you want to pledge. Usage: !pledge [noparse][user]NameInUserTag[/user][/noparse] interactiontype";
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
                        errorMessage = $"Casual interactions (Bully, Cuddle, Handhold, Kiss, and Spank) aren't significant enough to pledge. Only involved, commitment, and consequence interactions can be pledged.";
                        valid = false;
                    }
                    else
                    {
                        //get identifier the interaction needs from command terms
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

                // increment active pledge count
                MonDB.incrementCount(characterName, "pledgesactive");
                int activePledges = (pledgerProfile.counts.ContainsKey("pledgesactive")) ? pledgerProfile.counts["pledgesactive"] : 1;
                int abandonedPledges = (pledgerProfile.counts.ContainsKey("pledgesabandoned")) ? pledgerProfile.counts["pledgesabandoned"] : 0;
                int fulfilledPledges = (pledgerProfile.counts.ContainsKey("pledgesfulfilled")) ? pledgerProfile.counts["pledgesfulfilled"] : 0;
                float abandonmentToFulfilledRatio = (fulfilledPledges == 0) ? abandonedPledges : (float)abandonedPledges / fulfilledPledges;
                float activeToFulfilledRatio = (fulfilledPledges == 0) ? activePledges : (float)activePledges / fulfilledPledges;
                // Send confirmation message
                string message = $"{pledgerProfile.displayName} has pledged to {interactionType} {pledgeeProfile.displayName} when the time is right.";

                // history of abandoning pledges
                if (abandonmentToFulfilledRatio >= 1.0f)
                {
                    message += $" Be warned that {pledgerProfile.displayName} has abandoned at least as many pledges as they have fulfilled... they don't value their word, it would seem. Maybe you can help restore their honor by holding them to their word this time, if you're willing to help them redeem themselves.";
                }
                else if (abandonmentToFulfilledRatio >= 0.5f)
                {
                    message += $" Word to the wise, {pledgerProfile.displayName} has abandoned at least half as many pledges as they have fulfilled... there are worse offenders, to be sure, but their word is far from ironclad. You might have to remind them about their commitment.";
                }
                else if (abandonmentToFulfilledRatio >= 0.25f)
                {
                    message += $" Hmmm... {pledgerProfile.displayName} has abandoned about a fourth as many pledges as they have fulfilled... at least they were honest and told our clerks they wouldn't be able to follow through. They clearly take their word seriously. Do you?";
                }
                else if (abandonmentToFulfilledRatio > 0.0f)
                {
                    message += $" {pledgerProfile.displayName} has a history of abandoning pledges, but they can usually be trusted to follow through, so their word holds some weight.";
                }
                // abandonment ratio is 0 if we reach this point
                // large ratio of active pledges, but no abandoned pledges
                else if (activePledges >= 5 && fulfilledPledges == 0)
                {
                    message += $" According to our records, {pledgerProfile.displayName} has made a large number of pledges, but hasn't fulfilled a single one to date... maybe they're too chicken to formally abandon them?";
                }
                else if (activeToFulfilledRatio >= 3.0f)
                {
                    message += $" According to our records, {pledgerProfile.displayName} doesn't have a great follow through rate... They've yet to break a promise though, so maybe they're just over committed?";
                }
                else if (activeToFulfilledRatio >= 1.0f && activePledges > 2)
                {
                    message += $" According to our records, {pledgerProfile.displayName} still has quite a few pledges to honor. They always follow through eventually, but you might have to wait in line...";
                }
                else if (activeToFulfilledRatio >= 0.5f && activePledges > 2)
                {
                    message += $" According to our records, {pledgerProfile.displayName} has a pretty full plate. Don't overextend yourself!";
                }
                //no notable quantity of active pledges if we reach this point
                else if (fulfilledPledges > 0)
                {
                    message += $" {pledgerProfile.displayName} always follows through on their word, so look forward to it!";
                }
                //decided this was unneeded, no other interactions have a 'first time' message
                //else if (activePledges == 1) //and no fulfilled or abandoned pledges implied by logic
                //{
                //    message += $" This is {pledgerProfile.displayName}'s first pledge! How exciting!";
                //}


                bot.SendMessageInChannel(message, channel);
            }
            else
            {
                bot.SendPrivateMessage(errorMessage, characterName);
            }
        }
    }
}
