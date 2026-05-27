using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.StatusEffectContributors
{
    /// <summary>
    /// Surfaces a profile's active parasites as flavor fragments in subsequent interactions'
    /// completion messages. Independent from <see cref="ParasiteSpreadEffect"/>: flavor
    /// fires whether or not spread fires, and at a higher rate, so the host's infection is
    /// visibly present even when it's not jumping to a partner this round.
    ///
    /// SymmetricInvocation = true — both initiator's and recipient's flavors surface, because
    /// either side might be hosting. Self-target collapses to a single invocation per the
    /// base class.
    ///
    /// Flavor fires on every investment level (including casual) — per spec, the host's
    /// signs are noticeable everywhere. The only skip is the !infest interaction itself,
    /// because the completion already speaks about the parasite and a second flavor line
    /// piled on top would read redundantly.
    /// </summary>
    public class ParasiteFlavorContributor : IStatusEffectContributor
    {
        /// <summary>Per-parasite probability of flavor fragment firing on a single
        /// completion call. Independent rolls per parasite carried.</summary>
        public const double FlavorChance = 0.25;

        /// <summary>
        /// Per-parasite flavor templates, keyed by lowercase parasite name. <c>{subject}</c>
        /// is substituted with the host's display name (falling back to userName).
        /// Parasites without an entry contribute nothing — graceful degradation for custom
        /// parasites added through the deferred <c>!defineparasite</c> flow.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> FlavorMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "paraslime", "{subject} lets out a little gasp as their [color=pink]paraslime[/color] hits a sensitive spot" },
                { "bimboslime", "{subject} gets a vacant look, their eyes flashing pink as [color=pink]slime[/color] drips from their ear" },
                { "lustleeches", "{subject} looks utterly blissful, hearts in their eyes as their [color=purple]lust leeches[/color] spoil them`[eicon]qcpurplehearts[/eicon]`" },
                { "love", "{subject} feels full to the brim with [color=pink]love ♥[/color]" },
                { "tentacles", "{subject} can't help but squirm as their parasitic tentacles readjust inside them" },
                { "nymites", "A creeping familial warmth worms its way through the thoughts of {subject}, flushing their cheeks" },
            };

        private readonly Random _rng;

        public ParasiteFlavorContributor() : this(new Random()) { }

        /// <summary>Test seam: caller supplies a deterministic Random.</summary>
        public ParasiteFlavorContributor(Random rng)
        {
            _rng = rng ?? new Random();
        }

        public bool SymmetricInvocation => true;

        public StatusEffectFragments Contribute(
            Profile profile,
            StatusEffectCallSite callSite,
            string interactionType,
            string parentIdentifier,
            bool isInitiator)
        {
            var result = new StatusEffectFragments();
            if (profile == null) return result;

            // Consent-phase fragments would spoil the consent prompt. Per Dose's precedent
            // we keep flavor strictly to completion.
            if (callSite != StatusEffectCallSite.Completion) return result;

            // The !infest completion already speaks about the parasite — stacking flavor
            // on top reads redundant. Every other interaction type is fair game.
            if (string.Equals(interactionType, InfestProcessor.InfestType, StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            var parasites = ParasiteInstance.LoadAll(profile);
            if (parasites.Count == 0) return result;

            string subjectName = string.IsNullOrEmpty(profile.displayName) ? profile.userName : profile.displayName;

            foreach (var parasite in parasites)
            {
                if (string.IsNullOrEmpty(parasite.Parasite)) continue;
                if (!FlavorMap.TryGetValue(parasite.Parasite, out string template)) continue;
                if (_rng.NextDouble() >= FlavorChance) continue;

                result.CompletionAppendix.Add(template.Replace("{subject}", subjectName));
            }

            return result;
        }
    }
}
