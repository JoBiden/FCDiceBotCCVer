using FChatDicebot.BotCommands.Support;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Casual
{
    /// <summary>
    /// Shared command-side wiring for <c>!lap</c> and <c>!sit</c>. Both parse the same way
    /// (a single <c>[user]</c> tag) and differ only in the typed verb, so the parse,
    /// not-found handling, pending-command creation, and consent message all live here.
    ///
    /// Lives outside <c>FChatDicebot.BotCommands</c> so the reflection-based loader in
    /// <see cref="BotCommandController"/> doesn't try to <c>Activator.CreateInstance</c> it
    /// as a chat command. Mirrors <see cref="Commitment.CorruptionCommandSupport"/>.
    /// </summary>
    public static class LapsitCommandSupport
    {
        public static void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string characterName, string channel, string typedVerb)
        {
            // Multi-target → lap stack. Type and identifier both carry the typed verb so the
            // group resolver can credit per-position counts and render the right opener.
            var groupTargets = commandController.GetUserNamesFromCommandTerms(rawTerms);
            if (groupTargets.Count > 1)
            {
                CasualGroupCommandSupport.Run(bot, characterName, channel, typedVerb, groupTargets, typedVerb);
                return;
            }

            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                return;
            }

            var interaction = new Interaction
            {
                initiator = characterName,
                recipient = recipient,
                // Type carries the typed verb so ProcessInteraction can credit give/take to
                // the right side. The identifier mirrors it so the completion message and
                // consent warning (which only receive the identifier) can render the right
                // wording.
                type = typedVerb,
                identifier = typedVerb,
                investmentLevel = "casual",
                interactionTime = DateTime.UtcNow
            };

            var pending = new PendingCommand
            {
                pendingInteraction = interaction,
                awaitingConsentFrom = recipient
            };
            MonDB.addPendingCommand(pending);

            var processor = InteractionProcessorRegistry.GetProcessor(typedVerb);
            string consentMessage = processor != null
                ? processor.GetConsentWarning(initiatorProfile, recipientProfile, interaction.identifier)
                : (initiatorProfile.displayName + " wants to share a lap with " + recipientProfile.displayName + ". Do you !consent?");
            bot.SendMessageInChannel(consentMessage, channel);
        }
    }
}
