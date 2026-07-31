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
            sb.Append(ReadoutText.Title("The Chateau at a Glance")).Append('\n');
            sb.Append("A record of life within the Chateau, in broad strokes~\n\n");

            // Each block's rows are gathered first and the header is only emitted when at least
            // one survived. Previously the headers were unconditional, so a young Chateau with
            // no corruption or climaxes yet printed a bare "Influence" heading introducing
            // nothing — invisible when headers were plain, obvious once they took a colour.
            var population = new List<string>();
            AppendLifetimeOrSnapshotLine(population, "Converted to Monsterkind", monsterizedTotal, "most common", monsterizedTop);
            AppendLifetimeOrSnapshotLine(population, "Monsters Birthed", birthedTotal, "most bred", birthedTop);
            AppendLifetimeOrSnapshotLine(population, "People Planted", plantsTotal, "most planted", plantsTop);
            AppendLifetimeOrSnapshotLine(population, "Statues Petrified", statuesTotal, "most decorated", statuesTop);
            AppendLifetimeOrSnapshotLine(population, "Infested Individuals", infestedDistinctHosts, "most widespread", infestedTop);
            if (parasitesSpreadTotal > 0 || parasitesPurgedTotal > 0)
            {
                population.Add(ReadoutText.Row("Parasites Spread", ReadoutText.Num(FormatNumber(parasitesSpreadTotal)))
                    + ReadoutText.InlineSeparator
                    + ReadoutText.Row("Parasites Purged", ReadoutText.Num(FormatNumber(parasitesPurgedTotal))));
            }
            AppendBlock(sb, "Population", ReadoutDomain.Relationship, population);

            var influence = new List<string>();
            if (climaxes > 0)
            {
                influence.Add(ReadoutText.Row("Climaxes Recorded", ReadoutText.Num(FormatNumber(climaxes))));
            }
            if (corruption > 0 || purity > 0)
            {
                influence.Add(ReadoutText.Row("Corruption Cultivated", ReadoutText.Num(FormatNumber(corruption)))
                    + ReadoutText.InlineSeparator
                    + ReadoutText.Row("Purity Promoted", ReadoutText.Num(FormatNumber(purity))));
                influence.Add(FormatNetTilt(corruption, purity));
            }
            AppendBlock(sb, "Influence", ReadoutDomain.Affliction, influence);

            var workforce = new List<string>();
            if (totalEmployees > 0)
            {
                workforce.Add(ReadoutText.Row("Total Employees", ReadoutText.Num(FormatNumber(totalEmployees))));
                if (!string.IsNullOrEmpty(topJobs))
                {
                    workforce.Add(ReadoutText.Row("Most Employed", topJobs));
                }
            }
            if (totalDuties > 0)
            {
                workforce.Add(ReadoutText.Row("Duties Completed", ReadoutText.Num(FormatNumber(totalDuties))));
            }
            if (!string.IsNullOrEmpty(topCurrencies))
            {
                workforce.Add(ReadoutText.Row("Most Earned", topCurrencies));
            }
            AppendBlock(sb, "Workforce", ReadoutDomain.Economy, workforce);

            sb.Append(ReadoutText.Footer("Further information can be found through !statues, !populations,"
                + " !flora, !birthrates, !parasites, !payroll, !economics."));
            return sb.ToString();
        }

        /// <summary>
        /// Emits a headed block, or nothing at all when no row qualified.
        /// </summary>
        private static void AppendBlock(System.Text.StringBuilder sb, string header, ReadoutDomain domain, List<string> rows)
        {
            if (rows.Count == 0) return;
            sb.Append(ReadoutText.Section(header, domain)).Append('\n');
            foreach (string row in rows)
            {
                sb.Append(ReadoutText.RowIndent).Append(row).Append('\n');
            }
        }

        private static void AppendLifetimeOrSnapshotLine(List<string> rows, string label, int total, string superlativeLabel, string superlativeValue)
        {
            if (total <= 0) return; // hide the whole line when there's nothing to count yet
            string row = ReadoutText.Row(label, ReadoutText.Num(FormatNumber(total)));
            if (!string.IsNullOrEmpty(superlativeValue))
            {
                // Small print: the superlative is context for the number, not a second fact.
                row += " " + ReadoutText.Small("(" + superlativeLabel + ": " + superlativeValue + ")");
            }
            rows.Add(row);
        }

        private static string FormatTopCurrencies(Dictionary<string, int> totals, int topN)
        {
            if (totals == null || totals.Count == 0) return string.Empty;
            // Plain "Name: value" cells rather than [u] labels: these sit inside a row that
            // already carries one, and nesting underlines makes the line unreadable.
            var ordered = totals
                .Where(kv => kv.Value > 0)
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .Take(topN)
                .Select(kv => Utils.Capitalize(kv.Key) + ": " + ReadoutText.Num(FormatNumber(kv.Value)));
            return string.Join(ReadoutText.InlineSeparator, ordered);
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
