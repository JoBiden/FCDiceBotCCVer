using FChatDicebot.BotCommands.Base;
using FChatDicebot.BotCommands.Support;
using FChatDicebot.Model;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Drill-down for the !statistics "Converted to Monsterkind" line: lists every
    /// currently-monsterized resident grouped by species, with display names. Snapshot only.
    /// </summary>
    public class ChateauPopulations : ChatBotCommand
    {
        public ChateauPopulations()
        {
            Name = "populations";
            Aliases = new string[] { };
            Category = "Information";
            ShortDescription = "View the Chateau's current monsterized population by species";
            LongDescription = "List every resident currently transformed via !monsterize, grouped by species. Snapshot of who is dwelling as which monster right now.";
            Usage = "!populations";
            RelatedCommands = new string[] { "statistics", "monsterize", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string message = BuildPopulations(MonDB.getAllProfiles());
            bot.SendPrivateMessage(message, characterName);
        }

        public static string BuildPopulations(List<Profile> profiles)
        {
            if (profiles == null) profiles = new List<Profile>();
            // Group monsterized residents by species; keep one display-name list per species.
            var bySpecies = new Dictionary<string, List<string>>();
            foreach (var profile in profiles)
            {
                if (profile?.characteristics == null) continue;
                if (!profile.characteristics.ContainsKey("monster")) continue;
                string species = (profile.characteristics["monster"] ?? string.Empty).ToLowerInvariant();
                if (string.IsNullOrEmpty(species)) continue;
                if (!bySpecies.ContainsKey(species)) bySpecies[species] = new List<string>();
                bySpecies[species].Add(string.IsNullOrEmpty(profile.displayName) ? profile.userName : profile.displayName);
            }

            if (bySpecies.Count == 0)
            {
                return "No residents are currently transformed into monsters. The Chateau awaits its first !monsterize.";
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("Currently dwelling in the Chateau as monsters:\n");
            var ordered = bySpecies.OrderByDescending(kv => kv.Value.Count).ThenBy(kv => kv.Key);
            foreach (var entry in ordered)
            {
                entry.Value.Sort();
                sb.Append("  ").Append(Utils.Capitalize(entry.Key)).Append(": ")
                  .Append(entry.Value.Count).Append(" — ")
                  .Append(string.Join(", ", entry.Value)).Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }
    }
}
