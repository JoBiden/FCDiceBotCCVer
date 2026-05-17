using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

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
        /// Basic validation - checks that profiles exist.
        /// Override to add interaction-specific validation.
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

            return ValidationResult.Success();
        }

        /// <summary>
        /// Default consent warning. Override to provide interaction-specific warnings.
        /// </summary>
        public virtual string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return $"{initiatorProfile.displayName} wants to {InteractionType} with {recipientProfile.displayName}. Do you !consent?";
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