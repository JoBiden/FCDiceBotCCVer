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
    public class ChateauObjectify : ChatBotCommand
    {
        public ChateauObjectify()
        {
            Name = "objectify";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Transform someone into an object";
            LongDescription = "Turn another resident into an inanimate object. They have to consent to the sudden loss of personhood, even if it's the last thing they do.";
            Usage = "!objectify [noparse][user]NameInUserTag[/user][/noparse] {object}";
            RelatedCommands = new string[] { "monsterize", "petrify", "plant" };
            CooldownDuration = "1 Day";
            CooldownAppliesTo = "recipient";
            IdentifierCategory = "object";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string identifierType = "object";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string objectType = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile initiatorProfile = MonDB.getProfile(characterName);

            // Object-identifier pre-check stays at the command level so its error wording
            // matches the user-facing "object" type label (the processor's own missing-
            // identifier failure uses the same text via typeNotFoundText, but the command
            // checks first to avoid even attempting processor lookup with a missing type).
            if (objectType == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                return;
            }

            // ObjectifyProcessor's namespace is Consequence (the file lives under
            // InteractionProcessors/Commitment but the namespace declaration predates
            // the folder reshuffle) — using the actual declared namespace here.
            var processor = (InteractionProcessors.Consequence.ObjectifyProcessor)
                InteractionProcessors.InteractionProcessorRegistry.GetProcessor("objectify");
            var validation = processor.ValidateInteraction(characterName, recipient, objectType);
            if (!validation.IsValid)
            {
                bot.SendPrivateMessage(validation.ErrorMessage, characterName);
                return;
            }

            Profile recipientProfile = MonDB.getProfile(recipient);

            Interaction objectifyInteraction = new Interaction();
            objectifyInteraction.initiator = characterName;
            objectifyInteraction.recipient = recipient;
            objectifyInteraction.type = "objectify";
            objectifyInteraction.identifier = objectType;
            objectifyInteraction.investmentLevel = "commitment";
            objectifyInteraction.interactionTime = DateTime.UtcNow;

            PendingCommand pendingObjectify = new PendingCommand();
            pendingObjectify.pendingInteraction = objectifyInteraction;
            pendingObjectify.awaitingConsentFrom = recipient;

            MonDB.addPendingCommand(pendingObjectify);

            // Delegate consent wording to the processor so it stays in one place.
            string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, objectType);
            bot.SendMessageInChannel(message, channel);
        }
    }
}
