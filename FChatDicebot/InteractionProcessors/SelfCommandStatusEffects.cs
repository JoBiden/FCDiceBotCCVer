using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Runs the registered status-effect contributors for a command that has no second party
    /// and therefore no consent flow to hang them off.
    ///
    /// Status effects normally fire from <see cref="InteractionProcessorBase"/> at the
    /// completion of something a recipient consented to; system commands deliberately skip the
    /// hook. <c>!drink</c> is the exception the convention allows for: the drinker is consenting
    /// with themselves, and its effects (a satisfied craving, a corruption drift) are things the
    /// channel is supposed to witness. This is the public seam that lets such a command invoke
    /// the same contributors against a single subject.
    ///
    /// <c>!detox</c> and <c>!wash</c> are the obvious future callers.
    /// </summary>
    public static class SelfCommandStatusEffects
    {
        /// <summary>
        /// Completion-time fragments for <paramref name="subject"/> acting on themselves.
        /// Contributors are invoked with <c>isInitiator: true</c> — there is only one party, and
        /// they are the one doing it.
        ///
        /// A contributor that throws is skipped rather than allowed to break the command, the
        /// same contract <see cref="InteractionProcessorBase"/> applies.
        /// </summary>
        public static StatusEffectFragments GetCompletionFragments(
            Profile subject, string interactionType, string parentIdentifier)
        {
            var merged = new StatusEffectFragments();
            if (subject == null) return merged;

            foreach (var contributor in StatusEffectRegistry.GetAllContributors())
            {
                StatusEffectFragments contributed;
                try
                {
                    contributed = contributor.Contribute(
                        subject,
                        StatusEffectCallSite.Completion,
                        interactionType,
                        parentIdentifier ?? string.Empty,
                        isInitiator: true);
                }
                catch (Exception)
                {
                    continue;
                }
                merged.MergeWith(contributed);
            }
            return merged;
        }
    }
}
