using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using System.ComponentModel;

namespace FChatDicebot.BotCommands
{
    public class ChateauMonsterize : ChatBotCommand
    {
        public ChateauMonsterize()
        {
            Name = "monsterize";
            Aliases = new string[] { };
            Category = "Consequence Interaction";
            ShortDescription = "Transform someone into a monster";
            LongDescription = "Transform another resident into a monstrous species. They must consent to their new body, which they are likely to sustain for the rest of their lives. The Chateau is home to monsters, so this rite of passage is a very common occurence.";
            Usage = "!monsterize [noparse][user]NameInUserTag[/user][/noparse] {monster}";
            RelatedCommands = new string[] { "petrify", "plant", "dossier" };
            CooldownDuration = "7 days";
            CooldownAppliesTo = "recipient";
            IdentifierCategory = "monster";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string recipient = commandController.GetUserNameFromCommandTerms(rawTerms);
            string monsterType = commandController.GetIdentifierFromCommandTerms(rawTerms, "monster");
            Profile initiatorProfile = MonDB.getProfile(characterName);

            // Delegate validation to the processor so the command-time check uses the same
            // rules (profile existence, monster-identifier required, recipient-cooldown
            // recheck) as the consent-time path. Validation can fail because the recipient
            // was not found OR because monster-identifier was not supplied — keep the
            // pre-check on the identifier separately so the typed error wording matches.
            if (monsterType == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.typeNotFoundText("monster"), characterName);
                return;
            }

            var processor = (InteractionProcessors.Consequence.MonsterizeProcessor)
                InteractionProcessors.InteractionProcessorRegistry.GetProcessor("monsterize");
            var validation = processor.ValidateInteraction(characterName, recipient, monsterType);
            if (!validation.IsValid)
            {
                bot.SendPrivateMessage(validation.ErrorMessage, characterName);
                return;
            }

            Profile recipientProfile = MonDB.getProfile(recipient);

            Interaction monsterize = new Interaction();
            monsterize.initiator = characterName;
            monsterize.recipient = recipient;
            monsterize.type = "monsterize";
            monsterize.identifier = monsterType;
            monsterize.investmentLevel = "consequence";
            monsterize.interactionTime = DateTime.UtcNow;

            PendingCommand pendingMonsterize = new PendingCommand();
            pendingMonsterize.pendingInteraction = monsterize;
            pendingMonsterize.awaitingConsentFrom = recipient;

            MonDB.addPendingCommand(pendingMonsterize);

            // Delegate consent wording to the processor so it stays in one place.
            string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, monsterType);
            bot.SendMessageInChannel(message, channel);
        }
    }
}
