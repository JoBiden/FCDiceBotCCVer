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
    public class ChateauBully : ChatBotCommand
    {
        public ChateauBully()
        {
            Name = "bully";
            Aliases = new string[] { };
            Category = "Casual Interaction";
            ShortDescription = "Bully another resident (or residents)";
            LongDescription = "Bully someone in a playful way. They must !consent and thus admit that they deserve a bit of bullying. Note that the Chateau does not condone actual bullying in any way shape or form.";
            Usage = "!bully [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "spank", "consent", "dossier" };
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
                Support.CasualGroupCommandSupport.Run(bot, characterName, channel, "bully", groupTargets);
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
                string message = initiatorProfile.displayName + " is gearing up to bully " + recipientProfile.displayName + ". Do you !consent to whatever is coming?";

                Interaction bully = new Interaction();
                bully.initiator = characterName;
                bully.recipient = recipient;
                bully.type = "bully";
                bully.investmentLevel = "casual";
                bully.interactionTime = DateTime.UtcNow;

                PendingCommand pendingBully = new PendingCommand();
                pendingBully.pendingInteraction = bully;
                pendingBully.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingBully);

                bot.SendMessageInChannel(message, channel);
            }
            
        }
    }
}
