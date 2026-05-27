using FChatDicebot.BotCommands.Base;
using FChatDicebot.BotCommands.Support;
using FChatDicebot.Model;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Drill-down for the !statistics "People planted" line: lifetime plant cultivations
    /// grouped by plant type, sorted descending. Counts !plant interactions; !plant has no
    /// reversal so a "currently growing" snapshot is identical to the lifetime total.
    /// </summary>
    public class ChateauFlora : ChatBotCommand
    {
        public ChateauFlora()
        {
            Name = "flora";
            Aliases = new string[] { };
            Category = "Information";
            ShortDescription = "View every plant ever cultivated in the Chateau gardens";
            LongDescription = "List every plant type ever cultivated via !plant, with lifetime counts. The gardens never forget.";
            Usage = "!flora";
            RelatedCommands = new string[] { "statistics", "plant", "dossier" };
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
            string message = BuildFlora(MonDB.getInteractionsByType("plant"));
            bot.SendPrivateMessage(message, characterName);
        }

        public static string BuildFlora(List<Interaction> plantInteractions)
        {
            var counts = ChateauStatisticsSupport.CountByIdentifier(plantInteractions);
            if (counts.Count == 0)
            {
                return "Nothing has ever been planted in the Chateau gardens. The earth waits for its first !plant.";
            }
            var sb = new System.Text.StringBuilder();
            sb.Append("Plants ever cultivated in the Chateau gardens:\n");
            foreach (var entry in counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key))
            {
                sb.Append("  ").Append(Utils.Capitalize(entry.Key)).Append(": ")
                  .Append(entry.Value.ToString("N0", CultureInfo.InvariantCulture)).Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }
    }
}
