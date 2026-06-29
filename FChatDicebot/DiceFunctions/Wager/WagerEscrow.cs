using System;

namespace FChatDicebot.DiceFunctions.Wager
{
    /// <summary>
    /// Builds and parses the Mongo-safe escrow labels used in <c>Profile.escrow</c>. A label is
    /// "{sanitizedGameKey}|{currency}", so a player can have stakes in several games at once
    /// without their escrow entries colliding, and a game can drain exactly its own stakes by
    /// matching the gameKey prefix.
    /// </summary>
    public static class WagerEscrow
    {
        // Mongo field names cannot contain '.' and cannot start with '$'. Channel keys can be
        // free-form, so fold those characters to '_' before using the key as a sub-field name.
        public static string SanitizeGameKey(string gameKey)
        {
            if (string.IsNullOrEmpty(gameKey)) return "game";
            return gameKey.Replace('.', '_').Replace('$', '_');
        }

        public static string Prefix(string gameKey)
        {
            return SanitizeGameKey(gameKey) + "|";
        }

        public static string Key(string gameKey, string currency)
        {
            return Prefix(gameKey) + currency;
        }

        /// <summary>
        /// The staked currency of any escrow label, regardless of which game it belongs to: the
        /// suffix after the last '|' (currency names never contain '|'). Null if malformed.
        /// </summary>
        public static string CurrencyOf(string escrowLabel)
        {
            if (string.IsNullOrEmpty(escrowLabel)) return null;
            int bar = escrowLabel.LastIndexOf('|');
            if (bar < 0 || bar == escrowLabel.Length - 1) return null;
            return escrowLabel.Substring(bar + 1);
        }

        /// <summary>
        /// True when <paramref name="escrowLabel"/> belongs to <paramref name="gameKey"/>, with
        /// the staked currency returned in <paramref name="currency"/>.
        /// </summary>
        public static bool TryGetCurrency(string escrowLabel, string gameKey, out string currency)
        {
            currency = null;
            if (string.IsNullOrEmpty(escrowLabel)) return false;
            string prefix = Prefix(gameKey);
            if (!escrowLabel.StartsWith(prefix, StringComparison.Ordinal)) return false;
            currency = escrowLabel.Substring(prefix.Length);
            return currency.Length > 0;
        }
    }
}
