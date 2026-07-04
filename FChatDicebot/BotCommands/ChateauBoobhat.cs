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
    public class ChateauBoobhat : ChatBotCommand
    {
        public ChateauBoobhat()
        {
            Name = "boobhat";
            Aliases = new string[] { };
            Category = "Casual Interaction";
            ShortDescription = "Put your chest on another resident's (or residents') head";
            LongDescription = "Rest your chest atop another resident's (or residents') head like a hat, as soon as they !consent. Intended for those with a proper set of boobs, but no one is stopping you from doing it with your pecs instead.";
            Usage = "!boobhat [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "lick", "cuddle", "consent", "dossier" };
            CooldownDuration = "30 minutes (but can still interact without incrementing count)";
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
                Support.CasualGroupCommandSupport.Run(bot, characterName, channel, "boobhat", groupTargets);
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
                var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor("boobhat");
                string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, null);

                Interaction boobhat = new Interaction();
                boobhat.initiator = characterName;
                boobhat.recipient = recipient;
                boobhat.type = "boobhat";
                boobhat.investmentLevel = "casual";
                boobhat.interactionTime = DateTime.UtcNow;

                PendingCommand pendingBoobhat = new PendingCommand();
                pendingBoobhat.pendingInteraction = boobhat;
                pendingBoobhat.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingBoobhat);

                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
