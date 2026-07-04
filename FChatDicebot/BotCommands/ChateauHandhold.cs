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
            ShortDescription = "Hold hands with another resident (or residents)";
            LongDescription = "Hold hands with someone. The recipient must !consent before the forbidden act takes place. Maybe you should get a room first...";
            Usage = "!handhold [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "kiss", "cuddle", "consent", "dossier" };
            CooldownDuration = "30 Minutes";
            CooldownAppliesTo = "both initiator and recipient";
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
            var groupTargets = commandController.GetUserNamesFromCommandTerms(rawTerms);
            if (groupTargets.Count > 1)
            {
                Support.CasualGroupCommandSupport.Run(bot, characterName, channel, "handhold", groupTargets);
                return;
            }

            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
            }
            else
            {
                var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor("handhold");
                string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, null);

                Interaction handhold = new Interaction();
                handhold.initiator = characterName;
                handhold.recipient = recipient;
                handhold.type = "handhold";
                handhold.investmentLevel = "casual";
                handhold.interactionTime = DateTime.UtcNow;

                PendingCommand pendingHandhold = new PendingCommand();
                pendingHandhold.pendingInteraction = handhold;
                pendingHandhold.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingHandhold);

                bot.SendMessageInChannel(message, channel);
            }
            
        }
    }
}
