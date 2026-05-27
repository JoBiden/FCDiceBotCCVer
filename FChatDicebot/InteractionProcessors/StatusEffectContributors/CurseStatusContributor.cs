using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.StatusEffectContributors
{
    /// <summary>
    /// Surfaces a profile's active curses to other interactions.
    ///
    /// <list type="bullet">
    /// <item>Disabler curses emit <see cref="ValidationBlock"/>s at the Consent call site
    /// when the parent interaction matches one of the curse's <c>BlockedInteractions</c>
    /// and the side is correct (recipient block when the cursed profile is the recipient
    /// of the parent; initiator block when it's the initiator).</item>
    /// <item>Modifier curses emit completion fragments — one per active modifier curse —
    /// substituting <c>{subject}</c> with the cursed party's display name. Composition is
    /// additive: a recipient with multiple modifier curses surfaces every fragment.</item>
    /// </list>
    ///
    /// Curses attach to the interaction's primary subject (the cursed party), not to both
    /// sides symmetrically; <see cref="SymmetricInvocation"/> is therefore false. The base
    /// class's <see cref="InteractionProcessorBase.ValidateInteraction"/> still calls this
    /// contributor once per side via the single-profile helper, so initiator-side blockers
    /// still work — they just aren't routed via the symmetric-completion wrapper.
    /// </summary>
    public class CurseStatusContributor : IStatusEffectContributor
    {
        // Curses attach to the cursed party; the wrapper's subject routing is correct.
        public bool SymmetricInvocation => false;

        // Skip self-referential parent interactions — a !curse's own completion already
        // names the curse, and !cleanse is a system command that doesn't run through this
        // pipeline anyway (defensive).
        private static readonly HashSet<string> SelfReferentialInteractions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            CurseProcessor.CurseType,
            "cleanse",
        };

        public StatusEffectFragments Contribute(
            Profile profile,
            StatusEffectCallSite callSite,
            string interactionType,
            string parentIdentifier,
            bool isInitiator)
        {
            var result = new StatusEffectFragments();
            if (profile == null) return result;
            if (string.IsNullOrEmpty(interactionType)) return result;
            if (SelfReferentialInteractions.Contains(interactionType)) return result;

            var curses = CurseInstance.LoadAll(profile);
            if (curses.Count == 0) return result;

            string subjectName = string.IsNullOrEmpty(profile.displayName)
                ? profile.userName
                : profile.displayName;

            foreach (var instance in curses)
            {
                if (!CurseProcessor.CatalogMap.TryGetValue(instance.Curse ?? string.Empty, out var spec))
                {
                    continue;
                }

                if (spec.Bucket == CurseProcessor.CurseBucket.Disabler)
                {
                    EmitDisablerBlocker(result, spec, interactionType, isInitiator, subjectName, instance.Curse);
                }
                else if (spec.Bucket == CurseProcessor.CurseBucket.Modifier
                         && callSite == StatusEffectCallSite.Completion)
                {
                    EmitModifierFragment(result, spec, subjectName);
                }
            }

            return result;
        }

        /// <summary>
        /// If the curse's <see cref="CurseProcessor.CurseSpec.BlockedInteractions"/> covers
        /// <paramref name="interactionType"/> on the side the cursed profile is on, append
        /// a <see cref="ValidationBlock"/>. The single-profile call shape means we set
        /// exactly one of BlocksInitiator / BlocksRecipient — whichever matches
        /// <paramref name="isInitiator"/>.
        /// </summary>
        private static void EmitDisablerBlocker(
            StatusEffectFragments result,
            CurseProcessor.CurseSpec spec,
            string interactionType,
            bool isInitiator,
            string subjectName,
            string curseName)
        {
            if (spec.BlockedInteractions == null) return;
            if (!spec.BlockedInteractions.TryGetValue(interactionType, out var side)) return;

            bool matches = side == CurseProcessor.BlockSide.Both
                || (isInitiator && side == CurseProcessor.BlockSide.Initiator)
                || (!isInitiator && side == CurseProcessor.BlockSide.Recipient);
            if (!matches) return;

            result.Blockers.Add(new ValidationBlock
            {
                Reason = ComposeBlockReason(subjectName, curseName, interactionType),
                Source = "curse:" + curseName,
                BlocksInitiator = isInitiator,
                BlocksRecipient = !isInitiator,
            });
        }

        /// <summary>
        /// Substitute the modifier curse's <c>{subject}</c> placeholder with the display
        /// name and append the result. Whitespace-leading: false — the base class's
        /// <see cref="InteractionProcessorBase.AppendStatusFragments"/> inserts a single
        /// space between the completion message and each fragment.
        /// </summary>
        private static void EmitModifierFragment(
            StatusEffectFragments result,
            CurseProcessor.CurseSpec spec,
            string subjectName)
        {
            if (string.IsNullOrEmpty(spec.ModifierTemplate)) return;
            string fragment = spec.ModifierTemplate.Replace("{subject}", subjectName ?? string.Empty);
            result.CompletionAppendix.Add(fragment);
        }

        /// <summary>
        /// User-facing block reason. Single template for all disabler curses keeps the
        /// surface uniform: <c>"{name} is cursed with {curse} and can't !{interaction} right
        /// now."</c>. Per-curse copy can be overridden later by adding cases here.
        /// </summary>
        public static string ComposeBlockReason(string subjectName, string curseName, string interactionType)
        {
            return subjectName + " is cursed with [b]" + curseName + "[/b] and can't !"
                + interactionType + " right now.";
        }
    }
}
