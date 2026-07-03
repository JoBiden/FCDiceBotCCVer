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
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauEntitle : ChatBotCommand
    {
        public ChateauEntitle()
        {
            Name = "entitle";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Grant a custom title to another resident";
            LongDescription = "Grant someone a custom title. The recipient must !consent for the title to be added to their dossier, so you know it describes them aptly if it goes through. Be sure to capitalize appropriately! Can include bbcode as long as the total length is less than 100 characters.";
            Usage = "!entitle [noparse][user]NameInUserTag[/user][/noparse] \"Title Text\"";
            RelatedCommands = new string[] { "bond", "titles", "settitle", "dossier" };
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
            entitleInteraction.interactionTime = DateTime.UtcNow;

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