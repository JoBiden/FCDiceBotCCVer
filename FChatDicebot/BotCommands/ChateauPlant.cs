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
    public class ChateauPlant : ChatBotCommand
    {
        public ChateauPlant()
        {
            Name = "plant";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Transform another resident into a plant";
            LongDescription = "Transform someone into a specific type of plant. The recipient must !consent to their botanical fate. It's not easy being green...";
            Usage = "!plant [noparse][user]NameInUserTag[/user][/noparse] {plant}";
            RelatedCommands = new string[] { "monsterize", "petrify", "objectify", "consent", "dossier" };
            CooldownDuration = "1 Day";
            CooldownAppliesTo = "recipient";
            IdentifierCategory = "plant";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string identifierType = "plant";
            string timerString = "plant";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string plant = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (plant == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                valid = false;
            }
            else if (recipientProfile.timers.ContainsKey(timerString)) { 

                if (recipientProfile.timers[timerString].timerEnd.CompareTo(DateTime.UtcNow) > 0) //recipient was plantified too recently
                {
                    string tooSoonText = "You're trying to plant " + recipientProfile.displayName + " but they were already planted! Please respect that 'Commitment' interactions are meant to be just that - a commitment. Wait a little longer for them to recover before you plant them again. \n\n"
                      + recipientProfile.displayName + " will be available to plant in " + Utils.GetTimeSpanPrint(recipientProfile.timers["plant"].timerEnd - DateTime.UtcNow);
                    bot.SendPrivateMessage(tooSoonText, characterName);
                    valid = false;
                } 
            }
            if (valid)
            {
                // Delegate consent wording to the processor so it stays in one place.
                var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor("plant");
                string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, plant);

                Interaction plantInteraction = new Interaction();
                plantInteraction.initiator = characterName;
                plantInteraction.recipient = recipient;
                plantInteraction.type = timerString;
                plantInteraction.identifier = plant;
                plantInteraction.investmentLevel = "commitment";
                plantInteraction.interactionTime = DateTime.UtcNow;

                PendingCommand pendingPlant = new PendingCommand();
                pendingPlant.pendingInteraction = plantInteraction;
                pendingPlant.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingPlant);

                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
