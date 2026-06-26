using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using System.ComponentModel;

namespace FChatDicebot.BotCommands
{
    public class ChateauBond : ChatBotCommand
    {
        public ChateauBond()
        {
            Name = "bond";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Declare your bond with another resident";
            LongDescription = "Create a specific type of bond with another character (owner/pet, master/slave, etc.). The recipient must !consent to acknowledge the mutual connection. ";
            Usage = "!bond [noparse][user]NameInUserTag[/user][/noparse] {bondtype}";
            RelatedCommands = new string[] { "mark", "employ"};
            CooldownDuration = "1 Day";
            CooldownAppliesTo = "both initiator and recipient";
            IdentifierCategory = "bond";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string identifierType = "bond";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string bond = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (bond == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                valid = false;
            }
            // A bond drops a 1-day "bond" cooldown on BOTH parties, so neither can declare
            // another bond until it lapses. Enforce it at command time for immediate feedback.
            else if (initiatorProfile.timers.ContainsKey("bond")
                && initiatorProfile.timers["bond"].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string tooSoonText = "You've declared a bond too recently! Please respect that 'Commitment' interactions are meant to be meaningful, and not spammed. You'll be able to declare another bond in "
                    + Utils.GetTimeSpanPrint(initiatorProfile.timers["bond"].timerEnd - DateTime.UtcNow);
                bot.SendPrivateMessage(tooSoonText, characterName);
                valid = false;
            }
            else if (recipientProfile.timers.ContainsKey("bond")
                && recipientProfile.timers["bond"].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string tooSoonText = "You're trying to bond with " + recipientProfile.displayName + " but they declared a bond too recently! Please respect that 'Commitment' interactions are meant to be meaningful, and not spammed. They'll be able to take part in another bond in "
                    + Utils.GetTimeSpanPrint(recipientProfile.timers["bond"].timerEnd - DateTime.UtcNow);
                bot.SendPrivateMessage(tooSoonText, characterName);
                valid = false;
            }
            if (valid)
            {
                Interaction bondInteraction = new Interaction();
                bondInteraction.initiator = characterName;
                bondInteraction.recipient = recipient;
                bondInteraction.identifier = bond;
                bondInteraction.type = "bond";
                bondInteraction.investmentLevel = "commitment";
                bondInteraction.interactionTime = DateTime.UtcNow;

                PendingCommand pendingBond = new PendingCommand();
                pendingBond.pendingInteraction = bondInteraction;
                pendingBond.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingBond);

                // Delegate consent wording to the processor so it stays in one place.
                var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor("bond");
                string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, bond);
                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
