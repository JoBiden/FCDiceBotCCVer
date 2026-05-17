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

        public string GetAndClearRateLimitMessage()
        {
            string msg = _lastRateLimitMessage;
            _lastRateLimitMessage = string.Empty;
            return msg;
        }

        /// <summary>
        /// Process the interaction. Override this to implement interaction-specific logic.
        /// </summary>
        public abstract string ProcessInteraction(PendingCommand command);

        /// <summary>
        /// Get the completion message. Override this to customize the message.
        /// </summary>
        public abstract string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier);

        /// <summary>
        /// Basic validation - checks that profiles exist and honors any recipient-blocking
        /// status effects (e.g. a !break on the relevant body part). Override to add
        /// interaction-specific validation; overrides that still want status-effect gating
        /// should call <see cref="GetActiveStatusEffects"/> themselves.
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

            var recipientEffects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Consent, isInitiator: false);
            var recipientBlocker = recipientEffects.Blockers.FirstOrDefault(b => b.BlocksRecipient);
            if (recipientBlocker != null)
            {
                return ValidationResult.Failure(recipientBlocker.Reason);
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
            var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Consent, isInitiator: false);
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
        /// <param name="isInitiator">True if <paramref name="profile"/> is the initiator of
        /// the parent interaction. Defaults to false (recipient) to match the most common
        /// call shape.</param>
        protected StatusEffectFragments GetActiveStatusEffects(
            Profile profile,
            StatusEffectCallSite callSite,
            bool isInitiator = false)
        {
            var merged = new StatusEffectFragments();
            if (profile == null) return merged;

            foreach (var contributor in StatusEffectRegistry.GetAllContributors())
            {
                StatusEffectFragments contributed;
                try
                {
                    contributed = contributor.Contribute(profile, callSite, InteractionType, isInitiator);
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