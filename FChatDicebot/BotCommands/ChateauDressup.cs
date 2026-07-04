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
    public class ChateauDressup : ChatBotCommand
    {
        public ChateauDressup()
        {
            Name = "dressup";
            Aliases = new string[] { "dress" };
            Category = "Involved Interaction";
            ShortDescription = "Dress another resident, or yourself, in specific attire";
            LongDescription = "Dress someone up in specific attire. The recipient must !consent for their outfit to actually change. Some jobs might have unique outcomes only available based on your attire!";
            Usage = "!dressup [noparse][user]NameInUserTag[/user][/noparse] {attire}\n(use your own username to dress yourself)";
            RelatedCommands = new string[] { "volunteer", "work", "dossier" };
            CooldownDuration = "30 Minutes";
            CooldownAppliesTo = "both initiator and recipient";
            IdentifierCategory = "attire";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string identifierType = "attire";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string attire = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (attire == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                valid = false;
            }
            if (valid)
            {
                var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor("dressup");
                string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, attire);

                Interaction objectifyInteraction = new Interaction();
                objectifyInteraction.initiator = characterName;
                objectifyInteraction.recipient = recipient;
                objectifyInteraction.identifier = attire;
                objectifyInteraction.type = "dressup";
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
