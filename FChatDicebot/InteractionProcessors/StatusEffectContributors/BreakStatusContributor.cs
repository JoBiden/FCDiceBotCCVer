using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.StatusEffectContributors
{
    /// <summary>
    /// Surfaces a profile's active breaks as validation blockers (when a relevant part
    /// blocks the interaction outright), as a !breed-specific flavor fragment (the only
    /// interaction with non-block-only flavor that overrides the generic pass-through),
    /// and as a generic "still sore from earlier" pass-through fragment listing every
    /// active break when nothing blocks.
    ///
    /// Block matrix (per spec):
    /// <list type="table">
    /// <item><term>!kiss / !feed (recipient)</term><description>mouth, tongue</description></item>
    /// <item><term>!cuddle (recipient)</term><description>body, torso, arm</description></item>
    /// <item><term>!handhold (recipient)</term><description>hand</description></item>
    /// <item><term>!spank (recipient)</term><description>ass, body, torso</description></item>
    /// <item><term>!climaxfor (recipient is climaxer)</term><description>dick, ball, pussy, ass</description></item>
    /// <item><term>!climax (initiator is climaxer)</term><description>dick, ball, pussy, ass</description></item>
    /// <item><term>!bully (initiator)</term><description>body, torso</description></item>
    /// </list>
    ///
    /// `!golden`, `!milk`, and `!train` carry per-call context (targeted part, substance,
    /// training type) that this contributor doesn't see in its signature; those processors
    /// implement their break gating directly in their own <c>ValidateInteraction</c>
    /// override. <c>!breed</c> never blocks — it only emits flavor.
    /// </summary>
    public class BreakStatusContributor : IStatusEffectContributor
    {
        // Skip self-referential and untouched-by-breaks interactions entirely.
        private static readonly HashSet<string> UntouchedInteractions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "break",
            "consume", "petrify", "plant", "corrupt", "purify",
            "bond", "objectify", "entitle", "employ",
            "monsterize", "rename", "dressup",
        };

        // !breed gets the special "who knows how, with their {parts} in the state ..." line
        // instead of the generic "still sore" pass-through. It never blocks.
        private const string BreedType = "breed";
        private static readonly HashSet<string> BreedRelevantParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "pussy", "ass", "dick", "ball",
        };

        // Recipient-side block table — interactionType → broken parts that prevent it.
        private static readonly Dictionary<string, HashSet<string>> RecipientBlocks = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["kiss"]      = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mouth", "tongue" },
            ["feed"]      = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mouth", "tongue" },
            ["cuddle"]    = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "body", "torso", "arm" },
            ["handhold"]  = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "hand" },
            ["spank"]     = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ass", "body", "torso" },
            ["climaxfor"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "dick", "ball", "pussy", "ass" },
        };

        // Initiator-side block table — interactionType → broken parts on the *initiator*
        // that prevent it.
        private static readonly Dictionary<string, HashSet<string>> InitiatorBlocks = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["climax"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "dick", "ball", "pussy", "ass" },
            ["bully"]  = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "body", "torso" },
        };

        // Per-interaction verb used in the block-replacement template
        // "{name}'s {parts} {is/are} too broken for {verb}." Falls back to the literal
        // interaction type when no entry exists, since not every interaction needs custom
        // wording.
        private static readonly Dictionary<string, string> BlockVerbs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["kiss"]      = "kissing",
            ["feed"]      = "feeding",
            ["cuddle"]    = "cuddling",
            ["handhold"]  = "hand-holding",
            ["spank"]     = "spanking",
            ["bully"]     = "bullying",
            ["milk"]      = "milking",
            ["climax"]    = "to climax",
            ["climaxfor"] = "to climax",
            ["golden"]    = "a golden shower",
            ["train"]     = "this training",
            ["mark"]      = "marking",
        };

        public StatusEffectFragments Contribute(
            Profile profile,
            StatusEffectCallSite callSite,
            string interactionType,
            bool isInitiator)
        {
            var result = new StatusEffectFragments();
            if (profile == null) return result;
            if (string.IsNullOrEmpty(interactionType)) return result;
            if (UntouchedInteractions.Contains(interactionType)) return result;

            var breaks = BreakInstance.LoadAllWithTick(profile);
            if (breaks.Count == 0) return result;

            string subjectName = string.IsNullOrEmpty(profile.displayName) ? profile.userName : profile.displayName;

            // ----- Special case D: !breed never blocks; emits a custom flavor at completion. -----
            if (string.Equals(interactionType, BreedType, StringComparison.OrdinalIgnoreCase))
            {
                if (callSite != StatusEffectCallSite.Completion) return result;
                var breedRelevant = breaks.Where(b => BreedRelevantParts.Contains(b.Part))
                                          .Select(b => b.Part)
                                          .ToList();
                if (breedRelevant.Count == 0) return result;
                result.CompletionAppendix.Add(ComposeBreedFlavor(subjectName, breedRelevant));
                return result;
            }

            // ----- Blocking table (B-list, static bodypart routing) -----
            HashSet<string> blockingParts = isInitiator
                ? (InitiatorBlocks.TryGetValue(interactionType, out var ib) ? ib : null)
                : (RecipientBlocks.TryGetValue(interactionType, out var rb) ? rb : null);

            if (blockingParts != null)
            {
                var brokenAndBlocking = breaks.Where(b => blockingParts.Contains(b.Part))
                                              .Select(b => b.Part)
                                              .ToList();
                if (brokenAndBlocking.Count > 0)
                {
                    result.Blockers.Add(new ValidationBlock
                    {
                        Reason = ComposeBlockReason(subjectName, brokenAndBlocking, interactionType),
                        Source = "break:" + interactionType,
                        BlocksInitiator = isInitiator,
                        BlocksRecipient = !isInitiator,
                    });
                    return result;
                }
            }

            // ----- Pass-through flavor (F): recipient-side only, completion-time only. -----
            if (callSite == StatusEffectCallSite.Completion && !isInitiator)
            {
                var partNames = breaks.Select(b => b.Part).ToList();
                result.CompletionAppendix.Add(ComposeSoreFromEarlier(subjectName, partNames));
            }
            return result;
        }

        public static string ComposeBlockReason(string subjectName, List<string> brokenParts, string interactionType)
        {
            string parts = JoinParts(brokenParts);
            string verbToBe = brokenParts.Count == 1 ? "is" : "are";
            string verb = BlockVerbs.TryGetValue(interactionType, out var v) ? v : interactionType;
            return subjectName + "'s " + parts + " " + verbToBe + " too broken for " + verb + ".";
        }

        public static string ComposeBreedFlavor(string subjectName, List<string> brokenParts)
        {
            string parts = JoinParts(brokenParts);
            string possessive = brokenParts.Count == 1 ? "it's" : "they're";
            return "...who knows how, with " + subjectName + "'s " + parts + " in the state " + possessive + " in.";
        }

        public static string ComposeSoreFromEarlier(string subjectName, List<string> brokenParts)
        {
            string parts = JoinParts(brokenParts);
            string verbToBe = brokenParts.Count == 1 ? "is" : "are";
            return "..." + subjectName + "'s " + parts + " " + verbToBe + " still sore from earlier.";
        }

        private static string JoinParts(List<string> parts)
        {
            if (parts == null || parts.Count == 0) return string.Empty;
            if (parts.Count == 1) return parts[0];
            if (parts.Count == 2) return parts[0] + " and " + parts[1];
            return string.Join(", ", parts.GetRange(0, parts.Count - 1)) + ", and " + parts[parts.Count - 1];
        }
    }
}
