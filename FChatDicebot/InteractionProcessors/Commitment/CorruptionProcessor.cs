using FChatDicebot.Database;
using FChatDicebot.Model;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Commitment
{
    /// <summary>
    /// Single processor that backs both <c>!corrupt</c> and <c>!purify</c>. The same instance
    /// is registered in <see cref="InteractionProcessorRegistry"/> under both interaction
    /// types. The corresponding command derives the *effective* verb from the user-typed verb
    /// composed with the sign of the amount (a negative amount flips the verb's direction, so
    /// <c>!corrupt -3</c> ≡ <c>!purify 3</c>) and stores that effective verb on
    /// <see cref="Interaction.type"/> — the processor never has to re-derive direction.
    ///
    /// Rate-limiting is by a per-day, per-(initiator, recipient) magnitude quota, not a hard
    /// cooldown — the same initiator may run multiple corrupts/purifies against the same
    /// recipient in a UTC day as long as the cumulative absolute magnitude stays within
    /// <see cref="DailyMagnitudeLimit"/>. The quota counts both directions: a 6 followed by a
    /// 5 the same day partially clamps the second to 4. Clamping happens at process (consent)
    /// time so that queuing multiple pending commands can't bypass the limit.
    ///
    /// Per-call magnitude is carried on the in-memory <see cref="Interaction.identifier"/>
    /// as <c>"{effectiveVerb}|{magnitude}"</c>; the command writes the *requested* magnitude
    /// there, and <see cref="ProcessInteraction"/> overwrites it with the *applied* magnitude
    /// before <see cref="GetCompletionMessage"/> is called so the channel message is truthful.
    /// </summary>
    public class CorruptionProcessor : InteractionProcessorBase
    {
        public const string CorruptType = "corrupt";
        public const string PurifyType = "purify";
        public const string CorruptionCharacteristicKey = "corruption";
        public const int DailyMagnitudeLimit = 10;
        // 1-in-EasterEggChanceDenominator chance the completion message renders the verb
        // as "un-purify" / "un-corrupt" instead of the literal verb. 20 ⇒ 5%.
        public const int EasterEggChanceDenominator = 20;

        // Single mutable RNG used for easter-egg rolls. Tests can swap this for a seeded
        // Random to make the easter-egg outcome deterministic.
        internal static Random Rng = new Random();

        // Primary registry key for this processor. The "purify" alias is registered
        // separately in InteractionProcessorRegistry; both keys map to the same instance.
        public override string InteractionType => CorruptType;
        public override string InvestmentLevel => "commitment";

        public CorruptionProcessor(IChateauDatabase database) : base(database) { }
        public CorruptionProcessor() : base() { }

        public override string GetInteractionVerb(VerbTense tense)
        {
            // The base would yield "corruption" by stripping "Processor"; we want "corrupt".
            switch (tense)
            {
                case VerbTense.Past: return "corrupted";
                case VerbTense.Present: return "corrupts";
                case VerbTense.Future: return "will corrupt";
                default: return "corrupt";
            }
        }

        /// <summary>+1 for <c>purify</c>, -1 for <c>corrupt</c>, 0 otherwise.</summary>
        public static int VerbSign(string verb)
        {
            if (string.Equals(verb, PurifyType, StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(verb, CorruptType, StringComparison.OrdinalIgnoreCase)) return -1;
            return 0;
        }

        /// <summary>
        /// Compose the user-typed verb with the sign of the user-supplied amount and return
        /// the *effective* verb. A negative amount flips the verb's direction (so
        /// <c>!corrupt -3</c> becomes a "purify"). Zero amounts keep the typed verb.
        /// </summary>
        public static string EffectiveVerb(string typedVerb, int signedAmount)
        {
            int typedSign = VerbSign(typedVerb);
            int amountSign = signedAmount < 0 ? -1 : 1;
            int composed = typedSign * amountSign;
            if (composed < 0) return CorruptType;
            if (composed > 0) return PurifyType;
            return typedVerb;
        }

        /// <summary>
        /// Quota tracker key. Stored on the *initiator's* dailyMagnitudes so the limit is
        /// "this initiator against this recipient on this UTC day" — different initiators
        /// can stack on the same recipient and one initiator can spend budget independently
        /// against multiple recipients.
        /// </summary>
        public static string QuotaKey(string recipientUserName, DateTime utcDay)
        {
            return "corruption_" + recipientUserName + "_" + utcDay.ToString("yyyy-MM-dd");
        }

        /// <summary>Suffix used when pruning stale entries from another day.</summary>
        public static string TodayQuotaSuffix(DateTime utcDay)
        {
            return "_" + utcDay.ToString("yyyy-MM-dd");
        }

        /// <summary>Read corruption int from a profile; absent / unparseable returns 0.</summary>
        public static int ReadCorruption(Profile profile)
        {
            if (profile == null || profile.characteristics == null) return 0;
            if (!profile.characteristics.TryGetValue(CorruptionCharacteristicKey, out string raw)) return 0;
            return int.TryParse(raw, out int value) ? value : 0;
        }

        /// <summary>Magnitude already spent today by initiator against recipient.</summary>
        public static int GetUsedQuota(Profile initiatorProfile, string recipientUserName, DateTime utcDay)
        {
            if (initiatorProfile == null || initiatorProfile.dailyMagnitudes == null) return 0;
            string key = QuotaKey(recipientUserName, utcDay);
            return initiatorProfile.dailyMagnitudes.TryGetValue(key, out int used) ? used : 0;
        }

        /// <summary>Remaining quota for today's call; clamps to zero so callers can hand it to Math.Min.</summary>
        public static int RemainingQuota(int usedMagnitude)
        {
            int remaining = DailyMagnitudeLimit - usedMagnitude;
            return remaining > 0 ? remaining : 0;
        }

        /// <summary>
        /// Set today's quota entry to the new total and drop any stale-dated entries from
        /// previous days. Mutates the profile in place; caller is responsible for persisting.
        /// </summary>
        public static void RecordUsedQuota(Profile initiatorProfile, string recipientUserName, DateTime utcDay, int newUsedTotal)
        {
            if (initiatorProfile == null) return;
            if (initiatorProfile.dailyMagnitudes == null)
            {
                initiatorProfile.dailyMagnitudes = new Dictionary<string, int>();
            }
            string todaySuffix = TodayQuotaSuffix(utcDay);
            var stale = initiatorProfile.dailyMagnitudes.Keys
                .Where(k => k.StartsWith("corruption_", StringComparison.Ordinal)
                            && !k.EndsWith(todaySuffix, StringComparison.Ordinal))
                .ToList();
            foreach (var key in stale)
            {
                initiatorProfile.dailyMagnitudes.Remove(key);
            }
            initiatorProfile.dailyMagnitudes[QuotaKey(recipientUserName, utcDay)] = newUsedTotal;
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string effectiveVerb = command.pendingInteraction.type;
            int requestedMagnitude = ParseMagnitudeFromIdentifier(command.pendingInteraction.identifier);
            DateTime utcDay = DateTime.UtcNow.Date;
            bool isSelf = string.Equals(initiator, recipient, StringComparison.Ordinal);

            Profile initiatorProfile = Database.GetProfile(initiator);
            // For self-target, share the single in-memory Profile so reads and writes against
            // the same underlying record don't desync.
            Profile recipientProfile = isSelf ? initiatorProfile : Database.GetProfile(recipient);

            int used = GetUsedQuota(initiatorProfile, recipient, utcDay);
            int remaining = RemainingQuota(used);
            int appliedMagnitude = Math.Min(requestedMagnitude, remaining);
            int sign = VerbSign(effectiveVerb);

            if (appliedMagnitude > 0 && sign != 0)
            {
                int oldCorruption = ReadCorruption(recipientProfile);
                int newCorruption = oldCorruption + sign * appliedMagnitude;
                if (recipientProfile.characteristics == null)
                {
                    recipientProfile.characteristics = new Dictionary<string, string>();
                }
                recipientProfile.characteristics[CorruptionCharacteristicKey] = newCorruption.ToString();

                RecordUsedQuota(initiatorProfile, recipient, utcDay, used + appliedMagnitude);
            }
            else
            {
                // TOCTOU edge case: the command pre-clamped against today's remaining quota
                // and queued a pending interaction, but by the time the recipient consented,
                // *other* queued pendings from the same initiator had already eaten the
                // remaining quota. Suppress the channel-visible completion message (set the
                // identifier's magnitude to 0 so GetCompletionMessage returns empty) and
                // stash a private heads-up for the initiator instead — same wording as the
                // command-time refusal, drained by ChateauConsent via
                // GetAndClearInitiatorPrivateMessage.
                string recipientName = recipientProfile?.displayName ?? recipient;
                _lastInitiatorPrivateMessage = QuotaExhaustedPrivateMessage(effectiveVerb, recipientName);
            }

            // Stamp the applied magnitude back onto the interaction so the completion message,
            // which is called after ProcessInteraction with the same in-memory PendingCommand,
            // can report the truthful (post-clamp) value. Done before AddInteraction so the
            // saved historical record is also accurate.
            command.pendingInteraction.identifier = ComposeIdentifier(effectiveVerb, appliedMagnitude);
            Database.AddInteraction(command.pendingInteraction);

            // Save profiles FIRST. The IncrementDifferentCounts call below mutates the DB
            // record directly (it does its own read-modify-write), so doing it after
            // SetProfile avoids the in-memory profile clobbering the count back to zero.
            Database.SetProfile(initiator, initiatorProfile);
            if (!isSelf)
            {
                Database.SetProfile(recipient, recipientProfile);
            }

            if (appliedMagnitude > 0 && sign != 0)
            {
                // Counts are keyed by effective direction (not by what the user typed) so a
                // !corrupt -5 (which behaves as a purify) doesn't pollute the corrupt counters.
                if (sign < 0)
                {
                    IncrementDifferentCounts(initiator, recipient, "corruptgive", "corrupttake");
                }
                else
                {
                    IncrementDifferentCounts(initiator, recipient, "purifygive", "purifytake");
                }
            }

            Database.DeletePendingCommand(command.Id);

            return effectiveVerb;
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            int appliedMagnitude = ParseMagnitudeFromIdentifier(identifier);
            // Process-time clamp resolved to 0 (rare TOCTOU; see ProcessInteraction) —
            // suppress channel output. The initiator already received a private heads-up
            // via _lastInitiatorPrivateMessage.
            if (appliedMagnitude <= 0) return string.Empty;

            string verb = ParseVerbFromIdentifier(identifier);
            int sign = VerbSign(verb);
            string verbLabel = RenderVerb(verb, sign, useEasterEgg: RollEasterEgg());

            // After ProcessInteraction the recipient's persisted corruption holds the new
            // value; derive the prior value from (new − sign * applied) so the side/action
            // descriptor can be chosen from where they actually ended up vs where they were.
            int newCorruption = ReadCorruption(recipientProfile);
            int oldCorruption = newCorruption - sign * appliedMagnitude;
            LevelChange change = LevelChange.From(oldCorruption, newCorruption);

            string subject = initiatorProfile?.displayName ?? "Someone";
            bool isSelf = string.Equals(initiatorProfile?.userName, recipientProfile?.userName, StringComparison.Ordinal);
            string target = isSelf ? "themself" : (recipientProfile?.displayName ?? "someone");

            return subject + " has " + PastTense(verbLabel) + " " + target + "! Their " + change.Side + " has "
                + change.ActionPast + " by " + appliedMagnitude + ", " + DescribeCurrentLevel(newCorruption) + ".";
        }

        public override CooldownSpec CooldownRule => Cooldown;

        public static readonly CooldownSpec Cooldown = new CooldownSpec
        {
            Kind = CooldownKind.MagnitudeQuota,
            Binds = CooldownBinds.Initiator,
            PeriodDays = 1,
            QuotaMagnitude = DailyMagnitudeLimit
        };

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string verb = ParseVerbFromIdentifier(identifier);
            int requestedMagnitude = ParseMagnitudeFromIdentifier(identifier);
            int sign = VerbSign(verb);

            // Project the post-consent value so the consent text describes the actual
            // end-state side ("purity" / "corruption") rather than just the action's
            // direction. Example: !purify 5 on someone at -7 ends at -2 — still on the
            // corruption side, so the warning reads "decreasing their corruption by 5".
            int currentCorruption = ReadCorruption(recipientProfile);
            int projectedCorruption = currentCorruption + sign * requestedMagnitude;
            LevelChange change = LevelChange.From(currentCorruption, projectedCorruption);

            string subject = initiatorProfile?.displayName ?? "Someone";
            bool isSelf = string.Equals(initiatorProfile?.userName, recipientProfile?.userName, StringComparison.Ordinal);
            string target = isSelf ? "themself" : (recipientProfile?.displayName ?? "you");

            // B3: disclose that the recipient's corruption/purity surfaces elsewhere. The
            // direction is taken from the effective verb (the movement currently in play),
            // not the projected end-state side.
            string visibilityClause = sign < 0
                ? "Corruption will be visible in most interactions, and in anything milked from you."
                : "Purity will be visible in most interactions, and in anything milked from you.";

            // Shape D: a per-day magnitude quota, so the prompt states the rule and (when the
            // initiator has already spent some of today's budget against this recipient) how
            // much has been used so far.
            int usedToday = GetUsedQuota(initiatorProfile, recipientProfile?.userName, DateTime.UtcNow.Date);
            string quotaClause = ConsentWarningText.FrequencyQuota(subject, verb, Cooldown.QuotaMagnitude, Cooldown.PeriodDays);
            string consumedClause = ConsentWarningText.ConsumedClause(subject, PastTense(verb), usedToday);

            string seriousness = ConsentWarningText.Block(visibilityClause, quotaClause, consumedClause);

            return subject + " wants to " + verb + " " + target + ", " + change.ActionPresent + " their "
                + change.Side + " by " + requestedMagnitude + "! " + seriousness + " "
                + "Do you !consent to being " + PastTense(verb) + "?";
        }

        /// <summary>
        /// Renders the trailing "leaving them with X degrees of (corruption|purity)" clause
        /// on the completion message. The exact-zero end state gets a flowery override per
        /// spec — "leaving them once again neutral in the eternal tug of war of purity and
        /// corruption." — so the user-facing message reads cleanly without a clunky
        /// "0 degrees of purity" line.
        /// </summary>
        public static string DescribeCurrentLevel(int corruption)
        {
            if (corruption == 0)
            {
                return "leaving them once again neutral in the eternal tug of war of purity and corruption";
            }
            int abs = Math.Abs(corruption);
            string side = corruption < 0 ? "corruption" : "purity";
            return "leaving them with " + abs + " degrees of " + side;
        }

        /// <summary>Past-tense form for the consent/completion templates: "corrupted", "purified", "un-purified", "un-corrupted".</summary>
        public static string PastTense(string verb)
        {
            if (verb.EndsWith("y", StringComparison.Ordinal)) return verb.Substring(0, verb.Length - 1) + "ied";
            if (verb.EndsWith("e", StringComparison.Ordinal)) return verb + "d";
            return verb + "ed";
        }

        /// <summary>
        /// Bundle of "what side is this on" + "did the action increase or decrease that side"
        /// derived from the old and new corruption values. Used by both consent (projected
        /// new value) and completion (actual new value) so the wording is symmetrical.
        ///
        /// Examples:
        ///   2 → 7  : Side="purity",     Increase (purify drove purity up)
        ///   7 → 2  : Side="purity",     Decrease (corrupt ate into existing purity)
        ///   -7 → -2: Side="corruption", Decrease (purify ate into existing corruption)
        ///   -2 → 3 : Side="purity",     Increase (cross-side; landed on purity side)
        ///   5 → 0  : Side="purity",     Decrease (zero end-state inherits side from prior)
        /// </summary>
        public struct LevelChange
        {
            public string Side;
            public string ActionPresent;
            public string ActionPast;

            public static LevelChange From(int oldValue, int newValue)
            {
                int delta = newValue - oldValue;
                if (newValue > 0)
                {
                    bool increasing = delta > 0;
                    return new LevelChange
                    {
                        Side = "purity",
                        ActionPresent = increasing ? "increasing" : "decreasing",
                        ActionPast = increasing ? "increased" : "decreased",
                    };
                }
                if (newValue < 0)
                {
                    // More-negative = more corruption, so a *negative* delta means corruption increased.
                    bool increasing = delta < 0;
                    return new LevelChange
                    {
                        Side = "corruption",
                        ActionPresent = increasing ? "increasing" : "decreasing",
                        ActionPast = increasing ? "increased" : "decreased",
                    };
                }
                // newValue == 0: inherit side from where they were before (can't have arrived
                // here from 0 itself — zero-amount calls are rejected at the command layer —
                // so oldValue is guaranteed non-zero). The action is necessarily a decrease
                // toward neutrality.
                return new LevelChange
                {
                    Side = oldValue > 0 ? "purity" : "corruption",
                    ActionPresent = "decreasing",
                    ActionPast = "decreased",
                };
            }
        }

        /// <summary>
        /// Shared "you've hit your daily corruption/purity quota" message used by both the
        /// command-time pre-check in <see cref="CorruptionCommandSupport"/> and the rare
        /// process-time TOCTOU path in <see cref="ProcessInteraction"/>. Centralized so the
        /// two paths can't drift in wording.
        /// </summary>
        public static string QuotaExhaustedPrivateMessage(string effectiveVerb, string recipientDisplayName)
        {
            DateTime now = DateTime.UtcNow;
            string untilReset = Utils.GetTimeSpanPrint(now.Date.AddDays(1) - now);
            return "You can only increase or decrease someone's corruption/purity by "
                + DailyMagnitudeLimit + " per day! Please respect that "
                + "'Commitment' interactions are meant to be meaningful, and not spammed. "
                + "You can " + effectiveVerb + " " + recipientDisplayName + " further in " + untilReset + ".";
        }

        /// <summary>
        /// Hook for tests: swap <see cref="Rng"/> to a seeded Random to deterministically
        /// trigger or suppress the easter egg. Returns true with probability
        /// <c>1 / EasterEggChanceDenominator</c>.
        /// </summary>
        internal static bool RollEasterEgg()
        {
            return Rng.Next(EasterEggChanceDenominator) == 0;
        }

        /// <summary>
        /// Render the verb for display. When <paramref name="useEasterEgg"/> is true the verb
        /// flips to its inverse form ("un-purify" for a corrupt action, "un-corrupt" for a
        /// purify action) — describes the same movement from the opposite perspective.
        /// </summary>
        public static string RenderVerb(string verb, int sign, bool useEasterEgg)
        {
            if (!useEasterEgg) return verb;
            if (sign < 0) return "un-purify";
            if (sign > 0) return "un-corrupt";
            return verb;
        }

        // -----------------------------------------------------------------------
        // Identifier payload helpers.
        //
        // Thin façade over the shared <see cref="IdentifierPayload"/> encoder, applying
        // corruption-specific defaults (missing verb → "corrupt", missing or negative
        // magnitude → 1). New processors that need to carry a per-call number alongside
        // a verb should reuse <see cref="IdentifierPayload"/> directly rather than
        // recreating this pipe encoder.
        // -----------------------------------------------------------------------

        public static string ComposeIdentifier(string verb, int magnitude)
        {
            return IdentifierPayload.Compose(verb, magnitude);
        }

        public static string ParseVerbFromIdentifier(string identifier)
        {
            return IdentifierPayload.ExtractHead(identifier) ?? CorruptType;
        }

        public static int ParseMagnitudeFromIdentifier(string identifier)
        {
            // Corruption rejects negative magnitudes (direction lives on the verb), so a
            // stray negative tail collapses back to the default 1.
            if (!IdentifierPayload.TryExtractTail(identifier, out int value) || value < 0) return 1;
            return value;
        }
    }
}
