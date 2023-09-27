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
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string identifierType = "object";
            string timerString = "objectify";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string objectType = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (objectType == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                valid = false;
            }
            else if (recipientProfile.timers.ContainsKey(timerString)) { 

                if (recipientProfile.timers[timerString].timerEnd.CompareTo(DateTime.UtcNow) > 0) //recipient was plantified too recently
                {
                    string tooSoonText = "You're trying to objectify " + recipientProfile.displayName + " but they were recently objectified! Please respect that 'Commitment' interactions are meant to be just that - a commitment. Wait a little longer for them to recover before you objectify them again. \n\n"
                      + recipientProfile.displayName + " will be available to objectify no sooner than " + recipientProfile.timers[timerString].timerEnd + " (the current time is " + DateTime.UtcNow + ")";
                    bot.SendPrivateMessage(tooSoonText, characterName);
                    valid = false;
                } 
            }
            if (valid)
            {
                string message = initiatorProfile.displayName + " is going to turn " + recipientProfile.displayName + " into some sort of " + objectType + "! [b]This should not be taken lightly, and can not be done frequently.[/b] Do you !consent to becoming an object?";

                Interaction objectifyInteraction = new Interaction();
                objectifyInteraction.initiator = characterName;
                objectifyInteraction.recipient = recipient;
                objectifyInteraction.type = timerString;
                objectifyInteraction.identifier = objectType;
                objectifyInteraction.investmentLevel = "commitment";
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
