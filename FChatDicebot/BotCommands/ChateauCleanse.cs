using FChatDicebot.BotCommands.Base;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !cleanse is the self-targeted reversal for !curse. Removes one curse at the
    /// per-curse cost in <see cref="CurseProcessor.CatalogMap"/>. With no argument the
    /// caller must carry exactly one curse to disambiguate the target; zero curses yields
    /// a polite refusal, two or more yields a "name it" error. Output is private
    /// (matching !purge and !detox precedent).
    /// </summary>
    public class ChateauCleanse : ChatBotCommand
    {
        public ChateauCleanse()
        {
            Name = "cleanse";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Cleanse a curse, at a cost.";
            LongDescription = "Remove a curse placed upon you. Each curse has a specific cost of removal, which could be missing a day of work, exhausting a body part (see !break and !rest), or losing training prowess. Look at the details for a specific curse with !whatis {curse} to see what that cost is specifically.";
            Usage = "!cleanse\n!cleanse {curse}";
            RelatedCommands = new string[] { "curse", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = CurseProcessor.CurseCategory;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string specifiedCurse = commandController.GetIdentifierFromCommandTerms(rawTerms, CurseProcessor.CurseCategory);
            CleanseResult result = ExecuteCleanse(MonDB.GetDatabase(), characterName, specifiedCurse);

            if (!string.IsNullOrEmpty(result.ChannelMessage))
            {
                bot.SendMessageInChannel(result.ChannelMessage, channel);
            }
            if (!string.IsNullOrEmpty(result.PrivateMessage))
            {
                bot.SendPrivateMessage(result.PrivateMessage, characterName);
            }
        }

        public class CleanseResult
        {
            public string ChannelMessage { get; set; } = string.Empty;
            public string PrivateMessage { get; set; } = string.Empty;
        }

        /// <summary>
        /// Pure cleanse logic, factored out for testability. PrivateMessage is non-empty
        /// in every outcome (success, no-curses, ambiguity, wrong-name); ChannelMessage
        /// stays empty per the spec decision that cleansing is a private affair (matches
        /// !purge / !detox precedent).
        /// </summary>
        /// <param name="specifiedCurse">Empty/null forces the single-curse shortcut:
        /// cleanses when the caller carries exactly one, otherwise asks which.</param>
        public static CleanseResult ExecuteCleanse(IChateauDatabase database, string characterName, string specifiedCurse)
        {
            return ExecuteCleanse(database, characterName, specifiedCurse, new Random());
        }

        /// <summary>
        /// Test seam: caller supplies the rng used by the cost applier (which break part
        /// is picked, which training is decremented).
        /// </summary>
        public static CleanseResult ExecuteCleanse(
            IChateauDatabase database,
            string characterName,
            string specifiedCurse,
            Random rng)
        {
            var result = new CleanseResult();
            Profile caller = database.GetProfile(characterName);
            if (caller == null)
            {
                result.PrivateMessage = ChateauInteractionHandler.notFoundText(characterName);
                return result;
            }

            List<CurseInstance> curses = CurseInstance.LoadAll(caller);
            if (curses.Count == 0)
            {
                result.PrivateMessage = "You aren't currently burdened by any curses. Someone might be willing to !curse you if that's what you want...";
                return result;
            }

            CurseInstance target;
            if (!string.IsNullOrEmpty(specifiedCurse))
            {
                target = curses.FirstOrDefault(c =>
                    string.Equals(c.Curse, specifiedCurse, StringComparison.OrdinalIgnoreCase));
                if (target == null)
                {
                    result.PrivateMessage = "You aren't currently cursed with [b]" + specifiedCurse + "[/b], "
                        + "but you are cursed with " + DescribeCarriedCurses(curses)
                        + ". Maybe you'd like to !cleanse one of those instead?";
                    return result;
                }
            }
            else if (curses.Count == 1)
            {
                target = curses[0];
            }
            else
            {
                result.PrivateMessage = "You have multiple curses, "
                    + DescribeCarriedCurses(curses)
                    + ". Be sure you specify which you are trying to !cleanse";
                return result;
            }

            string callerName = string.IsNullOrEmpty(caller.displayName) ? caller.userName : caller.displayName;
            string curseName = target.Curse;

            // Resolve cleanse cost. Unknown curse → MissedWork as a safe default (the
            // CatalogMap should always have an entry; this is defensive against a curse
            // that ships in Identifiers without a matching map entry).
            PurgeCostType costType = PurgeCostType.MissedWork;
            if (CurseProcessor.CatalogMap.TryGetValue(curseName ?? string.Empty, out var spec))
            {
                costType = spec.CleanseCost;
            }
            // Defensive: cleanse cost must never recursively apply another curse.
            if (costType == PurgeCostType.RandomCurse)
            {
                costType = PurgeCostType.MissedWork;
            }

            curses.Remove(target);
            CurseInstance.SaveAll(caller, curses);

            // Persist curse-list removal BEFORE the cost applier writes — the applier
            // does its own SetProfile for work/training paths and would otherwise stomp
            // the list update we just made.
            database.SetProfile(characterName, caller);

            PurgeCostResult cost = PurgeCostApplier.Apply(database, caller, costType, rng);

            string suffix = string.IsNullOrEmpty(cost.Description) ? string.Empty : " " + cost.Description;
            result.PrivateMessage = callerName + " is cleansed of the [b]" + curseName + "[/b] curse." + suffix;
            return result;
        }

        /// <summary>
        /// Render the caller's current curse inventory as a comma-separated phrase — used
        /// when !cleanse is ambiguous or when they named one they don't carry, so the
        /// error message points at real options.
        /// </summary>
        private static string DescribeCarriedCurses(List<CurseInstance> curses)
        {
            var parts = curses
                .Select(c => c.Curse)
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(n => "[b]" + n + "[/b]")
                .ToList();
            if (parts.Count == 0) return string.Empty;
            if (parts.Count == 1) return parts[0];
            if (parts.Count == 2) return parts[0] + " and " + parts[1];
            return string.Join(", ", parts.GetRange(0, parts.Count - 1)) + ", and " + parts[parts.Count - 1];
        }
    }
}
