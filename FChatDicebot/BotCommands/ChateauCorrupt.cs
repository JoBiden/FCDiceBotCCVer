using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors.Commitment;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!corrupt</c> shifts the recipient's corruption value down by the given magnitude.
    /// Pairs with <c>!purify</c>: both commands share <see cref="CorruptionProcessor"/> and
    /// derive the effective direction from <c>verb_sign * sign(amount)</c>, so
    /// <c>!corrupt -3</c> is equivalent to <c>!purify 3</c>. Amount defaults to 1.
    /// Self-target is allowed. Daily quota clamping happens at process time inside the
    /// shared processor (see <see cref="CorruptionProcessor.DailyMagnitudeLimit"/>).
    /// </summary>
    public class ChateauCorrupt : ChatBotCommand
    {
        public ChateauCorrupt()
        {
            Name = "corrupt";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Push another resident toward corruption";
            LongDescription = "Move another resident's corruption value downward (toward 'corrupt') by the given magnitude. Negative amounts flip the direction, behaving as !purify of the same magnitude. Amount defaults to 1 if omitted. Self-target is allowed. A single initiator may move any single recipient's corruption value by at most 10 total magnitude per day, summed across both directions — the excess is clamped silently at process time. Once it builds up, a resident's corruption (or purity) becomes visible in most interactions and flavors anything milked from them.";
            Usage = "!corrupt [noparse][user]NameInUserTag[/user][/noparse] {amount}\nor\n!corrupt [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "purify", "consent", "dossier" };
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
            CorruptionCommandSupport.Run(bot, commandController, rawTerms, characterName, channel, CorruptionProcessor.CorruptType);
        }
    }
}
