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
    public class ChateauEmploy : ChatBotCommand
    {
        public ChateauEmploy()
        {
            Name = "employ";
            Aliases = new string[] { "hire" };
            Category = "Commitment Interaction";
            ShortDescription = "Employ yourself or another resident to do jobs for the Chateau";
            LongDescription = "Employ yourself or another resident to do jobs for the Chateau. Once employed, residents can use !work once a day to earn currency and job experience. On top of working their primary job, residents can also !volunteer once a day to explore other careers.";
            Usage = "!employ [noparse][user]NameInUserTag[/user][/noparse] {job}\n(use your own username to self-employ)";
            RelatedCommands = new string[] { "work", "bank", "volunteer"};
            CooldownDuration = "1 Day";
            CooldownAppliesTo = "recipient";
            IdentifierCategory = "job";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string identifierType = "job";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string job = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (job == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                valid = false;
            }
            if (valid)
            {
                // Delegate consent wording to the processor so it stays in one place.
                var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor("employ");
                string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, job);

                Interaction employInteraction = new Interaction();
                employInteraction.initiator = characterName;
                employInteraction.recipient = recipient;
                employInteraction.identifier = job;
                employInteraction.type = "employ";
                employInteraction.investmentLevel = "commitment";
                employInteraction.interactionTime = DateTime.UtcNow;

                PendingCommand pendingObjectify = new PendingCommand();
                pendingObjectify.pendingInteraction = employInteraction;
                pendingObjectify.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingObjectify);

                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
