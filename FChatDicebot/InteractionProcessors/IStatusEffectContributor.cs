using FChatDicebot.Model;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Implemented by consequence interactions (odorize, dose, break, infest, corrupt, curse)
    /// that want to surface text or validation gates inside *other* interactions' consent and
    /// completion phases. Contributors are registered in <see cref="StatusEffectRegistry"/>;
    /// the parent interaction's processor calls
    /// <c>InteractionProcessorBase.GetActiveStatusEffects</c>, which walks the registry and
    /// merges results.
    ///
    /// Contributors own their own state mutations. For fade-by-mention semantics (e.g. odorize
    /// decrementing a use counter every time it contributes a non-empty fragment), perform the
    /// decrement inside <see cref="Contribute"/> before returning.
    ///
    /// **Whitespace convention:** fragments should not include leading whitespace. The base
    /// class inserts a single space between the host message and each fragment via
    /// <c>AppendStatusFragments</c>.
    ///
    /// **Symmetric vs subject-only invocation:** the completion-time wrapper
    /// <see cref="InteractionProcessorBase.GetCompletionMessageWithStatusEffects"/> walks each
    /// contributor either once (on the "subject" profile — usually the recipient, or whatever
    /// <see cref="InteractionProcessorBase.GetStatusEffectSubject"/> returns) or twice (once
    /// for the initiator with <c>isInitiator: true</c>, once for the recipient with
    /// <c>isInitiator: false</c>) based on <see cref="SymmetricInvocation"/>. Contributors
    /// whose state attaches to one specific party (corruption auras, broken parts) return
    /// false; contributors whose state is symmetric across both sides (dose cravings, odorize
    /// scent layers) return true.
    /// </summary>
    public interface IStatusEffectContributor
    {
        /// <summary>
        /// True when this contributor should be invoked once per side at completion time
        /// (initiator with <c>isInitiator: true</c>, recipient with <c>isInitiator: false</c>);
        /// false when it should be invoked once on the interaction's primary subject only.
        /// Self-target (initiator == recipient) collapses to a single invocation regardless.
        /// Consent and Validation call sites are unaffected — they invoke contributors with
        /// whatever profile the caller passed.
        /// </summary>
        bool SymmetricInvocation { get; }

        /// <param name="profile">The profile being inspected for active status effects.
        /// Depending on <paramref name="isInitiator"/> this is either the initiator or the
        /// recipient of the parent interaction.</param>
        /// <param name="callSite">Whether the parent interaction is asking for Consent-phase
        /// or Completion-phase contributions.</param>
        /// <param name="interactionType">The parent interaction's type (e.g. "kiss", "milk").
        /// Lets a contributor tailor its fragment to the interaction.</param>
        /// <param name="parentIdentifier">The parent interaction's identifier (e.g. the
        /// substance for <c>!feed</c>, the scent for <c>!odorize</c>, the vice for <c>!dose</c>).
        /// Empty/null when the parent interaction carries no identifier. Lets a contributor
        /// decide whether the parent satisfies one of its own state entries (e.g. dose
        /// satisfying a vice craving when the matching substance is fed).</param>
        /// <param name="isInitiator">True if <paramref name="profile"/> is the initiator of
        /// the parent interaction; false if it is the recipient.</param>
        StatusEffectFragments Contribute(
            Profile profile,
            StatusEffectCallSite callSite,
            string interactionType,
            string parentIdentifier,
            bool isInitiator);
    }
}
