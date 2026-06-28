using System;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauBreed : ChatBotCommand
    {
        public ChateauBreed()
        {
            Name = "breed";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Breed another resident with new monster life";
            LongDescription = "Breed a willing host. The host must !consent. Gestation duration varies per species. Multiple pregnancies can be carried at once. Once gestation completes, the host may give !birth at any time. The cooldown is per breeding direction - impregnating someone doesn't stop you breeding others, being bred by others, or even breeding that same partner back.";
            Usage = "!breed [noparse][user]NameInUserTag[/user][/noparse] {monster}";
            RelatedCommands = new string[] { "birth", "monsterize", "consent", "dossier" };
            CooldownDuration = "1 Day";
            CooldownAppliesTo = "initiator (per recipient)";
            IdentifierCategory = "monster";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string identifierType = "monster";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string monster = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);

            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                return;
            }
            if (monster == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                return;
            }

            string directionKey = BreedProcessor.DirectionTimerKey(recipient);
            if (initiatorProfile.timers.ContainsKey(directionKey)
                && initiatorProfile.timers[directionKey].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string remaining = Utils.GetTimeSpanPrint(initiatorProfile.timers[directionKey].timerEnd - DateTime.UtcNow);
                bot.SendPrivateMessage(
                    "You've already bred " + recipientProfile.displayName + " too recently. Please respect that 'Commitment' interactions are meant to be meaningful, and not spammed. You'll be able to breed " + recipientProfile.displayName + " again in " + remaining,
                    characterName);
                return;
            }

            // Delegate consent wording (including the existing-pregnancy note) to the processor
            // so it stays in one place.
            var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor("breed");
            string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, monster);

            Interaction breedInteraction = new Interaction
            {
                initiator = characterName,
                recipient = recipient,
                type = "breed",
                identifier = monster,
                investmentLevel = "commitment",
                interactionTime = DateTime.UtcNow
            };

            PendingCommand pendingBreed = new PendingCommand
            {
                pendingInteraction = breedInteraction,
                awaitingConsentFrom = recipient
            };

            MonDB.addPendingCommand(pendingBreed);
            bot.SendMessageInChannel(message, channel);
        }
    }
}
