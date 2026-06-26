using FChatDicebot.BotCommands.Base;
using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !rest is the self-targeted reversal for !break. Heals one extra level of one (or
    /// all) active breaks in exchange for skipping that day's !work. Once per Chateau day.
    ///
    /// The "skip work" cost reuses the existing <c>work</c> timer that !work itself sets
    /// when a day's work is consumed — !rest sets the same timer so !work refuses for the
    /// remainder of the UTC day. A separate <c>rest_used</c> timer prevents re-resting in
    /// the same Chateau day.
    /// </summary>
    public class ChateauRest : ChatBotCommand
    {
        public const string RestUsedTimerKey = "rest_used";
        public const string WorkTimerKey = "work";

        public ChateauRest()
        {
            Name = "rest";
            Aliases = new string[] { };
            Category = "Recovery";
            ShortDescription = "Skip today's !work to recover broken body parts one day faster";
            LongDescription = "Recover broken body parts by one day, in exchange for skipping today's !work. With no argument, all of your broken parts are improved. With a bodypart argument, only that break is affected. !rest can only be used once per Chateau day, and only if you haven't already worked that day.";
            Usage = "!rest\n!rest {bodypart}";
            RelatedCommands = new string[] { "break", "work" };
            CooldownDuration = "1 day";
            CooldownAppliesTo = "initiator";
            IdentifierCategory = "break";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string specifiedPart = commandController.GetIdentifierFromCommandTerms(rawTerms, "break");
            RestResult result = ExecuteRest(MonDB.GetDatabase(), characterName, specifiedPart);

            if (!string.IsNullOrEmpty(result.ChannelMessage))
            {
                bot.SendMessageInChannel(result.ChannelMessage, channel);
            }
            if (!string.IsNullOrEmpty(result.PrivateMessage))
            {
                bot.SendPrivateMessage(result.PrivateMessage, characterName);
            }
        }

        public class RestResult
        {
            public string ChannelMessage { get; set; } = string.Empty;
            public string PrivateMessage { get; set; } = string.Empty;
        }

        /// <summary>
        /// Pure rest logic, factored out for testability. Non-empty ChannelMessage on a
        /// successful rest; non-empty PrivateMessage when the caller has no active breaks,
        /// has already rested today, has already worked today, or named a part they
        /// aren't broken at.
        /// </summary>
        /// <param name="specifiedPart">Null/empty rests every active break; a specific
        /// part rests only that one.</param>
        public static RestResult ExecuteRest(IChateauDatabase database, string characterName, string specifiedPart)
        {
            var result = new RestResult();
            Profile caller = database.GetProfile(characterName);
            if (caller == null)
            {
                result.PrivateMessage = ChateauInteractionHandler.notFoundText(characterName);
                return result;
            }
            if (caller.timers == null) caller.timers = new Dictionary<string, CoolDown>();

            if (caller.timers.ContainsKey(RestUsedTimerKey)
                && caller.timers[RestUsedTimerKey].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string remaining = Utils.GetTimeSpanPrint(caller.timers[RestUsedTimerKey].timerEnd - DateTime.UtcNow);
                result.PrivateMessage = "You've already rested today. You can !rest again in " + remaining + ".";
                return result;
            }

            if (caller.timers.ContainsKey(WorkTimerKey)
                && caller.timers[WorkTimerKey].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string remaining = Utils.GetTimeSpanPrint(caller.timers[WorkTimerKey].timerEnd - DateTime.UtcNow);
                result.PrivateMessage = "You've already worked today! !rest costs a day's !work, so please wait until the next Chateau day in " + remaining + ".";
                return result;
            }

            var breaks = BreakInstance.LoadAllWithTick(caller);
            if (breaks.Count == 0)
            {
                result.PrivateMessage = "You don't have any body parts in need of !rest. If that's not what you want, someone may be willing to !break you if you ask nicely...";
                return result;
            }

            List<BreakInstance> targets;
            if (!string.IsNullOrEmpty(specifiedPart))
            {
                BreakInstance target = null;
                foreach (var entry in breaks)
                {
                    if (string.Equals(entry.Part, specifiedPart, StringComparison.OrdinalIgnoreCase))
                    {
                        target = entry;
                        break;
                    }
                }
                if (target == null)
                {
                    string brokenList = JoinPartsSimple(breaks.ConvertAll(b => b.Part));
                    string demonstrative = breaks.Count == 1 ? "that" : "one of those";
                    result.PrivateMessage = "Your " + specifiedPart + " isn't broken. You currently have a broken "
                        + brokenList + ". You can !rest with " + demonstrative + ", or just !rest with no argument to help everything recover faster.";
                    return result;
                }
                targets = new List<BreakInstance> { target };
            }
            else
            {
                targets = new List<BreakInstance>(breaks);
            }

            var clearedParts = new List<string>();
            var partiallyHealed = new List<BreakInstance>();
            foreach (var entry in targets)
            {
                entry.Severity -= 1;
                if (entry.Severity <= 0)
                {
                    breaks.Remove(entry);
                    clearedParts.Add(entry.Part);
                }
                else
                {
                    partiallyHealed.Add(entry);
                }
            }
            BreakInstance.SaveAll(caller, breaks);

            DateTime nextMidnight = DateTime.UtcNow.Date.AddDays(1);
            caller.timers[RestUsedTimerKey] = new CoolDown { timerStart = DateTime.UtcNow, timerEnd = nextMidnight };
            caller.timers[WorkTimerKey] = new CoolDown { timerStart = DateTime.UtcNow, timerEnd = nextMidnight };

            database.SetProfile(characterName, caller);

            result.ChannelMessage = ComposeRestSummary(caller.displayName, clearedParts, partiallyHealed);
            return result;
        }

        /// <summary>
        /// Builds the channel-facing rest success message. Routes between four shapes
        /// depending on whether only fully-recovered parts, only partially-healed parts,
        /// or a mix are present.
        /// </summary>
        public static string ComposeRestSummary(string displayName, List<string> clearedParts, List<BreakInstance> partiallyHealed)
        {
            bool anyCleared = clearedParts != null && clearedParts.Count > 0;
            bool anyPartial = partiallyHealed != null && partiallyHealed.Count > 0;

            if (anyCleared && anyPartial)
            {
                string clearedList = JoinPartsSimple(clearedParts);
                string clearedVerb = clearedParts.Count == 1 ? "has" : "have";
                var partialParts = partiallyHealed.ConvertAll(b => b.Part);
                string partialList = JoinPartsSimple(partialParts);
                string partialVerb = partiallyHealed.Count == 1 ? "is" : "are";
                return displayName + " takes the day off !work to rest. Their " + clearedList + " " + clearedVerb
                    + " fully recovered, and their " + partialList + " " + partialVerb + " feeling a little better.";
            }

            if (anyCleared)
            {
                string clearedList = JoinPartsSimple(clearedParts);
                string verbToBe = clearedParts.Count == 1 ? "is" : "are";
                return displayName + " takes the day off to rest, and their " + clearedList + " " + verbToBe + " now fully recovered!";
            }

            // anyPartial only
            if (partiallyHealed.Count == 1)
            {
                var entry = partiallyHealed[0];
                string daysWord = entry.Severity == 1 ? "day" : "days";
                return displayName + " takes the day off !work to rest, letting their " + entry.Part
                    + " recover. It'll now be fully healed in " + entry.Severity + " " + daysWord + ".";
            }
            var parts = partiallyHealed.ConvertAll(b => b.Part);
            return displayName + " takes the day off !work to rest. Their " + JoinPartsSimple(parts)
                + " are feeling a little better.";
        }

        private static string JoinPartsSimple(List<string> parts)
        {
            if (parts == null || parts.Count == 0) return string.Empty;
            if (parts.Count == 1) return parts[0];
            if (parts.Count == 2) return parts[0] + " and " + parts[1];
            return string.Join(", ", parts.GetRange(0, parts.Count - 1)) + ", and " + parts[parts.Count - 1];
        }
    }
}
