using System;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !infest hands the recipient a parasite that has a chance to spread on every
    /// non-casual interaction they're part of. Mirrors !dose's command shape — a
    /// consent-driven Consequence interaction with a per-(initiator, recipient, parasite)
    /// 7-day cooldown on the initiator's timers. !purge is the self-targeted reversal,
    /// free during the 24-hour spread grace window and costed otherwise.
    /// </summary>
    public class ChateauInfest : ChatBotCommand
    {
        public ChateauInfest()
        {
            Name = "infest";
            Aliases = new string[] { };
            Category = "Consequence Interaction";
            ShortDescription = "Infest someone with new parasitic life.";
            LongDescription = "Give someone a parasite to host and care for. This parasite has a chance of spreading on its own during any non-casual interaction, but can be removed without complication if they !purge within 24 hours. Otherwise, !purge will come with a cost (see !purge).";
            Usage = "!infest [noparse][user]NameInUserTag[/user][/noparse] {parasite}";
            RelatedCommands = new string[] { "purge", "consent", "dossier" };
            CooldownDuration = "7 days";
            CooldownAppliesTo = "initiator (per parasite per recipient)";
            IdentifierCategory = InfestProcessor.ParasiteCategory;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string identifierType = InfestProcessor.ParasiteCategory;
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string parasite = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);

            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                return;
            }
            if (parasite == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                return;
            }

            string cooldownKey = InfestProcessor.CooldownTimerKey(parasite, recipient);
            if (initiatorProfile.timers.ContainsKey(cooldownKey)
                && initiatorProfile.timers[cooldownKey].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string remaining = Utils.GetTimeSpanPrint(initiatorProfile.timers[cooldownKey].timerEnd - DateTime.UtcNow);
                string parasitePhrase = ParasiteText.ParasiteName(parasite);
                bot.SendPrivateMessage(
                    "You've already infested " + recipientProfile.displayName + " with " + parasitePhrase
                    + " too recently. Please respect that 'Consequence' interactions are meant to be meaningful, and not spammed. You'll be able to infest "
                    + recipientProfile.displayName + " with " + parasitePhrase + " again in " + remaining + ".",
                    characterName);
                return;
            }

            Interaction infest = new Interaction
            {
                initiator = characterName,
                recipient = recipient,
                type = InfestProcessor.InfestType,
                identifier = parasite,
                investmentLevel = "consequence",
                interactionTime = DateTime.UtcNow
            };

            PendingCommand pendingInfest = new PendingCommand
            {
                pendingInteraction = infest,
                awaitingConsentFrom = recipient
            };

            MonDB.addPendingCommand(pendingInfest);

            var processor = InteractionProcessorRegistry.GetProcessor(InfestProcessor.InfestType);
            string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, parasite);
            bot.SendMessageInChannel(message, channel);
        }
    }
}
