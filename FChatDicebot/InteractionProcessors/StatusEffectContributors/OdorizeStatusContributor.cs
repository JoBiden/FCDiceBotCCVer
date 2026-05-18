using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.StatusEffectContributors
{
    /// <summary>
    /// Surfaces a recipient's (or initiator's) active scent layers in other interactions'
    /// consent and completion phases. On each Completion call the fragment's layer level
    /// is what gets emitted, then RemainingMentions ticks down by one; when it crosses a
    /// MentionsPerLayer boundary the layer descriptor steps down, and when it (or Layers)
    /// reaches zero the entry is removed entirely. Consent-phase fragments use the
    /// current layer count without mutating — a "mention" is one completed interaction.
    ///
    /// State is mutated on the in-memory Profile and persisted via the injected database
    /// when (and only when) Completion fires; the !odorize interaction itself doesn't
    /// register as a self-referential mention because OdorizeProcessor.InteractionType
    /// (= "odorize") is intentionally skipped.
    /// </summary>
    public class OdorizeStatusContributor : IStatusEffectContributor
    {
        private readonly IChateauDatabase _database;

        public OdorizeStatusContributor(IChateauDatabase database)
        {
            _database = database;
        }

        public StatusEffectFragments Contribute(
            Profile profile,
            StatusEffectCallSite callSite,
            string interactionType,
            bool isInitiator)
        {
            var result = new StatusEffectFragments();
            if (profile == null) return result;

            // The !odorize interaction's own consent/completion already speaks about the
            // scent; layering a contributor fragment on top would be redundant and would
            // double-count the freshly-applied scent against its own mention budget.
            if (string.Equals(interactionType, "odorize", System.StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            List<ScentLayer> layers = ScentLayer.LoadAll(profile);
            if (layers.Count == 0) return result;

            bool mutated = false;
            string subjectName = string.IsNullOrEmpty(profile.displayName) ? profile.userName : profile.displayName;

            // Iterate over a copy of indices so we can remove exhausted layers safely.
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                var layer = layers[i];

                if (layer.Layers <= 0 || layer.RemainingMentions <= 0)
                {
                    layers.RemoveAt(i);
                    mutated = true;
                    continue;
                }

                Identifier scentIdentifier = _database?.GetIdentifier(layer.Scent);
                string fragment = DescribeLayer(layer.Layers, scentIdentifier, layer.Scent, layer.AppliedBy, subjectName);

                if (callSite == StatusEffectCallSite.Consent)
                {
                    result.ConsentWarnings.Add(fragment);
                    continue;
                }

                // Completion: emit fragment and tick the layer down by one mention.
                result.CompletionAppendix.Add(fragment);

                layer.RemainingMentions -= 1;
                if (layer.RemainingMentions <= (layer.Layers - 1) * OdorizeProcessor.MentionsPerLayer)
                {
                    layer.Layers -= 1;
                }
                if (layer.RemainingMentions <= 0 || layer.Layers <= 0)
                {
                    layers.RemoveAt(i);
                }
                mutated = true;
            }

            if (mutated)
            {
                ScentLayer.SaveAll(profile, layers);
                if (_database != null && !string.IsNullOrEmpty(profile.userName))
                {
                    _database.SetProfile(profile.userName, profile);
                }
            }

            return result;
        }

        /// <summary>
        /// Renders the layer-level-appropriate descriptor. The scent rendering is delegated
        /// to <see cref="ScentText.ScentPhrase"/>, so "personal" scents read as
        /// "{appliedBy}'s {scent}", "scentof" scents as "a scent of {scent}", and default
        /// scents as "a {scent} scent". Layer values above MaxLayers collapse to the
        /// strongest template — defense against stale data.
        /// </summary>
        public static string DescribeLayer(int layers, Identifier scentIdentifier, string scentName, string appliedBy, string subjectName)
        {
            string phrase = ScentText.ScentPhrase(scentIdentifier, scentName, appliedBy);
            string capitalized = ScentText.Capitalize(phrase);

            if (layers >= 5) return "The room is [b]thick[/b] with " + phrase + ".";
            if (layers == 4) return capitalized + " fills the air around " + subjectName + ".";
            if (layers == 3) return capitalized + " hangs heavy on " + subjectName + ".";
            if (layers == 2) return capitalized + " clings to " + subjectName + ".";
            return capitalized + " lingers faintly on " + subjectName + ".";
        }
    }
}
