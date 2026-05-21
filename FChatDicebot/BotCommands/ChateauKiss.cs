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
            ShortDescription = "Give another resident a kiss";
            LongDescription = "Give another resident a kiss, as soon as they !consent. This is a sweet, affectionate gesture often used as a greeting.";
            Usage = "!kiss [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "cuddle", "handhold", "consent", "dossier" };
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
                return;
            }

            Interaction kiss = new Interaction();
            kiss.initiator = characterName;
            kiss.recipient = recipient;
            kiss.type = "kiss";
            kiss.investmentLevel = "casual";

            PendingCommand pendingKiss = new PendingCommand();
            pendingKiss.pendingInteraction = kiss;
            pendingKiss.awaitingConsentFrom = recipient;

            MonDB.addPendingCommand(pendingKiss);

            // Delegate consent wording to the processor so it stays in one place.
            var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor("kiss");
            string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, "");
            bot.SendMessageInChannel(message, channel);
        }
    }
}
