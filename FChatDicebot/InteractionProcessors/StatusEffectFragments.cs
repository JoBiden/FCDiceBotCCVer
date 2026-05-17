using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Where in the parent interaction lifecycle a status-effect contributor is being asked
    /// to contribute. Consent is shown before the recipient !consents; Completion is shown
    /// in the channel after the interaction has been performed.
    /// </summary>
    public enum StatusEffectCallSite
    {
        Consent,
        Completion
    }

    /// <summary>
    /// A validation gate produced by a status-effect contributor. A processor that calls
    /// the status-effect helper from inside <see cref="IInteractionProcessor.ValidateInteraction"/>
    /// can refuse the interaction by returning <see cref="ValidationResult.Failure"/> with
    /// <see cref="Reason"/>.
    /// </summary>
    public class ValidationBlock
    {
        // Plain text shown to the channel, e.g. "Bob's mouth is broken and cannot be kissed."
        public string Reason;

        // Diagnostic tag identifying which contributor produced this block, e.g. "break:mouth".
        // Not shown to users; useful for tests and future logging.
        public string Source;

        // True if the initiator of the parent interaction is the blocked party.
        public bool BlocksInitiator;

        // True if the recipient of the parent interaction is the blocked party.
        public bool BlocksRecipient;
    }

    /// <summary>
    /// Aggregated contributions from all status-effect contributors for a single call site.
    /// Returned by <c>InteractionProcessorBase.GetActiveStatusEffects</c>; processors append
    /// the relevant collection to their consent/completion text or check <see cref="Blockers"/>
    /// in validation.
    /// </summary>
    public class StatusEffectFragments
    {
        public List<string> ConsentWarnings = new List<string>();
        public List<string> CompletionAppendix = new List<string>();
        public List<ValidationBlock> Blockers = new List<ValidationBlock>();

        /// <summary>
        /// Merge another fragments instance into this one in place. Order within each list
        /// is preserved (registration order of contributors).
        /// </summary>
        public void MergeWith(StatusEffectFragments other)
        {
            if (other == null) return;
            if (other.ConsentWarnings != null) ConsentWarnings.AddRange(other.ConsentWarnings);
            if (other.CompletionAppendix != null) CompletionAppendix.AddRange(other.CompletionAppendix);
            if (other.Blockers != null) Blockers.AddRange(other.Blockers);
        }
    }
}
