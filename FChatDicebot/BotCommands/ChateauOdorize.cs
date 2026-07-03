using System;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauOdorize : ChatBotCommand
    {
        public ChateauOdorize()
        {
            Name = "odorize";
            Aliases = new string[] { };
            Category = "Consequence Interaction";
            ShortDescription = "Saturate another resident with a lingering scent";
            LongDescription = "Saturate another resident with a chosen scent. They must !consent. Once applied, the scent becomes readily apparent during their subsequent interactions and fades a little each time it gets mentioned. Re-applying the same scent stacks layers, up to a cap. The recipient can !wash once per day to scrub off a single layer.";
            Usage = "!odorize [noparse][user]NameInUserTag[/user][/noparse] {scent}";
            RelatedCommands = new string[] { "wash", "consent", "dossier" };
            CooldownDuration = "7 days";
            CooldownAppliesTo = "initiator (per scent per recipient)";
            IdentifierCategory = "scent";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string identifierType = "scent";
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string scent = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);
            Profile recipientProfile = MonDB.getProfile(recipient);
            Profile initiatorProfile = MonDB.getProfile(characterName);

            if (recipientProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(recipient), characterName);
                return;
            }
            if (string.Equals(recipient, characterName, StringComparison.OrdinalIgnoreCase))
            {
                bot.SendPrivateMessage("You can't odorize yourself. Ask someone else to give you a proper new smell.", characterName);
                return;
            }
            if (scent == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText(identifierType), characterName);
                return;
            }

            Identifier scentIdentifier = MonDB.GetDatabase().GetIdentifier(scent);
            string scentPhrase = ScentText.ScentPhrase(scentIdentifier, scent, initiatorProfile.displayName);

            string cooldownKey = OdorizeProcessor.CooldownTimerKey(scent, recipient);
            if (initiatorProfile.timers.ContainsKey(cooldownKey)
                && initiatorProfile.timers[cooldownKey].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string remaining = Utils.GetTimeSpanPrint(initiatorProfile.timers[cooldownKey].timerEnd - DateTime.UtcNow);
                bot.SendPrivateMessage(
                    "You've already saturated " + recipientProfile.displayName + " with " + scentPhrase + " too recently. Please respect that 'Consequence' interactions are meant to be meaningful, and not spammed. You'll be able to apply " + scentPhrase + " to " + recipientProfile.displayName + " again in " + remaining + ".",
                    characterName);
                return;
            }

            Interaction odorize = new Interaction
            {
                initiator = characterName,
                recipient = recipient,
                type = "odorize",
                identifier = scent,
                investmentLevel = "consequence",
                interactionTime = DateTime.UtcNow
            };

            PendingCommand pendingOdorize = new PendingCommand
            {
                pendingInteraction = odorize,
                awaitingConsentFrom = recipient
            };

            MonDB.addPendingCommand(pendingOdorize);

            // Delegate consent wording to the processor so it stays in one place.
            var processor = InteractionProcessorRegistry.GetProcessor("odorize");
            string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, scent);
            bot.SendMessageInChannel(message, channel);
        }
    }
}
