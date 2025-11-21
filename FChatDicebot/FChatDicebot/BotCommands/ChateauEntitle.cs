using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using FChatDicebot.InteractionProcessors;

namespace FChatDicebot.BotCommands
{
    public class ChateauEntitle : ChatBotCommand
    {
        public ChateauEntitle()
        {
            Name = "entitle";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            // Need at least a recipient and a title
            if (rawTerms.Length < 2)
            {
                bot.SendPrivateMessage("You must specify both a recipient and a title. Usage: [noparse]!entitle [user]recipient[/user] \"title in quotes\"[/noparse]", characterName);
                return;
            }

            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string titleText = commandController.GetQuotedTextFromCommandTerms(rawTerms);

            if (string.IsNullOrEmpty(titleText))
            {
                bot.SendPrivateMessage("You must specify a title in quotes. Usage: !entitle {recipient} {\"title in quotes\"}", characterName);
                return;
            }

            // Get the processor to validate
            var processor = InteractionProcessorRegistry.GetProcessor("entitle") as EntitleProcessor;
            if (processor == null)
            {
                bot.SendPrivateMessage("The entitle system is not available at this time.", characterName);
                return;
            }

            // Validate the interaction
            var validation = processor.ValidateInteraction(characterName, recipient, titleText);
            if (!validation.IsValid)
            {
                bot.SendPrivateMessage(validation.ErrorMessage, characterName);
                return;
            }

            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);

            // Create the pending interaction
            Interaction entitleInteraction = new Interaction();
            entitleInteraction.initiator = characterName;
            entitleInteraction.recipient = recipient;
            entitleInteraction.type = "entitle";
            entitleInteraction.identifier = titleText;
            entitleInteraction.investmentLevel = "commitment";

            PendingCommand pendingEntitle = new PendingCommand();
            pendingEntitle.pendingInteraction = entitleInteraction;
            pendingEntitle.awaitingConsentFrom = recipient;

            MonDB.addPendingCommand(pendingEntitle);

            // Get consent warning from processor
            string consentMessage = processor.GetConsentWarning(initiatorProfile, recipientProfile, titleText);
            bot.SendMessageInChannel(consentMessage, channel);
        }
    }
}