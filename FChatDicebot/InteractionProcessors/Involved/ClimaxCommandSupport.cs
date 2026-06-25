using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Shared command-side wiring for <c>!climaxfor</c> and <c>!climax</c>. Both
    /// commands parse the same way (zero args → self-target; <c>[user]Bob[/user]</c>
    /// → other-target) and differ only in the verb the user typed, so the parse,
    /// malformed-args rejection, self vs other branching, and consent-message
    /// dispatch all live here.
    ///
    /// Lives outside <c>FChatDicebot.BotCommands</c> so the reflection-based loader
    /// in <see cref="BotCommandController"/> doesn't try to <c>Activator.CreateInstance</c>
    /// it as a chat command. Mirrors the
    /// <see cref="Commitment.CorruptionCommandSupport"/> pattern.
    /// </summary>
    public static class ClimaxCommandSupport
    {
        public static void Run(
            BotMain bot,
            BotCommandController commandController,
            string[] rawTerms,
            string characterName,
            string channel,
            string typedVerb)
        {
            Profile initiatorProfile = MonDB.getProfile(characterName);
            if (initiatorProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(characterName), characterName);
                return;
            }

            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            bool hasTextArgs = rawTerms != null && rawTerms.Length > 0;
            bool hasRecipientTag = !string.IsNullOrEmpty(recipient);

            // Malformed: user typed something other than a [user] tag and we couldn't
            // parse a recipient out of it. Do *not* silently fall back to self-target —
            // a typo shouldn't accidentally credit a self-climax. Lean on the standard
            // notFoundText helper (same wording every other Chateau command uses) so the
            // [user]-tag hint stays consistent across the bot.
            if (hasTextArgs && !hasRecipientTag)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                return;
            }

            bool isSelf = !hasRecipientTag
                || string.Equals(characterName, recipient, StringComparison.Ordinal);

            var processor = (ClimaxforProcessor)InteractionProcessorRegistry.GetProcessor(
                ClimaxforProcessor.ClimaxforType);

            // ----- Self-target shortcut --------------------------------------
            // No second party to consent, so bypass the pending-command flow entirely
            // and let the processor run its self-target path. We're responsible for
            // emitting the channel message and running the title check (the consent
            // handler normally does both).
            if (isSelf)
            {
                // Self-targets skip the consent flow, but must still honor status-effect
                // blockers (e.g. the chastity curse) — otherwise a cursed resident's solo
                // climax would auto-resolve before anything could stop it. ValidateInteraction
                // can't be reused here (it rejects self-targets), so use the self-target check.
                // Pass the typed verb so the right side of the chastity block is consulted.
                var selfValidation = processor.ValidateSelfTarget(characterName, typedVerb);
                if (!selfValidation.IsValid)
                {
                    bot.SendPrivateMessage(selfValidation.ErrorMessage, characterName);
                    return;
                }

                string channelMessage = processor.PerformSelfTarget(characterName, typedVerb);
                if (!string.IsNullOrEmpty(channelMessage))
                {
                    string titlesText = ChateauSystemTitles.CheckAndGrantTitles(characterName);
                    if (!string.IsNullOrEmpty(titlesText))
                    {
                        channelMessage += titlesText;
                    }
                    bot.SendMessageInChannel(channelMessage, channel);
                }
                return;
            }

            // ----- Other-target: standard consent flow -----------------------
            // Pre-process identifier shape is "{typeKey}|0" — the processor overwrites the
            // count portion with the post-increment daily count once it runs. We seed the
            // typeKey now so both the verb-aware status-effect gate below and GetConsentWarning
            // can read which verb was typed (the climaxer side a chastity block applies to
            // flips between !climax and !climaxfor).
            string preProcessIdentifier = ClimaxforProcessor.ComposeIdentifier(typedVerb, 0);

            // Delegate validation to the processor (profile existence + status-effect blockers
            // such as chastity, evaluated against the typed verb).
            var validation = processor.ValidateInteraction(characterName, recipient, preProcessIdentifier);
            if (!validation.IsValid)
            {
                bot.SendPrivateMessage(validation.ErrorMessage, characterName);
                return;
            }

            Profile recipientProfile = MonDB.getProfile(recipient);

            var interaction = new Interaction
            {
                initiator = characterName,
                recipient = recipient,
                // Type carries the user-typed verb so ProcessInteraction can pick the
                // right climaxer (initiator for climaxfor, recipient for climax).
                type = typedVerb,
                identifier = preProcessIdentifier,
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow,
            };

            var pending = new PendingCommand
            {
                pendingInteraction = interaction,
                awaitingConsentFrom = recipient,
            };
            MonDB.addPendingCommand(pending);

            string consentMessage = processor.GetConsentWarning(
                initiatorProfile, recipientProfile, interaction.identifier, typedVerb);
            bot.SendMessageInChannel(consentMessage, channel);
        }
    }
}
