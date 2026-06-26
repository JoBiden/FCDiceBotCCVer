using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauIdentifiersAlias : ChatBotCommand
    {
        public ChateauIdentifiersAlias()
        {
            Name = "identifiers";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !category";
            LongDescription = "Shorthand for the !category command. Lists all available identifiers for a given category. See !help category for full details.";
            Usage = "!identifiers {category}";
            RelatedCommands = new string[] { "category", "identifier", "whatis" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            ChateauIdentifiers category = new ChateauIdentifiers();
            category.Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
