using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
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
            ShortDescription = "Take a seat on another resident's (or residents') lap";
            LongDescription = "Sit on another resident's (or residents') lap once they !consent. Rin's specialty. If you'd rather have someone else in your lap, try !lap.";
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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            LapsitCommandSupport.Run(bot, commandController, rawTerms, characterName, channel, LapsitProcessor.SitType);
        }
    }
}
