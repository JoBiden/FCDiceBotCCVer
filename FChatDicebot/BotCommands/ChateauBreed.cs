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
            LongDescription = "Breed a willing host. The host must !consent. Gestation duration varies per species. Multiple pregnancies can be carried at once. Once gestation completes, the host may give !birth at any time. Cooldowns are per breeder/bred pair - you can impregnate other people or be bred by others without waiting.";
            Usage = "!breed [noparse][user]NameInUserTag[/user][/noparse] {monster}";
            RelatedCommands = new string[] { "birth", "monsterize", "consent", "dossier" };
            CooldownDuration = "1 Day";
            CooldownAppliesTo = "both";
            IdentifierCategory = "monster";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
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

            string pairKey = BreedProcessor.PairTimerKey(recipient);
            if (initiatorProfile.timers.ContainsKey(pairKey)
                && initiatorProfile.timers[pairKey].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string remaining = Utils.GetTimeSpanPrint(initiatorProfile.timers[pairKey].timerEnd - DateTime.UtcNow);
                bot.SendPrivateMessage(
                    "You've already bred " + recipientProfile.displayName + " too recently. Please respect that 'Commitment' interactions are meant to be meaningful, and not spammed. " + recipientProfile.displayName + " will be available to bred by you again in " + remaining,
                    characterName);
                //test comment to confirm Claude sees this edit
                return;
            }

            int existingPregnancies = recipientProfile.pregnancies != null ? recipientProfile.pregnancies.Count : 0;
            string pregnancyCountText = existingPregnancies > 0
                ? " (" + recipientProfile.displayName + " is already carrying " + existingPregnancies + " other "
                    + (existingPregnancies == 1 ? "pregnancy" : "pregnancies") + ".)"
                : string.Empty;

            string message = initiatorProfile.displayName + " wants to breed " + recipientProfile.displayName
                + " with new " + monster + " life! [b]This should not be taken lightly, and can not be done frequently.[/b]"
                + pregnancyCountText
                + " Do you !consent to being bred?";

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
