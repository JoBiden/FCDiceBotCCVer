using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Base class for interaction processors that provides common functionality.
    /// Refactored to support dependency injection for testability.
    /// Inherit from this to reduce boilerplate code.
    /// </summary>
    public abstract class InteractionProcessorBase : IInteractionProcessor
    {
        protected readonly IChateauDatabase Database;
        protected string _lastRateLimitMessage = string.Empty;
        // Optional out-of-band note a processor wants the consent handler to send privately
        // to the initiator (e.g. corrupt/purify's "this didn't land because your queued
        // pending raced against the daily quota" notice). Drained after ProcessInteraction
        // by ChateauConsent via GetAndClearInitiatorPrivateMessage.
        protected string _lastInitiatorPrivateMessage = string.Empty;

        public enum VerbTense
        {
            Past,
            Present,
            Future,
            Infinitive
        }

        /// <summary>
        /// Returns a verb in the specified tense for the interaction.
        /// </summary>
        public virtual string GetInteractionVerb(VerbTense tense)
        {
            // Strip "Processor" from class name
            string className = this.GetType().Name;
            string verb = className.Replace("Processor", "").ToLower();

            switch (tense)
            {
                case VerbTense.Past:
                    if (!verb.EndsWith("e"))
                    {
                        return verb + "ed";
                    }
                    return verb + "d";
                case VerbTense.Present:
                    return verb + "s";
                case VerbTense.Future:
                    return "will " + verb;
                default:
                    return verb;
            }
        }

        /// <summary>
        /// Returns a verb in the specified tense for the interaction, handling pluralization.
        /// </summary>
        public string GetInteractionVerb(VerbTense tense, bool isPlural)
        {
            // Default implementation calls the abstract method for singular
            // Derived classes can override this if they need custom plural handling
            string verb = GetInteractionVerb(tense);

            if (isPlural && tense == VerbTense.Present)
            {
                // Remove the 's' for plural present tense
                // "feeds" -> "feed", "dresses" -> "dress"
                if (verb.EndsWith("s") && !verb.EndsWith("ss"))
                {
                    return verb.Substring(0, verb.Length - 1);
                }
            }

            return verb;
        }

        /// <summary>
        /// Constructor for dependency injection.
        /// </summary>
        protected InteractionProcessorBase(IChateauDatabase database)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Legacy constructor for backward compatibility.
        /// Uses MonDB static methods via adapter.
        /// </summary>
        protected InteractionProcessorBase()
        {
            // Use the static database adapter for backward compatibility
            Database = MonDB.GetDatabase();
        }

        public abstract string InteractionType { get; }
        public abstract string InvestmentLevel { get; }

        /// <summary>
        /// Structured rate-limit description, or null when the interaction carries no warned
        /// cooldown. Warned processors override this to return their static spec; the help
        /// layer reads it so the cooldown strings can't drift from the consent warning.
        /// </summary>
        public virtual CooldownSpec CooldownRule => null;

        public string GetAndClearRateLimitMessage()
        {
            string msg = _lastRateLimitMessage;
            _lastRateLimitMessage = string.Empty;
            return msg;
        }

        /// <summary>
        /// Drain any pending out-of-band private note the processor wants sent to the
        /// initiator (used by corrupt/purify when a queued pending interaction lands
        /// after the daily quota has been spent — no channel message, but the initiator
        /// gets a privately-routed heads-up). Returns empty string when nothing's pending.
        /// </summary>
        public string GetAndClearInitiatorPrivateMessage()
        {
            string msg = _lastInitiatorPrivateMessage;
            _lastInitiatorPrivateMessage = string.Empty;
            return msg;
        }

        /// <summary>
        /// Process the interaction. Override this to implement interaction-specific logic.
        /// </summary>
        public abstract string ProcessInteraction(PendingCommand command);

        /// <summary>
        /// Get the completion message. Override this to customize the message.
        ///
        /// This returns the processor's own message body. Callers in the consent pipeline
        /// should use <see cref="GetCompletionMessageWithStatusEffects"/> instead, which
        /// wraps this with the registered status-effect contributors so every interaction
        /// surfaces e.g. corruption auras and scent layers uniformly. Tests that want to
        /// pin a processor's bare wording (without contributor fragments) can keep calling
        /// this directly.
        /// </summary>
        public abstract string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier);

        /// <summary>
        /// Channel-bound entry point used by <see cref="BotCommands.ChateauConsent"/> when a
        /// consented interaction lands: composes the processor's own
        /// <see cref="GetCompletionMessage"/> output and then appends any active
        /// status-effect completion fragments for the relevant subject profile.
        ///
        /// Default subject is the recipient (most interactions act on them, so their auras
        /// / scents / etc. are the natural ones to surface). Processors where the relevant
        /// party is someone else — e.g. <see cref="Involved.ClimaxforProcessor"/>, whose
        /// climaxer can be either side depending on the typed verb — override
        /// <see cref="GetStatusEffectSubject"/> to redirect.
        ///
        /// When the base message is empty (e.g. milk's TOCTOU clamp-to-zero path
        /// suppresses channel output), status fragments are skipped — appending them would
        /// leave a stray leading space and surface fragments that no longer have a host
        /// sentence to attach to.
        /// </summary>
        public string GetCompletionMessageWithStatusEffects(
            Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string baseMessage = GetCompletionMessage(initiatorProfile, recipientProfile, identifier);
            if (string.IsNullOrEmpty(baseMessage)) return baseMessage;

            Profile subject = GetStatusEffectSubject(initiatorProfile, recipientProfile, identifier);
            var effects = AggregateCompletionStatusEffects(
                initiatorProfile, recipientProfile, subject, identifier);
            var postEffectFragments = RunPostInteractionEffects(
                initiatorProfile, recipientProfile, identifier);

            string withStatusFragments = AppendStatusFragments(baseMessage, effects.CompletionAppendix);
            return AppendStatusFragments(withStatusFragments, postEffectFragments);
        }

        /// <summary>
        /// Walk <see cref="PostInteractionEffectRegistry"/> after the parent interaction has
        /// been processed, letting each effect inspect both profiles, mutate them, and emit
        /// completion-time fragments. A misbehaving effect must not break the parent
        /// interaction — exceptions are swallowed, same contract as
        /// <see cref="GetActiveStatusEffects"/>.
        /// </summary>
        private List<string> RunPostInteractionEffects(
            Profile initiatorProfile, Profile recipientProfile, string parentIdentifier)
        {
            var fragments = new List<string>();
            string safeIdentifier = parentIdentifier ?? string.Empty;
            foreach (var effect in PostInteractionEffectRegistry.GetAllEffects())
            {
                List<string> produced;
                try
                {
                    produced = effect.OnInteractionCompleted(
                        initiatorProfile, recipientProfile,
                        InteractionType, InvestmentLevel, safeIdentifier, Database);
                }
                catch (Exception)
                {
                    continue;
                }
                if (produced != null) fragments.AddRange(produced);
            }
            return fragments;
        }

        /// <summary>
        /// Completion-time aggregator that routes each registered contributor to either the
        /// interaction's primary <paramref name="subject"/> (for contributors with
        /// <see cref="IStatusEffectContributor.SymmetricInvocation"/> == false — corruption,
        /// break) or to both initiator and recipient (for symmetric contributors — dose
        /// cravings). Self-target collapses to a single invocation per contributor so
        /// symmetric contributors don't double-emit when the same profile sits on both sides.
        /// </summary>
        private StatusEffectFragments AggregateCompletionStatusEffects(
            Profile initiatorProfile, Profile recipientProfile, Profile subject, string parentIdentifier)
        {
            var merged = new StatusEffectFragments();
            bool isSelfTarget = initiatorProfile != null
                && recipientProfile != null
                && (ReferenceEquals(initiatorProfile, recipientProfile)
                    || string.Equals(initiatorProfile.userName, recipientProfile.userName, StringComparison.Ordinal));

            foreach (var contributor in StatusEffectRegistry.GetAllContributors())
            {
                if (contributor.SymmetricInvocation)
                {
                    // Symmetric contributors (dose) attend to BOTH parties' state.
                    InvokeContributor(contributor, recipientProfile, StatusEffectCallSite.Completion,
                        parentIdentifier, isInitiator: false, into: merged);
                    if (!isSelfTarget)
                    {
                        InvokeContributor(contributor, initiatorProfile, StatusEffectCallSite.Completion,
                            parentIdentifier, isInitiator: true, into: merged);
                    }
                }
                else
                {
                    // Subject-only contributors (corruption, break) attach to one party.
                    if (subject == null) continue;
                    bool subjectIsInitiator = ReferenceEquals(subject, initiatorProfile);
                    InvokeContributor(contributor, subject, StatusEffectCallSite.Completion,
                        parentIdentifier, isInitiator: subjectIsInitiator, into: merged);
                }
            }
            return merged;
        }

        /// <summary>
        /// Single contributor call, with the same exception-swallowing contract as
        /// <see cref="GetActiveStatusEffects"/> — a misbehaving contributor must not break
        /// the parent interaction.
        /// </summary>
        private void InvokeContributor(
            IStatusEffectContributor contributor,
            Profile profile,
            StatusEffectCallSite callSite,
            string parentIdentifier,
            bool isInitiator,
            StatusEffectFragments into)
        {
            if (profile == null) return;
            StatusEffectFragments contributed;
            try
            {
                contributed = contributor.Contribute(profile, callSite, InteractionType, parentIdentifier ?? string.Empty, isInitiator);
            }
            catch (Exception)
            {
                return;
            }
            into.MergeWith(contributed);
        }

        /// <summary>
        /// Returns the profile whose status effects should be surfaced on the completion
        /// message. Default: the recipient. Override when the interaction's natural
        /// subject is the initiator (or some other party derived from the identifier).
        /// </summary>
        protected virtual Profile GetStatusEffectSubject(
            Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return recipientProfile;
        }

        /// <summary>
        /// Basic validation - checks that profiles exist and honors any recipient- or
        /// initiator-blocking status effects (e.g. a !break on the relevant body part).
        /// Override to add interaction-specific validation; overrides that still want
        /// status-effect gating should call <see cref="GetActiveStatusEffects"/> themselves.
        ///
        /// Recipient blockers are checked first (preserves prior behavior); initiator
        /// blockers second, since they only matter for interactions where the initiator's
        /// anatomy is in play (climax, bully). Contributors that only ever block the
        /// recipient remain unaffected.
        /// </summary>
        public virtual ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            Profile initiatorProfile = Database.GetProfile(initiator);
            Profile recipientProfile = Database.GetProfile(recipient);

            if (initiatorProfile == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(initiator));
            }

            if (recipientProfile == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(recipient));
            }

            var recipientEffects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Consent, identifier, isInitiator: false);
            var recipientBlocker = recipientEffects.Blockers.FirstOrDefault(b => b.BlocksRecipient);
            if (recipientBlocker != null)
            {
                return ValidationResult.Failure(recipientBlocker.Reason);
            }

            var initiatorEffects = GetActiveStatusEffects(initiatorProfile, StatusEffectCallSite.Consent, identifier, isInitiator: true);
            var initiatorBlocker = initiatorEffects.Blockers.FirstOrDefault(b => b.BlocksInitiator);
            if (initiatorBlocker != null)
            {
                return ValidationResult.Failure(initiatorBlocker.Reason);
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Default consent warning. Appends any active status-effect consent fragments for the
        /// recipient, prefixing each with a single space (contributors should not include
        /// their own leading whitespace). Override to provide interaction-specific warnings;
        /// overrides that still want status-effect text should call
        /// <see cref="GetActiveStatusEffects"/> with <see cref="StatusEffectCallSite.Consent"/>
        /// and use <see cref="AppendStatusFragments"/> to apply the same spacing convention.
        /// </summary>
        public virtual string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string baseWarning = $"{initiatorProfile.displayName} wants to {InteractionType} with {recipientProfile.displayName}. Do you !consent?";
            var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Consent, identifier, isInitiator: false);
            return AppendStatusFragments(baseWarning, effects.ConsentWarnings);
        }

        /// <summary>
        /// Concatenate a base message with status-effect fragments, separating each non-empty
        /// fragment with a single leading space. Use this in overridden
        /// <see cref="GetConsentWarning"/> / <see cref="GetCompletionMessage"/> implementations
        /// so spacing stays consistent across processors.
        /// </summary>
        protected static string AppendStatusFragments(string baseMessage, IEnumerable<string> fragments)
        {
            if (fragments == null) return baseMessage ?? string.Empty;
            string result = baseMessage ?? string.Empty;
            foreach (var fragment in fragments)
            {
                if (string.IsNullOrEmpty(fragment)) continue;
                result += " " + fragment;
            }
            return result;
        }

        /// <summary>
        /// Walks <see cref="StatusEffectRegistry"/> and aggregates each contributor's fragments
        /// for the given profile. Returns an empty fragments instance if no contributors are
        /// registered. Processors call this from <see cref="GetConsentWarning"/>,
        /// <see cref="GetCompletionMessage"/>, and <see cref="ValidateInteraction"/> to opt
        /// into status-effect surfacing.
        ///
        /// Contributors own their own state mutations (e.g. odorize decrementing its use
        /// counter); the helper itself is a pure aggregator.
        /// </summary>
        /// <param name="profile">Initiator or recipient profile to inspect.</param>
        /// <param name="callSite">Which lifecycle phase is asking.</param>
        /// <param name="parentIdentifier">The parent interaction's identifier (e.g. the
        /// substance for <c>!feed</c>, the scent for <c>!odorize</c>). Forwarded to
        /// contributors so they can decide whether the parent satisfies one of their
        /// state entries (dose vice cravings, for example).</param>
        /// <param name="isInitiator">True if <paramref name="profile"/> is the initiator of
        /// the parent interaction. Defaults to false (recipient) to match the most common
        /// call shape.</param>
        protected StatusEffectFragments GetActiveStatusEffects(
            Profile profile,
            StatusEffectCallSite callSite,
            string parentIdentifier = null,
            bool isInitiator = false)
        {
            var merged = new StatusEffectFragments();
            if (profile == null) return merged;

            string safeIdentifier = parentIdentifier ?? string.Empty;
            foreach (var contributor in StatusEffectRegistry.GetAllContributors())
            {
                StatusEffectFragments contributed;
                try
                {
                    contributed = contributor.Contribute(profile, callSite, InteractionType, safeIdentifier, isInitiator);
                }
                catch (Exception)
                {
                    // A misbehaving contributor must not break the parent interaction.
                    continue;
                }
                merged.MergeWith(contributed);
            }
            return merged;
        }

        /// <summary>
        /// Helper method to get a random descriptor from a list
        /// </summary>
        protected string GetRandomDescriptor(List<string> descriptors)
        {
            var random = new Random();
            return descriptors[random.Next(descriptors.Count)];
        }

        /// <summary>
        /// Helper method to increment counts for both participants
        /// </summary>
        protected void IncrementBothCounts(string initiator, string recipient, string countLabel)
        {
            Database.IncrementCount(initiator, countLabel);
            Database.IncrementCount(recipient, countLabel);
        }

        /// <summary>
        /// Helper method to increment counts for both participants with rate limiting.
        /// Returns a rate limit message if either participant was rate limited.
        /// </summary>
        protected string IncrementBothCountsWithRateLimit(string initiator, string recipient, string countLabel, TimeSpan rateLimit)
        {
            bool initiatorCounted = Database.IncrementCountWithRateLimit(initiator, countLabel, rateLimit);
            bool recipientCounted = Database.IncrementCountWithRateLimit(recipient, countLabel, rateLimit);

            return GetRateLimitMessage(initiator, recipient, initiatorCounted, recipientCounted);
        }

        /// <summary>
        /// Helper method to increment different counts for initiator and recipient with rate limiting.
        /// Returns a rate limit message if either participant was rate limited.
        /// </summary>
        protected string IncrementDifferentCountsWithRateLimit(string initiator, string recipient,
            string initiatorLabel, string recipientLabel, TimeSpan rateLimit)
        {
            bool initiatorCounted = Database.IncrementCountWithRateLimit(initiator, initiatorLabel, rateLimit);
            bool recipientCounted = Database.IncrementCountWithRateLimit(recipient, recipientLabel, rateLimit);

            return GetRateLimitMessage(initiator, recipient, initiatorCounted, recipientCounted);
        }

        /// <summary>
        /// Generate the rate limit message based on who was rate limited
        /// </summary>
        private string GetRateLimitMessage(string initiator, string recipient, bool initiatorCounted, bool recipientCounted)
        {
            Profile initiatorProfile = Database.GetProfile(initiator);
            Profile recipientProfile = Database.GetProfile(recipient);

            if (!initiatorCounted && !recipientCounted)
            {
                // Both rate limited
                return $"\n\n[sub]Looks like that didn't make it into either dossier though... The clerks were probably still busy processing their last {InteractionType}s.[/sub]";
            }
            else if (!initiatorCounted)
            {
                // Only initiator rate limited
                return $"\n\n[sub]Looks like that didn't make it into {initiatorProfile.displayName}'s dossier though... The clerks were probably still busy processing their last {InteractionType}.[/sub]";
            }
            else if (!recipientCounted)
            {
                // Only recipient rate limited
                return $"\n\n[sub]Looks like that didn't make it into {recipientProfile.displayName}'s dossier though... The clerks were probably still busy processing their last {InteractionType}.[/sub]";
            }

            // Both counted successfully
            return string.Empty;
        }

        /// <summary>
        /// Helper method to increment different counts for initiator and recipient
        /// </summary>
        protected void IncrementDifferentCounts(string initiator, string recipient, string initiatorLabel, string recipientLabel)
        {
            Database.IncrementCount(initiator, initiatorLabel);
            Database.IncrementCount(recipient, recipientLabel);
        }

        // ====================================================================
        // Group interaction support (B4). Only casual processors opt in by
        // overriding GroupSpec; everything below is a no-op for the rest.
        // ====================================================================

        /// <summary>
        /// Declares how this interaction credits counts when resolved as a multi-person
        /// "moment". Null means the interaction is not group-capable (non-casuals), in which
        /// case multi-target commands fall back to independent 1:1 fan-out.
        /// </summary>
        public virtual GroupSpec GroupSpec => null;

        /// <summary>True when this interaction supports the hybrid group flow.</summary>
        public bool SupportsGroup => GroupSpec != null;

        /// <summary>Rate-limit window applied to group count increments (all casuals: 30m).</summary>
        protected virtual TimeSpan GroupRateLimit => TimeSpan.FromMinutes(30);

        /// <summary>
        /// Apply the group count math for a resolved moment over the consenting recipients,
        /// in consent order. Default handles symmetric and directional models; lapsit
        /// overrides for its per-position rule. Returns a "[sub]…clerks were busy…[/sub]"
        /// note naming any participant whose increment was rate-limited (or empty).
        ///
        /// The database is threaded in by the resolver rather than using the processor's own
        /// bound <see cref="Database"/>: registry instances bind to the live MonDB, so passing
        /// the caller's database keeps counting on the same store as the rest of resolution
        /// (and lets the resolver be unit-tested against the test database).
        /// </summary>
        public virtual string ApplyGroupCounts(IChateauDatabase database, string initiator, IReadOnlyList<string> consentersInOrder, string identifier)
        {
            var spec = GroupSpec;
            if (spec == null) return string.Empty;

            int recipientCount = consentersInOrder.Count; // R
            int participantCount = recipientCount + 1;     // M = R + 1
            var limited = new List<string>();

            if (spec.Kind == GroupCountKind.Symmetric)
            {
                // +1 per other participant → +(M-1) each.
                ApplyGroupIncrement(database, initiator, spec.SymmetricKey, participantCount - 1, limited);
                foreach (var c in consentersInOrder)
                    ApplyGroupIncrement(database, c, spec.SymmetricKey, participantCount - 1, limited);
            }
            else if (spec.Kind == GroupCountKind.Directional)
            {
                ApplyGroupIncrement(database, initiator, spec.GiveKey, recipientCount, limited);
                foreach (var c in consentersInOrder)
                    ApplyGroupIncrement(database, c, spec.TakeKey, 1, limited);
            }
            // Lapsit handles its own counts in an override.

            return BuildGroupRateLimitNote(limited);
        }

        /// <summary>
        /// Grant group-achievement titles for a resolved moment and return, per participant,
        /// the titles that were newly added. Default model:
        ///   - Symmetric (kiss / cuddle / handhold): every participant earns the interaction's
        ///     size titles — participating is enough.
        ///   - Directional one-way (spank / bully / lick / boobhat): only the initiator earns
        ///     them — they're the one doing it.
        /// Lapsit overrides this for its per-position rule; non-group interactions grant
        /// nothing. Size is M = recipients + 1 (the initiator), counting everyone.
        ///
        /// Mirrors <see cref="ApplyGroupCounts"/>: the database is threaded in by the resolver
        /// so granting hits the same store the rest of resolution uses (and stays unit-testable).
        /// </summary>
        public virtual List<GroupTitleGrant> GrantGroupTitles(IChateauDatabase database, string initiator, IReadOnlyList<string> consentersInOrder, string identifier)
        {
            var grants = new List<GroupTitleGrant>();
            var spec = GroupSpec;
            if (spec == null) return grants;

            int participantCount = consentersInOrder.Count + 1; // M
            var sizeTitles = ChateauSystemTitles.GetGroupSizeTitles(InteractionType, participantCount);
            if (sizeTitles.Count == 0) return grants;

            if (spec.Kind == GroupCountKind.Symmetric)
            {
                AddTitleGrant(database, initiator, sizeTitles, grants);
                foreach (var consenter in consentersInOrder)
                    AddTitleGrant(database, consenter, sizeTitles, grants);
            }
            else if (spec.Kind == GroupCountKind.Directional)
            {
                // One-way interaction: only the initiator is credited with the moment's size.
                AddTitleGrant(database, initiator, sizeTitles, grants);
            }
            // Lapsit (per-position) is handled in an override.

            return grants;
        }

        /// <summary>
        /// Grant the given achievement title texts to a user through the supplied database,
        /// skipping any already held, and append a <see cref="GroupTitleGrant"/> to
        /// <paramref name="grants"/> only when at least one title was actually new. Shared by
        /// the base group-title path and the lapsit per-position override.
        /// </summary>
        protected void AddTitleGrant(IChateauDatabase database, string userName, IReadOnlyList<string> titleTexts, List<GroupTitleGrant> grants)
        {
            if (titleTexts == null || titleTexts.Count == 0) return;
            Profile profile = database.GetProfile(userName);
            if (profile == null) return;
            if (profile.titles == null) profile.titles = new List<Title>();

            var newlyGranted = new List<string>();
            foreach (var titleText in titleTexts)
            {
                bool alreadyHas = profile.titles.Any(t =>
                    t.IsSystemTitle && t.titleText.Equals(titleText, StringComparison.OrdinalIgnoreCase));
                if (alreadyHas) continue;

                profile.titles.Add(new Title
                {
                    titleText = titleText,
                    givenBy = "Chateau",
                    grantedTime = DateTime.UtcNow,
                });
                newlyGranted.Add(titleText);
            }

            if (newlyGranted.Count == 0) return;
            database.SetProfile(userName, profile);
            grants.Add(new GroupTitleGrant
            {
                UserName = userName,
                DisplayName = profile.displayName,
                NewTitles = newlyGranted,
            });
        }

        /// <summary>
        /// Apply a single rate-limited +N increment for a group participant against the given
        /// database. A non-positive amount (e.g. the lap at position 0 has +0 lapsitgive) is a
        /// silent no-op — no timer is armed and no rate-limit note is produced. Participants
        /// whose increment is suppressed by their cooldown are collected (by display name).
        /// </summary>
        protected void ApplyGroupIncrement(IChateauDatabase database, string user, string countKey, int amount, List<string> limitedDisplayNames)
        {
            if (amount <= 0) return;
            bool applied = database.ChangeCountByWithRateLimit(user, countKey, amount, GroupRateLimit);
            if (!applied)
            {
                string displayName = database.GetDisplayName(user) ?? user;
                if (!limitedDisplayNames.Contains(displayName))
                    limitedDisplayNames.Add(displayName);
            }
        }

        /// <summary>
        /// Build the rate-limit sub-note for a resolved group, naming whichever participants'
        /// dossiers were too busy to record. Empty when nobody was limited.
        /// </summary>
        protected string BuildGroupRateLimitNote(List<string> limitedDisplayNames)
        {
            if (limitedDisplayNames == null || limitedDisplayNames.Count == 0) return string.Empty;
            string names = JoinNamesSerial(limitedDisplayNames);
            string dossierWord = limitedDisplayNames.Count > 1 ? "dossiers" : "dossier";
            return $"\n\n[sub]Looks like that didn't make it into {names}'s {dossierWord} though... The clerks were probably still busy processing their last {InteractionType}.[/sub]";
        }

        /// <summary>
        /// Combined completion message for a resolved group moment, over the consenting
        /// recipients in consent order. A single consenter degenerates to the 1:1 message.
        /// Casual processors override this for their own flavor; the fallback just lists
        /// everyone. (Casual interactions carry no status-effect contributors, so unlike the
        /// 1:1 path this is not wrapped with status fragments.)
        /// </summary>
        public virtual string GetGroupCompletionMessage(Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder, string identifier)
        {
            if (consentersInOrder.Count == 1)
                return GetCompletionMessage(initiatorProfile, consentersInOrder[0], identifier);

            var names = new List<string> { initiatorProfile.displayName };
            names.AddRange(consentersInOrder.Select(p => p.displayName));
            return $"{JoinNamesSerial(names)} share a {InteractionType} moment together.";
        }

        /// <summary>
        /// Directional group sentence helper: "Initiator {verbPresent} A, B, and C. {descriptor}".
        /// </summary>
        protected string BuildDirectionalGroupMessage(string initiatorDisplayName, IReadOnlyList<Profile> consenters, string verbPresent, string descriptor)
        {
            string names = JoinNamesSerial(consenters.Select(p => p.displayName).ToList());
            string message = $"{initiatorDisplayName} {verbPresent} {names}.";
            if (!string.IsNullOrEmpty(descriptor)) message += " " + descriptor;
            return message;
        }

        /// <summary>
        /// Symmetric group sentence helper: "A, B, and C {predicate}. {descriptor}" with the
        /// initiator listed first.
        /// </summary>
        protected string BuildSymmetricGroupMessage(Profile initiatorProfile, IReadOnlyList<Profile> consenters, string predicate, string descriptor)
        {
            var names = new List<string> { initiatorProfile.displayName };
            names.AddRange(consenters.Select(p => p.displayName));
            string message = $"{JoinNamesSerial(names)} {predicate}.";
            if (!string.IsNullOrEmpty(descriptor)) message += " " + descriptor;
            return message;
        }

        /// <summary>
        /// Group consent announcement shown when a multi-target casual command is invoked.
        /// Default reads "{initiator} wants to {verb} A, B, and C. Each of you, do you
        /// !consent?". Processors with awkward infinitives (lapsit) override.
        /// </summary>
        public virtual string GetGroupConsentWarning(Profile initiatorProfile, IReadOnlyList<Profile> recipients, string identifier)
        {
            string names = JoinNamesSerial(recipients.Select(p => p.displayName).ToList());
            string verb = GetInteractionVerb(VerbTense.Infinitive);
            return $"{initiatorProfile.displayName} wants to {verb} {names}. Do you each !consent? (or !no)";
        }

        /// <summary>
        /// Serial-comma name join: "A" / "A and B" / "A, B, and C".
        /// </summary>
        public static string JoinNamesSerial(IReadOnlyList<string> names)
        {
            if (names == null || names.Count == 0) return string.Empty;
            if (names.Count == 1) return names[0];
            if (names.Count == 2) return names[0] + " and " + names[1];
            return string.Join(", ", names.Take(names.Count - 1)) + ", and " + names[names.Count - 1];
        }
    }
}