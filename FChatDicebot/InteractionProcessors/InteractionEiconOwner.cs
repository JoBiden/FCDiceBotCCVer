namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Which side of an interaction its custom eicon (<c>!seteicon {interaction}</c>) belongs to.
    /// Resolved through the processor's <see cref="RoleSpec"/>, so it follows the verb the
    /// resident actually typed rather than assuming the initiator.
    /// </summary>
    public enum InteractionEiconOwner
    {
        /// <summary>
        /// Whoever the act attaches to — the spanker, the climaxer, the drinker. The default:
        /// an interaction is decorated by the person performing it.
        /// </summary>
        Actor,

        /// <summary>
        /// The other party. Two shapes of interaction want this.
        ///
        /// <para>
        /// <b>Collection-style</b> ones — <c>!milk</c>, <c>!panties</c>, <c>!givepanties</c> —
        /// mint a <see cref="Model.Collectible"/> that came off the counterpart and carries
        /// their name (<see cref="Model.Collectible.subjectName"/>) for as long as it exists.
        /// The icon decorates the <i>item</i>, so it is the source's to choose: a resident
        /// decides what their own milk or panties look like, and whoever ends up holding a
        /// bottle or a pair is who everyone sees it on.
        /// </para>
        ///
        /// <para>
        /// <b>Received</b> ones — <c>!pet</c> — where residents keep a "this happens to me"
        /// icon rather than a "I do this" one.
        /// </para>
        /// </summary>
        Counterpart,
    }
}
