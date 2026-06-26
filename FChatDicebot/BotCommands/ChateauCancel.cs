using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!cancel</c> — alias for <c>!oops</c>. See <see cref="ChateauOops"/>.
    /// </summary>
    public class ChateauCancel : ChatBotCommand
    {
        public ChateauCancel()
        {
            Name = "cancel";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !oops";
            LongDescription = "Alternative wording for the !oops command. See !help oops for full details.";
            Usage = "!cancel\nor\n!cancel all\nor\n!cancel {number}\nor\n!cancel {name}";
            RelatedCommands = new string[] { "oops", "consent", "no" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            new ChateauOops().Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
