using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Milk a specific substance out of another resident. Produces 1–3 tagged bottles
    /// (rolled at consent) in the initiator's milk inventory. One milking per direction
    /// per day, regardless of substance — milking someone doesn't stop them milking you
    /// back today. The Chateau provides the empties; no inventory precondition.
    ///
    /// Self-target is a shortcut: bypasses the consent flow and the milkInventory entry,
    /// instantly crediting the initiator with 1 copper + 1 bottle-currency (the same
    /// effective payout as milking and then immediately !selling one bottle of a
    /// common-tier substance). Still consumes the daily self-direction lock.
    /// </summary>
    public class ChateauMilk : ChatBotCommand
    {
        public ChateauMilk()
        {
            Name = "milk";
            Aliases = new string[] { };
            Category = "Involved Interaction";
            ShortDescription = "Milk a substance from another resident.";
            LongDescription = "Milk a specified substance from another resident, gaining 1 to 3 bottles. The bottles might be pure or corrupted based on who was milked. You can only milk a specified resident once per day.";
            Usage = "!milk [noparse][user]NameInUserTag[/user][/noparse] {substance}";
            RelatedCommands = new string[] { "sell", "feed", "golden", "consent", "dossier" };
            CooldownDuration = "1 day, per-direction";
            CooldownAppliesTo = "initiator (per recipient)";
            IdentifierCategory = "substance";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string identifierType = "substance";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string substance = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);

            Profile initiatorProfile = MonDB.getProfile(characterName);

            // Recipient missing — could be a typo, or the user omitted the [user] tag
            // and meant to self-milk. Self-milking requires explicit self-targeting via
            // the [user] tag (so we don't accidentally fire a self-sale shortcut on a
            // typo), so missing recipient is just a "not found" failure.
            if (recipient == null || MonDB.getProfile(recipient) == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                return;
            }

            Profile recipientProfile = MonDB.getProfile(recipient);

            bool isSelf = string.Equals(characterName, recipient, StringComparison.Ordinal);

            // ----- Self-milk shortcut path --------------------------------------
            // Bypass the consent flow entirely: the Chateau buys the bottle straight off
            // the producer for 1 copper + 1 bottle-currency, then locks the pair (self)
            // for the day. No milkInventory entry, no PendingCommand.
            if (isSelf)
            {
                if (substance == null)
                {
                    bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                    return;
                }
                if (MilkProcessor.HasActiveDirectionLock(initiatorProfile, characterName))
                {
                    bot.SendPrivateMessage(
                        MilkProcessor.DirectionLockMessage(initiatorProfile.displayName, isSelf: true),
                        characterName);
                    return;
                }

                // Targeted timer write (re-fetches fresh at call time) rather than a
                // whole-profile SetProfile, so it can't revert a concurrent currency/count
                // change made to this profile since the GetProfile at the top of Run.
                MonDB.GetDatabase().SetTimer(characterName, MilkProcessor.DirectionTimerKey(characterName), new CoolDown
                {
                    timerEnd = DateTime.UtcNow.Date.AddDays(1)
                });
                MonDB.GetDatabase().ChangeCurrency(characterName, ChateauCurrency.SellPayoutCurrency, 1);
                MonDB.GetDatabase().ChangeCurrency(characterName, ChateauCurrency.BottleCurrency, 1);

                string substanceText = Utils.SubstanceToText(substance);
                string channelMessage = initiatorProfile.displayName + " milks themself for a bottle of "
                    + substanceText + " and trades it straight to the Chateau for [b]1 "
                    + ChateauCurrency.SellPayoutCurrency + "[/b] and [b]1 "
                    + ChateauCurrency.BottleCurrency + "[/b].";
                bot.SendMessageInChannel(channelMessage, channel);
                return;
            }

            // ----- Normal (other-target) path -----------------------------------
            // Delegate validation to the processor so the command-time check uses the
            // exact same rules as the consent-time recheck (substance category, pair
            // lock not active, status-effect blockers). Any failure surfaces the
            // processor's own reason text.
            var processor = (MilkProcessor)InteractionProcessors.InteractionProcessorRegistry.GetProcessor("milk");
            var validation = processor.ValidateInteraction(characterName, recipient, substance);
            if (!validation.IsValid)
            {
                bot.SendPrivateMessage(validation.ErrorMessage, characterName);
                return;
            }

            Interaction milkInteraction = new Interaction
            {
                initiator = characterName,
                recipient = recipient,
                identifier = substance,
                type = "milk",
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow,
            };

            PendingCommand pendingMilk = new PendingCommand
            {
                pendingInteraction = milkInteraction,
                awaitingConsentFrom = recipient,
            };

            MonDB.addPendingCommand(pendingMilk);

            string consentMessage = processor.GetConsentWarning(initiatorProfile, recipientProfile, substance);
            bot.SendMessageInChannel(consentMessage, channel);
        }
    }
}
