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
            ShortDescription = "Set your personal eicon for an interaction or a bodypart";
            LongDescription = "Set a special eicon to show for your interactions of the specified type. Once set, whenever you perform that interaction onlookers will see your eicon alongside the result. For mutual interactions (!kiss, !cuddle, !handhold and !bond) and group interactions, all participants' eicons will show. For !climax and !climaxfor, the eicon for the one climaxing will show. For !drinkfrom and !forcedrink, the eicon for the one drinking will show. For the collection interactions (!milk, !panties and !givepanties), the eicon belongs to whoever the item came from, so yours says what your own milk or panties look like once someone else is holding them. For !pet, the eicon for the one being petted will show (set your own 'being petted' eicon). For all other interactions, the initiator's eicon will show.\n\n"
                + "You can also pin an eicon to one of your own bodyparts, and it will show whenever an interaction involves that part. Use !category bodypart to see the parts you can pick from. Your part shows when someone !marks, !goldens or !breaks it, when they !spank you (your ass), !feeds you (your mouth), or !milks you (your breast); your own part shows when you !consume with it, !lick someone (your tongue) or offer a !boobhat (your breast); and both partners' hands show on a !handhold.\n\n"
                + "Set one with !seteicon {interaction} [noparse][eicon]YourEicon[/eicon][/noparse] or !seteicon {bodypart} [noparse][eicon]YourEicon[/eicon][/noparse]. Leave the eicon off to clear it. Message !seteicon on its own to see everything you've set.";
            Usage = "!seteicon {interaction} [noparse][eicon]YourEicon[/eicon][/noparse]\nor\n!seteicon {bodypart} [noparse][eicon]YourEicon[/eicon][/noparse]\nor\n!seteicon {interaction}   (to clear it)\nor\n!seteicon   (to list what you've set)";
            RelatedCommands = new string[] { "dossier", "category" };
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

            // Resolution order: interaction names (and their aliases) win, then bodypart
            // identifiers. There's no overlap today; the precedence rule is the guard if one
            // ever appears. Note the bodypart lookup keeps the token as typed — only the
            // interaction side folds aliases onto a canonical name.
            bool isInteraction = TryResolveTypedToken(
                commandController, token, out string canonicalToken, out string[] verbKeys);
            if (isInteraction) token = canonicalToken;
            string bodypart = isInteraction ? null : ResolveBodypart(token);

            if (!isInteraction && bodypart == null)
            {
                bot.SendPrivateMessage(
                    "'" + token + "' isn't an interaction or bodypart you can pin an eicon to. Try one like cuddle, kiss, or ass. See !help seteicon for the full list.",
                    characterName);
                return;
            }

            // GetEIconFromCommandTerms returns null when no [eicon]...[/eicon] token is present.
            string eicon = commandController.GetEIconFromCommandTerms(rawTerms);

            // No eicon supplied -> clear it.
            if (string.IsNullOrEmpty(eicon))
            {
                if (isInteraction)
                {
                    foreach (string verbKey in verbKeys)
                    {
                        InteractionEiconSupport.ClearInteractionEicon(profile, verbKey);
                    }
                    MonDB.setProfile(characterName, profile);
                    bot.SendPrivateMessage(
                        "Cleared, your " + token + " interactions won't show a personal eicon anymore.",
                        characterName);
                }
                else
                {
                    InteractionEiconSupport.ClearBodypartEicon(profile, bodypart);
                    MonDB.setProfile(characterName, profile);
                    bot.SendPrivateMessage(
                        "Cleared, your " + Utils.BodypartToText(bodypart) + " won't show a personal eicon anymore.",
                        characterName);
                }
                return;
            }

            if (eicon.Length > InteractionEiconSupport.MaxEiconLength)
            {
                bot.SendPrivateMessage("That icon name is way too long! Are you sure it's a real eicon?", characterName);
                return;
            }

            if (isInteraction)
            {
                foreach (string verbKey in verbKeys)
                {
                    InteractionEiconSupport.SetInteractionEicon(profile, verbKey, eicon);
                }
                MonDB.setProfile(characterName, profile);

                bot.SendPrivateMessage(
                    "Done! From now on, " + SetConfirmationClause(token) + ", onlookers will see " + eicon + ".",
                    characterName);
                return;
            }

            InteractionEiconSupport.SetBodypartEicon(profile, bodypart, eicon);
            MonDB.setProfile(characterName, profile);

            bot.SendPrivateMessage(
                "Done! From now on, whenever an interaction involves your " + Utils.BodypartToText(bodypart) + ", onlookers will see " + eicon + ".",
                characterName);
        }

        /// <summary>
        /// Resolve a token a resident typed — an interaction command name, or any alias of one —
        /// to the verb key(s) its eicon is stored under, plus the canonical name to say it back
        /// with (<c>hug</c> resolves to <c>cuddle</c>).
        ///
        /// <para>
        /// Aliases are written down in exactly one place, <see cref="ChatBotCommand.Aliases"/>:
        /// this asks <see cref="BotCommandController.FindCommandByName"/>, which already folds
        /// every alias onto the command that declared it, and looks that command's name up in
        /// the eicon map. So <c>!seteicon</c> answers to a new alias the moment it is added to
        /// the array, with no second table to keep in step — the failure mode where <c>!cum</c>
        /// dispatched fine but <c>!seteicon cum</c> didn't recognise it can't recur.
        /// </para>
        ///
        /// <para>
        /// The fallback to the raw token matters when no command owns it: the map is the gate
        /// for what counts as an interaction, so a token it doesn't hold is rejected here and
        /// tried as a bodypart by the caller.
        /// </para>
        /// </summary>
        internal static bool TryResolveTypedToken(BotCommandController commandController, string token,
            out string canonicalToken, out string[] verbKeys)
        {
            string typed = string.IsNullOrEmpty(token) ? string.Empty : token.Trim();
            ChatBotCommand command = commandController == null ? null : commandController.FindCommandByName(typed);

            string candidate = command != null && !string.IsNullOrEmpty(command.Name)
                ? command.Name
                : typed.ToLowerInvariant();

            if (InteractionEiconSupport.TryResolveTokenToVerbKeys(candidate, out verbKeys))
            {
                canonicalToken = candidate;
                return true;
            }

            canonicalToken = null;
            verbKeys = null;
            return false;
        }

        /// <summary>
        /// The "whenever ..." clause of the set confirmation, phrased from the side whose eicon
        /// actually shows. Almost every interaction surfaces the initiator's, which reads as
        /// "whenever you {token} someone" — the entries below are the ones that don't. Each
        /// mirrors the processor's <c>EiconOwner</c>: <c>!pet</c>'s icon belongs to the one
        /// being petted; the collection interactions' belong to whoever the milk or panties
        /// came from, so they're phrased around someone else ending up with your things; and a
        /// directional pair's belongs to whoever the act happened to, so both of its verbs
        /// describe that one person. Any interaction whose eicon isn't the initiator's needs an
        /// entry here too.
        /// </summary>
        private static readonly Dictionary<string, string> ConfirmationClauses =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "pet", "whenever someone pets you" },
            { "climax", "whenever you climax" },
            { "climaxfor", "whenever you climax" },
            { "drinkfrom", "whenever you drink from someone" },
            { "forcedrink", "whenever you drink from someone" },
            { "milk", "whenever someone ends up with a bottle of your milk" },
            { "panties", "whenever someone ends up with a pair of your panties" },
            { "givepanties", "whenever someone ends up with a pair of your panties" },
        };

        /// <summary>See <see cref="ConfirmationClauses"/>.</summary>
        internal static string SetConfirmationClause(string token)
        {
            string clause;
            return ConfirmationClauses.TryGetValue(token ?? string.Empty, out clause)
                ? clause
                : "whenever you " + token + " someone";
        }

        /// <summary>
        /// The bodypart identifier this token names, or null when it isn't one. Bodyparts are
        /// whatever the live Identifiers collection tags with the <c>bodypart</c> category —
        /// adding a new one to the database is enough, no code change needed.
        /// </summary>
        internal static string ResolveBodypart(string token)
        {
            string normalized = token.Trim().ToLowerInvariant();
            Identifier identifier = MonDB.getIdentifier(normalized);
            if (identifier?.categories == null) return null;
            return identifier.categories.Contains(BodypartCategory, StringComparer.OrdinalIgnoreCase)
                ? normalized
                : null;
        }

        private const string BodypartCategory = "bodypart";

        /// <summary>
        /// DM readout of everything the resident has an eicon set for, in two sections.
        /// Interactions iterate the canonical (alias-free) token list so aliases don't produce
        /// duplicate rows; bodyparts are read straight off the stored keys, so a section only
        /// appears when something's in it.
        /// </summary>
        internal static string BuildListMessage(Profile profile)
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

            var bodypartLines = InteractionEiconSupport.GetAllBodypartEicons(profile)
                .Select(entry => "[b]" + Utils.BodypartToText(entry.Key) + "[/b]: " + entry.Value)
                .ToList();

            if (lines.Count == 0 && bodypartLines.Count == 0)
            {
                return "You haven't set any eicons yet. Try something like !seteicon kiss [noparse][eicon]YourEicon[/eicon][/noparse], or !seteicon ass [noparse][eicon]YourEicon[/eicon][/noparse]. See !help seteicon for the full list.";
            }

            var sections = new List<string>();
            if (lines.Count > 0)
                sections.Add("Here are the interaction eicons you've set:\n" + string.Join("\n", lines));
            if (bodypartLines.Count > 0)
                sections.Add("Here are the bodypart eicons you've set:\n" + string.Join("\n", bodypartLines));
            return string.Join("\n\n", sections);
        }
    }
}
