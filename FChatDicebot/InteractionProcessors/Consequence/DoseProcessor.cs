using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the !dose interaction. Hooks the recipient on an addictive vice that
    /// then surfaces in subsequent interactions via DoseStatusContributor as recurring
    /// cravings. Re-dosing the same vice stacks AddictionLevel (capped at
    /// <see cref="MaxAddictionLevel"/>); <c>!feed</c> of the matching substance or
    /// <c>!odorize</c> of the matching scent satisfies a craving without raising severity;
    /// <c>!detox</c> clears the vice entirely at a cost.
    ///
    /// Cooldown is per-(initiator → recipient → vice) on the *initiator's* timers, mirroring
    /// <see cref="OdorizeProcessor"/>. Self-dose is explicitly allowed — you can give
    /// yourself an addiction.
    /// </summary>
    public class DoseProcessor : InteractionProcessorBase
    {
        /// <summary>Stable string used by other systems (e.g. DoseStatusContributor's
        /// satisfaction check) so they don't have to reference the InteractionType
        /// property indirectly.</summary>
        public const string DoseType = "dose";

        public override string InteractionType => DoseType;
        public override string InvestmentLevel => "consequence";

        public const string ViceCategory = "vice";
        public const int MaxAddictionLevel = 10;
        public const PurgeCostType DefaultDetoxCost = PurgeCostType.RandomBreak;

        /// <summary>The three vices that <c>!climaxfor</c>/<c>!climax</c> auto-dose intensifies on
        /// the partner (or the climaxer for solo climaxes). Order is stable so tests and
        /// flavor text can rely on it.</summary>
        public static readonly string[] ClimaxDoseVices = new[] { "cum", "pre", "seminal" };

        public DoseProcessor(IChateauDatabase database) : base(database) { }
        public DoseProcessor() : base() { }

        public static string CooldownTimerKey(string vice, string recipientUserName)
        {
            return "dose_" + vice + "_" + recipientUserName;
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid) return baseValidation;

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText(ViceCategory));
            }

            Identifier viceIdentifier = Database.GetIdentifier(identifier);
            if (viceIdentifier == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(identifier));
            }
            if (viceIdentifier.categories == null
                || !viceIdentifier.categories.Contains(ViceCategory, StringComparer.OrdinalIgnoreCase))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText(ViceCategory));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string vice = command.pendingInteraction.identifier;

            Database.AddInteraction(command.pendingInteraction);

            bool isSelfDose = string.Equals(initiator, recipient, StringComparison.Ordinal);
            Profile recipientProfile = Database.GetProfile(recipient);
            // For self-dose, share the same Profile reference for initiator and recipient.
            // Two separate GetProfile loads would diverge in memory and one SetProfile
            // would overwrite the other — losing either the vice or the cooldown.
            Profile initiatorProfile = isSelfDose ? recipientProfile : Database.GetProfile(initiator);

            string doserName = string.IsNullOrEmpty(initiatorProfile?.displayName)
                ? initiator
                : initiatorProfile.displayName;
            ApplyDose(recipientProfile, vice, doserName);

            DateTime now = DateTime.UtcNow;
            CoolDown cooldown = new CoolDown { timerStart = now, timerEnd = now.AddDays(7) };
            if (initiatorProfile.timers == null)
            {
                initiatorProfile.timers = new Dictionary<string, CoolDown>();
            }
            initiatorProfile.timers[CooldownTimerKey(vice, recipient)] = cooldown;

            Database.SetProfile(recipient, recipientProfile);
            if (!isSelfDose)
            {
                Database.SetProfile(initiator, initiatorProfile);
            }

            Database.DeletePendingCommand(command.Id);

            return "dose";
        }

        /// <summary>
        /// Add or stack a vice on the recipient profile. New vice → AddictionLevel = 1.
        /// Existing vice → +1 AddictionLevel capped at <see cref="MaxAddictionLevel"/>.
        /// Mutates the profile but does not save — callers (ProcessInteraction, the climax
        /// intensifier) own persistence.
        /// </summary>
        public static void ApplyDose(Profile recipientProfile, string vice, string dosedBy)
        {
            if (recipientProfile == null || string.IsNullOrEmpty(vice)) return;

            var entries = ViceInstance.LoadAll(recipientProfile);
            DateTime now = DateTime.UtcNow;

            ViceInstance match = null;
            foreach (var entry in entries)
            {
                if (string.Equals(entry.Vice, vice, StringComparison.OrdinalIgnoreCase))
                {
                    match = entry;
                    break;
                }
            }

            if (match == null)
            {
                entries.Add(new ViceInstance
                {
                    Vice = vice.ToLowerInvariant(),
                    AddictionLevel = 1,
                    DosedBy = dosedBy,
                    FirstDosedAt = now,
                    LastEscalatedAt = now,
                });
            }
            else
            {
                if (match.AddictionLevel < MaxAddictionLevel) match.AddictionLevel += 1;
                match.DosedBy = dosedBy;
                match.LastEscalatedAt = now;
            }

            ViceInstance.SaveAll(recipientProfile, entries);
        }

        /// <summary>
        /// Climax integration: intensify any of <paramref name="viceNames"/> already present
        /// on <paramref name="targetProfile"/> by +1 AddictionLevel (capped at
        /// <see cref="MaxAddictionLevel"/>). Vices not already present are NOT created — the
        /// climax-dose is an escalator, not an introducer. Returns the list of vices that
        /// were actually intensified, in the same order as the input. Mutates the profile
        /// in-place but does not persist — caller (ClimaxforProcessor) owns the write.
        /// </summary>
        public static List<string> IntensifyExistingVices(Profile targetProfile, IEnumerable<string> viceNames)
        {
            var intensified = new List<string>();
            if (targetProfile == null || viceNames == null) return intensified;

            var entries = ViceInstance.LoadAll(targetProfile);
            if (entries.Count == 0) return intensified;
            DateTime now = DateTime.UtcNow;
            bool mutated = false;

            foreach (var vice in viceNames)
            {
                if (string.IsNullOrEmpty(vice)) continue;
                ViceInstance match = entries.FirstOrDefault(
                    e => string.Equals(e.Vice, vice, StringComparison.OrdinalIgnoreCase));
                if (match == null) continue;
                if (match.AddictionLevel >= MaxAddictionLevel)
                {
                    // Already maxed — still counts as "intensified" for the flavor fragment
                    // so the player sees the climax's effect, but don't bump the cap.
                    intensified.Add(vice);
                    continue;
                }
                match.AddictionLevel += 1;
                match.LastEscalatedAt = now;
                mutated = true;
                intensified.Add(vice);
            }

            if (mutated)
            {
                ViceInstance.SaveAll(targetProfile, entries);
            }
            return intensified;
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            int level = ReadAddictionLevel(recipientProfile, identifier);
            string vicePhrase = ViceText.ViceName(Database?.GetIdentifier(identifier), identifier, initiatorProfile?.displayName);
            return initiatorProfile.displayName + " has dosed " + recipientProfile.displayName
                + " with " + vicePhrase + ". [sub](addiction "
                + level + "/" + MaxAddictionLevel + ")[/sub]";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string vicePhrase = ViceText.ViceName(Database?.GetIdentifier(identifier), identifier, initiatorProfile?.displayName);
            string baseWarning = initiatorProfile.displayName + " wants to dose " + recipientProfile.displayName
                + " with " + vicePhrase + "! "
                + "[b]This should not be taken lightly, and can not be done frequently. "
                + "Addictive cravings can show up in any interaction until you !detox at a hefty cost. "
                + "Everyone will know when you're satisfying an addictive craving.[/b] "
                + "Do you !consent?";
            var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Consent, identifier, isInitiator: false);
            return AppendStatusFragments(baseWarning, effects.ConsentWarnings);
        }

        /// <summary>
        /// Reads the current AddictionLevel for a given vice on a profile, or 0 if the vice
        /// isn't present. Used by the completion message to render the post-dose level.
        /// </summary>
        public static int ReadAddictionLevel(Profile profile, string vice)
        {
            if (profile == null || string.IsNullOrEmpty(vice)) return 0;
            var entries = ViceInstance.LoadAll(profile);
            foreach (var entry in entries)
            {
                if (string.Equals(entry.Vice, vice, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.AddictionLevel;
                }
            }
            return 0;
        }
    }
}
