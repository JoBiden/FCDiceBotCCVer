using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.InteractionProcessors.Involved;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!climaxfor</c> — the initiator is the one climaxing. With no arguments,
    /// defaults to a self-targeted solo climax (no consent flow). With
    /// <c>[user]Bob[/user]</c>, climaxes for/with Bob (requires Bob's consent).
    ///
    /// Pairs with <c>!climax</c>: same processor, inverted role assignment.
    /// All parsing, validation, and dispatch live in
    /// <see cref="ClimaxCommandSupport"/>.
    /// </summary>
    public class ChateauClimaxfor : ChatBotCommand
    {
        public ChateauClimaxfor()
        {
            Name = "climaxfor";
            Aliases = new string[] { };
            Category = "Involved Interaction";
            ShortDescription = "Bring yourself to orgasm, solo or for another resident.";
            LongDescription = "Bring yourself to orgasm. Specify who you'd like to cum for (with !consent), "
                + "or just make a mess for all to see! Descriptions are gender neutral. "
                + "Please use the 'cums' !climaxfor joke sparingly and responsibly!";
            Usage = "!climaxfor [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "climax", "consent", "dossier" };
            CooldownDuration = "30 minutes";
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
            ClimaxCommandSupport.Run(bot, commandController, rawTerms, characterName, channel,
                ClimaxforProcessor.ClimaxforType);
        }
    }
}
