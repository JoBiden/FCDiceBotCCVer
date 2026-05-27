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
    /// !purge is the self-targeted reversal for !infest. Removes one parasite at a cost,
    /// unless the parasite was spread-acquired within the
    /// <see cref="InfestProcessor.SpreadGracePeriod"/> grace window — in which case it
    /// purges for free. With no argument the caller must have exactly one parasite to
    /// disambiguate the target; zero parasites yields a polite refusal, two or more yields
    /// a "name it" error.
    /// </summary>
    public class ChateauPurge : ChatBotCommand
    {
        public ChateauPurge()
        {
            Name = "purge";
            Aliases = new string[] { };
            Category = "Recovery";
            ShortDescription = "Purge a parasite from your body, at a cost (unless caught early).";
            LongDescription = "Remove one of your active parasites. Each parasite has a specific cost of removal, which could be missing a day of work, losing training prowess, exhausting a body part (see !break and !rest), or suffering a curse. Look at the details for a specific parasite with !whatis {parasite} to see what that cost is specifically.";
            Usage = "!purge\n!purge {parasite}";
            RelatedCommands = new string[] { "infest", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = InfestProcessor.ParasiteCategory;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string specifiedParasite = commandController.GetIdentifierFromCommandTerms(rawTerms, InfestProcessor.ParasiteCategory);
            PurgeResult result = ExecutePurge(MonDB.GetDatabase(), characterName, specifiedParasite);

            if (!string.IsNullOrEmpty(result.ChannelMessage))
            {
                bot.SendMessageInChannel(result.ChannelMessage, channel);
            }
            if (!string.IsNullOrEmpty(result.PrivateMessage))
            {
                bot.SendPrivateMessage(result.PrivateMessage, characterName);
            }
        }

        public class PurgeResult
        {
            public string ChannelMessage { get; set; } = string.Empty;
            public string PrivateMessage { get; set; } = string.Empty;
        }

        /// <summary>
        /// Pure purge logic, factored out for testability. ChannelMessage is non-empty on a
        /// successful purge; PrivateMessage is non-empty when the caller has nothing to purge,
        /// named a parasite they don't carry, or has multiple parasites and didn't specify which.
        /// </summary>
        /// <param name="specifiedParasite">Empty/null forces the single-parasite shortcut:
        /// purges when the caller carries exactly one, otherwise asks which.</param>
        public static PurgeResult ExecutePurge(IChateauDatabase database, string characterName, string specifiedParasite)
        {
            return ExecutePurge(database, characterName, specifiedParasite, new Random());
        }

        /// <summary>
        /// Test seam: caller supplies the rng used by the cost applier (which break part is
        /// picked, which training is decremented).
        /// </summary>
        public static PurgeResult ExecutePurge(
            IChateauDatabase database,
            string characterName,
            string specifiedParasite,
            Random rng)
        {
            var result = new PurgeResult();
            Profile caller = database.GetProfile(characterName);
            if (caller == null)
            {
                result.PrivateMessage = ChateauInteractionHandler.notFoundText(characterName);
                return result;
            }

            List<ParasiteInstance> parasites = ParasiteInstance.LoadAll(caller);
            if (parasites.Count == 0)
            {
                result.PrivateMessage = "You aren't currently infested with any parasites. If you'd like to change that, see if someone is willing to !infest you...";
                return result;
            }

            ParasiteInstance target;
            if (!string.IsNullOrEmpty(specifiedParasite))
            {
                target = parasites.FirstOrDefault(p =>
                    string.Equals(p.Parasite, specifiedParasite, StringComparison.OrdinalIgnoreCase));
                if (target == null)
                {
                    result.PrivateMessage = "You aren't currently infested with "
                        + ParasiteText.ParasiteName(specifiedParasite)
                        + ", but you are infested with " + DescribeCarriedParasites(parasites)
                        + ". Maybe you'd like to !purge one of those instead?";
                    return result;
                }
            }
            else if (parasites.Count == 1)
            {
                target = parasites[0];
            }
            else
            {
                result.PrivateMessage = "You're infested with more than one parasite — "
                    + DescribeCarriedParasites(parasites)
                    + ". Specify which to purge: !purge {parasite}.";
                return result;
            }

            string callerName = string.IsNullOrEmpty(caller.displayName) ? caller.userName : caller.displayName;
            string parasiteName = target.Parasite;
            string parasitePhrase = ParasiteText.ParasiteName(parasiteName);
            DateTime now = DateTime.UtcNow;
            bool inGrace = target.SpreadFromContact && now < target.GraceUntil;

            parasites.Remove(target);
            ParasiteInstance.SaveAll(caller, parasites);
            database.SetProfile(characterName, caller);

            // Detox precedent: purge outcomes are private to the caller — nobody else
            // needs to see who's being eaten from the inside by withdrawal.
            if (inGrace)
            {
                result.PrivateMessage = callerName + " catches the " + parasitePhrase
                    + " infestation early and purges it before it can take hold.";
                return result;
            }

            PurgeCostType costType = InfestProcessor.PurgeCostFor(parasiteName);
            PurgeCostResult cost = PurgeCostApplier.Apply(database, caller, costType, rng);

            string suffix = string.IsNullOrEmpty(cost.Description) ? string.Empty : " " + cost.Description;
            result.PrivateMessage = callerName + " purges " + parasitePhrase + " from their body." + suffix;
            return result;
        }

        /// <summary>
        /// Render the user's current parasite inventory as a comma-separated phrase — used
        /// when the !purge target is ambiguous or when they named one they don't carry, so
        /// the error points at real options.
        /// </summary>
        private static string DescribeCarriedParasites(List<ParasiteInstance> parasites)
        {
            var parts = parasites
                .Select(p => p.Parasite)
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(ParasiteText.ParasiteName)
                .ToList();
            if (parts.Count == 0) return string.Empty;
            if (parts.Count == 1) return parts[0];
            if (parts.Count == 2) return parts[0] + " and " + parts[1];
            return string.Join(", ", parts.GetRange(0, parts.Count - 1)) + ", and " + parts[parts.Count - 1];
        }
    }
}
