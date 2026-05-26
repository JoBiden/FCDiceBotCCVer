using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Processor for <c>!milk</c>. Produces 1–3 tagged bottles (rolled at process time)
    /// in the initiator's <see cref="Profile.milkInventory"/> and applies a symmetric
    /// 24-hour daily pair lock so the same (initiator, recipient) pair can only milk
    /// once per Chateau day, regardless of substance. The Chateau provides the empty
    /// bottles — no precondition on the initiator's inventory.
    ///
    /// Self-target is allowed but handled inline by <see cref="BotCommands.ChateauMilk"/>
    /// as a special-cased self-sale: the consent flow is skipped and the initiator
    /// receives 1 copper + 1 bottle-currency in place of the milkInventory entry. The
    /// processor itself therefore never sees a self-target PendingCommand under normal
    /// flow; the validation below still rejects it as a defensive guard.
    ///
    /// The rolled quantity travels into <see cref="GetCompletionMessage"/> via a
    /// <c>"substance|quantity"</c> payload on <see cref="Interaction.identifier"/>
    /// (the same trick <see cref="Commitment.CorruptionProcessor"/> uses for its
    /// applied-magnitude).
    /// </summary>
    public class MilkProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "milk";
        public override string InvestmentLevel => "involved";

        // Single mutable RNG used for the 1–3 bottle roll. Tests can swap this for a
        // seeded Random to make quantity outcomes deterministic. Not thread-safe; the
        // bot processes one interaction at a time.
        internal static Random Rng = new Random();

        public MilkProcessor(IChateauDatabase database) : base(database)
        {
        }

        public MilkProcessor() : base()
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:    return "milked";
                case VerbTense.Present: return "milks";
                case VerbTense.Future:  return "will milk";
                default:                return "milk";
            }
        }

        /// <summary>
        /// Key used for the symmetric per-pair daily lock. Same shape on both profiles:
        /// alice has <c>milk_pair_Bob</c>, bob has <c>milk_pair_Alice</c>. For
        /// self-milking the key is <c>milk_pair_&lt;self&gt;</c> on the single profile.
        /// </summary>
        public static string PairTimerKey(string otherUser) => "milk_pair_" + otherUser;

        /// <summary>
        /// True when <paramref name="profile"/> has an active milk-pair lock against
        /// <paramref name="otherUser"/>. Used by both the command's pre-check and the
        /// processor's TOCTOU recheck.
        /// </summary>
        public static bool HasActivePairLock(Profile profile, string otherUser)
        {
            if (profile?.timers == null) return false;
            string key = PairTimerKey(otherUser);
            if (!profile.timers.TryGetValue(key, out var timer)) return false;
            return DateTime.UtcNow < timer.timerEnd;
        }

        /// <summary>
        /// Channel-facing wording for the once-per-day pair lock. Centralized so the
        /// command's pre-check and the (unlikely) TOCTOU path don't drift. Pass
        /// <paramref name="isSelf"/>=true for the self-milk shortcut path so the message
        /// reads "You've already milked yourself" instead of the awkward
        /// "You've already milked &lt;your display name&gt;".
        /// </summary>
        public static string PairLockMessage(string recipientDisplayOrName, bool isSelf = false)
        {
            DateTime now = DateTime.UtcNow;
            string untilReset = Utils.GetTimeSpanPrint(now.Date.AddDays(1) - now);
            if (isSelf)
            {
                return "You've already milked yourself today. You can milk yourself again in "
                    + untilReset + ".";
            }
            return "You've already milked " + recipientDisplayOrName
                + " today. You can milk them again in " + untilReset + ".";
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            // Base validation runs profile existence + status-effect blockers (so a
            // future break:breast contributor automatically gates this without code here).
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            // Self-target is *allowed* at the command layer, but it's handled by a
            // shortcut path that never enters this processor (no PendingCommand created).
            // Reject here as a defensive guard so a stray self-targeted Interaction can't
            // sneak through and produce a milkInventory entry sourced from yourself.
            if (string.Equals(initiator, recipient, StringComparison.Ordinal))
            {
                return ValidationResult.Failure(
                    "Self-milking is a shortcut that should be handled by the command layer, not the processor.");
            }

            // identifier here may already be the composite "substance|qty" if this is
            // called from inside the processor flow — strip the qty before lookup.
            string substance = ParseSubstanceFromIdentifier(identifier);

            if (string.IsNullOrEmpty(substance))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("substance"));
            }

            Identifier substanceIdentifier = Database.GetIdentifier(substance);
            if (substanceIdentifier == null
                || substanceIdentifier.categories == null
                || !(substanceIdentifier.categories.Contains("substance", StringComparer.OrdinalIgnoreCase)
                     || substanceIdentifier.categories.Contains("vice", StringComparer.OrdinalIgnoreCase)))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("substance"));
            }

            Profile initiatorProfile = Database.GetProfile(initiator);
            if (HasActivePairLock(initiatorProfile, recipient))
            {
                Profile recipientProfile = Database.GetProfile(recipient);
                string recipientDisplay = recipientProfile?.displayName ?? recipient;
                return ValidationResult.Failure(PairLockMessage(recipientDisplay));
            }

            // Break gating by substance → bodypart map. BreakStatusContributor doesn't see
            // the substance, so the check lives here. (See Break-and-Rest spec, section B.)
            var milkBreakBlock = CheckBreakBlockForSubstance(recipient, substance);
            if (milkBreakBlock != null) return ValidationResult.Failure(milkBreakBlock);

            return ValidationResult.Success();
        }

        /// <summary>
        /// Maps a substance to the recipient's bodyparts that produce it, then checks the
        /// recipient's active breaks against that set. Returns the block message when a
        /// relevant part is broken; null otherwise (no break, or substance unmapped).
        /// </summary>
        private string CheckBreakBlockForSubstance(string recipient, string substance)
        {
            HashSet<string> relevantParts = BodypartsForSubstance(substance);
            if (relevantParts == null) return null;

            Profile recipientProfile = Database.GetProfile(recipient);
            if (recipientProfile == null) return null;

            var breaks = BreakInstance.LoadAllWithTick(recipientProfile);
            var brokenAndRelevant = breaks.Where(b => relevantParts.Contains(b.Part))
                                          .Select(b => b.Part)
                                          .ToList();
            if (brokenAndRelevant.Count == 0) return null;

            string recipientDisplay = string.IsNullOrEmpty(recipientProfile.displayName)
                ? recipient
                : recipientProfile.displayName;
            string parts = brokenAndRelevant.Count == 1
                ? brokenAndRelevant[0]
                : (brokenAndRelevant.Count == 2
                    ? brokenAndRelevant[0] + " and " + brokenAndRelevant[1]
                    : string.Join(", ", brokenAndRelevant.GetRange(0, brokenAndRelevant.Count - 1)) + ", and " + brokenAndRelevant[brokenAndRelevant.Count - 1]);
            string verbToBe = brokenAndRelevant.Count == 1 ? "is" : "are";
            return recipientDisplay + "'s " + parts + " " + verbToBe + " too broken for milking.";
        }

        private static HashSet<string> BodypartsForSubstance(string substance)
        {
            if (string.IsNullOrEmpty(substance)) return null;
            switch (substance.ToLowerInvariant())
            {
                case "milk":
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "breast" };
                case "saliva":
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mouth", "tongue" };
                case "golden":
                case "pre":
                case "cum":
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "dick", "ball", "pussy" };
                default:
                    return null;
            }
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string substance = ParseSubstanceFromIdentifier(command.pendingInteraction.identifier);

            Profile initiatorProfile = Database.GetProfile(initiator);
            Profile recipientProfile = Database.GetProfile(recipient);

            // TOCTOU recheck. Between !milk being typed and consent being given, another
            // pending milk against the same recipient could have landed. If the pair lock
            // is now active, stamp produced=0 and let GetCompletionMessage suppress
            // channel output.
            int produced = 0;
            if (!HasActivePairLock(initiatorProfile, recipient))
            {
                int rolled = Rng.Next(ChateauCurrency.MilkRollMin, ChateauCurrency.MilkRollMax + 1);
                // The Chateau provides empty bottles — no clamp against initiator inventory.
                // The roll is the produced count, bounded only by MilkRollMin/Max.
                produced = Math.Max(ChateauCurrency.MilkRollMin, rolled);

                string corruptionTag = ChateauCurrency.GetCorruptionTagForValue(
                    Commitment.CorruptionProcessor.ReadCorruption(recipientProfile));

                var bottle = new MilkBottle
                {
                    substance = substance,
                    sourceName = recipient,
                    milkedAt = DateTime.UtcNow,
                    quantity = produced,
                    corruptionTag = corruptionTag,
                };
                if (initiatorProfile.milkInventory == null)
                {
                    initiatorProfile.milkInventory = new List<MilkBottle>();
                }
                initiatorProfile.milkInventory.Add(bottle);

                // Symmetric 24h pair lock — until the *next* day-boundary regardless of
                // time-of-day. Same shape both sides so either party's command-time check
                // catches a re-milk attempt.
                var pairTimer = new CoolDown { timerEnd = DateTime.UtcNow.Date.AddDays(1) };
                if (initiatorProfile.timers == null)
                    initiatorProfile.timers = new Dictionary<string, CoolDown>();
                if (recipientProfile.timers == null)
                    recipientProfile.timers = new Dictionary<string, CoolDown>();
                initiatorProfile.timers[PairTimerKey(recipient)] = pairTimer;
                recipientProfile.timers[PairTimerKey(initiator)] = pairTimer;
            }

            // Stamp the truthful produced quantity onto the in-memory PendingCommand so
            // GetCompletionMessage (called next with the same toConsent reference) can
            // read it. Done before AddInteraction so the saved historical record also
            // carries the composite identifier.
            command.pendingInteraction.identifier = ComposeIdentifier(substance, produced);
            Database.AddInteraction(command.pendingInteraction);

            // Save profiles BEFORE the count-increment helper; it does its own
            // read-modify-write on the DB record and would otherwise be clobbered.
            Database.SetProfile(initiator, initiatorProfile);
            Database.SetProfile(recipient, recipientProfile);

            if (produced > 0)
            {
                IncrementDifferentCounts(initiator, recipient, "milkgive", "milktake");
            }

            Database.DeletePendingCommand(command.Id);

            return "milk";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            int produced = ParseQuantityFromIdentifier(identifier);
            if (produced <= 0)
            {
                // TOCTOU no-op path: pair lock fired between command and consent.
                // Suppress channel text.
                return string.Empty;
            }

            string substance = ParseSubstanceFromIdentifier(identifier);
            string substanceText = Utils.SubstanceToText(substance);
            string bottleWord = produced == 1 ? "bottle" : "bottles";

            string flavorAppendix = string.Empty;
            int recipientCorruption = Commitment.CorruptionProcessor.ReadCorruption(recipientProfile);
            string tag = ChateauCurrency.GetCorruptionTagForValue(recipientCorruption);
            if (tag == ChateauCurrency.CorruptTag)
            {
                flavorAppendix = " The " + substanceText + " has a faint dark sheen to it...";
            }
            else if (tag == ChateauCurrency.PurifiedTag)
            {
                flavorAppendix = " The " + substanceText + " practically glows.";
            }

            return initiatorProfile.displayName + " milks " + recipientProfile.displayName + " for "
                + produced + " " + bottleWord + " of " + substanceText
                + ". Bottled, sealed, and tagged." + flavorAppendix;
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            string substance = ParseSubstanceFromIdentifier(identifier);
            string substanceText = Utils.SubstanceToText(substance);

            // Project the corruption tag from the recipient's *current* value so the
            // warning surfaces whether the bottles will pick up corrupt/purified flavor.
            string tag = ChateauCurrency.GetCorruptionTagForValue(
                Commitment.CorruptionProcessor.ReadCorruption(recipientProfile));
            string tagFlavor = string.Empty;
            if (tag == ChateauCurrency.CorruptTag)
            {
                tagFlavor = " " + recipientProfile.displayName + "'s " + substanceText
                    + " is going to be quite [b]corrupt[/b].";
            }
            else if (tag == ChateauCurrency.PurifiedTag)
            {
                tagFlavor = " " + recipientProfile.displayName + "'s " + substanceText
                    + " is going to be quite [b]pure[/b].";
            }

            return initiatorProfile.displayName + " wants to milk " + recipientProfile.displayName
                + " for some " + substanceText + "." + tagFlavor + " Do you !consent to being milked?";
        }

        // -----------------------------------------------------------------------
        // Identifier payload helpers.
        //
        // Thin façade over the shared <see cref="InteractionProcessors.IdentifierPayload"/>
        // encoder, applying milk-specific defaults (missing substance → empty string,
        // missing quantity → -1 sentinel meaning "not yet stamped" — the TOCTOU no-op
        // path in <see cref="GetCompletionMessage"/> distinguishes a real 0 from this
        // pre-process shape). New processors that need to carry a per-call number
        // alongside a substance/verb should reuse <see cref="IdentifierPayload"/>
        // directly rather than recreating this pipe encoder.
        // -----------------------------------------------------------------------

        public static string ComposeIdentifier(string substance, int quantity)
        {
            return IdentifierPayload.Compose(substance, quantity);
        }

        public static string ParseSubstanceFromIdentifier(string identifier)
        {
            return IdentifierPayload.ExtractHead(identifier) ?? string.Empty;
        }

        public static int ParseQuantityFromIdentifier(string identifier)
        {
            // -1 sentinel distinguishes "no quantity recorded" (command-time shape)
            // from "quantity recorded as 0" (TOCTOU clamp-to-zero path).
            return IdentifierPayload.ExtractTailOr(identifier, -1);
        }
    }
}
