using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Offer a substance straight out of your own body for another resident to drink. The
    /// source is the one who typed it, so the drinker is the one asked to consent.
    ///
    /// "Force" is flavor, the same way it is in <c>!breed</c> and <c>!corrupt</c> — this is
    /// consent-gated like every other interaction in the Chateau, and the prompt says so.
    ///
    /// Mirror of <see cref="ChateauDrinkfrom"/>; both run through
    /// <see cref="SourceDrinkProcessor"/>, which resolves the roles from the typed verb.
    /// </summary>
    public class ChateauForcedrink : ChatBotCommand
    {
        public ChateauForcedrink()
        {
            Name = "forcedrink";
            Aliases = new string[] { };
            Category = "Involved Interaction";
            ShortDescription = "Offer another resident a drink straight from you.";
            LongDescription = "Offer a substance straight out of your own body for another resident to drink. They still have to !consent. It hits them harder than a bottled drink of the same thing, carrying extra corruption or purity, and a higher chance of deepening an addiction they already carry. Feeding another in this way uses up their one draw from you for the day, the same as being milked would.";
            Usage = "!forcedrink [noparse][user]NameInUserTag[/user][/noparse] {substance}";
            RelatedCommands = new string[] { "drinkfrom", "drink", "milk", "bottles", "dose", "detox", "consent" };
            CooldownDuration = "1 day, per-direction";
            CooldownAppliesTo = "drinker (per source)";
            IdentifierCategory = "substance";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            SourceDrinkCommandSupport.Run(
                bot, commandController, rawTerms, address, SourceDrinkProcessor.ForceDrinkType);
        }
    }
}
