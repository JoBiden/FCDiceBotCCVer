namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// What kind of rate limit an interaction carries.
    /// </summary>
    public enum CooldownKind
    {
        None,
        Cooldown,        // a hard cooldown that fires once the interaction lands
        MagnitudeQuota   // a per-day magnitude budget (corrupt / purify)
    }

    /// <summary>
    /// Who a cooldown binds. Determines the recipient/initiator framing of the
    /// consent-warning frequency clause.
    /// </summary>
    public enum CooldownBinds
    {
        None,
        Initiator,   // the initiator, usually scoped to an axis + the specific recipient
        Recipient,   // the recipient, globally
        Both,        // both parties
        Pair         // the specific (initiator, recipient) pair
    }

    /// <summary>
    /// Structured description of an interaction's rate limit. Authored once on the
    /// processor (the canonical interaction unit) and used as the single source of
    /// truth for both the player-facing consent-warning frequency clause and the
    /// derived help / <c>!whatis</c> cooldown strings, so the two can never drift.
    ///
    /// See <see cref="ConsentWarningText"/> for the consent-warning rendering and
    /// <see cref="FormatDuration"/> / <see cref="FormatAppliesTo"/> for the help text.
    /// </summary>
    public class CooldownSpec
    {
        public CooldownKind Kind;
        public CooldownBinds Binds;

        /// <summary>Cooldown period in whole days. 1 renders "day"/"per day", 7 renders "week"/"per week".</summary>
        public int PeriodDays;

        /// <summary>
        /// The per-axis scope for initiator-bound cooldowns: "vice", "curse",
        /// "parasite", "scent", "bodypart". Null when the cooldown is not axis-scoped.
        /// </summary>
        public string Scope;

        /// <summary>The daily magnitude budget for <see cref="CooldownKind.MagnitudeQuota"/> shapes (corrupt / purify = 10).</summary>
        public int QuotaMagnitude;

        /// <summary>
        /// The duration string shown in <c>!help</c> / <c>!whatis</c> (the
        /// <c>CooldownDuration</c> field). Derived so it can't diverge from the warning.
        /// </summary>
        public string FormatDuration()
        {
            if (Kind == CooldownKind.MagnitudeQuota)
            {
                return "Daily quota (" + QuotaMagnitude + " magnitude per recipient)";
            }
            if (PeriodDays == 1)
            {
                return "1 Day";
            }
            return PeriodDays + " Days";
        }

        /// <summary>
        /// The "applies to" string shown in <c>!help</c> / <c>!whatis</c> (the
        /// <c>CooldownAppliesTo</c> field). Derived so it can't diverge from the warning.
        /// </summary>
        public string FormatAppliesTo()
        {
            if (Kind == CooldownKind.MagnitudeQuota)
            {
                return "initiator";
            }
            switch (Binds)
            {
                case CooldownBinds.Both:
                    return "both initiator and recipient";
                case CooldownBinds.Pair:
                    return "both initiator and recipient (per pair)";
                case CooldownBinds.Recipient:
                    return "recipient";
                case CooldownBinds.Initiator:
                    return string.IsNullOrEmpty(Scope)
                        ? "initiator"
                        : "initiator (per " + Scope + " per recipient)";
                default:
                    return null;
            }
        }
    }
}
