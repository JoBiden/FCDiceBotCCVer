using FChatDicebot.BotCommands.Base;
using FChatDicebot.BotCommands.Support;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Chateau-wide statistics overview. Aggregates lifetime totals and current-population
    /// snapshots into a single summary message; pointers to drill-down commands appear at
    /// the bottom for residents who want details on a specific category.
    /// </summary>
    public class ChateauStatistics : ChatBotCommand
    {
        public ChateauStatistics()
        {
            Name = "statistics";
            Aliases = new string[] { "stats" };
            Category = "Information";
            ShortDescription = "View chateau-wide statistics across every interaction";
            LongDescription = "Display a broad overview of life in the Chateau: population snapshots, lifetime totals, the corruption/purity balance, and current workforce. For category-level breakdowns, see the drill-down commands listed at the bottom of the readout.";
            Usage = "!statistics";
            RelatedCommands = new string[] { "statues", "populations", "flora", "birthrates", "parasites", "payroll", "economics" };
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
            List<Profile> profiles = MonDB.getAllProfiles();
            List<MonsterStats> monsterStats = MonDB.getAllMonsterStats();

            List<Interaction> plantInteractions = MonDB.getInteractionsByType("plant");
            List<Interaction> petrifyInteractions = MonDB.getInteractionsByType("petrify");
            List<Interaction> infestInteractions = MonDB.getInteractionsByType(InfestProcessor.InfestType);
            List<Interaction> purgeInteractions = MonDB.getInteractionsByType(ChateauPurge.PurgeType);
            List<Interaction> climaxforInteractions = MonDB.getInteractionsByType(ClimaxforProcessor.ClimaxforType);
            List<Interaction> climaxInteractions = MonDB.getInteractionsByType(ClimaxforProcessor.ClimaxType);
            List<Interaction> corruptInteractions = MonDB.getInteractionsByType(CorruptionProcessor.CorruptType);
            List<Interaction> purifyInteractions = MonDB.getInteractionsByType(CorruptionProcessor.PurifyType);

            string message = BuildStatistics(
                profiles, monsterStats,
                plantInteractions, petrifyInteractions,
                infestInteractions, purgeInteractions,
                climaxforInteractions, climaxInteractions,
                corruptInteractions, purifyInteractions);

            bot.SendPrivateMessage(message, characterName);
        }

        /// <summary>
        /// Pure render path — every data source is passed in so unit tests can exercise the
        /// wording rules (ties, net-tilt sign, empty-state) without touching MongoDB.
        /// </summary>
        public static string BuildStatistics(
            List<Profile> profiles, List<MonsterStats> monsterStats,
            List<Interaction> plantInteractions, List<Interaction> petrifyInteractions,
            List<Interaction> infestInteractions, List<Interaction> purgeInteractions,
            List<Interaction> climaxforInteractions, List<Interaction> climaxInteractions,
            List<Interaction> corruptInteractions, List<Interaction> purifyInteractions)
        {
            if (profiles == null) profiles = new List<Profile>();

            // --- Population snapshot + lifetime ---
            var monsterized = ChateauStatisticsSupport.CountMonsterizedByType(profiles);
            int monsterizedTotal = monsterized.Values.Sum();
            string monsterizedTop = ChateauStatisticsSupport.MostFromMap(monsterized);

            var offspringByType = ChateauStatisticsSupport.OffspringByMonsterType(monsterStats);
            int birthedTotal = offspringByType.Values.Sum();
            string birthedTop = ChateauStatisticsSupport.MostFromMap(offspringByType);

            var plantsByType = ChateauStatisticsSupport.CountByIdentifier(plantInteractions);
            int plantsTotal = plantsByType.Values.Sum();
            string plantsTop = ChateauStatisticsSupport.MostFromMap(plantsByType);

            var statuesByLocation = ChateauStatisticsSupport.CountByIdentifier(petrifyInteractions);
            int statuesTotal = statuesByLocation.Values.Sum();
            string statuesTop = ChateauStatisticsSupport.MostFromMap(statuesByLocation);

            var infestedHosts = ChateauStatisticsSupport.CountCurrentParasiteHosts(profiles);
            int infestedDistinctHosts = profiles.Count(p =>
                p?.lists != null && p.lists.ContainsKey(ParasiteInstance.ParasitesListKey)
                && p.lists[ParasiteInstance.ParasitesListKey].Count > 0);
            string infestedTop = ChateauStatisticsSupport.MostFromMap(infestedHosts, ParasiteText.ParasiteName);

            var lifetimeSpread = ChateauStatisticsSupport.CountLifetimeParasiteSpread(profiles, infestInteractions);
            int parasitesSpreadTotal = lifetimeSpread.Values.Sum();
            int parasitesPurgedTotal = (purgeInteractions?.Count) ?? 0;

            // --- Influence ---
            int climaxes = ((climaxforInteractions?.Count) ?? 0) + ((climaxInteractions?.Count) ?? 0);
            int corruption = ChateauStatisticsSupport.SumCorruptionVolume(corruptInteractions, CorruptionProcessor.CorruptType);
            int purity = ChateauStatisticsSupport.SumCorruptionVolume(purifyInteractions, CorruptionProcessor.PurifyType);

            // --- Workforce ---
            var jobsBySpecies = ChateauStatisticsSupport.CountEmployedByJob(profiles);
            int totalEmployees = jobsBySpecies.Values.Sum();
            string topJobs = ChateauStatisticsSupport.FormatTopJobs(jobsBySpecies, 3);

            var dutiesByJob = ChateauStatisticsSupport.SumDutiesByJob(profiles);
            int totalDuties = dutiesByJob.Values.Sum();

            var currencyTotals = ChateauStatisticsSupport.SumCurrenciesAcrossProfiles(profiles);
            string topCurrencies = FormatTopCurrencies(currencyTotals, 3);

            // --- Compose ---
            var sb = new System.Text.StringBuilder();
            sb.Append("A record of life within the Chateau, in broad strokes~\n");

            sb.Append("[b]Population[/b]\n");
            AppendLifetimeOrSnapshotLine(sb, "Converted to Monsterkind", monsterizedTotal, "most common", monsterizedTop);
            AppendLifetimeOrSnapshotLine(sb, "Monsters birthed", birthedTotal, "most bred", birthedTop);
            AppendLifetimeOrSnapshotLine(sb, "People planted", plantsTotal, "most planted", plantsTop);
            AppendLifetimeOrSnapshotLine(sb, "Statues petrified", statuesTotal, "most decorated", statuesTop);
            AppendLifetimeOrSnapshotLine(sb, "Infested Individuals", infestedDistinctHosts, "most widespread", infestedTop);
            if (parasitesSpreadTotal > 0 || parasitesPurgedTotal > 0)
            {
                sb.Append("  Parasites spread: ").Append(FormatNumber(parasitesSpreadTotal))
                  .Append("   Parasites purged: ").Append(FormatNumber(parasitesPurgedTotal)).Append('\n');
            }

            sb.Append("[b]Influence[/b]\n");
            if (climaxes > 0)
            {
                sb.Append("  Climaxes recorded: ").Append(FormatNumber(climaxes)).Append('\n');
            }
            if (corruption > 0 || purity > 0)
            {
                sb.Append("  Corruption Cultivated: ").Append(FormatNumber(corruption))
                  .Append("    Purity Promoted: ").Append(FormatNumber(purity)).Append('\n');
                sb.Append("  ").Append(FormatNetTilt(corruption, purity)).Append('\n');
            }

            sb.Append("[b]Workforce[/b]\n");
            if (totalEmployees > 0)
            {
                sb.Append("  Total employees: ").Append(FormatNumber(totalEmployees)).Append('\n');
                if (!string.IsNullOrEmpty(topJobs))
                {
                    sb.Append("  Most employed: ").Append(topJobs).Append('\n');
                }
            }
            if (totalDuties > 0)
            {
                sb.Append("  Duties completed: ").Append(FormatNumber(totalDuties)).Append('\n');
            }
            if (!string.IsNullOrEmpty(topCurrencies))
            {
                sb.Append("  Most earned currencies - ").Append(topCurrencies).Append('\n');
            }

            sb.Append("[sub]Further information can be found through !statues, !populations,\n");
            sb.Append("!flora, !birthrates, !parasites, !payroll, !economics.[/sub]");
            return sb.ToString();
        }

        private static void AppendLifetimeOrSnapshotLine(System.Text.StringBuilder sb, string label, int total, string superlativeLabel, string superlativeValue)
        {
            if (total <= 0) return; // hide the whole line when there's nothing to count yet
            sb.Append("  ").Append(label).Append(": ").Append(FormatNumber(total));
            if (!string.IsNullOrEmpty(superlativeValue))
            {
                sb.Append(" (").Append(superlativeLabel).Append(": ").Append(superlativeValue).Append(')');
            }
            sb.Append('\n');
        }

        private static string FormatTopCurrencies(Dictionary<string, int> totals, int topN)
        {
            if (totals == null || totals.Count == 0) return string.Empty;
            var ordered = totals
                .Where(kv => kv.Value > 0)
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .Take(topN)
                .Select(kv => Utils.Capitalize(kv.Key) + ": " + FormatNumber(kv.Value));
            return string.Join("    ", ordered);
        }

        /// <summary>
        /// Pick the right phrasing for the corruption-vs-purity tug-of-war line. Ties resolve
        /// to the "Balanced, as all things should be" line — user-approved.
        /// </summary>
        public static string FormatNetTilt(int corruption, int purity)
        {
            if (corruption == purity) return "Balanced, as all things should be";
            if (corruption > purity)
            {
                return "Corruption Conquers Purity by " + FormatNumber(corruption - purity);
            }
            return "Purity Prevails over Corruption by " + FormatNumber(purity - corruption);
        }

        private static string FormatNumber(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
