using FChatDicebot.Model;

namespace FChatDicebot.BotCommands.Support
{
    /// <summary>
    /// Shared parsing for the authored duty-choice conditionals that !work and !volunteer
    /// filter on. A conditional's type is a three-letter kind prefix followed by the key to
    /// look up: "jobadventurer" (5+ adventurer experience via value), "trndeepthroat" (has
    /// the training), "curgold" (holds value-or-more of that currency), "monflight" (species
    /// identifier carries that category), or "none" (always available).
    ///
    /// The kind prefix is matched case-insensitively because live Duties documents contain
    /// both "none" and "None". A null conditional or blank type counts as "none" — the model
    /// requires every duty to keep at least one unconditional choice, and a malformed type
    /// must never crash the duty roll or silently strip every choice from a player's day.
    /// </summary>
    public static class DutyConditionalSupport
    {
        public static string Kind(Conditional conditional)
        {
            string type = conditional == null || conditional.type == null ? "" : conditional.type.Trim();
            if (type.Length == 0)
                return "non";
            if (type.Length < 3)
                return "";
            return type.Substring(0, 3).ToLowerInvariant();
        }

        public static string Key(Conditional conditional)
        {
            string type = conditional == null || conditional.type == null ? "" : conditional.type.Trim();
            return type.Length > 3 ? type.Substring(3) : "";
        }
    }
}
