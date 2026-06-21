using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors.Casual;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!sit</c> — the initiator is the sitter; the recipient is the lap (the one being sat
    /// on). Pairs with <c>!lap</c> (initiator is the lap): both share
    /// <see cref="LapsitProcessor"/>. Single-target form; the lap-stack (3+) is future work.
    /// </summary>
    public class ChateauSit : ChatBotCommand
    {
        public ChateauSit()
        {
            Name = "sit";
            Aliases = new string[] { };
            Category = "Casual Interaction";
            ShortDescription = "Take a seat on another resident's lap";
            LongDescription = "Sit on another resident's lap once they !consent. You get the comfy seat — they're the foundation. Pairs with !lap, where you're the one offering your lap instead.";
            Usage = "!sit [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "lap", "cuddle", "consent", "dossier" };
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
            LapsitCommandSupport.Run(bot, commandController, rawTerms, characterName, channel, LapsitProcessor.SitType);
        }
    }
}
