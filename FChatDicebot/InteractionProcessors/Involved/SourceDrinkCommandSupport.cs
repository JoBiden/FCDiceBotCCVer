using FChatDicebot.BotCommands;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Shared command-side wiring for <c>!drinkfrom</c> and <c>!forcedrink</c>. Both parse
    /// identically (<c>[user]Bob[/user] {substance}</c>) and differ only in the verb typed, which
    /// decides which side drinks and therefore which side is asked to consent — so the parse,
    /// validation and consent dispatch all live here.
    ///
    /// Lives outside <c>FChatDicebot.BotCommands</c> so the reflection-based loader in
    /// <see cref="BotCommandController"/> doesn't try to <c>Activator.CreateInstance</c> it as a
    /// chat command. Mirrors the <see cref="ClimaxCommandSupport"/> pattern.
    /// </summary>
    public static class SourceDrinkCommandSupport
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

            // Bare substance, deliberately: DoseStatusContributor matches this identifier
            // against a vice name to decide whether the drink satisfied a craving, so it must
            // not be wrapped in a composite payload. The typed verb travels separately.
            string substance = commandController.GetIdentifierFromCommandTerms(rawTerms, "substance");

            var processor = (SourceDrinkProcessor)InteractionProcessorRegistry.GetProcessor(
                SourceDrinkProcessor.DrinkFromType);

            // The typed overload, so the role-dependent gates (the shared draw lock and the
            // broken-part check) are evaluated against the right side of the interaction.
            var validation = processor.ValidateInteraction(characterName, recipient, substance, typedVerb);
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
                // Type carries the typed verb so ProcessInteraction can tell who drinks
                // (the initiator on !drinkfrom, the recipient on !forcedrink).
                type = typedVerb,
                identifier = substance,
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow,
            };

            var pending = new PendingCommand
            {
                pendingInteraction = interaction,
                // Always the other party: on !drinkfrom that's the source being asked to be
                // drunk from, on !forcedrink it's the drinker being offered the drink.
                awaitingConsentFrom = recipient,
            };
            MonDB.addPendingCommand(pending);

            string consentMessage = processor.GetConsentWarning(
                initiatorProfile, recipientProfile, substance, typedVerb);
            bot.SendMessageInChannel(consentMessage, channel);
        }
    }
}
