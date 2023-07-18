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
    public class ChateauBully : ChatBotCommand
    {
        public ChateauBully()
        {
            Name = "bully";
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
                string message = initiatorProfile.displayName + " is gearing up to bully " + recipientProfile.displayName + ". Do you !consent to whatever is coming?";

                Interaction bully = new Interaction();
                bully.initiator = characterName;
                bully.recipient = recipient;
                bully.type = "bully";
                bully.investmentLevel = "casual";

                PendingCommand pendingBully = new PendingCommand();
                pendingBully.pendingInteraction = bully;
                pendingBully.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingBully);

                bot.SendMessageInChannel(message, channel);
            }
            
        }
    }
}
