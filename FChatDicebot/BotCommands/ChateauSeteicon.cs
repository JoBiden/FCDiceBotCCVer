using System;
using System.Collections.Generic;
using System.Linq;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!seteicon {interaction} [eicon]YourEicon[/eicon]</c> — pin one of your own eicons to an
    /// interaction, so it shows up on that interaction's completion message (the way Chateau
    /// Contract has its own easter-egg icons). Global replacement for the old <c>!setmark</c>,
    /// which now delegates to the same storage. See <see cref="InteractionEiconSupport"/>.
    /// </summary>
    public class ChateauSeteicon : ChatBotCommand
    {
        public ChateauSeteicon()
        {
            Name = "seteicon";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Set your personal eicon for an interaction";
            LongDescription = "Pin one of your own eicons to an interaction, the way Chateau Contract has its own little easter-egg icons. Once set, whenever you perform that interaction the onlookers will see your eicon alongside the result.\n\n"
                + "Works for almost every interaction across the Casual, Involved, Commitment, and Consequence categories — for example !cuddle, !kiss, !spank, !mark, !corrupt, !pay, and more (but not system, recovery, or dicebot commands). For mutual interactions like !kiss, !cuddle, !handhold and !bond, both people's eicons show; for one-sided ones, only yours (the one performing it) does.\n\n"
                + "Set one with !seteicon {interaction} [noparse][eicon]YourEicon[/eicon][/noparse]. Leave the eicon off to clear it. Message !seteicon on its own to see everything you've set.";
            Usage = "!seteicon {interaction} [noparse][eicon]YourEicon[/eicon][/noparse]\nor\n!seteicon {interaction}   (to clear it)\nor\n!seteicon   (to list what you've set)";
            RelatedCommands = new string[] { "setmark", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            Profile profile = MonDB.getProfile(characterName);
            if (profile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(characterName), characterName);
                return;
            }

            string token = terms != null && terms.Length > 0 ? terms[0] : null;

            // Bare "!seteicon" -> list everything the resident has set.
            if (string.IsNullOrEmpty(token))
            {
                bot.SendPrivateMessage(BuildListMessage(profile), characterName);
                return;
            }

            if (!InteractionEiconSupport.TryResolveTokenToVerbKeys(token, out string[] verbKeys))
            {
                bot.SendPrivateMessage(
                    "'" + token + "' isn't an interaction you can pin an eicon to. Try one like !cuddle, !kiss, or !spank. See !help seteicon for the full list.",
                    characterName);
                return;
            }

            // GetEIconFromCommandTerms returns null when no [eicon]...[/eicon] token is present.
            string eicon = commandController.GetEIconFromCommandTerms(rawTerms);

            // No eicon supplied -> clear it.
            if (string.IsNullOrEmpty(eicon))
            {
                foreach (string verbKey in verbKeys)
                {
                    InteractionEiconSupport.ClearInteractionEicon(profile, verbKey);
                }
                MonDB.setProfile(characterName, profile);
                bot.SendPrivateMessage(
                    "Cleared, your " + token + " interactions won't show a personal eicon anymore.",
                    characterName);
                return;
            }

            if (eicon.Length > InteractionEiconSupport.MaxEiconLength)
            {
                bot.SendPrivateMessage("That icon name is way too long! Are you sure it's a real eicon?", characterName);
                return;
            }

            foreach (string verbKey in verbKeys)
            {
                InteractionEiconSupport.SetInteractionEicon(profile, verbKey, eicon);
            }
            MonDB.setProfile(characterName, profile);

            bot.SendPrivateMessage(
                "Done! From now on, whenever you " + token + " someone, the onlookers will see " + eicon + ".",
                characterName);
        }

        /// <summary>
        /// DM readout of every interaction the resident has an eicon set for. Iterates the
        /// canonical (alias-free) token list so aliases don't produce duplicate rows.
        /// </summary>
        private string BuildListMessage(Profile profile)
        {
            var lines = new List<string>();
            foreach (string token in InteractionEiconSupport.CanonicalTokensInOrder)
            {
                if (!InteractionEiconSupport.TryResolveTokenToVerbKeys(token, out string[] verbKeys)) continue;
                string eicon = verbKeys
                    .Select(k => InteractionEiconSupport.GetInteractionEicon(profile, k))
                    .FirstOrDefault(e => !string.IsNullOrEmpty(e));
                if (string.IsNullOrEmpty(eicon)) continue;
                lines.Add("[b]!" + token + "[/b]: " + eicon);
            }

            if (lines.Count == 0)
            {
                return "You haven't set any interaction eicons yet. Try something like !seteicon kiss [noparse][eicon]YourEicon[/eicon][/noparse]. See !help seteicon for the full list.";
            }
            return "Here are the interaction eicons you've set:\n" + string.Join("\n", lines);
        }
    }
}
