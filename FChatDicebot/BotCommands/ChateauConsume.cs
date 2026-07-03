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
    public class ChateauConsume : ChatBotCommand
    {
        public ChateauConsume()
        {
            Name = "consume";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Consume/devour another resident";
            LongDescription = "Consume or devour someone in whole or in part, whether that's their soul, their body, or their mind. The recipient must !consent before surrendering to their fate. It's up to you whether this is a permanent fate or something that can be recovered from.";
            Usage = "!consume [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "mark", "bond"};
            CooldownDuration = "1 Day";
            CooldownAppliesTo = "recipient";
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string timerString = "consume";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (recipientProfile.timers.ContainsKey(timerString)) { 

                if (recipientProfile.timers[timerString].timerEnd.CompareTo(DateTime.UtcNow) > 0) //recipient was plantified too recently
                {
                    string tooSoonText = "You're trying to consume " + recipientProfile.displayName + " but they were recently consumed! Please respect that 'Commitment' interactions are meant to be just that - a commitment. Wait a little longer for them to recover before you consume them again. \n\n"
                      + recipientProfile.displayName + " will be available to consume in " + Utils.GetTimeSpanPrint(recipientProfile.timers[timerString].timerEnd - DateTime.UtcNow);
                    bot.SendPrivateMessage(tooSoonText, characterName);
                    valid = false;
                } 
            }
            if (valid)
            {
                // Delegate consent wording to the processor so it stays in one place.
                var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor("consume");
                string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, null);

                Interaction objectifyInteraction = new Interaction();
                objectifyInteraction.initiator = characterName;
                objectifyInteraction.recipient = recipient;
                objectifyInteraction.type = timerString;
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
