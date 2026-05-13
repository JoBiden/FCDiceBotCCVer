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
    public class ChateauMark : ChatBotCommand
    {
        public ChateauMark()
        {
            Name = "mark";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Place your mark upon another residents's body";
            LongDescription = "Place your mark on the specified bodypart, defaulting to the midriff if no bodypart is provided. You must first put your mark on record using !setmark before you can mark others. The recipient must !consent to receiving your mark, which usually represents ownership or deep connection in some fashion.";
            Usage = "!mark [noparse][user]NameInUserTag[/user][/noparse] {bodypart}";
            RelatedCommands = new string[] { "setmark", "dossier" };
            CooldownDuration = "1 Day";
            CooldownAppliesTo = "recipient";
            IdentifierCategory = "bodypart";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string identifierType = "bodypart";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string bodypart = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (bodypart == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                valid = false;
            }
            else if (!initiatorProfile.characteristics.ContainsKey("mark")) 
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.markNotSetText(), characterName);
                valid = false;
            }
            if (valid)
            {
                string message = initiatorProfile.displayName + " is going to mark " + recipientProfile.displayName + " on their " + Utils.BodypartToText(bodypart) + "! [b]This should not be taken lightly, and can not be done frequently.[/b] Do you !consent to receiving their mark?";

                Interaction markInteraction = new Interaction();
                markInteraction.initiator = characterName;
                markInteraction.recipient = recipient;
                markInteraction.identifier = bodypart;
                markInteraction.type = "mark";
                markInteraction.investmentLevel = "commitment";
                markInteraction.interactionTime = DateTime.UtcNow;

                PendingCommand pendingMark = new PendingCommand();
                pendingMark.pendingInteraction = markInteraction;
                pendingMark.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingMark);

                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
