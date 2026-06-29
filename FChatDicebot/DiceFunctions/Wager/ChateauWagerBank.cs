using System;
using System.Collections.Generic;
using System.Linq;
using FChatDicebot.Database;
using FChatDicebot.Model;

namespace FChatDicebot.DiceFunctions.Wager
{
    /// <summary>
    /// <see cref="IWagerBank"/> backed by the Chateau profile store. All wallet/escrow moves go
    /// through the database's atomic <c>$inc</c> operations (ChangeCurrency / ChangeEscrow), so
    /// concurrent commits and payouts on the same profile don't lose updates the way a
    /// read-modify-write SetProfile would.
    /// </summary>
    public class ChateauWagerBank : IWagerBank
    {
        private readonly IChateauDatabase database;

        // Legacy/default constructor: bind to the live MonDB, mirroring InteractionProcessorBase.
        public ChateauWagerBank() : this(MonDB.GetDatabase())
        {
        }

        // DI constructor for tests.
        public ChateauWagerBank(IChateauDatabase db)
        {
            database = db ?? throw new ArgumentNullException(nameof(db));
        }

        public int BalanceOf(string player, string currency)
        {
            Profile profile = database.GetProfile(player);
            if (profile?.currencies == null) return 0;
            return profile.currencies.TryGetValue(currency, out int value) ? value : 0;
        }

        public bool Commit(string player, string gameKey, string currency, int amount)
        {
            if (amount <= 0) return true;
            if (BalanceOf(player, currency) < amount) return false;

            database.ChangeCurrency(player, currency, -amount);
            database.ChangeEscrow(player, WagerEscrow.Key(gameKey, currency), amount);
            return true;
        }

        public void Refund(string player, string gameKey, string currency, int amount)
        {
            if (amount <= 0) return;
            database.ChangeEscrow(player, WagerEscrow.Key(gameKey, currency), -amount);
            database.ChangeCurrency(player, currency, amount);
        }

        public void PayWinner(string player, string currency, int amount)
        {
            if (amount <= 0) return;
            database.ChangeCurrency(player, currency, amount);
        }

        public void Burn(string player, string currency, int amount)
        {
            // Odds-game stake going to the (virtual, minting) house: a straight debit, no escrow.
            if (amount <= 0) return;
            database.ChangeCurrency(player, currency, -amount);
        }

        public Dictionary<string, int> CollectPot(string gameKey, IEnumerable<string> players)
        {
            var totals = new Dictionary<string, int>();
            foreach (string player in players.Where(p => !string.IsNullOrEmpty(p)).Distinct())
            {
                foreach (var entry in DrainGameEscrow(player, gameKey))
                {
                    totals[entry.Key] = (totals.TryGetValue(entry.Key, out int sum) ? sum : 0) + entry.Value;
                }
            }
            return totals;
        }

        public Dictionary<string, int> AwardPot(string gameKey, IEnumerable<string> players, string winner)
        {
            var totals = CollectPot(gameKey, players);
            foreach (var entry in totals)
            {
                database.ChangeCurrency(winner, entry.Key, entry.Value);
            }
            return totals;
        }

        public void RefundPot(string gameKey, IEnumerable<string> players)
        {
            foreach (string player in players.Where(p => !string.IsNullOrEmpty(p)).Distinct())
            {
                RefundAllForGame(player, gameKey);
            }
        }

        public Dictionary<string, int> RefundAllForGame(string player, string gameKey)
        {
            var refunded = DrainGameEscrow(player, gameKey);
            foreach (var entry in refunded)
            {
                database.ChangeCurrency(player, entry.Key, entry.Value);
            }
            return refunded;
        }

        /// <summary>
        /// Refund EVERY player's escrow back to their wallet, across all profiles. Intended for
        /// startup only: after a restart the in-memory game sessions are gone, so any lingering
        /// escrow is from a game the restart interrupted and must be returned. Must NOT be called
        /// while games are live (it would refund active stakes). Returns the number of entries
        /// refunded.
        /// </summary>
        public int ReconcileAllEscrow()
        {
            int refunded = 0;
            var profiles = database.GetAllProfiles();
            if (profiles == null) return 0;
            foreach (var profile in profiles)
            {
                if (profile?.escrow == null || profile.escrow.Count == 0) continue;
                foreach (var entry in profile.escrow.ToList())
                {
                    int amount = entry.Value;
                    string currency = WagerEscrow.CurrencyOf(entry.Key);
                    // Always clear the escrow slot; only credit when there's a positive amount in a
                    // parseable currency.
                    database.ChangeEscrow(profile.userName, entry.Key, -amount);
                    if (amount > 0 && !string.IsNullOrEmpty(currency))
                    {
                        database.ChangeCurrency(profile.userName, currency, amount);
                        refunded++;
                    }
                }
            }
            return refunded;
        }

        /// <summary>
        /// Remove (zero out) every escrow entry this player holds for <paramref name="gameKey"/>
        /// and return the per-currency amounts that were held. Does NOT credit anyone — callers
        /// decide where the drained stake goes (winner vs. original staker).
        /// </summary>
        private Dictionary<string, int> DrainGameEscrow(string player, string gameKey)
        {
            var drained = new Dictionary<string, int>();
            Profile profile = database.GetProfile(player);
            if (profile?.escrow == null) return drained;

            foreach (var entry in profile.escrow.ToList())
            {
                if (!WagerEscrow.TryGetCurrency(entry.Key, gameKey, out string currency)) continue;
                int amount = entry.Value;
                if (amount == 0) continue;

                database.ChangeEscrow(player, entry.Key, -amount);
                drained[currency] = (drained.TryGetValue(currency, out int sum) ? sum : 0) + amount;
            }
            return drained;
        }
    }
}
