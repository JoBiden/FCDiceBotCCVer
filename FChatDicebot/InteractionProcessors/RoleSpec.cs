using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Declares which party an interaction is really *about* — and, for processors registered
    /// under two type keys, how that flips with the verb the resident typed.
    ///
    /// <para>
    /// Most interactions are <see cref="Fixed"/>: the initiator performs the act, so the
    /// initiator is the actor. A handful pair two verbs onto one processor and swap the roles
    /// between them — <c>!climaxfor</c>/<c>!climax</c> (who climaxed) and
    /// <c>!drinkfrom</c>/<c>!forcedrink</c> (who swallowed). Before this type existed, each of
    /// those pairs hand-wrote the same six pieces: two verb constants, a static resolver, its
    /// mirror, an <c>IsInverted</c>-style predicate, and overrides of
    /// <see cref="InteractionProcessorBase.GetEiconSubject"/> and
    /// <see cref="InteractionProcessorBase.GetStatusEffectSubject"/> that re-derived the same
    /// answer. Declaring the pair once replaces all of it.
    /// </para>
    ///
    /// <para>
    /// This is the role-direction sibling of <see cref="GroupSpec"/>, which declares the count
    /// model for group-capable casuals. <see cref="GroupSpec.Directional"/> hardcodes
    /// "initiator gives, recipients take" and cannot express a verb-dependent swap, which is
    /// why it doesn't cover this case.
    /// </para>
    ///
    /// <para>
    /// <b>Naming.</b> The "actor" is whoever the interaction's effects and flavor attach to,
    /// not necessarily whoever typed the command — the climaxer, the drinker. The
    /// "counterpart" is the other party. Each processor keeps its own domain-named wrappers
    /// (<c>ResolveClimaxer</c>, <c>ResolveDrinker</c>) over these, because those names are what
    /// the call sites and their tests read best.
    /// </para>
    /// </summary>
    public class RoleSpec
    {
        /// <summary>The verb on which the initiator is the actor. Null for a fixed spec.</summary>
        public string PrimaryVerb { get; private set; }

        /// <summary>The verb on which the <b>recipient</b> is the actor. Null for a fixed spec.</summary>
        public string InvertedVerb { get; private set; }

        /// <summary>True when a second verb flips the roles. False for the ordinary one-verb case.</summary>
        public bool IsInvertible => !string.IsNullOrEmpty(InvertedVerb);

        /// <summary>
        /// The ordinary case: the initiator is always the actor, whatever was typed. Every
        /// processor gets this unless it says otherwise.
        /// </summary>
        public static RoleSpec Fixed()
        {
            return new RoleSpec();
        }

        /// <summary>
        /// A two-verb processor. <paramref name="primaryVerb"/> reads "I do this" (initiator is
        /// the actor); <paramref name="invertedVerb"/> reads "you do this" (recipient is the
        /// actor). Both verbs must be registered against the same processor instance in
        /// <see cref="InteractionProcessorRegistry"/>.
        /// </summary>
        public static RoleSpec Invertible(string primaryVerb, string invertedVerb)
        {
            if (string.IsNullOrEmpty(primaryVerb)) throw new ArgumentException("primaryVerb is required", nameof(primaryVerb));
            if (string.IsNullOrEmpty(invertedVerb)) throw new ArgumentException("invertedVerb is required", nameof(invertedVerb));
            return new RoleSpec { PrimaryVerb = primaryVerb, InvertedVerb = invertedVerb };
        }

        /// <summary>
        /// True when the typed verb is the inverting one. An unrecognized or missing verb reads
        /// as the primary — the same lenient default the hand-written predicates had, so a
        /// consent record written before a verb existed still resolves to something sane rather
        /// than throwing at completion time.
        /// </summary>
        public bool IsInverted(string typedVerb)
        {
            return IsInvertible
                && string.Equals(typedVerb, InvertedVerb, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Whoever the interaction is about, for the verb actually typed.</summary>
        public string ResolveActor(string typedVerb, string initiator, string recipient)
        {
            return IsInverted(typedVerb) ? recipient : initiator;
        }

        /// <summary>Mirror of <see cref="ResolveActor"/> — the other party.</summary>
        public string ResolveCounterpart(string typedVerb, string initiator, string recipient)
        {
            return IsInverted(typedVerb) ? initiator : recipient;
        }

        /// <summary>
        /// Profile-shaped <see cref="ResolveActor"/> for the base class's eicon and
        /// status-effect redirects. Falls back to the initiator when either profile is missing,
        /// matching what every hand-written override did — a half-loaded pair is not the moment
        /// to start guessing.
        /// </summary>
        public Model.Profile ResolveActorProfile(string typedVerb, Model.Profile initiatorProfile, Model.Profile recipientProfile)
        {
            if (initiatorProfile == null || recipientProfile == null) return initiatorProfile;
            return IsInverted(typedVerb) ? recipientProfile : initiatorProfile;
        }

        /// <summary>Both type keys this spec covers, for registry wiring and sanity checks.</summary>
        public IEnumerable<string> Verbs
        {
            get
            {
                if (!IsInvertible) yield break;
                yield return PrimaryVerb;
                yield return InvertedVerb;
            }
        }
    }
}
