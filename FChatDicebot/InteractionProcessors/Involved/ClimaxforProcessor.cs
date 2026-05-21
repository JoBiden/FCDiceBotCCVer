using FChatDicebot.Database;
using FChatDicebot.Model;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Backs both <c>!climaxfor</c> (initiator is the climaxer) and <c>!climax</c>
    /// (recipient is the climaxer — a "you make me climax" reskin). The same instance
    /// is registered under both type keys in <see cref="InteractionProcessorRegistry"/>;
    /// every read of "who actually climaxed" derives from the interaction's
    /// <see cref="Interaction.type"/> via <see cref="ResolveClimaxer"/>.
    ///
    /// Count semantics follow the project-wide give/take convention from the climaxer's
    /// perspective: the **climaxer** receives <c>climaxtake</c> ("they experienced the
    /// climax"), and the **partner** receives <c>climaxgive</c> ("they helped"). A
    /// self-target (initiator == recipient) only increments the initiator's
    /// <c>climaxtake</c> — there is no second party to credit.
    ///
    /// Self-targets bypass the consent flow entirely: <see cref="BotCommands.ChateauClimaxfor"/>
    /// and <see cref="BotCommands.ChateauClimax"/> call <see cref="PerformSelfTarget"/>
    /// directly and emit its return value to the channel. The processor's
    /// <see cref="ValidateInteraction"/> still rejects self-targets as a defensive guard
    /// against a stray self-pending sneaking into the consent path.
    ///
    /// The post-process daily count travels into <see cref="GetCompletionMessage"/> via
    /// the <see cref="Interaction.identifier"/> field (no dedicated slot exists on the
    /// model) — same trick MilkProcessor and CorruptionProcessor use.
    /// </summary>
    public class ClimaxforProcessor : InteractionProcessorBase
    {
        public const string ClimaxforType = "climaxfor";
        public const string ClimaxType = "climax";

        public override string InteractionType => ClimaxforType;
        public override string InvestmentLevel => "involved";

        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        // Per spec: keep last 30 days of per-day climax entries on the profile. Older
        // entries are dropped on the next write so the map can't grow unbounded.
        public const int DailyCountRetentionDays = 30;

        // Daily-streak title thresholds. Mirrored in CheckDailyClimaxTitles for the title
        // award; duplicated here so the completion-message flavor selector can read the
        // same numbers without coupling to the titles module. Constant names match the
        // current title text: Rule of Three (3), Can Go All Night (5), Inhuman Stamina (10).
        public const int RuleOfThreeThreshold = 3;
        public const int AllNightThreshold = 5;
        public const int InhumanStaminaThreshold = 10;

        public ClimaxforProcessor(IChateauDatabase database) : base(database) { }
        public ClimaxforProcessor() : base() { }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:    return "climaxed";
                case VerbTense.Present: return "climaxes";
                case VerbTense.Future:  return "will climax";
                default:                return "climax";
            }
        }

        /// <summary>UTC-day key shared by the daily counter on the profile and by callers
        /// reading "today's count" (e.g. status effect contributors).</summary>
        public static string TodayKey() => DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        /// <summary>
        /// Returns the username of whoever physically climaxed for this interaction.
        /// Self-targets always resolve to the initiator. For non-self interactions, the
        /// <c>climaxfor</c> type reads as "initiator climaxes (for/with the recipient)"
        /// and the <c>climax</c> type reads as "initiator makes the recipient climax".
        /// </summary>
        public static string ResolveClimaxer(string interactionType, string initiator, string recipient)
        {
            if (string.Equals(initiator, recipient, StringComparison.Ordinal)) return initiator;
            if (string.Equals(interactionType, ClimaxType, StringComparison.OrdinalIgnoreCase)) return recipient;
            return initiator;
        }

        /// <summary>Mirror of <see cref="ResolveClimaxer"/> for the non-climaxing party;
        /// returns null on self-target.</summary>
        public static string ResolvePartner(string interactionType, string initiator, string recipient)
        {
            if (string.Equals(initiator, recipient, StringComparison.Ordinal)) return null;
            string climaxer = ResolveClimaxer(interactionType, initiator, recipient);
            return string.Equals(climaxer, initiator, StringComparison.Ordinal) ? recipient : initiator;
        }

        /// <summary>
        /// Bumps today's entry on <see cref="Profile.dailyClimaxCounts"/> by one and drops
        /// any entries older than <see cref="DailyCountRetentionDays"/>. Returns the new
        /// today count so callers don't have to re-read the map. Mutates the profile in
        /// place; the caller is responsible for persisting.
        /// </summary>
        public static int IncrementDailyClimaxCount(Profile profile)
        {
            if (profile == null) return 0;
            if (profile.dailyClimaxCounts == null)
            {
                profile.dailyClimaxCounts = new Dictionary<string, int>();
            }

            DateTime todayUtc = DateTime.UtcNow.Date;
            DateTime cutoff = todayUtc.AddDays(-DailyCountRetentionDays);
            var stale = profile.dailyClimaxCounts.Keys
                .Where(k => !IsKeyWithinRetention(k, cutoff))
                .ToList();
            foreach (var key in stale)
            {
                profile.dailyClimaxCounts.Remove(key);
            }

            string todayKey = todayUtc.ToString("yyyy-MM-dd");
            int current = profile.dailyClimaxCounts.TryGetValue(todayKey, out int existing) ? existing : 0;
            int next = current + 1;
            profile.dailyClimaxCounts[todayKey] = next;
            return next;
        }

        private static bool IsKeyWithinRetention(string key, DateTime cutoff)
        {
            if (!DateTime.TryParseExact(key, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out DateTime keyDate))
            {
                // Unparseable keys are pruned (defensive — should never happen, but a stray
                // malformed entry shouldn't stick around forever).
                return false;
            }
            return keyDate.Date >= cutoff;
        }

        /// <summary>
        /// Self-target path: applied directly by the command layer when no recipient was
        /// supplied (or the recipient is the initiator themselves). Bypasses the consent
        /// flow because there is no second party whose consent we could ask for. Returns
        /// the channel-visible completion message; the caller is responsible for emitting
        /// it and for running the post-interaction title check on the initiator.
        ///
        /// <paramref name="typeKey"/> is the user-typed interaction verb (climaxfor or
        /// climax). Both behave identically on self-target (the initiator is also the
        /// climaxer in both shapes), but we record the typed verb on the interaction
        /// history so analytics can distinguish which alias was used.
        /// </summary>
        public string PerformSelfTarget(string initiator, string typeKey)
        {
            Profile initiatorProfile = Database.GetProfile(initiator);
            if (initiatorProfile == null) return string.Empty;

            int newDailyCount = IncrementDailyClimaxCount(initiatorProfile);

            var interaction = new Interaction
            {
                Id = ObjectId.GenerateNewId(),
                initiator = initiator,
                recipient = initiator,
                type = typeKey,
                identifier = ComposeIdentifier(typeKey, newDailyCount),
                investmentLevel = InvestmentLevel,
                interactionTime = DateTime.UtcNow,
            };
            Database.AddInteraction(interaction);

            // Persist the updated daily counter before the count helper writes — same
            // ordering rule as MilkProcessor (count helper does its own DB read-modify-write).
            Database.SetProfile(initiator, initiatorProfile);

            // Self-target only credits the climaxer's climaxtake (no second party).
            Database.IncrementCountWithRateLimit(initiator, "climaxtake", RateLimit);

            // Re-read after the count helper wrote so the completion message picks up the
            // freshly-bumped totals (used by some flavor branches that key off counts).
            Profile freshInitiator = Database.GetProfile(initiator);
            return GetCompletionMessage(freshInitiator, freshInitiator, interaction.identifier);
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            // Run the base validation first — gets us profile-existence checks plus any
            // future status-effect blockers on the recipient.
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid) return baseValidation;

            // Self-target is allowed at the command layer through the PerformSelfTarget
            // shortcut, which never creates a PendingCommand. Reject self-target here as
            // a defensive guard so a stray self-pending can't sneak through and double-
            // credit climaxtake via the rate-limited consent path.
            if (string.Equals(initiator, recipient, StringComparison.Ordinal))
            {
                return ValidationResult.Failure(
                    "Self-climax is a shortcut handled at the command layer, not via the consent flow.");
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string typeKey = command.pendingInteraction.type;

            string climaxer = ResolveClimaxer(typeKey, initiator, recipient);

            Profile climaxerProfile = Database.GetProfile(climaxer);
            int newDailyCount = IncrementDailyClimaxCount(climaxerProfile);

            // Stamp the post-increment daily count onto the in-memory PendingCommand so
            // GetCompletionMessage (called next with the same reference by ChateauConsent)
            // can read it for flavor selection AND know which verb to render. Done before
            // AddInteraction so the saved historical record also carries both.
            command.pendingInteraction.identifier = ComposeIdentifier(typeKey, newDailyCount);
            Database.AddInteraction(command.pendingInteraction);

            // Persist the climaxer's profile (daily counter bump) BEFORE the count-helper
            // call — same ordering trap as MilkProcessor: the helper does its own DB
            // read-modify-write on counts, and would otherwise stomp the dailyClimaxCounts
            // update we just made.
            Database.SetProfile(climaxer, climaxerProfile);

            // Climaxer is climaxtake; partner is climaxgive. We swap label order based on
            // which role the initiator is playing this round.
            string initiatorLabel = string.Equals(initiator, climaxer, StringComparison.Ordinal)
                ? "climaxtake"
                : "climaxgive";
            string recipientLabel = string.Equals(recipient, climaxer, StringComparison.Ordinal)
                ? "climaxtake"
                : "climaxgive";
            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, initiatorLabel, recipientLabel, RateLimit);

            Database.DeletePendingCommand(command.Id);

            return typeKey;
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            if (initiatorProfile == null || recipientProfile == null) return string.Empty;

            string typeKey = ParseTypeFromIdentifier(identifier);
            int dailyCount = ParseDailyCountFromIdentifier(identifier);
            bool isSelf = string.Equals(initiatorProfile.userName, recipientProfile.userName, StringComparison.Ordinal);

            // Climaxer profile is used for corruption flavor and (future) vice-craving
            // suppression — both should attach to the person actually having the orgasm,
            // not to whoever typed the command.
            Profile climaxerProfile = string.Equals(
                ResolveClimaxer(typeKey, initiatorProfile.userName, recipientProfile.userName),
                initiatorProfile.userName,
                StringComparison.Ordinal)
                ? initiatorProfile
                : recipientProfile;

            string descriptor = PickFlavorDescriptor(dailyCount);
            string sentence = ComposeOpeningSentence(typeKey, isSelf, initiatorProfile, recipientProfile, descriptor);

            // Status-effect contributors (e.g. dose-craving once that lands) decorate the
            // completion message. Per spec, the climaxer is the relevant profile here:
            // their corruption supplies the wicked-moan flavor, and their vices are the
            // ones briefly satisfied by the climax.
            bool climaxerIsInitiator = ReferenceEquals(climaxerProfile, initiatorProfile);
            var effects = GetActiveStatusEffects(
                climaxerProfile,
                StatusEffectCallSite.Completion,
                isInitiator: climaxerIsInitiator);
            sentence = AppendStatusFragments(sentence, effects.CompletionAppendix);

            return sentence;
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            // Consent warnings are only emitted for non-self-target paths, so we can
            // safely assume initiator != recipient here. The interaction's typeKey is
            // not stored on the identifier yet at this stage (the pending was just
            // created), so callers must pass the typeKey via a wrapped identifier OR
            // we infer from the bot command layer. We default to ClimaxforType wording
            // and let the command layer override by calling a typed helper.
            return GetConsentWarning(initiatorProfile, recipientProfile, identifier, ClimaxforType);
        }

        /// <summary>
        /// Typed overload of <see cref="GetConsentWarning"/> — the command layer knows
        /// the verb the user actually typed and passes it through so the wording matches
        /// (<c>climax for</c> vs <c>make you climax</c>).
        /// </summary>
        public string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier, string typeKey)
        {
            string initName = initiatorProfile?.displayName ?? "Someone";
            string recName = recipientProfile?.displayName ?? "you";
            if (string.Equals(typeKey, ClimaxType, StringComparison.OrdinalIgnoreCase))
            {
                return initName + " is ready to make " + recName + " climax. Do you !consent to being made a mess of?";
            }
            return initName + " is about to climax for " + recName + ". Do you !consent to being responsible for the mess?";
        }

        /// <summary>
        /// Opening sentence (without status-effect appendix) for a climax completion.
        /// Centralized so the four shapes (self vs other × climaxfor vs climax) stay in
        /// one place and adopt new flavor descriptors uniformly.
        /// </summary>
        private static string ComposeOpeningSentence(
            string typeKey, bool isSelf, Profile initiatorProfile, Profile recipientProfile, string descriptor)
        {
            string initName = initiatorProfile.displayName ?? initiatorProfile.userName;
            string recName = recipientProfile.displayName ?? recipientProfile.userName;
            if (isSelf)
            {
                return initName + " makes a mess of themselves. " + descriptor;
            }
            if (string.Equals(typeKey, ClimaxType, StringComparison.OrdinalIgnoreCase))
            {
                return initName + " brings " + recName + " to a pleasure filled climax. " + descriptor;
            }
            return initName + " shudders in ecstasy for " + recName + ". " + descriptor;
        }

        /// <summary>
        /// Descriptor pool keyed by the climaxer's running count today. Counts 1, 2, 3,
        /// and 4 each get their own pool; 5–9 collapse into a single "we lost count"
        /// tier; 10+ ratchets to the "is this a record?" tier.
        ///
        /// Volume language is deliberately constant across counts — no "diminishing"
        /// or "running dry" descriptors on repeats. The regression test enforces this.
        /// </summary>
        public static string PickFlavorDescriptor(int dailyCount)
        {
            var rng = new Random();
            List<string> pool;
            if (dailyCount >= InhumanStaminaThreshold)
            {
                pool = new List<string>
                {
                    "It's never ending...",
                    "Is that a record?",
                    "Incredible!",
                    "When will it end?",
                    "One must imagine Sisyphus happy.",
                    "One more tally.",
                    "Eat, Cum, Live.",
                    "Don't forget to hydrate.",
                    "We might need some maids to stay overtime.",
                    "Cumming cumming and cumming cumming and...",
                };
            }
            else if (dailyCount >= AllNightThreshold)
            {
                pool = new List<string>
                {
                    "How many is this now? We lost count.",
                    "Clean up on aisle sex!",
                    "Just how quick is your refractory period?",
                    "Again?",
                    "Another! Another!",
                    "Cum for the Cum god!",
                    "Surely running on fumes now...",
                    "What a mess",
                    "It just keeps going and going and going...",
                };
            }
            else if (dailyCount == 4)
            {
                pool = new List<string>
                {
                    "The fourth is when you start to lose count.",
                    "Careful you don't lose yourself...",
                    "Don't [i]four[/i]ce it too hard!",
                };
            }
            else if (dailyCount == RuleOfThreeThreshold)
            {
                pool = new List<string>
                {
                    "The third is when you start to question how many more you can take.",
                    "Again and again...",
                    "Rule of three!",
                };
            }
            else if (dailyCount == 2)
            {
                pool = new List<string>
                {
                    "The second is where you really build momentum.",
                    "More orgasms, more!",
                    "Cum again?~",
                };
            }
            else
            {
                pool = new List<string>
                {
                    "The first is always such sweet release.",
                    "May there be many more to cum~",
                    "What a display!",
                };
            }
            return pool[rng.Next(pool.Count)];
        }

        // -----------------------------------------------------------------------
        // Identifier payload helpers.
        //
        // Thin façade over the shared <see cref="IdentifierPayload"/> encoder, applying
        // climax-specific normalization (the head is constrained to either ClimaxforType
        // or ClimaxType, missing daily count → 1). New processors that need to carry a
        // per-call number alongside a verb should reuse <see cref="IdentifierPayload"/>
        // directly rather than recreating this pipe encoder.
        // -----------------------------------------------------------------------

        public static string ComposeIdentifier(string typeKey, int dailyCount)
        {
            string safeType = string.IsNullOrEmpty(typeKey) ? ClimaxforType : typeKey;
            return IdentifierPayload.Compose(safeType, dailyCount);
        }

        public static string ParseTypeFromIdentifier(string identifier)
        {
            string raw = IdentifierPayload.ExtractHead(identifier);
            if (string.Equals(raw, ClimaxType, StringComparison.OrdinalIgnoreCase)) return ClimaxType;
            return ClimaxforType;
        }

        public static int ParseDailyCountFromIdentifier(string identifier)
        {
            // Daily count is always positive; non-positive values are treated as missing.
            if (!IdentifierPayload.TryExtractTail(identifier, out int value) || value <= 0) return 1;
            return value;
        }
    }
}
