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
            Aliases = new string[] { };
            Category = "Casual Interaction";
            ShortDescription = "Request to cuddle with another character";
            LongDescription = "Request to cuddle with another character. The recipient must !consent for it to be recorded in both dossiers. A warm, affectionate interaction.";
            Usage = "!cuddle [user]CharacterName[/user]";
            RelatedCommands = new string[] { "kiss", "handhold", "consent", "dossier" };
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
