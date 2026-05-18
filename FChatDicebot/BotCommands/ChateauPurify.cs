using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors.Commitment;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!purify</c> shifts the recipient's corruption value up by the given magnitude.
    /// Pairs with <c>!corrupt</c>: both commands share <see cref="CorruptionProcessor"/> and
    /// derive the effective direction from <c>verb_sign * sign(amount)</c>, so
    /// <c>!purify -3</c> is equivalent to <c>!corrupt 3</c>. Amount defaults to 1.
    /// </summary>
    public class ChateauPurify : ChatBotCommand
    {
        public ChateauPurify()
        {
            Name = "purify";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Push another resident toward purity";
            LongDescription = "Move another resident's corruption value upward (toward 'pure') by the given magnitude. Negative amounts flip the direction, behaving as !corrupt of the same magnitude. Amount defaults to 1 if omitted. Self-target is allowed. A single initiator may move any single recipient's corruption value by at most 10 total magnitude per UTC day, summed across both directions — the excess is clamped silently at process time.";
            Usage = "!purify [noparse][user]NameInUserTag[/user][/noparse] {amount}\nor\n!purify [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "corrupt", "consent", "dossier" };
            CooldownDuration = "Daily quota (10 magnitude per recipient)";
            CooldownAppliesTo = "initiator";
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            CorruptionCommandSupport.Run(bot, commandController, rawTerms, characterName, channel, CorruptionProcessor.PurifyType);
        }
    }
}
