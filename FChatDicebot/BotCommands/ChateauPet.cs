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
    public class ChateauPet : ChatBotCommand
    {
        public ChateauPet()
        {
            Name = "pet";
            Aliases = new string[] { };
            Category = "Casual Interaction";
            ShortDescription = "Pet another resident (or residents)";
            LongDescription = "Give another resident (or residents) some pets once they !consent. A gentle bit of headpats and scritches. With !seteicon pet, the icon that shows is the one being petted (set your own 'being petted' eicon so onlookers see it whenever someone pets you).";
            Usage = "!pet [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "cuddle", "lick", "consent", "dossier" };
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
                Support.CasualGroupCommandSupport.Run(bot, characterName, channel, "pet", groupTargets);
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
                var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor("pet");
                string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, null);

                Interaction pet = new Interaction();
                pet.initiator = characterName;
                pet.recipient = recipient;
                pet.type = "pet";
                pet.investmentLevel = "casual";
                pet.interactionTime = DateTime.UtcNow;

                PendingCommand pendingPet = new PendingCommand();
                pendingPet.pendingInteraction = pet;
                pendingPet.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingPet);

                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
