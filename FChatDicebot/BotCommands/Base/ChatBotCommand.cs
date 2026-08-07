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

        /// <summary>
        /// Other names this command answers to. This array is the single source of truth for
        /// aliases: <see cref="BotCommandController.FindCommandByName"/> dispatches on it, and
        /// <c>!help</c> both looks commands up by it and prints it. Adding an alias is one entry
        /// here — aliases used to need a delegating command class of their own, and no longer do.
        /// A name that's already a command's <see cref="Name"/> is ignored (the real command
        /// wins), so an alias can never shadow an existing command.
        /// </summary>
        public string[] Aliases = new string[0];

        /// <summary>
        /// What kind of command this is. Beyond labelling the <c>!help {command}</c> readout,
        /// this is what puts the command in the no-arg <c>!help</c> listing: the section is
        /// derived from this value by <see cref="ChateauHelp.SectionFor"/>, so setting it is the
        /// only step. A command with no Category is not listed at all — that's how the legacy
        /// dicebot commands that were never migrated stay out. The recognized values are the
        /// cases of that switch; a test fails on anything else.
        /// </summary>
        public string Category;
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
        /// Keep this command out of the no-arg <c>!help</c> listing while leaving it dispatchable
        /// and documented under <c>!help {command}</c>. For commands kept working for the
        /// residents who already learned them but no longer worth advertising — <c>!setmark</c>,
        /// superseded by <c>!seteicon mark</c>, is the case this exists for. Admin-only commands
        /// don't need it: <see cref="ChateauHelp.SectionFor"/> already excludes those.
        /// </summary>
        public bool HideFromHelpListing;

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

        /// <summary>
        /// Whether <paramref name="name"/> is one of this command's aliases. Case-insensitive,
        /// because a resident types whatever case they like and the dispatcher lowercases.
        /// </summary>
        public bool HasAlias(string name)
        {
            if (Aliases == null || string.IsNullOrEmpty(name))
                return false;

            return Aliases.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
        }

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
