using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Backs both <c>!panties</c> (asking for someone's) and <c>!givepanties</c> (handing over
    /// your own). One act, two directions: the same instance is registered under both type keys
    /// in <see cref="InteractionProcessorRegistry"/>, and which side ends up holding the pair
    /// derives from the typed verb through <see cref="Roles"/>.
    ///
    /// <para>
    /// Either way the <b>recipient</b> is the one asked to consent, because either way they are
    /// the one who didn't type the command. What flips is who gives and who gets:
    /// <c>!panties Bob</c> means "may I have yours" and <c>!givepanties Bob</c> means "please
    /// take mine".
    /// </para>
    ///
    /// <para>
    /// The <b>holder</b> is the acting party for <see cref="RoleSpec"/> purposes — theirs is the
    /// collection that changes and the eicon that shows. The <b>subject</b> (the counterpart) is
    /// whose panties they were, and is what gets frozen onto
    /// <see cref="Collectible.subjectName"/>.
    /// </para>
    ///
    /// <para>
    /// This is the first non-bottle collectible, and it is deliberately thin: the processor mints
    /// a <see cref="Panties"/> into the holder's collection and everything else — serials,
    /// listing, transfer, the dossier privacy rule — is inherited. Compare
    /// <see cref="MilkProcessor"/>, which had to build all of that.
    /// </para>
    /// </summary>
    public class PantiesProcessor : InteractionProcessorBase
    {
        public const string PantiesType = "panties";
        public const string GivePantiesType = "givepanties";

        /// <summary>
        /// <c>!panties</c> — the initiator asks, so the initiator holds. <c>!givepanties</c> —
        /// the initiator offers, so the recipient holds.
        /// </summary>
        public static readonly RoleSpec PantiesRoles = RoleSpec.Invertible(
            primaryVerb: PantiesType, invertedVerb: GivePantiesType);

        public override RoleSpec Roles => PantiesRoles;

        public override string InteractionType => PantiesType;
        public override string InvestmentLevel => "involved";

        /// <summary>
        /// No bodypart eicon. Panties aren't a body part, and the custom
        /// <c>!seteicon panties</c> path already renders on the right person through
        /// <see cref="InteractionProcessorBase.GetEiconSubject"/>, which does see the typed verb.
        /// </summary>
        public override BodypartEiconRule BodypartEiconRule => null;

        /// <summary>
        /// Timer key prefix for the once-per-day per-direction lock. Stamped on the
        /// <b>holder</b> and scoped to the subject, mirroring <c>milk_give_</c>: getting a pair
        /// from Alice today doesn't stop Alice getting a pair from you.
        ///
        /// Deliberately <b>not</b> part of <see cref="SourceDrawLock"/>. That lock is about
        /// drawing fluid out of a resident and gates <c>!milk</c> against <c>!drinkfrom</c>;
        /// panties are not fluid and have no business consuming a milking.
        /// </summary>
        public const string DirectionKeyPrefix = "panties_give_";

        /// <summary>Timer key for one (holder, subject) direction. Stamped on the holder.</summary>
        public static string DirectionTimerKey(string subjectUser)
        {
            return DirectionKeyPrefix + subjectUser;
        }

        /// <summary>True when this holder has already taken this subject's panties today.</summary>
        public static bool HasActiveDirectionLock(Profile holderProfile, string subjectUser)
        {
            if (holderProfile?.timers == null) return false;
            if (!holderProfile.timers.TryGetValue(DirectionTimerKey(subjectUser), out var timer)) return false;
            return DateTime.UtcNow < timer.timerEnd;
        }

        public PantiesProcessor(IChateauDatabase database) : base(database) { }
        public PantiesProcessor() : base() { }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:    return "collected panties from";
                case VerbTense.Present: return "collects panties from";
                case VerbTense.Future:  return "will collect panties from";
                default:                return "collect panties from";
            }
        }

        // No CooldownRule / CooldownSpec, deliberately, and for the same reason MilkProcessor
        // and SourceDrinkProcessor have none: CooldownSpec's Binds enum cannot express a
        // *directional* lock. The closest value, CooldownBinds.Pair, renders "per pair", which
        // would claim the lock is symmetric — and it isn't. Alice taking Bob's panties leaves
        // Bob free to take Alice's the same day. The two commands hand-write their help strings
        // the way !milk and !drinkfrom do, and the lock states itself where it fires, in
        // DirectionLockMessage.

        // -------------------------------------------------------------------
        // Roles
        // -------------------------------------------------------------------

        /// <summary>Who ends up holding the pair, for the verb actually typed.</summary>
        public static string ResolveHolder(string interactionType, string initiator, string recipient)
        {
            return PantiesRoles.ResolveActor(interactionType, initiator, recipient);
        }

        /// <summary>Mirror of <see cref="ResolveHolder"/> — whose panties they were.</summary>
        public static string ResolveSubject(string interactionType, string initiator, string recipient)
        {
            return PantiesRoles.ResolveCounterpart(interactionType, initiator, recipient);
        }

        /// <summary>
        /// The typed verb, recovered from the per-call stash for the base class's status-effect
        /// redirect. Null before <see cref="ProcessInteraction"/> runs, which leaves the base on
        /// its recipient default rather than guessing a direction.
        /// </summary>
        protected override string ResolveTypedVerb(string identifier)
        {
            return _lastTypedVerb;
        }

        private string _lastTypedVerb;

        // -------------------------------------------------------------------
        // Validation
        // -------------------------------------------------------------------

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            return ValidateInteraction(initiator, recipient, identifier, PantiesType);
        }

        /// <summary>
        /// Typed overload — the direction lock lives on whichever side ends up holding, so the
        /// verb has to be known to check the right profile.
        /// </summary>
        public ValidationResult ValidateInteraction(string initiator, string recipient, string identifier, string typedVerb)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            if (string.Equals(initiator, recipient, StringComparison.Ordinal))
            {
                return ValidationResult.Failure(SelfTargetText);
            }

            string holder = ResolveHolder(typedVerb, initiator, recipient);
            string subject = ResolveSubject(typedVerb, initiator, recipient);

            var lockFailure = CheckDirectionLock(holder, subject);
            if (lockFailure != null) return ValidationResult.Failure(lockFailure);

            return ValidationResult.Success();
        }

        /// <summary>
        /// Shared refusal wording for the daily lock, used by both the command-layer pre-check
        /// and the processor's recheck so the two paths can't word it differently.
        /// </summary>
        public static string DirectionLockMessage(string subjectDisplayOrName)
        {
            return "You already have a pair of panties from " + subjectDisplayOrName
                + " for today. Give them a day to find another pair!";
        }

        /// <summary>Refusal for a self-target. There is no one to ask.</summary>
        public const string SelfTargetText =
            "Your own panties are already yours! Name another resident to ask, "
            + "or use !givepanties to offer a pair to someone else.";

        private string CheckDirectionLock(string holder, string subject)
        {
            Profile holderProfile = Database.GetProfile(holder);
            if (!HasActiveDirectionLock(holderProfile, subject)) return null;

            Profile subjectProfile = Database.GetProfile(subject);
            return DirectionLockMessage(subjectProfile?.displayName ?? subject);
        }

        // -------------------------------------------------------------------
        // Processing
        // -------------------------------------------------------------------

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string typedVerb = command.pendingInteraction.type;

            _lastTypedVerb = typedVerb;

            string holder = ResolveHolder(typedVerb, initiator, recipient);
            string subject = ResolveSubject(typedVerb, initiator, recipient);

            Profile holderProfile = Database.GetProfile(holder);

            // TOCTOU recheck: another pending pair against the same subject could have landed
            // between this being typed and the consent arriving. Stamp the identifier with
            // serial 0 so GetCompletionMessage suppresses channel output, and tell the
            // initiator privately rather than failing silently.
            if (HasActiveDirectionLock(holderProfile, subject))
            {
                Profile subjectProfile = Database.GetProfile(subject);
                _lastInitiatorPrivateMessage = DirectionLockMessage(subjectProfile?.displayName ?? subject);
                command.pendingInteraction.identifier = ComposeIdentifier(typedVerb, 0);
                Database.AddInteraction(command.pendingInteraction);
                Database.DeletePendingCommand(command.Id);
                return PantiesType;
            }

            if (holderProfile.collectibles == null)
            {
                holderProfile.collectibles = new List<Collectible>();
            }

            int serial = Database.ClaimCollectibleSerials(1);
            holderProfile.collectibles.Add(new Panties
            {
                serial = serial,
                subjectName = subject,
                acquiredAt = DateTime.UtcNow,
            });

            // Per-direction daily lock, to the next day-boundary regardless of time of day,
            // matching every other daily lock in the Chateau.
            holderProfile.timers[DirectionTimerKey(subject)] = new CoolDown
            {
                timerStart = DateTime.UtcNow,
                timerEnd = DateTime.UtcNow.Date.AddDays(1),
            };

            // The serial travels to GetCompletionMessage on the identifier — no dedicated slot
            // exists on the model. Same trick MilkProcessor and CorruptionProcessor use.
            command.pendingInteraction.identifier = ComposeIdentifier(typedVerb, serial);
            Database.AddInteraction(command.pendingInteraction);

            // Save BEFORE the count helper: it does its own read-modify-write on the profile
            // record and would otherwise stomp the collectible and timer written above.
            Database.SetProfile(holder, holderProfile);

            // Counts follow the roles, not who typed. Matches milkgive/milktake: the party who
            // performs the collecting takes "give", the party it's done to takes "take".
            string initiatorLabel = string.Equals(initiator, holder, StringComparison.Ordinal)
                ? "pantiesgive"
                : "pantiestake";
            string recipientLabel = string.Equals(recipient, holder, StringComparison.Ordinal)
                ? "pantiesgive"
                : "pantiestake";
            IncrementDifferentCounts(initiator, recipient, initiatorLabel, recipientLabel);

            Database.DeletePendingCommand(command.Id);

            return PantiesType;
        }

        // -------------------------------------------------------------------
        // Messages
        // -------------------------------------------------------------------

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            int serial = ParseSerialFromIdentifier(identifier);
            if (serial <= 0)
            {
                // TOCTOU no-op path: the direction lock fired between command and consent.
                // The initiator already got a private heads-up; say nothing in channel.
                return string.Empty;
            }

            string typedVerb = ParseTypeFromIdentifier(identifier);
            Profile holderProfile = PantiesRoles.ResolveActorProfile(typedVerb, initiatorProfile, recipientProfile);
            Profile subjectProfile = ReferenceEquals(holderProfile, initiatorProfile)
                ? recipientProfile
                : initiatorProfile;

            string holderName = holderProfile?.displayName ?? "Someone";
            string subjectName = subjectProfile?.displayName ?? "someone";

            return holderName + " tucks away the panties from " + subjectName
                + ", automagically labeled [b]#" + serial + "[/b] for the record books.";
        }

        protected override string BuildConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return BuildConsentWarning(initiatorProfile, recipientProfile, identifier, PantiesType);
        }

        /// <summary>
        /// Typed overload — the two verbs ask opposite questions of the same person, so the
        /// command layer passes the verb it saw. Applies the same <c>(or !no)</c> reminder the
        /// untyped entry point does.
        /// </summary>
        public string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier, string typedVerb)
        {
            return ConsentWarningText.WithDeclineHint(
                BuildConsentWarning(initiatorProfile, recipientProfile, identifier, typedVerb));
        }

        private string BuildConsentWarning(
            Profile initiatorProfile, Profile recipientProfile, string identifier, string typedVerb)
        {
            string initName = initiatorProfile?.displayName ?? "Someone";
            string recName = recipientProfile?.displayName ?? "you";

            // No bold seriousness block: that is the commitment/consequence shape, and this is
            // an involved interaction. !milk and !drinkfrom, the other involved acts carrying a
            // daily lock, likewise state neither the block nor the frequency here — the lock
            // speaks for itself when it fires.
            if (PantiesRoles.IsInverted(typedVerb))
            {
                // !givepanties — the recipient is being offered a pair.
                return initName + " would like " + recName + " to have a pair of their panties! "
                    + "Do you !consent to keeping them?";
            }

            // !panties — the recipient is being asked for a pair.
            return initName + " wants a pair of panties from " + recName + " for their collection! "
                + "Do you !consent to parting with them?";
        }

        // -------------------------------------------------------------------
        // Identifier payload — "{typedVerb}|{serial}"
        // -------------------------------------------------------------------

        public static string ComposeIdentifier(string typedVerb, int serial)
        {
            return IdentifierPayload.Compose(typedVerb, serial);
        }

        public static string ParseTypeFromIdentifier(string identifier)
        {
            string head = IdentifierPayload.ExtractHead(identifier);
            return string.IsNullOrEmpty(head) ? PantiesType : head;
        }

        public static int ParseSerialFromIdentifier(string identifier)
        {
            // -1 sentinel distinguishes "no serial recorded" (command-time shape) from a
            // deliberate 0 (the TOCTOU no-op path). Both suppress the channel message.
            return IdentifierPayload.ExtractTailOr(identifier, -1);
        }
    }
}
