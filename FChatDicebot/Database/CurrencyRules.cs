namespace FChatDicebot.Database
{
    /// <summary>
    /// Single source of truth for which currencies are exempt from the zero-floor on debits.
    /// Every currency debit must go through <see cref="IChateauDatabase.TryDebitCurrency"/> with
    /// <see cref="AllowsNegative"/> deciding the floor, so the exemption can't drift between call
    /// sites.
    /// </summary>
    public static class CurrencyRules
    {
        /// <summary>Intended negative-balance easter egg currency.</summary>
        public const string IouCurrency = "ious";

        /// <summary>Pre-existing joke currency players can already "pay" without having.</summary>
        public const string NothingCurrency = "nothing";

        public static bool AllowsNegative(string currency) =>
            currency == IouCurrency || currency == NothingCurrency;
    }
}
