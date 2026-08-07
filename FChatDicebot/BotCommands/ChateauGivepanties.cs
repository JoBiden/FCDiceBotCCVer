using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!givepanties</c> — offer another resident a pair of your own panties. They must
    /// <c>!consent</c> to keeping them; if they do, the pair joins <i>their</i> collection with
    /// your name frozen onto it.
    ///
    /// Inverted reskin of <c>!panties</c>: same processor, opposite role assignment. The
    /// recipient consents either way, because either way they're the one who didn't type.
    /// </summary>
    public class ChateauGivepanties : ChatBotCommand
    {
        public ChateauGivepanties()
        {
            Name = "givepanties";
            Aliases = new string[] { };
            Category = "Involved Interaction";
            ShortDescription = "Offer another resident a pair of your own panties.";
            LongDescription = "Offer a pair of your own panties to another resident. If they !consent, "
                + "the pair joins their collection with its own number and your name on it. Panties are "
                + "keepsakes that they can collect or hand on to someone else with !pay. A given resident "
                + "can receive a pair from you once per Chateau day. Use !panties to ask someone for a pair "
                + "of theirs instead.";
            Usage = "!givepanties [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "panties", "consent", "bottles", "pay" };
            CooldownDuration = "1 day, per-direction";
            CooldownAppliesTo = "receiver (per giver)";
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            PantiesCommandSupport.Run(bot, commandController, rawTerms, address, PantiesProcessor.GivePantiesType);
        }
    }
}
