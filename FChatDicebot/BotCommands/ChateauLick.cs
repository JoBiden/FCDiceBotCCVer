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
    public class ChateauLick : ChatBotCommand
    {
        public ChateauLick()
        {
            Name = "lick";
            Aliases = new string[] { };
            Category = "Casual Interaction";
            ShortDescription = "Give another resident (or residents) a lick";
            LongDescription = "Give another resident (or residents) a lick once they !consent. Whether to groom, or taste, one of the Queen's favorite acts to receive.";
            Usage = "!lick [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "boobhat", "kiss", "consent", "dossier" };
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
                Support.CasualGroupCommandSupport.Run(bot, characterName, channel, "lick", groupTargets);
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
                var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor("lick");
                string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, null);

                Interaction lick = new Interaction();
                lick.initiator = characterName;
                lick.recipient = recipient;
                lick.type = "lick";
                lick.investmentLevel = "casual";
                lick.interactionTime = DateTime.UtcNow;

                PendingCommand pendingLick = new PendingCommand();
                pendingLick.pendingInteraction = lick;
                pendingLick.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingLick);

                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
