using FChatDicebot.Database;
using FChatDicebot.Model;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Cross-party side effect that runs after a parent interaction has been processed.
    /// Unlike <see cref="IStatusEffectContributor"/> (which inspects one profile at a time
    /// and emits fragments only), a post-interaction effect receives BOTH initiator and
    /// recipient profiles and may mutate either or both. Used for spread-style mechanics
    /// where one party's state can transfer to the other (parasites today; potentially
    /// future contagion effects).
    ///
    /// Effects are registered in <see cref="PostInteractionEffectRegistry"/>. They fire
    /// from <see cref="InteractionProcessorBase.GetCompletionMessageWithStatusEffects"/>,
    /// after status-effect aggregation but before the final message is composed, so any
    /// returned fragments append to the completion text alongside the contributor fragments.
    ///
    /// Effects own their own persistence — when they mutate a profile they should call
    /// <see cref="IChateauDatabase.SetProfile"/> themselves; the base class doesn't write
    /// on their behalf.
    /// </summary>
    public interface IPostInteractionEffect
    {
        /// <summary>
        /// Fire after the parent interaction's <c>ProcessInteraction</c> has run.
        /// Implementations may mutate <paramref name="initiator"/> and/or
        /// <paramref name="recipient"/> and persist via <paramref name="database"/>.
        ///
        /// Returns completion-message fragments to append (empty list when no text).
        /// Fragments should NOT include leading whitespace — the caller inserts a single
        /// space between the host message and each fragment, matching
        /// <see cref="InteractionProcessorBase.AppendStatusFragments"/>.
        /// </summary>
        /// <param name="initiator">Initiator of the parent interaction.</param>
        /// <param name="recipient">Recipient of the parent interaction. Same reference as
        /// <paramref name="initiator"/> for self-target interactions; implementations should
        /// be ready for that.</param>
        /// <param name="interactionType">The parent interaction type (e.g. "kiss", "dose").
        /// Lets an effect skip self-referential cases (e.g. parasite spread skips
        /// <c>!infest</c>).</param>
        /// <param name="investmentLevel">Investment level of the parent interaction
        /// ("casual"/"involved"/"commitment"/"consequence"). Lets an effect filter casual-
        /// interaction targets that shouldn't trigger spread.</param>
        /// <param name="parentIdentifier">The parent interaction's identifier or empty.</param>
        /// <param name="database">Database handle for persisting mutations.</param>
        List<string> OnInteractionCompleted(
            Profile initiator,
            Profile recipient,
            string interactionType,
            string investmentLevel,
            string parentIdentifier,
            IChateauDatabase database);
    }
}
