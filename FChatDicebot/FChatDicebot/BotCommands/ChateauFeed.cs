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
            ShortDescription = "Feed another character a substance";
            LongDescription = "Feed another character a specific substance. The recipient must !consent for it to be recorded in both dossiers. This is a caring, nurturing interaction.";
            Usage = "!feed [user]CharacterName[/user] [substance]";
            RelatedCommands = new string[] { "dressup", "golden", "consent", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = "substance";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
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
