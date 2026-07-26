using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.BotCommands.Base
{
    public abstract class ChatBotCommand
    {
        public string Name;
        public string[] Aliases;
        public string Category; // "casual", "involved", "commitment", "consequence", "recovery", "general", "admin"
        public string ShortDescription; // One-liner for list views
        public string LongDescription; // Detailed explanation with examples/notes
        public string Usage; // Syntax format
        public string[] RelatedCommands; // Other relevant commands
        public string CooldownDuration; // e.g., "30 minutes", "1 day", null if none
        public string CooldownAppliesTo; // "initiator", "recipient", "both", null if no cooldown
        public string IdentifierCategory; // e.g., "bodypart" for mark, "substance" for feed, null if N/A

        public bool RequireBotAdmin;
        public bool RequireChannelAdmin;
        public bool RequireBotIsChannelAdmin;
        public bool RequireChannel;

        /// <summary>
        /// Whether this command targets another resident, and so should accept a bare name in
        /// place of a <c>[user]</c> tag. Leave unset to derive it from <see cref="Usage"/>,
        /// which already documents the user-tag slot for every targeting command — a new
        /// command written to the usual pattern gets bare-name targeting without a second
        /// thing to remember. Set explicitly where <see cref="Usage"/> can't say it: the
        /// pending-lifecycle verbs document a <c>{name}</c> argument instead, and the
        /// single-letter aliases document nothing at all.
        /// </summary>
        public bool? AcceptsRecipient;

        /// <summary>
        /// Whether this command takes an interaction type as an argument ("!pledge {name}
        /// cuddle"). Those aren't Identifiers, so <see cref="IdentifierCategory"/> can't
        /// describe them and bare-name resolution would otherwise mistake "cuddle" for a
        /// name it failed to recognize.
        /// </summary>
        public bool TakesInteractionType;

        public bool ResolvesRecipient()
        {
            if (AcceptsRecipient.HasValue)
                return AcceptsRecipient.Value;

            return !string.IsNullOrEmpty(Usage) && Usage.Contains("[user]");
        }

        public CommandLockCategory LockCategory;

        public virtual void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {

        }

        public void SendMessageToChannelOrUser(BotMain bot, BotCommandController commandController, MessageAddress address, string sendMessage)
        {
            if (!commandController.MessageCameFromChannel(address))
            {
                bot.SendPrivateMessage(sendMessage, address);
            }
            else
            {
                bot.SendMessageInChannel(sendMessage, address);
            }

        }
    }
}
