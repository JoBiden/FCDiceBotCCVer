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
    public class ChateauPetrify : ChatBotCommand
    {
        public ChateauPetrify()
        {
            Name = "petrify";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Turn another resident into a statue";
            LongDescription = "Turn another character into a statue, specifying where they will spend their stony fate. The recipient must !consent to such a rocky outcome. Don't worry - our maids are very attentive to the decor, whether it was once animate or not.";
            Usage = "!petrify [noparse][user]NameInUserTag[/user][/noparse] {location}";
            RelatedCommands = new string[] { "statues", "plant", "objectify", "consent", "dossier" };
            CooldownDuration = "7 days";
            CooldownAppliesTo = "recipient";
            IdentifierCategory = "location";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string identifierType = "location";
            string timerString = "petrify";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string location = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                valid = false;
            } 
            else if (location == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                valid = false;
            }
            else if (recipientProfile.timers.ContainsKey(timerString)) { 

                if (recipientProfile.timers[timerString].timerEnd.CompareTo(DateTime.UtcNow) > 0) //recipient was petrified too recently
                {
                    string tooSoonText = "You're trying to petrify " + recipientProfile.displayName + " but they were already petrified! Please respect that 'Commitment' interactions are meant to be just that - a commitment. Wait a little longer for them to reanimate before you petrify them again. \n\n"
                      + recipientProfile.displayName + " will be available to petrify in " + Utils.GetTimeSpanPrint(recipientProfile.timers["petrify"].timerEnd - DateTime.UtcNow);
                    bot.SendPrivateMessage(tooSoonText, characterName);
                    valid = false;
                } 
            }
            if (valid)
            {
                // Delegate consent wording to the processor so it stays in one place.
                var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor("petrify");
                string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, location);

                Interaction petrify = new Interaction();
                petrify.initiator = characterName;
                petrify.recipient = recipient;
                petrify.type = timerString;
                petrify.identifier = location;
                petrify.investmentLevel = "commitment";
                petrify.interactionTime = DateTime.UtcNow;

                PendingCommand pendingPetrify = new PendingCommand();
                pendingPetrify.pendingInteraction = petrify;
                pendingPetrify.awaitingConsentFrom = recipient;

                MonDB.addPendingCommand(pendingPetrify);

                bot.SendMessageInChannel(message, channel);
            }
        }
    }
}
