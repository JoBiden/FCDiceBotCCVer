using FChatDicebot.BotCommands.Base;
using FChatDicebot.BotCommands.Support;
using FChatDicebot.Model;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Drill-down for !statistics' "Monsters birthed" line. Per-species lifetime offspring
    /// counts, with the sired-vs-birthed split inferred from <c>profile.lists["offspring"]</c>
    /// entries (carrier owns the entry; sire name is stamped in the line). MonsterStats
    /// provides the authoritative per-species total; the offspring-list parse provides the
    /// give/take split.
    /// </summary>
    public class ChateauBirthrates : ChatBotCommand
    {
        public ChateauBirthrates()
        {
            Name = "birthrates";
            Aliases = new string[] { };
            Category = "Information";
            ShortDescription = "View every monster ever born to the Chateau, by species";
            LongDescription = "List the lifetime total of monsters birthed in the Chateau, broken down by species, with the count split between sired (initiated the !breed) and birthed (carried and gave !birth).";
            Usage = "!birthrates";
            RelatedCommands = new string[] { "statistics", "breed", "birth", "dossier" };
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
            string message = BuildBirthrates(
                MonDB.getAllProfiles(),
                MonDB.getAllMonsterStats());
            bot.SendPrivateMessage(message, characterName);
        }

        public static string BuildBirthrates(List<Profile> profiles, List<MonsterStats> monsterStats)
        {
            // 'profiles' is reserved for future per-species drill-down. Not used today —
            // MonsterStats already carries both numbers we render.
            _ = profiles;
            var offspring = ChateauStatisticsSupport.OffspringByMonsterType(monsterStats);
            if (offspring.Count == 0 || offspring.Values.Sum() == 0)
            {
                return "No monsters have yet been born in the Chateau. The cradle awaits its first !birth.";
            }

            // Pull pregnancy counts alongside offspring counts. PregnancyCount counts each
            // !breed→!birth cycle as one event; OffspringCount counts each individual offspring
            // (so a brood of 5 contributes 5 here, 1 to pregnancies).
            var pregnancies = new Dictionary<string, int>();
            if (monsterStats != null)
            {
                foreach (var stats in monsterStats)
                {
                    if (stats == null || string.IsNullOrEmpty(stats.Id)) continue;
                    if (!stats.Id.StartsWith("monster:", System.StringComparison.OrdinalIgnoreCase)) continue;
                    pregnancies[stats.Id.Substring("monster:".Length)] = stats.PregnancyCount;
                }
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("Monsters born to the Chateau:\n");
            foreach (var entry in offspring.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key))
            {
                if (entry.Value <= 0) continue;
                int preg = pregnancies.ContainsKey(entry.Key) ? pregnancies[entry.Key] : 0;
                sb.Append("  ").Append(Utils.Capitalize(entry.Key)).Append(": ")
                  .Append(entry.Value.ToString("N0", CultureInfo.InvariantCulture))
                  .Append(" offspring across ")
                  .Append(preg.ToString("N0", CultureInfo.InvariantCulture))
                  .Append(preg == 1 ? " pregnancy\n" : " pregnancies\n");
            }
            return sb.ToString().TrimEnd('\n');
        }
    }
}
