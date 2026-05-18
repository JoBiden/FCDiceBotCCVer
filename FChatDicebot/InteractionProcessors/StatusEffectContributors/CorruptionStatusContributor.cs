using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;
using System;

namespace FChatDicebot.InteractionProcessors.StatusEffectContributors
{
    /// <summary>
    /// Surfaces a profile's stored corruption value as a flavor fragment on the completion
    /// messages of *other* interactions (so a "deeply corrupted Bob" reads differently from
    /// a "transcendently pure Bob" when they get kissed, fed, etc.).
    ///
    /// Pure read of <c>profile.characteristics["corruption"]</c>; this contributor does not
    /// mutate any state — corruption changes only via <see cref="CorruptionProcessor"/>.
    /// The <c>corrupt</c> and <c>purify</c> interactions themselves are skipped to avoid a
    /// redundant fragment on their own completion text (which already reports the new value).
    /// Consent and Validation call sites are deliberately not contributed to — the spec
    /// limits the descriptor to completion-time channel output.
    /// </summary>
    public class CorruptionStatusContributor : IStatusEffectContributor
    {
        public StatusEffectFragments Contribute(
            Profile profile,
            StatusEffectCallSite callSite,
            string interactionType,
            bool isInitiator)
        {
            var result = new StatusEffectFragments();
            if (profile == null) return result;
            if (callSite != StatusEffectCallSite.Completion) return result;
            if (string.Equals(interactionType, CorruptionProcessor.CorruptType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(interactionType, CorruptionProcessor.PurifyType, StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            int corruption = CorruptionProcessor.ReadCorruption(profile);
            string fragment = DescribeBand(corruption, string.IsNullOrEmpty(profile.displayName) ? profile.userName : profile.displayName);
            if (string.IsNullOrEmpty(fragment)) return result;

            // No leading whitespace — the base AppendStatusFragments will insert a single
            // separating space when composing the host completion message.
            result.CompletionAppendix.Add(fragment);
            return result;
        }

        /// <summary>
        /// Threshold bands per the corrupt/purify spec. Returns an empty string for the
        /// neutral band (-9..+9) so the contributor stays silent there. The strongest tier
        /// uses "radiates" instead of "emanates" to mark a step-change in intensity.
        ///
        /// | Range       | Fragment                                                |
        /// |-------------|---------------------------------------------------------|
        /// | ≤ -100      | An absolute aura of corruption radiates from {name}.    |
        /// | -99..-50    | A strong aura of corruption emanates from {name}.       |
        /// | -49..-10    | A faint aura of corruption emanates from {name}.        |
        /// | -9..9       | (no fragment)                                           |
        /// | 10..49      | A faint aura of purity emanates from {name}.            |
        /// | 50..99      | A strong aura of purity emanates from {name}.           |
        /// | ≥ 100       | An absolute aura of purity radiates from {name}.        |
        /// </summary>
        public static string DescribeBand(int corruption, string subjectName)
        {
            if (corruption <= -100) return "An absolute aura of corruption radiates from " + subjectName + ".";
            if (corruption <= -50)  return "A strong aura of corruption emanates from "    + subjectName + ".";
            if (corruption <= -10)  return "A faint aura of corruption emanates from "     + subjectName + ".";
            if (corruption >= 100)  return "An absolute aura of purity radiates from "     + subjectName + ".";
            if (corruption >= 50)   return "A strong aura of purity emanates from "        + subjectName + ".";
            if (corruption >= 10)   return "A faint aura of purity emanates from "         + subjectName + ".";
            return string.Empty;
        }
    }
}
