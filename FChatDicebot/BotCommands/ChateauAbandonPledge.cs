using System;
using System.Linq;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.InteractionProcessors;

namespace FChatDicebot.BotCommands
{
    public class ChateauAbandonPledge : ChatBotCommand
    {

        public ChateauAbandonPledge()
        {
            Name = "abandonpledge";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Abandon an active pledge";
            LongDescription = "Abandon an active pledges if it cannot or will not be fulfilled. Abandoning even one pledge will forever change how honor is described to others when making future pledges, but can sometimes be preferred to having too many outstanding pledges.";
            Usage = "!abandonpledge [noparse][user]NameInUserTag[/user][/noparse] {interactiontype}";
            RelatedCommands = new string[] { "pledge", "pledges", "fulfill" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null; //replace with list of interaction types once implemented
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
            // Takes an interaction type as its second argument; flagged so bare-name
            // resolution treats "cuddle" as that argument rather than an unknown resident.
            TakesInteractionType = true;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
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
                errorMessage = "Please specify which pledged interaction you intend to abandon. Usage: !abandon [noparse][user]NameInUserTag[/user][/noparse] interactiontype";
                valid = false;
            }

            Pledge pledgeToCancel = null;
            IInteractionProcessor processor = null;

            if (valid)
            {
                // Resolved before the no-match error, which names the verb — a misspelled
                // type has no processor to ask. Same shape as !fulfill.
                processor = InteractionProcessorRegistry.GetProcessor(interactionType);
            }

            // Find the matching active pledge
            if (valid)
            {
                var matchingPledges = MonDB.getActivePledges(characterName, pledgee, interactionType);

                if (matchingPledges.Count == 0)
                {
                    errorMessage = ChateauInteractionHandler.noMatchingPledgeText(processor, interactionType, pledgeeProfile.displayName);
                    valid = false;
                }
                else
                {
                    // If multiple pledges exist (shouldn't normally happen), use the oldest one
                    pledgeToCancel = matchingPledges.OrderBy(p => p.pledgeTime).First();
                }
            }

            if (valid && pledgeToCancel != null)
            {
                if (!pledgeToCancel.abandonWarned)
                {
                    // Warn only. The pledge deliberately stays active: the confirmation is a
                    // speed bump, so a pledger who reconsiders still sees it in !pledges and
                    // can still !fulfill it. (It used to move to a "warned" status, which
                    // every lookup filtered out — stranding the pledge for good.)
                    pledgeToCancel.abandonWarned = true;
                    MonDB.updatePledge(pledgeToCancel);
                    bot.SendPrivateMessage($"Abandoning an active pledge is not to be taken lightly. [b]Your dossier will carry a permanent record of how many pledges you have abandoned, and those you make pledges to will be informed of your likelihood to carry through a pledge.[/b] If you would still like to proceed with abandoning your pledge, try to do so once more. You will only be warned once per pledge.", characterName);
                }
                else
                {
                    // Mark pledge as abandoned
                    pledgeToCancel.status = "abandoned";
                    MonDB.updatePledge(pledgeToCancel);

                    // incremenet abandoned pledge count, decrement outstanding pledge count
                    MonDB.incrementCount(characterName, "pledgesabandoned");
                    MonDB.decrementCount(characterName, "pledgesactive");

                    // Send confirmation message
                    string message = $"{pledgerProfile.displayName} has abandoned their pledge to {Utils.interactionToVerb(pledgeToCancel.interactionType, false)} {pledgeeProfile.displayName}. The act of turning one's back on this promise has been permanently recorded on their dossier. Future pledges will carry with them an indication of this trend of oathbreaking...";

                    // Grant "Broke Their Promise" and its tiers here rather than leaving them
                    // for the next consented interaction to announce — the same timing bug
                    // !pledge had. Stays private, like the rest of this confirmation.
                    string titleNotification = ChateauSystemTitles.CheckAndGrantTitles(characterName);
                    if (!string.IsNullOrEmpty(titleNotification))
                    {
                        message += titleNotification;
                    }

                    bot.SendPrivateMessage(message, characterName);
                }
            }
            else if (!valid)
            {
                bot.SendPrivateMessage(errorMessage, characterName);
            }
        }
    }
}
