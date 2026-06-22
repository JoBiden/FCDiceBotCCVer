using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Consequence
{
    /// <summary>
    /// Processor for the !break interaction. Breaks a recipient's bodypart for a number of
    /// Chateau days; the break drives blocking and flavor in subsequent interactions via
    /// <see cref="StatusEffectContributors.BreakStatusContributor"/>, and decays one
    /// severity per Chateau day via the lazy tick in
    /// <see cref="BreakInstance.LoadAllWithTick"/>. !rest decrements an additional level
    /// in exchange for blocking !work that day.
    ///
    /// Cooldown is per-(initiator → recipient → part) on the *initiator's* timers, so
    /// different initiators can pile breaks on the same recipient and a single initiator
    /// can break different parts of the same recipient freely.
    /// </summary>
    public class BreakProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "break";
        public override string InvestmentLevel => "consequence";

        public const string BreakCategory = "break";
        public const int DefaultDays = 3;
        public const int MinDays = 1;
        public const int MaxDays = 30;

        /// <summary>
        /// Whether the user's typed duration was adjusted to one of the bounds. Used by
        /// the consent-warning composer to surface a "clamped to min/max" suffix.
        /// </summary>
        public enum ClampDirection { None, Min, Max }

        public BreakProcessor(IChateauDatabase database) : base(database) { }
        public BreakProcessor() : base() { }

        public static string CooldownTimerKey(string part, string recipientUserName)
        {
            return "break_" + part + "_" + recipientUserName;
        }

        /// <summary>
        /// Read the requested break duration (in days) from an Interaction's
        /// <c>extraParameters</c>, clamped to [<see cref="MinDays"/>, <see cref="MaxDays"/>].
        /// Missing / unparseable / out-of-range values fall back to <see cref="DefaultDays"/>
        /// or the nearest bound.
        /// </summary>
        public static int ReadDays(Interaction interaction)
        {
            if (interaction?.extraParameters == null || interaction.extraParameters.Count == 0)
            {
                return DefaultDays;
            }
            int days;
            try
            {
                days = interaction.extraParameters[0].ToInt32();
            }
            catch (Exception)
            {
                return DefaultDays;
            }
            if (days < MinDays) return MinDays;
            if (days > MaxDays) return MaxDays;
            return days;
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid) return baseValidation;

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("bodypart"));
            }

            Identifier partIdentifier = Database.GetIdentifier(identifier);
            if (partIdentifier == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(identifier));
            }
            if (partIdentifier.categories == null
                || !partIdentifier.categories.Contains(BreakCategory, StringComparer.OrdinalIgnoreCase))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText(BreakCategory));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string part = command.pendingInteraction.identifier;
            int requestedDays = ReadDays(command.pendingInteraction);

            Database.AddInteraction(command.pendingInteraction);

            Profile recipientProfile = Database.GetProfile(recipient);
            Profile initiatorProfile = Database.GetProfile(initiator);

            string applierName = string.IsNullOrEmpty(initiatorProfile?.displayName)
                ? initiator
                : initiatorProfile.displayName;
            ApplyBreak(recipientProfile, part, requestedDays, applierName);

            DateTime now = DateTime.UtcNow;
            CoolDown cooldown = new CoolDown { timerStart = now, timerEnd = now.AddDays(7) };
            if (initiatorProfile.timers == null)
            {
                initiatorProfile.timers = new Dictionary<string, CoolDown>();
            }
            initiatorProfile.timers[CooldownTimerKey(part, recipient)] = cooldown;

            Database.SetProfile(recipient, recipientProfile);
            Database.SetProfile(initiator, initiatorProfile);

            Database.DeletePendingCommand(command.Id);

            return "break";
        }

        /// <summary>
        /// Append or replace a break entry on the recipient. If the part is already broken
        /// with more time remaining than <paramref name="days"/>, the existing entry wins
        /// (the recipient was already in the worse state); otherwise the new entry
        /// replaces it. Mutates the profile but does not save — callers own persistence.
        /// </summary>
        public static void ApplyBreak(Profile recipientProfile, string part, int days, string brokenBy)
        {
            if (recipientProfile == null || string.IsNullOrEmpty(part)) return;

            var entries = BreakInstance.LoadAllWithTick(recipientProfile);
            DateTime today = DateTime.UtcNow.Date;

            BreakInstance match = null;
            foreach (var entry in entries)
            {
                if (string.Equals(entry.Part, part, StringComparison.OrdinalIgnoreCase))
                {
                    match = entry;
                    break;
                }
            }

            if (match == null)
            {
                entries.Add(new BreakInstance
                {
                    Part = part,
                    Severity = days,
                    BrokenBy = brokenBy,
                    BrokenAt = DateTime.UtcNow,
                    LastTickedAt = today
                });
            }
            else if (days > match.Severity)
            {
                match.Severity = days;
                match.BrokenBy = brokenBy;
                match.BrokenAt = DateTime.UtcNow;
                match.LastTickedAt = today;
            }

            BreakInstance.SaveAll(recipientProfile, entries);
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            int days = ReadActiveSeverity(recipientProfile, identifier);
            string baseMessage = ComposeCompletionText(initiatorProfile.displayName, recipientProfile.displayName, identifier, days);

            var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Completion);
            return AppendStatusFragments(baseMessage, effects.CompletionAppendix);
        }

        /// <summary>
        /// Composes the consent warning the recipient sees in-channel. The base override
        /// uses <see cref="DefaultDays"/> because the API signature doesn't carry the
        /// requested days; the command (<c>ChateauBreak</c>) calls
        /// <see cref="ComposeConsentText"/> directly with the actual requested days and
        /// clamp direction so the player sees the real duration before saying !consent.
        /// </summary>
        public override CooldownSpec CooldownRule => Cooldown;

        public static readonly CooldownSpec Cooldown = new CooldownSpec
        {
            Kind = CooldownKind.Cooldown,
            Binds = CooldownBinds.Initiator,
            PeriodDays = 7,
            Scope = "bodypart"
        };

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string baseWarning = ComposeConsentText(initiatorProfile.displayName, recipientProfile.displayName, identifier, DefaultDays, ClampDirection.None);
            var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Consent);
            return AppendStatusFragments(baseWarning, effects.ConsentWarnings);
        }

        /// <summary>
        /// Build the consent-warning body with the actual requested days. Callable from the
        /// command so the warning reflects the user's typed duration instead of always
        /// reading <see cref="DefaultDays"/>.
        /// </summary>
        public static string ComposeConsentText(string initiatorName, string recipientName, string part, int days, ClampDirection clamp)
        {
            string daysWord = days == 1 ? "day" : "days";
            string blockedVerbs = BlockedVerbListForPart(part);
            string seriousness = ConsentWarningText.Block(
                ConsentWarningText.FrequencyPerAxis(initiatorName, "break a given part of you", Cooldown.PeriodDays),
                "While broken, your sore " + part + " will keep you from " + blockedVerbs
                    + ", and might be noticed during other interactions as well.");
            string warning = initiatorName + " is going to break " + recipientName + "'s " + part
                + " for " + days + " " + daysWord + "! " + seriousness + " Do you !consent?";

            if (clamp == ClampDirection.Min)
            {
                warning += " (Duration was adjusted to the minimum of " + MinDays + " day.)";
            }
            else if (clamp == ClampDirection.Max)
            {
                warning += " (Duration was adjusted to the maximum of " + MaxDays + " days.)";
            }
            return warning;
        }

        public static string ComposeCompletionText(string initiatorName, string recipientName, string part, int days)
        {
            string daysWord = days == 1 ? "day" : "days";
            return initiatorName + " has broken " + recipientName + "'s " + part
                + " for " + days + " " + daysWord + "! "
                + recipientName + ", don't forget you can !rest to recover one day faster, at the cost of skipping that day's !work.";
        }

        /// <summary>
        /// Returns a friendly, comma-joined list of activities the recipient will be unable
        /// to do once the named part is broken. Surfaces what the recipient is consenting
        /// to lose access to. Returns "interactions that depend on it" as a generic
        /// fallback for parts with no curated list.
        /// </summary>
        public static string BlockedVerbListForPart(string part)
        {
            if (string.IsNullOrEmpty(part)) return "interactions that depend on it";
            switch (part.ToLowerInvariant())
            {
                case "mouth":
                case "tongue":
                    return "kissing, feeding, milking saliva, and climaxing";
                case "breast":
                    return "being milked and corset training";
                case "pussy":
                    return "being milked and climaxing";
                case "dick":
                    return "being milked and climaxing";
                case "ball":
                    return "climaxing";
                case "ass":
                    return "being spanked, being milked, climaxing, and anal training";
                case "body":
                    return "being cuddled, being spanked, bullying others, and ponygirl training";
                case "torso":
                    return "being cuddled, being spanked, and corset training";
                case "arm":
                    return "being cuddled";
                case "hand":
                    return "hand-holding and instrument training";
                case "foot":
                    return "heel, ponygirl, and dance training";
                case "leg":
                    return "ponygirl and dance training";
                case "wing":
                    return "flight training";
                case "nose":
                    return "sensing scents around them";
                case "mind":
                    return "mathematics and magic training";
                default:
                    return "interactions that depend on it";
            }
        }

        private static int ReadActiveSeverity(Profile recipientProfile, string part)
        {
            var entries = BreakInstance.LoadAll(recipientProfile);
            foreach (var entry in entries)
            {
                if (string.Equals(entry.Part, part, StringComparison.OrdinalIgnoreCase))
                {
                    return Math.Max(entry.Severity, 1);
                }
            }
            return DefaultDays;
        }
    }
}
