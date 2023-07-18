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
    public class ChateauSpank : ChatBotCommand
    {
        public ChateauSpank()
        {
            Name = "spank";
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
                string message = initiatorProfile.displayName + " is about to give " + recipientProfile.displayName + " a spank. Do you !consent to that sting?";

                Interaction spank = new Interaction();
                spank.initiator = characterName;
                spank.recipient = recipient;
                spank.type = "spank";
                spank.investmentLevel = "casual";

                PendingCommand pendingSpank = new PendingCommand();
                pendingSpank.pendingInteraction = spank;
                pendingSpank.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingSpank);

                bot.SendMessageInChannel(message, channel);
            }
            
        }
    }
}
