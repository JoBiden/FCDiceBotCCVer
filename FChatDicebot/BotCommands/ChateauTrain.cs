using System;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauTrain : ChatBotCommand
    {
        public ChateauTrain()
        {
            Name = "train";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Train with another resident in a skill";
            LongDescription = "Train with a partner in a special technique or skill. You can always train to level 10, but to get to new heights, you'll need to train with someone more skilled than you or close to your skill level (no more than 10 levels lower). Cooldowns are per pair - you can train with any individual once per day, but can train with as many people as you like.";
            Usage = "!train [noparse][user]NameInUserTag[/user][/noparse] {training}";
            RelatedCommands = new string[] { "consent", "dossier", "work", "category" };
            CooldownDuration = "1 Day";
            CooldownAppliesTo = "both";
            IdentifierCategory = "training";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string identifierType = "training";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string training = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);

            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                return;
            }
            if (training == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                return;
            }

            // Solo training isn't supported — the spec is explicit that this is a
            // mutual practice session, not a self-action.
            if (string.Equals(characterName, recipient, StringComparison.OrdinalIgnoreCase))
            {
                bot.SendPrivateMessage("You can't train with yourself. Find someone to train with!", characterName);
                return;
            }

            // Symmetric daily pair-lock across all training types. Checking the
            // initiator's side is sufficient because ProcessInteraction sets the timer
            // on both profiles (mirrors the BreedProcessor pattern).
            string pairKey = TrainProcessor.PairTimerKey(recipient);
            if (initiatorProfile.timers.ContainsKey(pairKey)
                && initiatorProfile.timers[pairKey].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string remaining = Utils.GetTimeSpanPrint(initiatorProfile.timers[pairKey].timerEnd - DateTime.UtcNow);
                bot.SendPrivateMessage(
                    "You and " + recipientProfile.displayName + " have already trained together today. You'll be able to train together again in " + remaining + ".",
                    characterName);
                return;
            }

            string message = initiatorProfile.displayName + " wants to do some " + training + " training with "
                + recipientProfile.displayName + "! Do you !consent to improving your skills together?";

            Interaction trainInteraction = new Interaction
            {
                initiator = characterName,
                recipient = recipient,
                identifier = training,
                type = "train",
                investmentLevel = "commitment",
                interactionTime = DateTime.UtcNow,
            };

            PendingCommand pendingTrain = new PendingCommand
            {
                pendingInteraction = trainInteraction,
                awaitingConsentFrom = recipient,
            };

            MonDB.addPendingCommand(pendingTrain);
            bot.SendMessageInChannel(message, channel);
        }
    }
}
