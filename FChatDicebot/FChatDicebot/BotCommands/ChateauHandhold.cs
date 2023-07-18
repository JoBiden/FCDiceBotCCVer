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

namespace FChatDicebot.BotCommands
{
    public class ChateauHandhold : ChatBotCommand
    {
        public ChateauHandhold()
        {
            Name = "handhold";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
            }
            else
            {
                string message = initiatorProfile.displayName + " wants to hold your hand (or equivalent appendage) " + recipientProfile.displayName + ". Do you !consent to some handholding?";

                Interaction handhold = new Interaction();
                handhold.initiator = characterName;
                handhold.recipient = recipient;
                handhold.type = "handhold";
                handhold.investmentLevel = "casual";

                PendingCommand pendingHandhold = new PendingCommand();
                pendingHandhold.pendingInteraction = handhold;
                pendingHandhold.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingHandhold);

                bot.SendMessageInChannel(message, channel);
            }
            
        }
    }
}
