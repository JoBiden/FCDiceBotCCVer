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
    public class ChateauFeed : ChatBotCommand
    {
        public ChateauFeed()
        {
            Name = "feed";
            Aliases = new string[] { };
            Category = "Involved Interaction";
            ShortDescription = "Feed another resident";
            LongDescription = "Feed someone a specific substance, which could be something normal like food or something wild like energy. Recipient must !consent, so be sure you've picked something they enjoy!";
            Usage = "!feed [noparse][user]NameInUserTag[/user][/noparse] {substance}";
            RelatedCommands = new string[] { "dressup", "golden", "consent", "dossier" };
            CooldownDuration = "30 minutes";
            CooldownAppliesTo = "both initiator and recipient";
            IdentifierCategory = "substance";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string identifierType = "substance";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string substance = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (substance == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                valid = false;
            }
            if (valid)
            {
                string message = initiatorProfile.displayName + " is going to feed " + recipientProfile.displayName + " some " + Utils.SubstanceToText(substance) + "! Do you !consent to consuming that?";

                Interaction objectifyInteraction = new Interaction();
                objectifyInteraction.initiator = characterName;
                objectifyInteraction.recipient = recipient;
                objectifyInteraction.identifier = substance;
                objectifyInteraction.type = "feed";
                objectifyInteraction.investmentLevel = "involved";
                objectifyInteraction.interactionTime = DateTime.UtcNow;

                PendingCommand pendingObjectify = new PendingCommand();
                pendingObjectify.pendingInteraction = objectifyInteraction;
                pendingObjectify.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingObjectify);

                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
