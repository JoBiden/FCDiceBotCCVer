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
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Employ another character or yourself in a job";
            LongDescription = "Employ another character in a specific job role, or employ yourself. Once employed, characters can use !work to earn currency. Employment creates a relationship between employer and employee tracked in dossiers.";
            Usage = "!employ [user]CharacterName[/user] [job]\nor\n!employ [job] (to self-employ)";
            RelatedCommands = new string[] { "work", "bank", "bond", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
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
                string message = initiatorProfile.displayName + " graciously offers to employ " + recipientProfile.displayName + " at the Chateau as their " + Utils.JobToText(job) + "! [b]This should not be taken lightly, and can not be done frequently.[/b] Do you !consent to this new career path?";

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
