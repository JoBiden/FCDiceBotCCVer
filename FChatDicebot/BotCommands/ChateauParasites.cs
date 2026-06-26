using FChatDicebot.BotCommands.Base;
using FChatDicebot.BotCommands.Support;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Drill-down for !statistics' parasite lines. Per-parasite: current distinct hosts +
    /// lifetime ever-spread (= direct !infest events + current spread-from-contact instances)
    /// + lifetime purges. Distinct from !statistics in that it lists every parasite ever
    /// encountered, not just the most widespread.
    /// </summary>
    public class ChateauParasites : ChatBotCommand
    {
        public ChateauParasites()
        {
            Name = "parasites";
            Aliases = new string[] { };
            Category = "Information";
            ShortDescription = "View every parasite ever spread in the Chateau, with current and lifetime counts";
            LongDescription = "List every parasite type ever recorded in the Chateau. For each: how many residents currently carry it, how many times it has ever been spread (direct !infest plus contact-spread cases), and how many times it has been !purged.";
            Usage = "!parasites";
            RelatedCommands = new string[] { "statistics", "infest", "purge", "dossier" };
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
            string channel = address.channel;
            string message = BuildParasites(
                MonDB.getAllProfiles(),
                MonDB.getInteractionsByType(InfestProcessor.InfestType),
                MonDB.getInteractionsByType(ChateauPurge.PurgeType));
            bot.SendPrivateMessage(message, characterName);
        }

        public static string BuildParasites(List<Profile> profiles, List<Interaction> infestInteractions, List<Interaction> purgeInteractions)
        {
            if (profiles == null) profiles = new List<Profile>();
            var current = ChateauStatisticsSupport.CountCurrentParasiteHosts(profiles);
            var spread = ChateauStatisticsSupport.CountLifetimeParasiteSpread(profiles, infestInteractions);
            var purged = ChateauStatisticsSupport.CountLifetimePurges(purgeInteractions);

            var allParasites = new HashSet<string>();
            foreach (var k in current.Keys) allParasites.Add(k);
            foreach (var k in spread.Keys) allParasites.Add(k);
            foreach (var k in purged.Keys) allParasites.Add(k);

            if (allParasites.Count == 0)
            {
                return "No parasites have ever been recorded in the Chateau. The residents remain unbothered.";
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("Parasites recorded in the Chateau:\n");
            var ordered = allParasites
                .Select(p => new
                {
                    Name = p,
                    Current = current.ContainsKey(p) ? current[p] : 0,
                    Spread = spread.ContainsKey(p) ? spread[p] : 0,
                    Purged = purged.ContainsKey(p) ? purged[p] : 0
                })
                .OrderByDescending(x => x.Spread)
                .ThenByDescending(x => x.Current)
                .ThenBy(x => x.Name);

            foreach (var entry in ordered)
            {
                sb.Append("  ").Append(Utils.Capitalize(ParasiteText.ParasiteName(entry.Name))).Append(": ")
                  .Append(entry.Current.ToString("N0", CultureInfo.InvariantCulture)).Append(" currently infested, ")
                  .Append(entry.Spread.ToString("N0", CultureInfo.InvariantCulture)).Append(" spread, ")
                  .Append(entry.Purged.ToString("N0", CultureInfo.InvariantCulture)).Append(" purged\n");
            }
            return sb.ToString().TrimEnd('\n');
        }
    }
}
