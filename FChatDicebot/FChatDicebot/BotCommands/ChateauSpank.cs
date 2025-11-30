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
    public class ChateauSpank : ChatBotCommand
    {
        public ChateauSpank()
        {
            Name = "spank";
            Aliases = new string[] { };
            Category = "Casual Interaction";
            ShortDescription = "Spank another resident";
            LongDescription = "Try to punish someone with a playful spank. The recipient must !consent for it to be recorded in both dossiers. Some people use this to cook turkey!";
            Usage = "!spank [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "bully", "consent", "dossier" };
            CooldownDuration = "30 minutes (but can still interact without incrementing count)";
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
            }
            else
            {
                string message = initiatorProfile.displayName + " is about to give " + recipientProfile.displayName + " a spank. Do you !consent to that sting?";

                Interaction spank = new Interaction();
                spank.initiator = characterName;
                spank.recipient = recipient;
                spank.type = "spank";
                spank.investmentLevel = "casual";

                PendingCommand pendingSpank = new PendingCommand();
                pendingSpank.pendingInteraction = spank;
                pendingSpank.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingSpank);

                bot.SendMessageInChannel(message, channel);
            }
            
        }
    }
}
