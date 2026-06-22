using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors.Casual;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!lap</c> — the initiator is the lap (the one being sat on); the recipient takes a
    /// seat. Pairs with <c>!sit</c> (initiator is the sitter): both share
    /// <see cref="LapsitProcessor"/>. Single-target form; the lap-stack (3+) is future work.
    /// </summary>
    public class ChateauLap : ChatBotCommand
    {
        public ChateauLap()
        {
            Name = "lap";
            Aliases = new string[] { };
            Category = "Casual Interaction";
            ShortDescription = "Pull another resident onto your lap";
            LongDescription = "Pull another resident onto your lap once they !consent. If you'd rather be the one taking a seat, try !sit.";
            Usage = "!lap [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "sit", "cuddle", "consent", "dossier" };
            CooldownDuration = "30 minutes (but can still interact without incrementing count)";
            CooldownAppliesTo = "both initiator and recipient";
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            LapsitCommandSupport.Run(bot, commandController, rawTerms, characterName, channel, LapsitProcessor.LapType);
        }
    }
}
