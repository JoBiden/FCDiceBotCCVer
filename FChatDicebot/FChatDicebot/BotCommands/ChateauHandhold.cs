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
            Aliases = new string[] { };
            Category = "Casual Interaction";
            ShortDescription = "Request to hold hands with another character";
            LongDescription = "Request to hold hands with another character. The recipient must !consent for it to be recorded in both dossiers. A sweet, innocent gesture of affection.";
            Usage = "!handhold [user]CharacterName[/user]";
            RelatedCommands = new string[] { "kiss", "cuddle", "consent", "dossier" };
            CooldownDuration = "30 minutes";
            CooldownAppliesTo = "initiator";
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
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
