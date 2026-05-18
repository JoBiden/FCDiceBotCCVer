using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Commitment
{
    /// <summary>
    /// Shared command-side wiring for <c>!corrupt</c> and <c>!purify</c>. Both commands
    /// parse the same way (user tag + optional signed integer amount) and the only
    /// difference is the typed verb, so we centralize the parse, validation, quota
    /// pre-clamping, and pending-command creation here.
    ///
    /// This deliberately lives outside <c>FChatDicebot.BotCommands</c> so the reflection-
    /// based loader in <c>BotCommandController</c> doesn't try to <c>Activator.CreateInstance</c>
    /// it as a chat command.
    /// </summary>
    public static class CorruptionCommandSupport
    {
        public const int DefaultMagnitudeWhenAmountOmitted = 1;

        public static void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string characterName, string channel, string typedVerb)
        {
            // No-target and not-registered cases fall through to the existing
            // notFoundText helper (same pattern as !breed / !odorize): a null recipient
            // yields a null profile lookup, which we then report below.
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                return;
            }

            int signedAmount = DefaultMagnitudeWhenAmountOmitted;
            string[] rawInts = commandController.GetIntsFromCommandTermsAsStrings(rawTerms);
            if (rawInts != null && rawInts.Length > 0)
            {
                try
                {
                    signedAmount = Convert.ToInt32(rawInts[0]);
                }
                catch (Exception)
                {
                    // Wording mirrors ChateauPay's invalid-number message.
                    bot.SendPrivateMessage("The amount must be a valid whole number.", characterName);
                    return;
                }
            }

            if (signedAmount == 0)
            {
                bot.SendPrivateMessage(
                    "We don't track corruption interactions that fail to move the needle. Please specify a non-zero whole number amount.",
                    characterName);
                return;
            }

            // Compose the *effective* verb from typed verb + amount sign. Stored on the
            // Interaction.type so the processor never has to re-derive direction.
            string effectiveVerb = CorruptionProcessor.EffectiveVerb(typedVerb, signedAmount);
            int requestedMagnitude = Math.Abs(signedAmount);

            DateTime utcNow = DateTime.UtcNow;
            DateTime utcDay = utcNow.Date;
            int used = CorruptionProcessor.GetUsedQuota(initiatorProfile, recipient, utcDay);
            int remaining = CorruptionProcessor.RemainingQuota(used);

            if (remaining <= 0)
            {
                // Quota fully exhausted → refuse before sending a consent prompt. Same
                // wording as the rare TOCTOU exhaustion that can happen at process time
                // (see CorruptionProcessor.QuotaExhaustedPrivateMessage).
                bot.SendPrivateMessage(
                    CorruptionProcessor.QuotaExhaustedPrivateMessage(effectiveVerb, recipientProfile.displayName),
                    characterName);
                return;
            }

            // Pre-clamp the magnitude to remaining quota so the recipient never sees a
            // consent prompt for a number larger than what could actually land. The
            // processor will re-clamp at process time as a TOCTOU safety net.
            int pendingMagnitude = Math.Min(requestedMagnitude, remaining);
            if (pendingMagnitude < requestedMagnitude)
            {
                string untilReset = Utils.GetTimeSpanPrint(utcDay.AddDays(1) - utcNow);
                bot.SendPrivateMessage(
                    "You can only increase or decrease someone's corruption/purity by "
                    + CorruptionProcessor.DailyMagnitudeLimit + " per day! We appreciate your enthusiasm, "
                    + "but have changed your pending interaction to the maximum you can currently perform, "
                    + pendingMagnitude + ". Feel free to " + effectiveVerb + " " + recipientProfile.displayName
                    + " further in " + untilReset + ".",
                    characterName);
            }

            Interaction interaction = new Interaction
            {
                initiator = characterName,
                recipient = recipient,
                type = effectiveVerb,
                identifier = CorruptionProcessor.ComposeIdentifier(effectiveVerb, pendingMagnitude),
                investmentLevel = "commitment",
                interactionTime = DateTime.UtcNow,
                extraParameters = new MongoDB.Bson.BsonArray { pendingMagnitude }
            };

            PendingCommand pending = new PendingCommand
            {
                pendingInteraction = interaction,
                awaitingConsentFrom = recipient
            };

            MonDB.addPendingCommand(pending);

            var processor = InteractionProcessorRegistry.GetProcessor(effectiveVerb);
            string consentMessage = processor != null
                ? processor.GetConsentWarning(initiatorProfile, recipientProfile, interaction.identifier)
                : (initiatorProfile.displayName + " wants to " + effectiveVerb + " " + recipientProfile.displayName + ". Do you !consent?");
            bot.SendMessageInChannel(consentMessage, channel);
        }
    }
}
