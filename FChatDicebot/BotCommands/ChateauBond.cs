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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
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
            if (valid)
            {
                string message = initiatorProfile.displayName + " would like to declare that " + recipientProfile.displayName + " is their " + Utils.BondToText(bond, true) + "! [b]This should not be taken lightly, and can not be done frequently.[/b] Do you !consent to this new bond?";

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

                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
