using System;
using System.Linq;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!setidentifiereicon {identifier} [eicon]TheEicon[/eicon]</c> — admin-only setter for an
    /// identifier's bot-wide decorative eicon (<see cref="Identifier.eicon"/>), surfaced by
    /// <c>!whatis</c>. Identifiers are otherwise curated directly in the database; this exists so
    /// a purely cosmetic field doesn't require database surgery.
    ///
    /// Not to be confused with <see cref="ChateauSeteicon"/>, which is every resident's own
    /// personal eicon for an interaction or one of their bodyparts.
    /// </summary>
    public class ChateauSetidentifiereicon : ChatBotCommand
    {
        public ChateauSetidentifiereicon()
        {
            Name = "setidentifiereicon";
            Aliases = new string[] { };
            Category = "Admin";
            ShortDescription = "Admin: Set the Chateau's eicon for an identifier";
            LongDescription = "ADMIN ONLY: Set the decorative eicon the Chateau shows for an identifier. Once set it appears beside the identifier's name in !whatis, for everyone. Leave the eicon off to clear it. This is the Chateau's own icon for the thing, not any resident's personal one (that's !seteicon).";
            Usage = "!setidentifiereicon {identifier} [noparse][eicon]TheEicon[/eicon][/noparse]\nor\n!setidentifiereicon {identifier}   (to clear it)";
            RelatedCommands = new string[] { "identifier", "seteicon" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;

            string token = terms != null ? terms.FirstOrDefault() : null;
            if (string.IsNullOrEmpty(token))
            {
                bot.SendPrivateMessage(
                    "Which identifier? Use !setidentifiereicon {identifier} [noparse][eicon]TheEicon[/eicon][/noparse].",
                    characterName);
                return;
            }

            string identifierType = token.Trim().ToLowerInvariant();
            Identifier identifier = MonDB.getIdentifier(identifierType);
            if (identifier == null)
            {
                bot.SendPrivateMessage("That identifier was not found in our records. Are you sure you spelled it right?", characterName);
                return;
            }

            // GetEIconFromCommandTerms returns null when no [eicon]...[/eicon] token is present.
            string eicon = commandController.GetEIconFromCommandTerms(rawTerms);

            if (string.IsNullOrEmpty(eicon))
            {
                MonDB.setIdentifierEicon(identifierType, null);
                bot.SendPrivateMessage("Cleared, " + identifierType + " won't show an eicon anymore.", characterName);
                return;
            }

            if (eicon.Length > InteractionEiconSupport.MaxEiconLength)
            {
                bot.SendPrivateMessage("That icon name is way too long! Are you sure it's a real eicon?", characterName);
                return;
            }

            MonDB.setIdentifierEicon(identifierType, eicon);
            bot.SendPrivateMessage("Done! " + identifierType + " now shows " + eicon + ".", characterName);
        }
    }
}
