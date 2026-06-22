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
    }
}