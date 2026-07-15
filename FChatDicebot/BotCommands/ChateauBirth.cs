using System;
using System.Collections.Generic;
using System.Linq;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
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
            LongDescription = "Birth your young once they have finished gestating. If multiple pregnancies are ready to be born, you will be messaged a numbered list of the pregnancies, and can choose which pregnancy to birth, or '!birth all' to birth every pregnancy you are able to. Message this command privately to check on your pregnancies' remaining gestation time without birthing anything — actually giving birth requires running !birth in a channel.";
            Usage = "!birth\nor\n!birth {index}\n";
            RelatedCommands = new string[] { "breed", "dossier" };
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

            if (!commandController.MessageCameFromChannel(address))
            {
                bot.SendPrivateMessage(BuildGestationStatusMessage(MonDB.GetDatabase(), characterName), characterName);
                return;
            }

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

        /// <summary>
        /// DM-only status readout: lists every pregnancy with its remaining gestation time
        /// (or "Ready now!"), without birthing anything. Actually giving birth requires
        /// running !birth in a channel, since the completion message is posted publicly.
        /// </summary>
        public static string BuildGestationStatusMessage(IChateauDatabase database, string characterName)
        {
            Profile carrier = database.GetProfile(characterName);
            if (carrier == null)
            {
                return ChateauInteractionHandler.notFoundText(characterName);
            }

            List<Pregnancy> pregnancies = carrier.pregnancies ?? new List<Pregnancy>();
            if (pregnancies.Count == 0)
            {
                return "You aren't pregnant! Ask someone to !breed you first.";
            }

            DateTime now = DateTime.UtcNow;
            string msg = "Here's the status of your pregnancies. Once one is ready, use !birth in a channel to actually give birth:";
            foreach (var pregnancy in pregnancies.OrderBy(p => p.ReadyAt))
            {
                string status = pregnancy.ReadyAt <= now
                    ? "Ready now!"
                    : "ready in " + Utils.GetTimeSpanPrint(pregnancy.ReadyAt - now);
                string broodPart = pregnancy.BroodSize > 1 ? " (brood of " + pregnancy.BroodSize + ")" : "";
                msg += "\n[b]" + DescribePregnancyType(pregnancy) + "[/b] sired by [user]" + pregnancy.Initiator + "[/user]" + broodPart + ": " + status;
            }
            return msg;
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
                // A mixed brood logs its composition (a "random" pregnancy's MonsterType is
                // already the real species — the log entry is only written post-reveal).
                string logType = pregnancy.IsMixedBrood && pregnancy.Children != null && pregnancy.Children.Count > 0
                    ? "mixed (" + string.Join(", ", pregnancy.Children.Select(c => c.Species)) + ")"
                    : pregnancy.MonsterType;
                carrier.lists["offspring"].Add(now.ToString("yyyy-MM-dd") + ": " + logType
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
            if (pregnancy.BroodSize <= 0) return;

            // Mixed broods count per child ("mixed" itself is not a monster and gets no
            // counter), each against its own breed-time category snapshot.
            if (pregnancy.IsMixedBrood && pregnancy.Children != null)
            {
                foreach (var child in pregnancy.Children)
                {
                    if (child == null || string.IsNullOrEmpty(child.Species)) continue;
                    database.IncrementMonsterStats(BreedProcessor.MonsterStatsKey(child.Species), pregnancyDelta: 0, offspringDelta: 1);
                    if (child.Categories == null) continue;
                    foreach (var category in child.Categories)
                    {
                        if (string.IsNullOrEmpty(category)) continue;
                        database.IncrementMonsterStats(BreedProcessor.CategoryStatsKey(category), pregnancyDelta: 0, offspringDelta: 1);
                    }
                }
                return;
            }

            if (string.IsNullOrEmpty(pregnancy.MonsterType)) return;
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
            // Birth is a solo resolution (no consent pipeline), so it appends the carrier's
            // own custom !seteicon birth eicon here rather than through the shared completion hook.
            string birthEicon = InteractionEiconSupport.GetInteractionEicon(carrier, "birth");
            string eiconSuffix = string.IsNullOrEmpty(birthEicon) ? string.Empty : " " + birthEicon;

            // Mixed brood: the reveal enumerates the litter (which also covers a host-rolled
            // rare-twins pair, so the twins flavor below never applies to mixed).
            if (pregnancy.IsMixedBrood && pregnancy.Children != null && pregnancy.Children.Count > 0)
            {
                if (pregnancy.Children.Count == 1)
                {
                    string only = pregnancy.Children[0].Species;
                    return carrier.displayName + " has given birth to the sole child of their mixed brood — "
                        + Utils.AnOrA(only) + " " + only + "! (Sired by " + pregnancy.Initiator + ".)" + eiconSuffix;
                }
                return carrier.displayName + " has given birth to a wonderfully mixed brood of " + pregnancy.BroodSize
                    + " — " + DescribeMixedLitter(pregnancy.Children) + "! (Sired by "
                    + pregnancy.Initiator + ".)" + eiconSuffix;
            }

            // Rare-twins flavor: the resolved brood range was [1,1] and the 1% roll fired.
            if (pregnancy.IsRareTwins)
            {
                if (pregnancy.IsMystery)
                {
                    return carrier.displayName + " has given birth, and the mystery is finally revealed — a pair of "
                        + pregnancy.MonsterType + " twins, a rare blessing for a species that normally bears just one! (Sired by "
                        + pregnancy.Initiator + ".)" + eiconSuffix;
                }
                return carrier.displayName + " has miraculously given birth to a pair of " + pregnancy.MonsterType
                    + " twins — a rare blessing for a species that normally bears just one! (Sired by "
                    + pregnancy.Initiator + ".)" + eiconSuffix;
            }

            string broodPhrase = pregnancy.BroodSize > 1
                ? "a brood of " + pregnancy.BroodSize + " " + TextFormat.PluralizeNoun(pregnancy.MonsterType)
                : Utils.AnOrA(pregnancy.MonsterType) + " " + pregnancy.MonsterType;

            // A "random" pregnancy announces the reveal.
            if (pregnancy.IsMystery)
            {
                return carrier.displayName + " has given birth, and the mystery is finally revealed — "
                    + broodPhrase + "! (Sired by " + pregnancy.Initiator + ".)" + eiconSuffix;
            }

            return carrier.displayName + " has given birth to " + broodPhrase
                + "! (Sired by " + pregnancy.Initiator + ".)" + eiconSuffix;
        }

        /// <summary>
        /// Pre-birth display name for a pregnancy: mystery pregnancies stay masked until the
        /// birth announcement reveals them.
        /// </summary>
        private static string DescribePregnancyType(Pregnancy pregnancy)
        {
            if (pregnancy.IsMixedBrood) return "??? (mixed brood)";
            if (pregnancy.IsMystery) return "???";
            return pregnancy.MonsterType;
        }

        /// <summary>
        /// Describe a mixed litter grouped by species in first-appearance order, e.g.
        /// "2 kitsunes, a slime, and a dragon".
        /// </summary>
        internal static string DescribeMixedLitter(List<BroodChild> children)
        {
            var order = new List<string>();
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in children)
            {
                if (child == null || string.IsNullOrEmpty(child.Species)) continue;
                if (!counts.ContainsKey(child.Species))
                {
                    counts[child.Species] = 0;
                    order.Add(child.Species);
                }
                counts[child.Species]++;
            }

            var parts = order
                .Select(s => counts[s] > 1
                    ? counts[s] + " " + TextFormat.PluralizeNoun(s)
                    : Utils.AnOrA(s) + " " + s)
                .ToList();

            if (parts.Count == 0) return "nothing at all, somehow";
            if (parts.Count == 1) return parts[0];
            if (parts.Count == 2) return parts[0] + " and " + parts[1];
            return string.Join(", ", parts.Take(parts.Count - 1)) + ", and " + parts[parts.Count - 1];
        }

        private static string BuildReadyList(List<Pregnancy> ready)
        {
            string msg = "Use '!birth #' to birth the pregnancy of your choice, or '!birth all' to birth all of them. You have the following pregnancies ready:";
            int n = 1;
            foreach (var pregnancy in ready)
            {
                msg += "\n" + n + ": [b]" + DescribePregnancyType(pregnancy) + "[/b] sired by [user]" + pregnancy.Initiator + "[/user]";
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
