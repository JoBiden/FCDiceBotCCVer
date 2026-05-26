using System;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !break — Consequence-tier interaction. Breaks a recipient's bodypart for a number
    /// of Chateau days, after which it gates or flavors interactions that depend on it.
    /// The recipient can !rest to heal one extra level per day in exchange for that day's
    /// !work.
    /// </summary>
    public class ChateauBreak : ChatBotCommand
    {
        public ChateauBreak()
        {
            Name = "break";
            Aliases = new string[] { };
            Category = "Consequence Interaction";
            ShortDescription = "Break a bodypart of another resident for several days";
            LongDescription = "Break one of another resident's bodyparts for a number of Chateau days. They must !consent. While broken, the part will block or flavor interactions that depend on it until it is fully recovered. The recipient can !rest to skip a day's !work and accelerate healing by a day. Defaults to 3 days, with a maximum of 30.";
            Usage = "!break [noparse][user]NameInUserTag[/user][/noparse] {bodypart} {days?}";
            RelatedCommands = new string[] { "rest", "consent", "dossier" };
            CooldownDuration = "7 days";
            CooldownAppliesTo = "initiator (per bodypart per recipient)";
            IdentifierCategory = "break";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string identifierType = "break";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string part = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);

            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                return;
            }
            if (part == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                return;
            }

            // Days argument — optional, default 3, clamped to [1, 30].
            string[] rawInts = commandController.GetIntsFromCommandTermsAsStrings(rawTerms);
            int requestedDays = BreakProcessor.DefaultDays;
            BreakProcessor.ClampDirection clampDir = BreakProcessor.ClampDirection.None;
            if (rawInts != null && rawInts.Length > 0)
            {
                int parsed;
                if (!Int32.TryParse(rawInts[0], out parsed))
                {
                    bot.SendPrivateMessage("The duration must be a valid whole number of days.", characterName);
                    return;
                }
                if (parsed < BreakProcessor.MinDays)
                {
                    requestedDays = BreakProcessor.MinDays;
                    clampDir = BreakProcessor.ClampDirection.Min;
                }
                else if (parsed > BreakProcessor.MaxDays)
                {
                    requestedDays = BreakProcessor.MaxDays;
                    clampDir = BreakProcessor.ClampDirection.Max;
                }
                else
                {
                    requestedDays = parsed;
                }
            }

            // Already-broken-with-more-time check uses the live tick so a stale long break
            // that has decayed under the new request doesn't block legitimately escalating
            // the duration.
            var currentBreaks = BreakInstance.LoadAllWithTick(recipientProfile);
            foreach (var entry in currentBreaks)
            {
                if (string.Equals(entry.Part, part, StringComparison.OrdinalIgnoreCase) && entry.Severity >= requestedDays)
                {
                    string daysWord = entry.Severity == 1 ? "day" : "days";
                    bot.SendPrivateMessage(
                        recipientProfile.displayName + "'s " + part + " is already broken for " + entry.Severity + " more " + daysWord + ". Try a longer duration or pick a different bodypart.",
                        characterName);
                    return;
                }
            }

            // Initiator-side per-(part, recipient) cooldown — mirrors odorize's per-scent
            // pattern so different initiators can pile on but a single initiator can't
            // hammer the same part on the same recipient repeatedly.
            string cooldownKey = BreakProcessor.CooldownTimerKey(part, recipient);
            if (initiatorProfile.timers.ContainsKey(cooldownKey)
                && initiatorProfile.timers[cooldownKey].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string remaining = Utils.GetTimeSpanPrint(initiatorProfile.timers[cooldownKey].timerEnd - DateTime.UtcNow);
                bot.SendPrivateMessage(
                    "You've already broken " + recipientProfile.displayName + "'s " + part + " too recently. Please respect that 'Consequence' interactions are meant to be meaningful, and not spammed. You'll be able to break that part again in " + remaining + ".",
                    characterName);
                return;
            }

            Interaction breakInteraction = new Interaction
            {
                initiator = characterName,
                recipient = recipient,
                type = "break",
                identifier = part,
                investmentLevel = "consequence",
                interactionTime = DateTime.UtcNow,
                extraParameters = new MongoDB.Bson.BsonArray { requestedDays }
            };

            PendingCommand pendingBreak = new PendingCommand
            {
                pendingInteraction = breakInteraction,
                awaitingConsentFrom = recipient
            };

            MonDB.addPendingCommand(pendingBreak);

            // Compose with the actual days (the processor's GetConsentWarning override
            // can't see extraParameters, so we call the static helper directly).
            string warning = BreakProcessor.ComposeConsentText(
                initiatorProfile.displayName, recipientProfile.displayName, part, requestedDays, clampDir);
            bot.SendMessageInChannel(warning, channel);
        }
    }
}
