using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Interface for processing different types of Chateau interactions.
    /// Each interaction type should have its own implementation of this interface.
    /// </summary>
    public interface IInteractionProcessor
    {
        /// <summary>
        /// The type identifier for this interaction (e.g., "kiss", "mark", "rename")
        /// </summary>
        string InteractionType { get; }

        /// <summary>
        /// The investment level of this interaction: "casual", "involved", "commitment", or "consequence"
        /// </summary>
        string InvestmentLevel { get; }

        /// <summary>
        /// Process the interaction after consent has been given.
        /// </summary>
        /// <param name="command">The pending command containing interaction details</param>
        /// <returns>The interaction type if successful, "NoInteraction" if failed</returns>
        string ProcessInteraction(PendingCommand command);

        /// <summary>
        /// Validate that this interaction can be performed.
        /// </summary>
        /// <param name="initiator">The character initiating the interaction</param>
        /// <param name="recipient">The character receiving the interaction</param>
        /// <param name="identifier">The identifier (if any) for this interaction</param>
        /// <returns>Validation result with any error messages</returns>
        ValidationResult ValidateInteraction(string initiator, string recipient, string identifier);

        /// <summary>
        /// Get the consent warning text shown to the recipient before they consent.
        /// </summary>
        /// <param name="initiatorProfile">Profile of the initiator</param>
        /// <param name="recipientProfile">Profile of the recipient</param>
        /// <param name="identifier">The identifier (if any) for this interaction</param>
        /// <returns>The warning/description text shown when requesting consent</returns>
        string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier);

        /// <summary>
        /// Get the message displayed in the channel after the interaction is completed.
        /// </summary>
        /// <param name="initiatorProfile">Profile of the initiator</param>
        /// <param name="recipientProfile">Profile of the recipient</param>
        /// <param name="identifier">The identifier (if any) for this interaction</param>
        /// <returns>The message to display in channel</returns>
        string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier);
    }

    /// <summary>
    /// Result of validating an interaction
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        public static ValidationResult Failure(string errorMessage)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = errorMessage };
        }
    }
}
