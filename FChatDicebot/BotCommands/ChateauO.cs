using FChatDicebot.BotCommands.Base;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!o</c> — short alias for <c>!oops</c>. See <see cref="ChateauOops"/>. (The initiator's
    /// counterpart to <c>!c</c>; <c>!w</c> is taken by <c>!work</c>, hence the rename.)
    /// </summary>
    public class ChateauO : ChatBotCommand
    {
        public ChateauO()
        {
            Name = "o";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !oops";
            LongDescription = "Shorthand for the !oops command. See !help oops for full details.";
            Usage = "!o\nor\n!o all\nor\n!o {number}\nor\n!o {name}";
            RelatedCommands = new string[] { "oops", "consent", "no" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            new ChateauOops().Run(bot, commandController, rawTerms, terms, characterName, channel, command);
        }
    }
}
