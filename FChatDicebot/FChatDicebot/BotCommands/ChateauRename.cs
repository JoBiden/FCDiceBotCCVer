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
    public class ChateauRename : ChatBotCommand
    {
        public ChateauRename()
        {
            Name = "rename";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string newName = commandController.GetQuotedTextFromCommandTerms(rawTerms);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (newName == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.needsQuotedText(), characterName);
                valid = false;
            }
            else if (recipientProfile.timers.ContainsKey("rename")) { 

                if (recipientProfile.timers["rename"].timerEnd.CompareTo(DateTime.UtcNow) > 0) //recipient was renamed too recently
                {
                    string tooSoonText = "You're trying to rename " + recipientProfile.displayName + " but they only recently received the name they have! Please respect that 'Consequence' interactions are also a Commitment. Wait a little longer before you change their name again. \n\n"
                      + recipientProfile.displayName + " will be available for a name change no sooner than " + recipientProfile.timers["rename"].timerEnd + " (the current time is " + DateTime.UtcNow + ")";
                    bot.SendPrivateMessage(tooSoonText, characterName);
                    valid = false;
                }
            } 
            else if (newName.Length > 100)
            {
                string textTooLong = "That new name is way too long! Names often need to be used multiple times in a message, and the character limit in room is only 4096. Your name can be up to 100 characters long, to leave room for tags. Please do your best to have less than 50 visible characters so the name doesn't take up more than one line on mobile. \n\nThe name you submitted was [b]" + newName.Length + "[/b] characters long.";
                bot.SendPrivateMessage(textTooLong, characterName);
                valid = false;
            }
            if (valid)
            {
                string message = initiatorProfile.displayName + " intends for " + recipientProfile.displayName + " to be known as " + newName + " from now on. [b]This should not be taken lightly, and can not be done frequently. The new name will show almost every time the Chateau mentions you.[/b] Do you !consent to this life-changing occasion?";

                Interaction rename = new Interaction();
                rename.initiator = characterName;
                rename.recipient = recipient;
                rename.type = "rename";
                rename.investmentLevel = "consequence";
                rename.interactionTime = DateTime.UtcNow;
                rename.extraParameters = new MongoDB.Bson.BsonArray
                {
                    newName
                };

                PendingCommand pendingRename = new PendingCommand();
                pendingRename.pendingInteraction = rename;
                pendingRename.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingRename);

                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
