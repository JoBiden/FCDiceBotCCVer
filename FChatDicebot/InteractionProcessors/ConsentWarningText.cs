using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// The single place the "[b]This should not be taken lightly...[/b]" seriousness block
    /// is composed. Each warned processor's <c>GetConsentWarning</c> builds its frequency
    /// clause from its <see cref="CooldownSpec"/> and any bespoke implication clause, then
    /// wraps them with <see cref="Block"/>. Keeping the opener and the per-shape frequency
    /// phrasings here means the warning wording can't drift between interactions, and stays
    /// in lockstep with the help text derived from the same <see cref="CooldownSpec"/>.
    ///
    /// The frequency clause is recipient-framed ("you"); initiator-bound shapes name the
    /// initiator by display name rather than "they".
    /// </summary>
    public static class ConsentWarningText
    {
        /// <summary>The fixed opening sentence of every seriousness block.</summary>
        public const string Opener = "This should not be taken lightly.";

        /// <summary>
        /// Wrap the opener followed by the supplied clauses (in order, empties skipped) in a
        /// single bold block. Callers control clause order so each interaction reads naturally
        /// (e.g. corrupt leads with its visibility disclosure before the quota rule).
        /// </summary>
        public static string Block(params string[] clauses)
        {
            var parts = new List<string> { Opener };
            if (clauses != null)
            {
                foreach (var clause in clauses)
                {
                    if (!string.IsNullOrEmpty(clause)) parts.Add(clause);
                }
            }
            return "[b]" + string.Join(" ", parts) + "[/b]";
        }

        /// <summary>"day" for a 1-day period, "week" for 7 days, an "N days" fallback otherwise.</summary>
        public static string PeriodWord(int periodDays)
        {
            if (periodDays == 1) return "day";
            if (periodDays == 7) return "week";
            return periodDays + " days";
        }

        /// <summary>
        /// Shape A — cooldown binding both parties (bond, breed). The verb phrase is the
        /// interaction's own ("declare a bond", "breed").
        /// </summary>
        public static string FrequencyBoth(string verbPhrase, int periodDays)
        {
            return "You can only " + verbPhrase + " between the two of you once per " + PeriodWord(periodDays) + ".";
        }

        /// <summary>
        /// Shape B — cooldown binding the recipient globally (employ, mark, objectify, plant,
        /// consume, monsterize, petrify, rename). The past participle is recipient-framed
        /// ("employed", "marked", ...).
        /// </summary>
        public static string FrequencyRecipient(string pastParticiple, int periodDays)
        {
            return "You can only be " + pastParticiple + " once per " + PeriodWord(periodDays) + ".";
        }

        /// <summary>
        /// Shape C — per-axis cooldown on the initiator (break, curse, dose, infest, odorize).
        /// The verb phrase carries the axis ("dose you with a given vice", "break a given part
        /// of you"); the initiator is named.
        /// </summary>
        public static string FrequencyPerAxis(string initiatorName, string verbPhrase, int periodDays)
        {
            return initiatorName + " can only " + verbPhrase + " once per " + PeriodWord(periodDays) + ".";
        }

        /// <summary>
        /// Shape D — per-day magnitude quota on the initiator (corrupt / purify). The verb is
        /// the effective verb infinitive; the initiator is named.
        /// </summary>
        public static string FrequencyQuota(string initiatorName, string verb, int quotaMagnitude, int periodDays)
        {
            return initiatorName + " can only " + verb + " you by " + quotaMagnitude + " per " + PeriodWord(periodDays) + ".";
        }

        /// <summary>
        /// Shape D — the "already spent today" line that follows the quota rule. Suppressed
        /// (returns empty) when nothing has been spent yet, so the prompt never reads
        /// "already corrupted you by 0".
        /// </summary>
        public static string ConsumedClause(string initiatorName, string verbPastTense, int usedToday)
        {
            if (usedToday <= 0) return string.Empty;
            return initiatorName + " has already " + verbPastTense + " you by " + usedToday + " today.";
        }
    }
}
