using FChatDicebot.BotCommands.Base;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!withdraw</c> — alias for <c>!oops</c>. See <see cref="ChateauOops"/>.
    /// </summary>
    public class ChateauWithdraw : ChatBotCommand
    {
        public ChateauWithdraw()
        {
            Name = "withdraw";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !oops";
            LongDescription = "Alternative wording for the !oops command. See !help oops for full details.";
            Usage = "!withdraw\nor\n!withdraw all\nor\n!withdraw {number}\nor\n!withdraw {name}";
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
