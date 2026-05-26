namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Shared "you don't get out of this for free" cost shape used by reversal commands
    /// that need to bite back. Introduced for <see cref="Consequence.DoseProcessor"/>'s
    /// <c>!detox</c>; the future <c>!purge</c> and <c>!cleanse</c> reversals are expected
    /// to reuse the same enum + applier so cost wiring is uniform across consequence
    /// reversals.
    ///
    /// <list type="bullet">
    /// <item><term>MissedWork</term><description>Blocks <c>!work</c> for the remainder of the current Chateau day by setting the caller's <c>work</c> timer to the next UTC midnight.</description></item>
    /// <item><term>RandomCurse</term><description>Applies a random curse from the curse catalog. Until <c>!curse</c>/<c>!cleanse</c> ship this falls back to <see cref="RandomBreak"/> so the cost is never silently skipped.</description></item>
    /// <item><term>RandomBreak</term><description>Applies a 1–3 day break to a random breakable part via <see cref="Consequence.BreakProcessor.ApplyBreak"/>.</description></item>
    /// <item><term>LostTrainingPoint</term><description>Decrements one random training the caller has at level &gt; 0 by 1. No-op if every training is at zero.</description></item>
    /// </list>
    /// </summary>
    public enum PurgeCostType
    {
        MissedWork,
        RandomCurse,
        RandomBreak,
        LostTrainingPoint
    }
}
