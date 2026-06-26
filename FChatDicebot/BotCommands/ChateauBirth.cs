using System;
using System.Collections.Generic;
using System.Linq;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Commitment;
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
            ShortDescription = "Birth a pregnancy that has finished gestating";
            LongDescription = "Birth your young once they have finished gestating. If multiple pregnancies are ready to be born, you will be messaged a numbered list of the pregnancies, and can choose which pregnancy to birth, or '!birth all' to birth every pregnancy you are able to.";
            Usage = "!birth\nor\n!birth {index}\n";
            RelatedCommands = new string[] { "breed", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            BirthResult result = ExecuteBirth(MonDB.GetDatabase(), characterName, rawTerms);
            if (!string.IsNullOrEmpty(result.PrivateMessage))
            {
                bot.SendPrivateMessage(result.PrivateMessage, characterName);
            }
            if (!string.IsNullOrEmpty(result.ChannelMessage))
            {
                bot.SendMessageInChannel(result.ChannelMessage, channel);
            }
        }

        public class BirthResult
        {
            public string ChannelMessage { get; set; } = string.Empty;
            public string PrivateMessage { get; set; } = string.Empty;
        }

        /// <summary>
        /// Pure birthing logic, factored out for testability. The returned result has
        /// non-empty ChannelMessage on a successful birth (or '!birth all'), non-empty
        /// PrivateMessage on errors or when the user has multiple ready pregnancies and
        /// must pick one. Both can be set simultaneously: when listing the choices to a
        /// user, a short prompt is also posted in the channel.
        /// </summary>
        public static BirthResult ExecuteBirth(IChateauDatabase database, string characterName, string[] rawTerms)
        {
            var result = new BirthResult();
            Profile carrier = database.GetProfile(characterName);
            if (carrier == null)
            {
                result.PrivateMessage = ChateauInteractionHandler.notFoundText(characterName);
                return result;
            }

            List<Pregnancy> pregnancies = carrier.pregnancies ?? new List<Pregnancy>();
            DateTime now = DateTime.UtcNow;

            var ready = pregnancies
                .Where(p => p.ReadyAt <= now)
                .OrderBy(p => p.ConceivedAt)
                .ToList();

            if (ready.Count == 0)
            {
                if (pregnancies.Count == 0)
                {
                    result.PrivateMessage = "You aren't pregnant! Ask someone to !breed you first.";
                }
                else
                {
                    var soonest = pregnancies.OrderBy(p => p.ReadyAt).First();
                    string remaining = Utils.GetTimeSpanPrint(soonest.ReadyAt - now);
                    result.PrivateMessage = pregnancies.Count == 1
                        ? "You aren't ready to give birth yet. Your young will be ready in " + remaining + "."
                        : "None of your young are ready yet. The next will be ready in " + remaining + ".";
                }
                return result;
            }

            bool birthAll = HasAllTerm(rawTerms);
            int? requestedIndex = birthAll ? (int?)null : ParseIndex(rawTerms);

            // Multi-ready, no explicit choice → list them and let the user pick. Mirrors
            // the multi-pending-!consent UX in ChateauConsent.cs.
            if (ready.Count > 1 && !birthAll && !requestedIndex.HasValue)
            {
                result.PrivateMessage = BuildReadyList(ready);
                result.ChannelMessage = "There's multiple pregnancies ready to be born, " + characterName
                    + ". Either '!birth all', or refer to the list we just messaged you.";
                return result;
            }

            // Decide which pregnancies to birth.
            List<Pregnancy> toBirth;
            if (birthAll)
            {
                toBirth = ready;
            }
            else if (requestedIndex.HasValue)
            {
                int idx = requestedIndex.Value;
                if (idx < 1 || idx > ready.Count)
                {
                    result.PrivateMessage = "Pregnancy index out of range. You have " + ready.Count + " ready "
                        + (ready.Count == 1 ? "pregnancy" : "pregnancies") + ".";
                    return result;
                }
                toBirth = new List<Pregnancy> { ready[idx - 1] };
            }
            else
            {
                toBirth = new List<Pregnancy> { ready[0] };
            }

            // All in-memory mutations on the carrier, then one save. References from
            // 'toBirth' point into carrier.pregnancies, so List.Remove finds them.
            if (carrier.lists == null) carrier.lists = new Dictionary<string, List<string>>();
            if (!carrier.lists.ContainsKey("offspring")) carrier.lists["offspring"] = new List<string>();

            var channelParts = new List<string>();
            foreach (var pregnancy in toBirth)
            {
                carrier.pregnancies.Remove(pregnancy);
                carrier.lists["offspring"].Add(now.ToString("yyyy-MM-dd") + ": " + pregnancy.MonsterType
                    + " brood of " + pregnancy.BroodSize
                    + " (parent: " + pregnancy.Initiator + ")");
                channelParts.Add(BuildCompletionMessage(carrier, pregnancy));
            }

            database.SetProfile(characterName, carrier);
            for (int i = 0; i < toBirth.Count; i++)
            {
                database.IncrementCount(characterName, "birth");
            }

            // Global offspring counters (monster type + each snapshotted category).
            foreach (var pregnancy in toBirth)
            {
                IncrementGlobalOffspringCounts(database, pregnancy);
            }

            result.ChannelMessage = string.Join("\n", channelParts);
            return result;
        }

        private static void IncrementGlobalOffspringCounts(IChateauDatabase database, Pregnancy pregnancy)
        {
            if (string.IsNullOrEmpty(pregnancy.MonsterType) || pregnancy.BroodSize <= 0) return;
            database.IncrementMonsterStats(BreedProcessor.MonsterStatsKey(pregnancy.MonsterType), pregnancyDelta: 0, offspringDelta: pregnancy.BroodSize);
            if (pregnancy.Categories == null) return;
            foreach (var category in pregnancy.Categories)
            {
                if (string.IsNullOrEmpty(category)) continue;
                database.IncrementMonsterStats(BreedProcessor.CategoryStatsKey(category), pregnancyDelta: 0, offspringDelta: pregnancy.BroodSize);
            }
        }

        private static string BuildCompletionMessage(Profile carrier, Pregnancy pregnancy)
        {
            // Rare-twins flavor: the resolved brood range was [1,1] and the 1% roll fired.
            if (pregnancy.IsRareTwins)
            {
                return carrier.displayName + " has miraculously given birth to a pair of " + pregnancy.MonsterType
                    + " twins — a rare blessing for a species that normally bears just one! (Sired by "
                    + pregnancy.Initiator + ".)";
            }

            string broodPhrase = pregnancy.BroodSize > 1
                ? "a brood of " + pregnancy.BroodSize + " " + pregnancy.MonsterType + "s"
                : Utils.AnOrA(pregnancy.MonsterType) + " " + pregnancy.MonsterType;

            return carrier.displayName + " has given birth to " + broodPhrase
                + "! (Sired by " + pregnancy.Initiator + ".)";
        }

        private static string BuildReadyList(List<Pregnancy> ready)
        {
            string msg = "Use '!birth #' to birth the pregnancy of your choice, or '!birth all' to birth all of them. You have the following pregnancies ready:";
            int n = 1;
            foreach (var pregnancy in ready)
            {
                msg += "\n" + n + ": [b]" + pregnancy.MonsterType + "[/b] sired by [user]" + pregnancy.Initiator + "[/user]";
                if (pregnancy.BroodSize > 1)
                {
                    msg += " (brood of " + pregnancy.BroodSize + ")";
                }
                n++;
            }
            return msg;
        }

        private static bool HasAllTerm(string[] rawTerms)
        {
            if (rawTerms == null) return false;
            foreach (string term in rawTerms)
            {
                if (string.Equals(term?.Trim(), "all", StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
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
