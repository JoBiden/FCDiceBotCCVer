using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Which verb drew from a resident, for the once-per-day per-direction lock the draw
    /// interactions share.
    /// </summary>
    public enum DrawVerb
    {
        /// <summary><c>!milk</c> — the draw that produces bottles.</summary>
        Milk,
        /// <summary><c>!drinkfrom</c> / <c>!forcedrink</c> — the draw that produces nothing.</summary>
        DrinkFrom,
    }

    /// <summary>
    /// The once-per-day, per-direction lock on drawing fluid out of another resident.
    ///
    /// A resident can be drawn from once per Chateau day per taker, whether or not the draw
    /// ends up in a bottle — so <c>!milk</c> and <c>!drinkfrom</c> gate each other. Each verb
    /// keeps its <b>own</b> timer key and both verbs check both keys, which is what lets a
    /// refusal name what actually happened: a <c>!milk</c> blocked by an earlier drink says
    /// "you've already drunk from Bob", not "you've already milked Bob".
    ///
    /// The lock is directional and lives on the <b>taker's</b> profile scoped to the source, so
    /// being milked by Alice never stops you milking Alice back. It runs to the next
    /// day-boundary regardless of time of day, matching every other daily lock in the Chateau.
    ///
    /// A third draw verb added later needs a <see cref="DrawVerb"/> entry, a prefix, and a
    /// message — and every existing caller keeps working, because they all resolve through
    /// <see cref="ActiveLock"/> rather than testing one key.
    /// </summary>
    public static class SourceDrawLock
    {
        /// <summary>
        /// Historical key prefix for <c>!milk</c>. Unchanged since the lock shipped — live
        /// profiles carry <c>milk_give_&lt;source&gt;</c> timers and renaming this would drop
        /// every lock currently held.
        /// </summary>
        public const string MilkKeyPrefix = "milk_give_";

        /// <summary>Key prefix for the source-drink verbs.</summary>
        public const string DrinkFromKeyPrefix = "drinkfrom_give_";

        /// <summary>
        /// Timer key for one (taker, source) direction under one verb. Stamped on the taker.
        /// </summary>
        public static string TimerKey(DrawVerb verb, string sourceUser)
        {
            return Prefix(verb) + sourceUser;
        }

        private static string Prefix(DrawVerb verb)
        {
            return verb == DrawVerb.DrinkFrom ? DrinkFromKeyPrefix : MilkKeyPrefix;
        }

        /// <summary>
        /// Which verb currently holds this direction's lock, or null when it is free.
        ///
        /// Milk is checked first purely so an ambiguous state (both keys live, which only
        /// happens if a future change stamps both) reports deterministically rather than
        /// depending on dictionary order.
        /// </summary>
        public static DrawVerb? ActiveLock(Profile taker, string sourceUser)
        {
            if (taker?.timers == null) return null;

            if (IsLive(taker.timers, TimerKey(DrawVerb.Milk, sourceUser))) return DrawVerb.Milk;
            if (IsLive(taker.timers, TimerKey(DrawVerb.DrinkFrom, sourceUser))) return DrawVerb.DrinkFrom;
            return null;
        }

        private static bool IsLive(Dictionary<string, CoolDown> timers, string key)
        {
            if (!timers.TryGetValue(key, out var timer)) return false;
            return DateTime.UtcNow < timer.timerEnd;
        }

        /// <summary>
        /// True when <paramref name="taker"/> has already drawn from <paramref name="sourceUser"/>
        /// today by any verb.
        /// </summary>
        public static bool IsLocked(Profile taker, string sourceUser)
        {
            return ActiveLock(taker, sourceUser) != null;
        }

        /// <summary>
        /// When the lock a draw stamps now should expire: the next day-boundary, so the whole
        /// Chateau's daily locks reset together rather than 24h after each individual act.
        /// </summary>
        public static CoolDown NextDayBoundary()
        {
            return new CoolDown { timerEnd = DateTime.UtcNow.Date.AddDays(1) };
        }

        /// <summary>
        /// Stamp this direction's lock on the taker's in-memory profile. The caller persists.
        /// </summary>
        public static void Stamp(Profile taker, DrawVerb verb, string sourceUser)
        {
            if (taker == null) return;
            if (taker.timers == null) taker.timers = new Dictionary<string, CoolDown>();
            taker.timers[TimerKey(verb, sourceUser)] = NextDayBoundary();
        }

        /// <summary>
        /// Channel-facing refusal naming the verb that actually consumed the day, so a
        /// <c>!milk</c> refused because of an earlier drink doesn't claim a milking that never
        /// happened. Centralized so the command pre-checks and the consent-time rechecks can't
        /// drift.
        ///
        /// Pass <paramref name="isSelf"/> for the self-milk shortcut so the message reads
        /// "yourself" rather than the awkward "already milked &lt;your own display name&gt;".
        /// </summary>
        public static string LockMessage(DrawVerb heldBy, string sourceDisplayOrName, bool isSelf = false)
        {
            DateTime now = DateTime.UtcNow;
            string untilReset = Utils.GetTimeSpanPrint(now.Date.AddDays(1) - now);

            if (heldBy == DrawVerb.DrinkFrom)
            {
                if (isSelf)
                {
                    return "You've already drunk from yourself today. You can drink from yourself again in "
                        + untilReset + ".";
                }
                return "You've already drunk from " + sourceDisplayOrName
                    + " today. You can drink from them again in " + untilReset + ".";
            }

            if (isSelf)
            {
                return "You've already milked yourself today. You can milk yourself again in "
                    + untilReset + ".";
            }
            return "You've already milked " + sourceDisplayOrName
                + " today. You can milk them again in " + untilReset + ".";
        }
    }
}
