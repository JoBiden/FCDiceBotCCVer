using FChatDicebot.BotCommands;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Shared command-side wiring for <c>!panties</c> and <c>!givepanties</c>. Both parse
    /// identically (a single <c>[user]</c> tag) and differ only in the verb typed, which decides
    /// which side ends up holding the pair — so the parse, validation and consent dispatch all
    /// live here.
    ///
    /// Lives outside <c>FChatDicebot.BotCommands</c> so the reflection-based loader in
    /// <see cref="BotCommandController"/> doesn't try to <c>Activator.CreateInstance</c> it as a
    /// chat command. Mirrors the <see cref="SourceDrinkCommandSupport"/> pattern.
    ///
    /// There is no self-target path: unlike a climax, a resident keeping their own panties isn't
    /// an act, so both verbs require a target and the processor refuses a self-target outright.
    /// </summary>
    public static class PantiesCommandSupport
    {
        public static void Run(
            BotMain bot,
            BotCommandController commandController,
            string[] rawTerms,
            MessageAddress address,
            string typedVerb)
        {
            string characterName = address.character;
            string channel = address.channel;

            Profile initiatorProfile = MonDB.getProfile(characterName);
            if (initiatorProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(characterName), characterName);
                return;
            }

            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            if (recipient == null || MonDB.getProfile(recipient) == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                return;
            }

            var processor = (PantiesProcessor)InteractionProcessorRegistry.GetProcessor(
                PantiesProcessor.PantiesType);

            // Delegate validation to the processor: profile existence, self-target, status-effect
            // blockers, and the per-direction daily lock (which sits on whichever side holds, so
            // the typed verb has to travel with it).
            var validation = processor.ValidateInteraction(characterName, recipient, null, typedVerb);
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
                // The typed verb rides here so ProcessInteraction can pick the right holder.
                type = typedVerb,
                identifier = PantiesProcessor.ComposeIdentifier(typedVerb, 0),
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
