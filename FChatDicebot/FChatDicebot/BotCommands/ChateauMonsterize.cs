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
    public class ChateauMonsterize : ChatBotCommand
    {
        public ChateauMonsterize()
        {
            Name = "monsterize";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string monsterType = commandController.GetIdentifierFromCommandTerms(rawTerms, "monster");
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (monsterType == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText("monster"), characterName);
                valid = false;
            }
            else if (recipientProfile.timers.ContainsKey("monsterize")) { 

                if (recipientProfile.timers["monsterize"].timerEnd.CompareTo(DateTime.UtcNow) > 0) //recipient was monsterized too recently
                {
                    string tooSoonText = "You're trying to monsterize " + recipientProfile.displayName + " but they only recently changed to the monster they currently are! Please respect that 'Commitment' interactions are meant to be just that - a commitment. Wait a little longer before you change their form again. \n\n"
                      + recipientProfile.displayName + " will be available to monsterize no sooner than " + recipientProfile.timers["monsterize"].timerEnd + " (the current time is " + DateTime.UtcNow + ")";
                    bot.SendPrivateMessage(tooSoonText, characterName);
                    valid = false;
                } 
            }
            if (valid)
            {
                string message = initiatorProfile.displayName + " is going to transform " + recipientProfile.displayName + " into a " + monsterType + "! [b]This should not be taken lightly, and can not be done frequently.[/b] Do you !consent to your new form?";

                Interaction monsterize = new Interaction();
                monsterize.initiator = characterName;
                monsterize.recipient = recipient;
                monsterize.type = "monsterize";
                monsterize.identifier = monsterType;
                monsterize.investmentLevel = "commitment";
                monsterize.interactionTime = DateTime.UtcNow;

                PendingCommand pendingMonsterize = new PendingCommand();
                pendingMonsterize.pendingInteraction = monsterize;
                pendingMonsterize.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingMonsterize);

                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
