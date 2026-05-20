using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors.Involved;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!climax</c> — inverted reskin of <c>!climaxfor</c> where the recipient is
    /// the one climaxing ("I make you climax"). With no arguments, falls back to a
    /// self-targeted solo climax identical to bare <c>!climaxfor</c> (the inversion
    /// is meaningless when there's only one party). With <c>[user]Bob[/user]</c>,
    /// Bob is the climaxer and you are credited as the partner who helped — Bob
    /// must !consent.
    ///
    /// Backed by the same <see cref="ClimaxforProcessor"/> as <c>!climaxfor</c>;
    /// the typed verb decides which party gets credited as the climaxer.
    /// </summary>
    public class ChateauClimax : ChatBotCommand
    {
        public ChateauClimax()
        {
            Name = "climax";
            Aliases = new string[] { };
            Category = "Involved Interaction";
            ShortDescription = "Bring another resident, or yourself, to orgasm.";
            LongDescription = "Bring another resident, or yourself, to orgasm. Specify who you'd like to "
                + "make a mess of (with !consent), or make a mess of yourself for all to see! "
                + "Descriptions are gender neutral. "
                + "Please use the 'cums' !climax joke sparingly and responsibly!";
            Usage = "!climax [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "climaxfor", "consent", "dossier" };
            CooldownDuration = "30 minutes";
            CooldownAppliesTo = "both initiator and recipient";
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ClimaxCommandSupport.Run(bot, commandController, rawTerms, characterName, channel,
                ClimaxforProcessor.ClimaxType);
        }
    }
}
