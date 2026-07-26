using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!decline</c> — alias for <c>!no</c>. See <see cref="ChateauNo"/>.
    /// </summary>
    public class ChateauDecline : ChatBotCommand
    {
        public ChateauDecline()
        {
            Name = "decline";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !no";
            LongDescription = "Alternative wording for the !no command. See !help no for full details.";
            Usage = "!decline\nor\n!decline all\nor\n!decline {number}\nor\n!decline {name}";
            RelatedCommands = new string[] { "no", "consent", "oops" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
            // Usage documents a {name} argument rather than a [user] tag, so the derived
            // default can't see that this command targets someone. Bare names already work
            // here via LifecycleTargeting; the flag adds multi-word and display-name matching.
            AcceptsRecipient = true;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            new ChateauNo().Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
