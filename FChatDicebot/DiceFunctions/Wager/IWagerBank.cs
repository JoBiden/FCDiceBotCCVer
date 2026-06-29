using System.Collections.Generic;

namespace FChatDicebot.DiceFunctions.Wager
{
    /// <summary>
    /// The wager economy's money mover, replacing the legacy per-channel ChipPile helpers
    /// (BetChips/ClaimPot/GiveChips) for games that run on the global Chateau wallet.
    ///
    /// Every method operates within a SINGLE named currency and never compares two currencies
    /// against each other — that is the whole point of the design. A stake is debited out of a
    /// player's <c>currencies</c> into their <c>escrow</c> at commit time (so it can't be
    /// double-spent and can be refunded exactly), and a payout credits <c>currencies</c>.
    /// </summary>
    public interface IWagerBank
    {
        /// <summary>How much of <paramref name="currency"/> the player currently holds (0 if none).</summary>
        int BalanceOf(string player, string currency);

        /// <summary>
        /// Move <paramref name="amount"/> of <paramref name="currency"/> from the player's wallet
        /// into escrow for <paramref name="gameKey"/>. Returns false (and moves nothing) if the
        /// player can't afford it. A non-positive amount is a no-op that succeeds.
        /// </summary>
        bool Commit(string player, string gameKey, string currency, int amount);

        /// <summary>Return a single committed stake from escrow back to the player's wallet.</summary>
        void Refund(string player, string gameKey, string currency, int amount);

        /// <summary>Credit a winner directly (used when handing them the collected pot or an odds win).</summary>
        void PayWinner(string player, string currency, int amount);

        /// <summary>Debit a stake straight to the (virtual, minting) house — odds games, no escrow.</summary>
        void Burn(string player, string currency, int amount);

        /// <summary>
        /// Drain every stake committed to <paramref name="gameKey"/> across all
        /// <paramref name="players"/> and return the per-currency totals (the "bag") WITHOUT
        /// crediting anyone — for split payouts where the caller distributes to several winners.
        /// </summary>
        Dictionary<string, int> CollectPot(string gameKey, IEnumerable<string> players);

        /// <summary>
        /// Drain every stake committed to <paramref name="gameKey"/> across all
        /// <paramref name="players"/> and award the per-currency totals to <paramref name="winner"/>.
        /// Returns the awarded totals by currency (the "bag") for messaging.
        /// </summary>
        Dictionary<string, int> AwardPot(string gameKey, IEnumerable<string> players, string winner);

        /// <summary>Return every stake committed to <paramref name="gameKey"/> to whoever staked it.</summary>
        void RefundPot(string gameKey, IEnumerable<string> players);

        /// <summary>Return one player's stakes for a game to their wallet; returns the per-currency totals refunded.</summary>
        Dictionary<string, int> RefundAllForGame(string player, string gameKey);
    }
}
