using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.StatusEffectContributors
{
    /// <summary>
    /// Cross-party spread effect for parasites. On every non-casual interaction that isn't
    /// !infest itself, each parasite carried by either party rolls independently against
    /// <see cref="SpreadChance"/>; on success, the parasite is added to whichever partner
    /// doesn't already have it, with a grace window equal to
    /// <see cref="InfestProcessor.SpreadGracePeriod"/>.
    ///
    /// Skipping !infest avoids the loop where a freshly-applied parasite immediately
    /// spreads back to the initiator. Casual interactions skip entirely so a kiss can't
    /// silently dose someone with their partner's bimboslime — per spec, spread requires
    /// the players to opt into a non-casual interaction.
    /// </summary>
    public class ParasiteSpreadEffect : IPostInteractionEffect
    {
        /// <summary>Per-parasite, per-interaction probability of spread. 10% baseline.</summary>
        public const double SpreadChance = 0.10;

        private readonly Random _rng;

        public ParasiteSpreadEffect() : this(new Random()) { }

        /// <summary>Test seam: caller supplies a deterministic Random.</summary>
        public ParasiteSpreadEffect(Random rng)
        {
            _rng = rng ?? new Random();
        }

        public List<string> OnInteractionCompleted(
            Profile initiator,
            Profile recipient,
            string interactionType,
            string investmentLevel,
            string parentIdentifier,
            IChateauDatabase database)
        {
            var fragments = new List<string>();
            if (initiator == null || recipient == null) return fragments;

            // Casual interactions don't carry the weight to spread an infestation.
            if (string.Equals(investmentLevel, "casual", StringComparison.OrdinalIgnoreCase))
            {
                return fragments;
            }

            // !infest's own completion would otherwise see the freshly-added parasite on the
            // recipient and roll spread back to the initiator on the same interaction.
            if (string.Equals(interactionType, InfestProcessor.InfestType, StringComparison.OrdinalIgnoreCase))
            {
                return fragments;
            }

            bool isSelfTarget = ReferenceEquals(initiator, recipient)
                || string.Equals(initiator.userName, recipient.userName, StringComparison.Ordinal);

            // Self-target: there's no partner to spread to, so the spread roll is meaningless.
            if (isSelfTarget) return fragments;

            bool initiatorMutated = TrySpread(
                source: recipient, target: initiator,
                sourceLabel: SafeDisplayName(recipient), targetLabel: SafeDisplayName(initiator),
                fragments);
            bool recipientMutated = TrySpread(
                source: initiator, target: recipient,
                sourceLabel: SafeDisplayName(initiator), targetLabel: SafeDisplayName(recipient),
                fragments);

            if (initiatorMutated && database != null && !string.IsNullOrEmpty(initiator.userName))
            {
                database.SetProfile(initiator.userName, initiator);
            }
            if (recipientMutated && database != null && !string.IsNullOrEmpty(recipient.userName))
            {
                database.SetProfile(recipient.userName, recipient);
            }

            return fragments;
        }

        /// <summary>
        /// For each parasite the source carries, roll spread to the target. Returns true if
        /// the target's parasite list changed (caller persists). Each parasite spreads
        /// independently, so a single interaction can transfer multiple.
        /// </summary>
        private bool TrySpread(
            Profile source, Profile target,
            string sourceLabel, string targetLabel,
            List<string> fragments)
        {
            var sourceParasites = ParasiteInstance.LoadAll(source);
            if (sourceParasites.Count == 0) return false;

            var targetParasites = ParasiteInstance.LoadAll(target);
            var targetNames = new HashSet<string>(
                targetParasites.Select(p => p.Parasite ?? string.Empty),
                StringComparer.OrdinalIgnoreCase);

            bool mutated = false;
            int graceHours = (int)InfestProcessor.SpreadGracePeriod.TotalHours;
            foreach (var parasite in sourceParasites)
            {
                if (string.IsNullOrEmpty(parasite.Parasite)) continue;
                if (targetNames.Contains(parasite.Parasite)) continue;
                if (_rng.NextDouble() >= SpreadChance) continue;

                InfestProcessor.ApplyInfestation(
                    target,
                    parasite.Parasite,
                    infesterName: sourceLabel,
                    spreadFromContact: true,
                    gracePeriod: InfestProcessor.SpreadGracePeriod);
                targetNames.Add(parasite.Parasite);
                mutated = true;

                string parasitePhrase = ParasiteText.ParasiteName(parasite.Parasite);
                string verb = ParasiteText.HasOrHave(parasite.Parasite);
                fragments.Add("Meanwhile, " + parasitePhrase + " " + verb
                    + " taken the opportunity to spread from " + sourceLabel + " to " + targetLabel + "! "
                    + "[b]If that's not a desired outcome, you have a " + graceHours
                    + " hour window to !purge the parasite without consequence.[/b]");
            }
            return mutated;
        }

        private static string SafeDisplayName(Profile profile)
        {
            if (profile == null) return string.Empty;
            return string.IsNullOrEmpty(profile.displayName) ? profile.userName : profile.displayName;
        }
    }
}
