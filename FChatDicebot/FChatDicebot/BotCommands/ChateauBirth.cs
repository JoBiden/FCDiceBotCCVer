using System;
using System.Collections.Generic;
using System.Linq;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauBirth : ChatBotCommand
    {
        public ChateauBirth()
        {
            Name = "birth";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Release a pregnancy that has finished gestating";
            LongDescription = "Self-action: release one of your pending pregnancies once it has finished gestating. With no argument, the oldest ready pregnancy is birthed. Pass a 1-based index to pick a specific ready pregnancy. (NPC naming arguments are accepted but ignored until the NPC system is installed.)";
            Usage = "!birth\nor\n!birth {index}\nor\n!birth {index} name=\"Name\"\nor\n!birth {index} name=\"Name\" description=\"...\"";
            RelatedCommands = new string[] { "breed", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string completion = ExecuteBirth(MonDB.GetDatabase(), characterName, rawTerms, out string error);
            if (error != null)
            {
                bot.SendPrivateMessage(error, characterName);
                return;
            }
            bot.SendMessageInChannel(completion, channel);
        }

        /// <summary>
        /// Pure birthing logic, factored out for testability. Returns the completion message on
        /// success and sets <paramref name="error"/> to a non-null reason on failure.
        /// </summary>
        public static string ExecuteBirth(Database.IChateauDatabase database, string characterName, string[] rawTerms, out string error)
        {
            error = null;
            Profile carrier = database.GetProfile(characterName);
            if (carrier == null)
            {
                error = ChateauInteractionHandler.notFoundText(characterName);
                return null;
            }

            List<Pregnancy> pregnancies = carrier.pregnancies ?? new List<Pregnancy>();
            DateTime now = DateTime.UtcNow;

            var ordered = pregnancies
                .Select((p, originalIndex) => new { Pregnancy = p, OriginalIndex = originalIndex })
                .OrderBy(x => x.Pregnancy.ConceivedAt)
                .ToList();
            var ready = ordered.Where(x => x.Pregnancy.ReadyAt <= now).ToList();

            int? requestedIndex = ParseIndex(rawTerms);

            if (ready.Count == 0)
            {
                if (pregnancies.Count == 0)
                {
                    error = "You have no pregnancies to birth.";
                }
                else
                {
                    var soonest = ordered.OrderBy(x => x.Pregnancy.ReadyAt).First().Pregnancy;
                    string remaining = Utils.GetTimeSpanPrint(soonest.ReadyAt - now);
                    error = "None of your pregnancies are ready yet. The next one is ready in " + remaining + ".";
                }
                return null;
            }

            var target = ready[0];
            if (requestedIndex.HasValue)
            {
                int idx = requestedIndex.Value;
                if (idx < 1 || idx > ready.Count)
                {
                    error = "Pregnancy index out of range. You have " + ready.Count + " ready "
                        + (ready.Count == 1 ? "pregnancy" : "pregnancies") + ".";
                    return null;
                }
                target = ready[idx - 1];
            }

            carrier.pregnancies.RemoveAt(target.OriginalIndex);

            string offspringLabel = "offspring";
            if (carrier.lists == null)
            {
                carrier.lists = new Dictionary<string, List<string>>();
            }
            if (!carrier.lists.ContainsKey(offspringLabel))
            {
                carrier.lists[offspringLabel] = new List<string>();
            }
            string offspringEntry = now.ToString("yyyy-MM-dd") + ": " + target.Pregnancy.MonsterType
                + " brood of " + target.Pregnancy.BroodSize
                + " (parent: " + target.Pregnancy.Initiator + ")";
            carrier.lists[offspringLabel].Add(offspringEntry);

            database.SetProfile(characterName, carrier);
            database.IncrementCount(characterName, "birth");

            string broodPhrase = target.Pregnancy.BroodSize > 1
                ? "a brood of " + target.Pregnancy.BroodSize + " " + target.Pregnancy.MonsterType + "s"
                : Utils.AnOrA(target.Pregnancy.MonsterType) + " " + target.Pregnancy.MonsterType;

            return carrier.displayName + " has given birth to " + broodPhrase
                + "! (Sired by " + target.Pregnancy.Initiator + ".)";
        }

        private static int? ParseIndex(string[] rawTerms)
        {
            if (rawTerms == null) return null;
            foreach (string term in rawTerms)
            {
                if (string.IsNullOrEmpty(term)) continue;
                if (int.TryParse(term, out int parsed))
                {
                    return parsed;
                }
            }
            return null;
        }
    }
}
