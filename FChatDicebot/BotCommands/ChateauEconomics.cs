using FChatDicebot.BotCommands.Base;
using FChatDicebot.BotCommands.Support;
using FChatDicebot.Model;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Drill-down for the !statistics economy line. Sums every wallet across every profile,
    /// per currency, sorted descending. Lists the full per-currency table — the !statistics
    /// overview only shows the top 3.
    /// </summary>
    public class ChateauEconomics : ChatBotCommand
    {
        public ChateauEconomics()
        {
            Name = "economics";
            Aliases = new string[] { };
            Category = "Information";
            ShortDescription = "View the Chateau's full per-currency wealth across all residents";
            LongDescription = "List every currency in circulation, summed across every resident's account, sorted from most abundant to least. Currencies are not weighted against each other — raw counts only.";
            Usage = "!economics";
            RelatedCommands = new string[] { "statistics", "bank", "work", "sell" };
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
            string message = BuildEconomics(MonDB.getAllProfiles());
            bot.SendPrivateMessage(message, characterName);
        }

        public static string BuildEconomics(List<Profile> profiles)
        {
            var totals = ChateauStatisticsSupport.SumCurrenciesAcrossProfiles(profiles ?? new List<Profile>());
            var positive = totals.Where(kv => kv.Value > 0).ToList();
            if (positive.Count == 0)
            {
                return "No currency has been accumulated by any resident yet. The vaults are quiet.";
            }
            var sb = new System.Text.StringBuilder();
            sb.Append("The accumulated wealth of Chateau residents, by currency:\n");
            foreach (var entry in positive.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key))
            {
                sb.Append("  ").Append(entry.Key).Append(": ")
                  .Append(entry.Value.ToString("N0", CultureInfo.InvariantCulture)).Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }
    }
}
