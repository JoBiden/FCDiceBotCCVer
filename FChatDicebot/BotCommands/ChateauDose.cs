using System;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !dose hooks the recipient on an addictive vice. Mirrors !odorize's command shape — a
    /// consent-driven Consequence interaction with a per-(initiator, recipient, vice) 7-day
    /// cooldown on the initiator's timers. Self-dose is explicitly allowed.
    /// </summary>
    public class ChateauDose : ChatBotCommand
    {
        public ChateauDose()
        {
            Name = "dose";
            Aliases = new string[] { };
            Category = "Consequence Interaction";
            ShortDescription = "Hook another resident on an addictive vice";
            LongDescription = "Administer a large enough dose of an addictive vice to get a resident addicted, or deepen their existing addiction. Once addicted, their cravings will be notable even in unrelated interactions. When their craving is satisfied, everyone will know. Requires !consent.";
            Usage = "!dose [noparse][user]NameInUserTag[/user][/noparse] {vice}";
            RelatedCommands = new string[] { "detox", "consent", "dossier" };
            CooldownDuration = "7 days";
            CooldownAppliesTo = "initiator (per vice per recipient)";
            IdentifierCategory = "vice";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string identifierType = DoseProcessor.ViceCategory;
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string vice = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);

            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                return;
            }
            if (vice == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                return;
            }

            string cooldownKey = DoseProcessor.CooldownTimerKey(vice, recipient);
            if (initiatorProfile.timers.ContainsKey(cooldownKey)
                && initiatorProfile.timers[cooldownKey].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string remaining = Utils.GetTimeSpanPrint(initiatorProfile.timers[cooldownKey].timerEnd - DateTime.UtcNow);
                Identifier viceIdentifier = MonDB.GetDatabase().GetIdentifier(vice);
                string vicePhrase = ViceText.ViceName(viceIdentifier, vice, initiatorProfile.displayName);
                bot.SendPrivateMessage(
                    "You've already dosed " + recipientProfile.displayName + " with " + vicePhrase + " too recently. Please respect that 'Consequence' interactions are meant to be meaningful, and not spammed. You'll be able to dose " + recipientProfile.displayName + " with " + vicePhrase + " again in " + remaining + ".",
                    characterName);
                return;
            }

            Interaction dose = new Interaction
            {
                initiator = characterName,
                recipient = recipient,
                type = DoseProcessor.DoseType,
                identifier = vice,
                investmentLevel = "consequence",
                interactionTime = DateTime.UtcNow
            };

            PendingCommand pendingDose = new PendingCommand
            {
                pendingInteraction = dose,
                awaitingConsentFrom = recipient
            };

            MonDB.addPendingCommand(pendingDose);

            var processor = InteractionProcessorRegistry.GetProcessor(DoseProcessor.DoseType);
            string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, vice);
            bot.SendMessageInChannel(message, channel);
        }
    }
}
