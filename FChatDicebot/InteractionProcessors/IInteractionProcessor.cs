using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using static FChatDicebot.InteractionProcessors.InteractionProcessorBase;

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
        /// Structured rate-limit description, or null when the interaction carries no warned
        /// cooldown. Single source of truth for both the consent-warning frequency clause and
        /// the derived help / <c>!whatis</c> cooldown strings. See <see cref="CooldownSpec"/>.
        /// </summary>
        CooldownSpec CooldownRule { get; }

        string GetInteractionVerb(VerbTense tense);
        string GetInteractionVerb(VerbTense tense, bool isPlural);

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

        /// <summary>
        /// Channel-bound entry point: returns <see cref="GetCompletionMessage"/> with any
        /// active status-effect completion fragments appended, plus the initiator's (and, for
        /// symmetric interactions, the recipient's) custom <c>!seteicon</c> flourish. The
        /// consent pipeline calls this so every interaction surfaces e.g. corruption auras /
        /// scent layers uniformly without each processor having to wire it in itself.
        ///
        /// <paramref name="interactionVerb"/> is the verb actually stamped on the pending
        /// (e.g. "purify" vs "corrupt", "sit" vs "lap"), used to look up the right custom
        /// eicon for shared-processor pairs. Null falls back to <see cref="InteractionType"/>.
        /// </summary>
        string GetCompletionMessageWithStatusEffects(Profile initiatorProfile, Profile recipientProfile, string identifier, string interactionVerb = null);

        /// <summary>
        /// Custom <c>!seteicon</c> flourish for a resolved group moment, over the consenting
        /// recipients in consent order. Default: the initiator's eicon (plus every consenter's
        /// for symmetric interactions); lapsit overrides for its per-position rule. Returns a
        /// leading-space-prefixed string, or empty when nobody involved has one set.
        /// </summary>
        string GetGroupEiconSuffix(string interactionVerb, Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder);

        /// <summary>
        /// Drain any out-of-band private note the processor wants delivered to the
        /// initiator after <see cref="ProcessInteraction"/> ran (e.g. corrupt/purify's
        /// "your queued interaction landed but daily quota was already spent" notice).
        /// Returns empty string when nothing is pending; the consent handler is expected
        /// to call this once per processed interaction and route any non-empty result to
        /// a private message.
        /// </summary>
        string GetAndClearInitiatorPrivateMessage();

        /// <summary>
        /// Drain the "clerks were too busy" rate-limit note a processor's
        /// <see cref="ProcessInteraction"/> populated via <c>IncrementBothCountsWithRateLimit</c>
        /// / <c>IncrementDifferentCountsWithRateLimit</c>. Returns empty string when the
        /// interaction wasn't rate-limited (or carries no count-based rate limit at all).
        /// The consent handler calls this once per processed interaction and appends any
        /// non-empty result to the channel completion message.
        /// </summary>
        string GetAndClearRateLimitMessage();

        // --- Group interaction support (B4). Non-group-capable processors return a null
        // GroupSpec; the other members are only invoked once a group has been resolved. ---

        /// <summary>Group count model, or null when the interaction isn't group-capable.</summary>
        GroupSpec GroupSpec { get; }

        /// <summary>True when this interaction supports the hybrid group flow (casuals).</summary>
        bool SupportsGroup { get; }

        /// <summary>Apply group counts over the consenting recipients (in consent order).</summary>
        string ApplyGroupCounts(Database.IChateauDatabase database, string initiator, IReadOnlyList<string> consentersInOrder, string identifier);

        /// <summary>
        /// Grant group-achievement titles (by resolved size, or lap-stack position) and return
        /// the newly-granted titles per participant so the command layer can announce them.
        /// </summary>
        List<GroupTitleGrant> GrantGroupTitles(Database.IChateauDatabase database, string initiator, IReadOnlyList<string> consentersInOrder, string identifier);

        /// <summary>Combined completion message for a resolved group moment.</summary>
        string GetGroupCompletionMessage(Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder, string identifier);

        /// <summary>Channel announcement shown when a multi-target casual command is invoked.</summary>
        string GetGroupConsentWarning(Profile initiatorProfile, IReadOnlyList<Profile> recipients, string identifier);
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
