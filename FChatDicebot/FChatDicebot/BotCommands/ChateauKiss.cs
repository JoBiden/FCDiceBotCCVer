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
    public class ChateauKiss : ChatBotCommand
    {
        public ChateauKiss()
        {
            Name = "kiss";
            Aliases = new string[] { };
            Category = "Casual Interaction";
            ShortDescription = "Request to give another character a kiss";
            LongDescription = "Request to give another character a kiss. The recipient must !consent to the kiss for it to be recorded in both of your dossiers. This is a sweet, affectionate gesture that builds your casual interaction count.";
            Usage = "!kiss [user]CharacterName[/user]";
            RelatedCommands = new string[] { "cuddle", "handhold", "consent", "dossier" };
            CooldownDuration = "30 minutes";
            CooldownAppliesTo = "initiator";
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
                string message = initiatorProfile.displayName + " wants to give you a kiss, " + recipientProfile.displayName + ". Do you !consent to a smooch?";

                Interaction kiss = new Interaction();
                kiss.initiator = characterName;
                kiss.recipient = recipient;
                kiss.type = "kiss";
                kiss.investmentLevel = "casual";

                PendingCommand pendingKiss = new PendingCommand();
                pendingKiss.pendingInteraction = kiss;
                pendingKiss.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingKiss);

                bot.SendMessageInChannel(message, channel);
            }
            
        }
    }
}
