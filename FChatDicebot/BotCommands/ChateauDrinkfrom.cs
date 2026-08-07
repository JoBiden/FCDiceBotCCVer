using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Drink a substance straight out of another resident, skipping the bottle entirely.
    /// The drinker is the one who typed it, so the source is the one asked to consent.
    ///
    /// Mirror of <see cref="ChateauForcedrink"/>; both run through
    /// <see cref="SourceDrinkProcessor"/>, which resolves the roles from the typed verb.
    /// </summary>
    public class ChateauDrinkfrom : ChatBotCommand
    {
        public ChateauDrinkfrom()
        {
            Name = "drinkfrom";
            Aliases = new string[] { };
            Category = "Involved Interaction";
            ShortDescription = "Drink a substance straight from another resident.";
            LongDescription = "Drink a substance directly out of another resident. It hits harder than a bottled drink of the same thing, carrying extra corruption or purity, and a higher chance of deepening an addiction you already carry. Drinking from someone this way uses up your one draw from them for the day, the same as milking them would.";
            Usage = "!drinkfrom [noparse][user]NameInUserTag[/user][/noparse] {substance}";
            RelatedCommands = new string[] { "forcedrink", "drink", "milk", "bottles", "dose", "detox", "consent" };
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
                bot, commandController, rawTerms, address, SourceDrinkProcessor.DrinkFromType);
        }
    }
}
