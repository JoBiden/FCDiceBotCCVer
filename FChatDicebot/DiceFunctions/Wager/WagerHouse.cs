namespace FChatDicebot.DiceFunctions.Wager
{
    /// <summary>
    /// House-funded odds games (Roulette, Slots). The house is virtual and non-conserving: a
    /// lost stake is burned out of circulation and a win is minted into the player's wallet. The
    /// owner accepts this inflation — currencies have no fixed spending power and the games' own
    /// odds keep a house edge. Everything is within ONE currency; nothing is ever compared across
    /// currencies. Because a bet is debit-first, a player can only wager a currency they hold, so
    /// no brand-new currency can be conjured at the wheel.
    /// </summary>
    public static class WagerHouse
    {
        /// <summary>
        /// Resolve a single odds bet of <paramref name="stake"/> in <paramref name="currency"/>
        /// against a TOTAL payout multiplier (stake-inclusive — e.g. 36 for a roulette single
        /// number = 35:1; 0 for a loss). Burns the stake, mints the gross payout on a win, and
        /// returns that payout (0 on loss). The caller must have confirmed affordability first.
        /// </summary>
        public static int Resolve(IWagerBank bank, string player, string currency, int stake, int totalMultiplier)
        {
            if (stake <= 0) return 0;
            bank.Burn(player, currency, stake);
            int payout = totalMultiplier > 0 ? stake * totalMultiplier : 0;
            if (payout > 0) bank.PayWinner(player, currency, payout);
            return payout;
        }
    }
}
