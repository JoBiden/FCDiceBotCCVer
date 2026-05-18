using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the odorize interaction. Saturates the recipient with a scent that
    /// then surfaces in subsequent interactions via OdorizeStatusContributor, fading by
    /// mention rather than by time. Re-odorizing with the same scent stacks layers (capped
    /// at MaxLayers).
    ///
    /// Cooldown is per-(initiator → recipient → scent) tuple on the *initiator's* timers,
    /// so different initiators can pile on without waiting, and a single initiator can
    /// still odorize the same recipient with a different scent freely.
    /// </summary>
    public class OdorizeProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "odorize";
        public override string InvestmentLevel => "consequence";

        public const int MaxLayers = 5;
        public const int MentionsPerLayer = 3;
        public const string ScentCategory = "scent";

        public OdorizeProcessor(IChateauDatabase database) : base(database) { }
        public OdorizeProcessor() : base() { }

        public static string CooldownTimerKey(string scent, string recipientUserName)
        {
            return "odorize_" + scent + "_" + recipientUserName;
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid) return baseValidation;

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("scent"));
            }

            Identifier scentIdentifier = Database.GetIdentifier(identifier);
            if (scentIdentifier == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(identifier));
            }
            if (scentIdentifier.categories == null
                || !scentIdentifier.categories.Contains(ScentCategory, StringComparer.OrdinalIgnoreCase))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText(ScentCategory));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string scent = command.pendingInteraction.identifier;

            Database.AddInteraction(command.pendingInteraction);

            Profile recipientProfile = Database.GetProfile(recipient);
            Profile initiatorProfile = Database.GetProfile(initiator);

            // Store the initiator's display name on the layer so the contributor and
            // !wash output read naturally (e.g. "Alice's musk", not "alice12's musk").
            string applierName = string.IsNullOrEmpty(initiatorProfile?.displayName)
                ? initiator
                : initiatorProfile.displayName;
            ApplyLayer(recipientProfile, scent, applierName);

            DateTime now = DateTime.UtcNow;
            CoolDown cooldown = new CoolDown { timerStart = now, timerEnd = now.AddDays(7) };
            initiatorProfile.timers[CooldownTimerKey(scent, recipient)] = cooldown;

            Database.SetProfile(recipient, recipientProfile);
            Database.SetProfile(initiator, initiatorProfile);

            Database.DeletePendingCommand(command.Id);

            return "odorize";
        }

        /// <summary>
        /// Add or stack a scent layer on the recipient profile. New scent → 1 layer / 3 mentions.
        /// Existing scent → +1 layer capped at MaxLayers, RemainingMentions refreshed to
        /// Layers * MentionsPerLayer. Mutates the profile but does not save — callers
        /// (the processor's ProcessInteraction) own persistence.
        /// </summary>
        public static void ApplyLayer(Profile recipientProfile, string scent, string appliedBy)
        {
            if (recipientProfile == null || string.IsNullOrEmpty(scent)) return;

            var existing = ScentLayer.LoadAll(recipientProfile);
            DateTime now = DateTime.UtcNow;

            ScentLayer match = null;
            foreach (var layer in existing)
            {
                if (string.Equals(layer.Scent, scent, StringComparison.OrdinalIgnoreCase))
                {
                    match = layer;
                    break;
                }
            }

            if (match == null)
            {
                existing.Add(new ScentLayer
                {
                    Scent = scent,
                    Layers = 1,
                    RemainingMentions = MentionsPerLayer,
                    AppliedBy = appliedBy,
                    LastAppliedAt = now
                });
            }
            else
            {
                if (match.Layers < MaxLayers) match.Layers += 1;
                match.RemainingMentions = match.Layers * MentionsPerLayer;
                match.AppliedBy = appliedBy;
                match.LastAppliedAt = now;
            }

            ScentLayer.SaveAll(recipientProfile, existing);
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string phrase = ScentText.ScentPhrase(Database.GetIdentifier(identifier), identifier, initiatorProfile.displayName);
            return initiatorProfile.displayName + " has saturated " + recipientProfile.displayName + " with " + phrase + "! "
                + "The scent will linger on them through several interactions before it fades. "
                + recipientProfile.displayName + " may !wash once per day to scrub off a single layer.";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string phrase = ScentText.ScentPhrase(Database.GetIdentifier(identifier), identifier, initiatorProfile.displayName);
            return initiatorProfile.displayName + " is going to saturate " + recipientProfile.displayName + " with " + phrase + "! "
                + "[b]This should not be taken lightly, and can not be done frequently.[/b] "
                + "The scent will follow them into other interactions for some time. Do you !consent to this lingering aroma?";
        }
    }
}
