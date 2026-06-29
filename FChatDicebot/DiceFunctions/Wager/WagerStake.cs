namespace FChatDicebot.DiceFunctions.Wager
{
    /// <summary>A single declared stake: an amount of one named currency.</summary>
    public class WagerStakeEntry
    {
        public string Currency;
        public int Amount;

        public WagerStakeEntry() { }
        public WagerStakeEntry(string currency, int amount)
        {
            Currency = currency;
            Amount = amount;
        }

        /// <summary>Same currency AND amount — i.e. an exact match that needs no acceptance.</summary>
        public bool Matches(WagerStakeEntry other)
        {
            return other != null
                && string.Equals(Currency, other.Currency, System.StringComparison.OrdinalIgnoreCase)
                && Amount == other.Amount;
        }

        public string Display()
        {
            return Amount + " " + Currency;
        }
    }

    /// <summary>
    /// A cross-currency stake one player has put on the table that another player must
    /// <c>!accept</c> (or <c>!no</c>) before the game can begin. Lives on the in-memory
    /// GameSession; it resolves in seconds, so it does not need to survive a bot restart.
    /// </summary>
    public class WagerProposal
    {
        public string Proposer;            // who offered the differing stake
        public string AwaitingAcceptFrom;  // who must !accept it
        public WagerStakeEntry Stake;      // the offered stake
    }
}
