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
    public class ChateauPlant : ChatBotCommand
    {
        public ChateauPlant()
        {
            Name = "plant";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string identifierType = "plant";
            string timerString = "plant";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string plant = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (plant == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                valid = false;
            }
            else if (recipientProfile.timers.ContainsKey(timerString)) { 

                if (recipientProfile.timers[timerString].timerEnd.CompareTo(DateTime.UtcNow) > 0) //recipient was plantified too recently
                {
                    string tooSoonText = "You're trying to plant " + recipientProfile.displayName + " but they were already planted! Please respect that 'Commitment' interactions are meant to be just that - a commitment. Wait a little longer for them to recover before you plant them again. \n\n"
                      + recipientProfile.displayName + " will be available to plant no sooner than " + recipientProfile.timers[timerString].timerEnd + " (the current time is " + DateTime.UtcNow + ")";
                    bot.SendPrivateMessage(tooSoonText, characterName);
                    valid = false;
                } 
            }
            if (valid)
            {
                string message = initiatorProfile.displayName + " is going to turn " + recipientProfile.displayName + " into a " + plant + "! [b]This should not be taken lightly, and can not be done frequently.[/b] Do you !consent to becoming plantlife?";

                Interaction plantInteraction = new Interaction();
                plantInteraction.initiator = characterName;
                plantInteraction.recipient = recipient;
                plantInteraction.type = timerString;
                plantInteraction.identifier = plant;
                plantInteraction.investmentLevel = "commitment";
                plantInteraction.interactionTime = DateTime.UtcNow;

                PendingCommand pendingPlant = new PendingCommand();
                pendingPlant.pendingInteraction = plantInteraction;
                pendingPlant.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingPlant);

                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
