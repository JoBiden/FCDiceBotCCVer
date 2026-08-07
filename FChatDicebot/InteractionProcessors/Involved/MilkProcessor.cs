using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Processor for <c>!milk</c>. Produces 1–3 tagged bottles (rolled at process time), each a
    /// separate serial-numbered entry in the initiator's <see cref="Profile.collectibles"/>,
    /// and applies a 24-hour daily
    /// per-direction lock so a given milker can only milk a given resident once per
    /// Chateau day, regardless of substance. The lock is directional: it does not stop
    /// the recipient from milking the initiator back the same day. The Chateau provides
    /// the empty bottles — no precondition on the initiator's inventory.
    ///
    /// Self-target is allowed but handled inline by <see cref="BotCommands.ChateauMilk"/>
    /// as a special-cased self-sale: the consent flow is skipped and the initiator
    /// receives 1 copper + 1 bottle-currency in place of the bottle. The
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

        // The milked party's breast is what's in play, so the recipient's breast eicon shows.
        // (The typed identifier here is the substance, not a part, hence the fixed rule.)
        public override BodypartEiconRule BodypartEiconRule => BodypartEiconRule.Part("breast", BodypartEiconOwner.Recipient);

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
        /// Key used for the per-direction daily lock, stamped on the milker (initiator)
        /// only and scoped to the resident they milked: alice has <c>milk_give_Bob</c>
        /// after milking Bob. Because it lives only on the milker's side, Bob milking
        /// Alice back the same day is unaffected. For self-milking the key is
        /// <c>milk_give_&lt;self&gt;</c> on the single profile.
        ///
        /// Milk's key is one of several the draw lock spans — see <see cref="SourceDrawLock"/>.
        /// </summary>
        public static string DirectionTimerKey(string recipientUser)
            => SourceDrawLock.TimerKey(DrawVerb.Milk, recipientUser);

        /// <summary>
        /// True when <paramref name="profile"/> (the prospective milker) has already drawn from
        /// <paramref name="recipientUser"/> today <b>by any verb</b> — a source drink consumes
        /// the day just as a milking does. Used by both the command's pre-check and the
        /// processor's TOCTOU recheck. Callers that need to name what actually happened should
        /// use <see cref="SourceDrawLock.ActiveLock"/> instead.
        /// </summary>
        public static bool HasActiveDirectionLock(Profile profile, string recipientUser)
        {
            return SourceDrawLock.IsLocked(profile, recipientUser);
        }

        /// <summary>
        /// Channel-facing wording for the once-per-day per-direction lock, in its milk flavor.
        /// Prefer <see cref="SourceDrawLock.LockMessage"/> with the verb from
        /// <see cref="SourceDrawLock.ActiveLock"/> where the lock might have been consumed by a
        /// drink instead — this overload always claims a milking.
        /// </summary>
        public static string DirectionLockMessage(string recipientDisplayOrName, bool isSelf = false)
        {
            return SourceDrawLock.LockMessage(DrawVerb.Milk, recipientDisplayOrName, isSelf);
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
            // sneak through and produce a bottle sourced from yourself.
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
            DrawVerb? heldBy = SourceDrawLock.ActiveLock(initiatorProfile, recipient);
            if (heldBy != null)
            {
                Profile recipientProfile = Database.GetProfile(recipient);
                string recipientDisplay = recipientProfile?.displayName ?? recipient;
                // Name the verb that actually spent the day: an earlier !drinkfrom locks this
                // direction too, and claiming a milking that never happened reads as a bug.
                return ValidationResult.Failure(
                    SourceDrawLock.LockMessage(heldBy.Value, recipientDisplay));
            }

            // Break gating by substance → bodypart map, shared with the source-drink path so a
            // newly mapped substance gates both. (See Break-and-Rest spec, section B.)
            var milkBreakBlock = SubstanceBodyparts.CheckBreakBlock(
                Database, recipient, substance, MilkBreakBlockTail);
            if (milkBreakBlock != null) return ValidationResult.Failure(milkBreakBlock);

            return ValidationResult.Success();
        }

        /// <summary>End of the break refusal, in milk's flavor: "…'s breast is too broken for milking."</summary>
        public const string MilkBreakBlockTail = "too broken for milking.";

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string substance = ParseSubstanceFromIdentifier(command.pendingInteraction.identifier);

            Profile initiatorProfile = Database.GetProfile(initiator);
            Profile recipientProfile = Database.GetProfile(recipient);

            // TOCTOU recheck. Between !milk being typed and consent being given, another
            // pending milk against the same recipient could have landed. If the direction
            // lock is now active, stamp produced=0 and let GetCompletionMessage suppress
            // channel output.
            int produced = 0;
            if (!HasActiveDirectionLock(initiatorProfile, recipient))
            {
                int rolled = Rng.Next(ChateauCurrency.MilkRollMin, ChateauCurrency.MilkRollMax + 1);
                // The Chateau provides empty bottles — no clamp against initiator inventory.
                // The roll is the produced count, bounded only by MilkRollMin/Max.
                produced = Math.Max(ChateauCurrency.MilkRollMin, rolled);

                string corruptionTag = ChateauCurrency.GetCorruptionTagForValue(
                    Commitment.CorruptionProcessor.ReadCorruption(recipientProfile));

                if (initiatorProfile.collectibles == null)
                {
                    initiatorProfile.collectibles = new List<Collectible>();
                }

                // One entry per physical bottle, each with its own serial, rather than a single
                // entry carrying a count. A serial names one bottle, so an aggregated entry has
                // nothing to name. The whole block is reserved in one call so the numbers stay
                // contiguous and a concurrent acquisition can't interleave into the middle of them.
                DateTime milkedAt = DateTime.UtcNow;
                int firstSerial = Database.ClaimCollectibleSerials(produced);
                for (int i = 0; i < produced; i++)
                {
                    initiatorProfile.collectibles.Add(new MilkBottle
                    {
                        serial = firstSerial + i,
                        substance = substance,
                        subjectName = recipient,
                        acquiredAt = milkedAt,
                        quantity = 1,
                        corruptionTag = corruptionTag,
                    });
                }

                // Per-direction daily lock — until the *next* day-boundary regardless of
                // time-of-day. Stamped on the milker only and scoped to the milked
                // resident, so the recipient can still milk the initiator back today.
                SourceDrawLock.Stamp(initiatorProfile, DrawVerb.Milk, recipient);
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

        protected override string BuildConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
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
