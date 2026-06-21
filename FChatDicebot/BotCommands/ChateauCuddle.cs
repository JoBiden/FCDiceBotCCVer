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
    public class ChateauCuddle : ChatBotCommand
    {
        public ChateauCuddle()
        {
            Name = "cuddle";
            Aliases = new string[] { "hug" };
            Category = "Casual Interaction";
            ShortDescription = "Cuddle with another resident";
            LongDescription = "Cuddle with another character as soon as they !consent. Whether it's spooning or a warm embrace, this is a favorite past time of Chateau residents.";
            Usage = "!cuddle [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "kiss", "handhold", "consent", "dossier" };
            CooldownDuration = "30 Minutes";
            CooldownAppliesTo = "both initiator and recipient";
            IdentifierCategory = null;
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
                string message = initiatorProfile.displayName + " wants to cuddle you, " + recipientProfile.displayName + ". Do you !consent to some cuddles?";

                Interaction cuddle = new Interaction();
                cuddle.initiator = characterName;
                cuddle.recipient = recipient;
                cuddle.type = "cuddle";
                cuddle.investmentLevel = "casual";

                PendingCommand pendingCuddle = new PendingCommand();
                pendingCuddle.pendingInteraction = cuddle;
                pendingCuddle.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingCuddle);

                bot.SendMessageInChannel(message, channel);
            }
            
        }
    }
}
