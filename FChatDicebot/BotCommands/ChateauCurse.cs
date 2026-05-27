using System;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !curse applies a consequence-tier curse from the curse catalog to the recipient.
    /// The curse then surfaces through CurseStatusContributor on subsequent interactions
    /// (disablers block, modifiers add completion flavor). !cleanse is the self-targeted
    /// reversal, with a per-curse cost.
    ///
    /// Mirrors !infest's command shape: consent-driven, per-(initiator, recipient, curse)
    /// 7-day cooldown on the initiator's timers.
    /// </summary>
    public class ChateauCurse : ChatBotCommand
    {
        public ChateauCurse()
        {
            Name = "curse";
            Aliases = new string[] { };
            Category = "Consequence Interaction";
            ShortDescription = "Place a curse on someone — a disabler or a modifier.";
            LongDescription = "Apply a curse to another resident. Requires !consent. The recipient can remove the curse with !cleanse, at a per-curse cost. See !whatis {curse} for the details of specific curses.";
            Usage = "!curse [noparse][user]NameInUserTag[/user][/noparse] {curse}";
            RelatedCommands = new string[] { "cleanse", "consent", "dossier" };
            CooldownDuration = "7 days";
            CooldownAppliesTo = "initiator (per curse per recipient)";
            IdentifierCategory = CurseProcessor.CurseCategory;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string identifierType = CurseProcessor.CurseCategory;
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string curseName = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);

            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                return;
            }
            if (curseName == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                return;
            }

            string cooldownKey = CurseProcessor.CooldownTimerKey(curseName, recipient);
            if (initiatorProfile.timers.ContainsKey(cooldownKey)
                && initiatorProfile.timers[cooldownKey].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string remaining = Utils.GetTimeSpanPrint(initiatorProfile.timers[cooldownKey].timerEnd - DateTime.UtcNow);
                bot.SendPrivateMessage(
                    "You've already cursed " + recipientProfile.displayName + " with [b]" + curseName + "[/b] "
                    + "too recently. Please respect that 'Consequence' interactions are meant to be meaningful, and not spammed. "
                    + "You'll be able to curse " + recipientProfile.displayName + " with [b]" + curseName + "[/b] again in " + remaining + ".",
                    characterName);
                return;
            }

            Interaction curse = new Interaction
            {
                initiator = characterName,
                recipient = recipient,
                type = CurseProcessor.CurseType,
                identifier = curseName,
                investmentLevel = "consequence",
                interactionTime = DateTime.UtcNow
            };

            PendingCommand pendingCurse = new PendingCommand
            {
                pendingInteraction = curse,
                awaitingConsentFrom = recipient
            };

            MonDB.addPendingCommand(pendingCurse);

            var processor = InteractionProcessorRegistry.GetProcessor(CurseProcessor.CurseType);
            string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, curseName);
            bot.SendMessageInChannel(message, channel);
        }
    }
}
