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
    /// </summary>
    public interface IStatusEffectContributor
    {
        /// <param name="profile">The profile being inspected for active status effects.
        /// Depending on <paramref name="isInitiator"/> this is either the initiator or the
        /// recipient of the parent interaction.</param>
        /// <param name="callSite">Whether the parent interaction is asking for Consent-phase
        /// or Completion-phase contributions.</param>
        /// <param name="interactionType">The parent interaction's type (e.g. "kiss", "milk").
        /// Lets a contributor tailor its fragment to the interaction.</param>
        /// <param name="isInitiator">True if <paramref name="profile"/> is the initiator of
        /// the parent interaction; false if it is the recipient.</param>
        StatusEffectFragments Contribute(
            Profile profile,
            StatusEffectCallSite callSite,
            string interactionType,
            bool isInitiator);
    }
}
