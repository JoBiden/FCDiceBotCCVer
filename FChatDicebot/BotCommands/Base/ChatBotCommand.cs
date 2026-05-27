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
        public bool RequireChannel;

        public CommandLockCategory LockCategory;

        public virtual void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {

        }
    }
}
