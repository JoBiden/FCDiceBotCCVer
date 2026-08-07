using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!panties</c> — ask another resident for a pair of their panties. They must
    /// <c>!consent</c>; if they do, the pair joins your collection under its own number with
    /// their name frozen onto it.
    ///
    /// Pairs with <c>!givepanties</c>: same processor, inverted role assignment.
    /// </summary>
    public class ChateauPanties : ChatBotCommand
    {
        public ChateauPanties()
        {
            Name = "panties";
            Aliases = new string[] { };
            Category = "Involved Interaction";
            ShortDescription = "Ask another resident for a pair of their panties.";
            LongDescription = "Ask another resident for a pair of their panties to keep. If they !consent, "
                + "the pair joins your collection with its own number and their name on it. Panties are "
                + "keepsakes that you can collect or hand on to someone else with !pay. You can collect a pair "
                + "from any one resident once per Chateau day. Use !givepanties to offer a pair of your own instead.";
            Usage = "!panties [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "givepanties", "consent", "bottles", "pay" };
            CooldownDuration = "1 day, per-direction";
            CooldownAppliesTo = "collector (per resident)";
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            PantiesCommandSupport.Run(bot, commandController, rawTerms, address, PantiesProcessor.PantiesType);
        }
    }
}
