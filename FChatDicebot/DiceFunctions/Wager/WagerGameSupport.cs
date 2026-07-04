using System.Collections.Generic;
using System.Linq;
using FChatDicebot;
using FChatDicebot.Model;

namespace FChatDicebot.DiceFunctions.Wager
{
    /// <summary>
    /// The whole game-side wager flow for currency-wagered games, kept in one place so the
    /// player-facing strings and the open → match/propose → accept/decline → commit → award
    /// lifecycle live together. The Chateau consent rail (PendingCommand/!consent) is deliberately
    /// NOT used: a wager has to be able to auto-start the game, which only the DiceBot layer can
    /// do, so the proposal lives on the in-memory GameSession and !accept / !no are bridged here.
    /// </summary>
    public static class WagerGameSupport
    {
        /// <summary>Games converted to the global Chateau wallet. Extend as later phases land.</summary>
        public static bool IsCurrencyWager(IGame game)
        {
            return game is SlamRoll          // 1v1 handshake
                || game is Chess             // 1v1 handshake (vs-bot is friendly; draws refund both)
                || game is RockPaperScissors // multiplayer winner-take-all
                || game is LiarsDice         // multiplayer winner-take-all
                || game is HighRoll          // multiplayer split pot
                || game is AlphaRoyale;      // multiplayer split pot
        }

        /// <summary>Odds-vs-house games (place a bet in a currency, the house pays a multiple).</summary>
        public static bool IsOddsGame(IGame game)
        {
            return game is Roulette;
        }

        /// <summary>
        /// Join/bet flow for odds games: validate the player can afford their named-currency bet,
        /// then let the game record the bet (it parses bet type and stamps the currency itself).
        /// The stake isn't committed here — odds games burn/mint at spin time. Bypasses the legacy
        /// chip-ante affordability path entirely.
        /// </summary>
        public static string HandleOddsJoin(BotMain bot, BotCommandController cc, MessageAddress address,
            GameSession sesh, IGame gametype, string[] terms, string[] rawTerms)
        {
            string player = address.character;
            if (sesh.State == GameState.GameInProgress)
                return gametype.GetGameName() + " has already been spun.";
            if (!sesh.Players.Contains(player) && sesh.Players.Count >= gametype.GetMaxPlayers())
                return gametype.GetGameName() + " is already full.";

            int amount = Utils.GetNumberFromInputs(terms);
            string currency = cc.GetIdentifierFromCommandTerms(rawTerms, "currency");
            if (string.IsNullOrEmpty(currency))
                return "Name a currency for your bet, e.g. !joingame " + gametype.GetGameName() + " 10 copper red.";
            if (amount <= 0)
                return "Name a bet amount, e.g. !joingame " + gametype.GetGameName() + " 10 " + currency + " red.";
            if (bot.DiceBot.WagerBank.BalanceOf(player, currency) < amount)
                return CannotAfford(player, new WagerStakeEntry(currency, amount));

            string betMsg;
            bool ok = gametype.AddGameDataForPlayerJoin(player, sesh, bot, terms, amount, out betMsg);
            if (!ok)
                return string.IsNullOrEmpty(betMsg) ? "That bet couldn't be placed." : betMsg;

            if (!sesh.Players.Contains(player))
                bot.DiceBot.JoinGame(address, gametype);

            return TextFormat.GetCharacterUserTags(player) + " places a bet of " + amount + " " + currency
                + ". [sub]!startgame " + gametype.GetGameName() + " to spin.[/sub]";
        }

        // ---- Join: open / match / propose -------------------------------------------------

        /// <summary>
        /// Handles a join for a currency-wager game (called instead of the legacy chip-ante path).
        /// First player opens with a stake (or a friendly no-stake game); the second either matches
        /// the opener's stake (instant, auto-starts) or proposes a different one (awaits !accept).
        /// Returns the channel message.
        /// </summary>
        public static string HandleWagerJoin(BotMain bot, BotCommandController cc, MessageAddress address,
            GameSession sesh, IGame gametype, string[] terms, string[] rawTerms)
        {
            // Games that seat more than two players can't run the pairwise "do you accept?"
            // handshake — there's no single opponent to negotiate with. Everyone just antes their
            // own currency into the shared bag, and the winner(s) take it.
            if (gametype.GetMaxPlayers() > 2)
                return HandleMultiplayerWagerJoin(bot, cc, address, sesh, gametype, terms, rawTerms);

            string player = address.character;

            if (sesh.State == GameState.GameInProgress)
                return gametype.GetGameName() + " is already in progress.";
            if (sesh.Players.Contains(player))
                return TextFormat.GetCharacterUserTags(player) + " is already in " + gametype.GetGameName() + ".";
            if (sesh.Players.Count >= gametype.GetMaxPlayers())
                return TextFormat.GetCharacterUserTags(player) + " cannot join " + gametype.GetGameName() + " — it's already full.";
            if (sesh.PendingWager != null)
                return "There's already a wager awaiting " + TextFormat.GetCharacterUserTags(sesh.PendingWager.AwaitingAcceptFrom) + "'s !accept.";

            WagerStakeEntry declared = ParseStake(cc, terms, rawTerms);
            var bank = bot.DiceBot.WagerBank;

            // ---- First player: opens the table ----
            if (sesh.Players.Count == 0)
            {
                if (declared != null && bank.BalanceOf(player, declared.Currency) < declared.Amount)
                    return CannotAfford(player, declared);

                ApplyRules(gametype, player, sesh, bot, terms, declared);
                bot.DiceBot.JoinGame(address, gametype);

                if (declared == null)
                    return TextFormat.GetCharacterUserTags(player) + " sets up a friendly game of " + gametype.GetGameName()
                        + " (no stakes). !joingame " + gametype.GetGameName() + " to join.";

                sesh.WagerStakes[player] = declared;
                return TextFormat.GetCharacterUserTags(player) + " sets up a game of " + gametype.GetGameName()
                    + " and stakes " + declared.Display() + ". !joingame " + gametype.GetGameName()
                    + " to match the stakes, or !joingame " + gametype.GetGameName()
                    + " [amount] [currency] to try and stake something different.";
            }

            // ---- Second player: match or propose ----
            string opener = sesh.Players[0];
            WagerStakeEntry openerStake = sesh.WagerStakes.ContainsKey(opener) ? sesh.WagerStakes[opener] : null;

            // Friendly (no-stakes) opener: just join and start, ignoring any stake terms.
            if (openerStake == null)
            {
                ApplyRules(gametype, player, sesh, bot, terms, null);
                bot.DiceBot.JoinGame(address, gametype);
                string joinMsg = TextFormat.GetCharacterUserTags(player) + " joins the friendly game.";
                return joinMsg + "\n" + bot.DiceBot.StartGame(address, player, gametype, bot, false, false);
            }

            // No stake given => match the opener's stake exactly.
            WagerStakeEntry myStake = declared ?? new WagerStakeEntry(openerStake.Currency, openerStake.Amount);
            if (bank.BalanceOf(player, myStake.Currency) < myStake.Amount)
                return CannotAfford(player, myStake);

            ApplyRules(gametype, player, sesh, bot, terms, myStake);
            bot.DiceBot.JoinGame(address, gametype);

            if (myStake.Matches(openerStake))
            {
                sesh.WagerStakes[player] = myStake;
                string matchMsg = TextFormat.GetCharacterUserTags(player) + " matches the stake with " + myStake.Display() + ".";
                return matchMsg + "\n" + bot.DiceBot.StartGame(address, player, gametype, bot, false, false);
            }

            // Differing stake => proposal awaiting the opener's acceptance.
            sesh.PendingWager = new WagerProposal
            {
                Proposer = player,
                AwaitingAcceptFrom = opener,
                Stake = myStake,
            };
            return TextFormat.GetCharacterUserTags(player) + " answers " + TextFormat.GetCharacterUserTags(opener) + "'s "
                + openerStake.Display() + " with " + myStake.Display() + " instead. "
                + TextFormat.GetCharacterUserTags(opener) + ", do you !accept their proposition?";
        }

        /// <summary>
        /// Join flow for 3+ player pot games: each player antes their own currency into the bag
        /// (no pairwise handshake). A player who names no stake matches the opener's. The table
        /// waits for !startgame. A no-stake opener makes it a friendly (no-money) table.
        /// </summary>
        private static string HandleMultiplayerWagerJoin(BotMain bot, BotCommandController cc, MessageAddress address,
            GameSession sesh, IGame gametype, string[] terms, string[] rawTerms)
        {
            string player = address.character;

            if (sesh.State == GameState.GameInProgress)
                return gametype.GetGameName() + " is already in progress.";
            if (sesh.Players.Contains(player))
                return TextFormat.GetCharacterUserTags(player) + " is already in " + gametype.GetGameName() + ".";
            if (sesh.Players.Count >= gametype.GetMaxPlayers())
                return TextFormat.GetCharacterUserTags(player) + " cannot join " + gametype.GetGameName() + " — it's already full.";

            WagerStakeEntry declared = ParseStake(cc, terms, rawTerms);
            var bank = bot.DiceBot.WagerBank;
            WagerStakeEntry openerStake = sesh.Players.Count > 0 && sesh.WagerStakes.ContainsKey(sesh.Players[0])
                ? sesh.WagerStakes[sesh.Players[0]] : null;
            bool friendlyTable = sesh.Players.Count > 0 && openerStake == null;

            // Opener with no stake => friendly table; players on a friendly table just join.
            if (friendlyTable || (sesh.Players.Count == 0 && declared == null))
            {
                ApplyRules(gametype, player, sesh, bot, terms, null);
                bot.DiceBot.JoinGame(address, gametype);
                string verb = sesh.Players.Count == 1 ? "sets up a friendly game of" : "joins the friendly game of";
                return TextFormat.GetCharacterUserTags(player) + " " + verb + " " + gametype.GetGameName() + ". " + ReadyLine(sesh, gametype);
            }

            // Wager table: each player stakes their own (defaulting to the opener's stake).
            WagerStakeEntry myStake = declared ?? new WagerStakeEntry(openerStake.Currency, openerStake.Amount);
            if (bank.BalanceOf(player, myStake.Currency) < myStake.Amount)
                return CannotAfford(player, myStake);

            ApplyRules(gametype, player, sesh, bot, terms, myStake);
            bot.DiceBot.JoinGame(address, gametype);
            sesh.WagerStakes[player] = myStake;

            string action = sesh.Players.Count == 1
                ? TextFormat.GetCharacterUserTags(player) + " opens a game of " + gametype.GetGameName() + " and stakes " + myStake.Display() + "."
                : TextFormat.GetCharacterUserTags(player) + " antes " + myStake.Display() + " into the pot.";
            return action + " " + ReadyLine(sesh, gametype);
        }

        private static string ReadyLine(GameSession sesh, IGame gametype)
        {
            string counts = sesh.Players.Count + " player" + (sesh.Players.Count == 1 ? "" : "s")
                + " ready. [i](min " + gametype.GetMinPlayers() + ", max " + gametype.GetMaxPlayers() + ")[/i]";
            if (sesh.Players.Count >= gametype.GetMinPlayers())
                counts += " !startgame " + gametype.GetGameName() + " when ready.";
            return counts;
        }

        // ---- Accept / decline a cross-currency proposal -----------------------------------

        /// <summary>
        /// If the player has a wager proposal awaiting their acceptance in this channel, accept it
        /// (lock the stake in, auto-start) and return the message. Returns null when there's nothing
        /// to accept, so the caller can fall through to the normal !consent flow.
        /// </summary>
        public static string TryAcceptWager(BotMain bot, MessageAddress address)
        {
            GameSession sesh = FindPendingFor(bot, address);
            if (sesh == null) return null;

            WagerProposal prop = sesh.PendingWager;
            if (bot.DiceBot.WagerBank.BalanceOf(prop.Proposer, prop.Stake.Currency) < prop.Stake.Amount)
            {
                sesh.PendingWager = null;
                bot.DiceBot.LeaveGame(new MessageAddress(address, prop.Proposer), sesh.CurrentGame);
                return TextFormat.GetCharacterUserTags(prop.Proposer) + " can no longer cover their stake of "
                    + prop.Stake.Display() + ". The proposal is withdrawn.";
            }

            sesh.WagerStakes[prop.Proposer] = prop.Stake;
            sesh.PendingWager = null;
            string acceptMsg = TextFormat.GetCharacterUserTags(address.character) + " accepts the wager.";
            return acceptMsg + "\n" + bot.DiceBot.StartGame(address, address.character, sesh.CurrentGame, bot, false, false);
        }

        /// <summary>
        /// If the player has a wager proposal awaiting them, decline it (drop the proposer's seat,
        /// leaving the table open). Returns null when there's nothing to decline.
        /// </summary>
        public static string TryDeclineWager(BotMain bot, MessageAddress address)
        {
            GameSession sesh = FindPendingFor(bot, address);
            if (sesh == null) return null;

            WagerProposal prop = sesh.PendingWager;
            sesh.PendingWager = null;
            bot.DiceBot.LeaveGame(new MessageAddress(address, prop.Proposer), sesh.CurrentGame);
            return TextFormat.GetCharacterUserTags(address.character) + " turns down the proposal of "
                + prop.Stake.Display() + " from " + TextFormat.GetCharacterUserTags(prop.Proposer) + ".";
        }

        // ---- Commit / award / refund (called from the game) -------------------------------

        /// <summary>
        /// Debit every recorded stake into escrow at game start. If any player can no longer cover
        /// their stake, refunds the ones already taken and returns false with an explanation.
        /// </summary>
        public static bool CommitAllStakes(DiceBot diceBot, GameSession sesh, out string error)
        {
            error = "";
            string gameKey = sesh.GetWagerGameKey();
            var committed = new List<string>();
            foreach (var entry in sesh.WagerStakes)
            {
                if (diceBot.WagerBank.Commit(entry.Key, gameKey, entry.Value.Currency, entry.Value.Amount))
                {
                    committed.Add(entry.Key);
                    continue;
                }
                foreach (string p in committed)
                    diceBot.WagerBank.RefundAllForGame(p, gameKey);
                error = TextFormat.GetCharacterUserTags(entry.Key) + " can no longer cover their stake of " + entry.Value.Display() + ".";
                return false;
            }
            return true;
        }

        /// <summary>Hand the whole bag to the winner; returns the "takes the pot" line (empty if no stakes).</summary>
        public static string AwardPotToWinner(DiceBot diceBot, GameSession sesh, string winner)
        {
            if (!sesh.HasWagerStakes) return "";
            var totals = diceBot.WagerBank.AwardPot(sesh.GetWagerGameKey(), sesh.Players, winner);
            sesh.WagerStakes.Clear();
            if (totals.Count == 0) return "";
            return TextFormat.GetCharacterUserTags(winner) + " takes the pot! All " + DisplayBag(totals) + ".";
        }

        /// <summary>
        /// Award the bag to several finishers by percentage, applied INDEPENDENTLY to each currency
        /// column (the design never converts between currencies). Place 0 gets its share plus any
        /// rounding dust. Returns each winner's awarded bag rendered for messaging (e.g. "7 copper
        /// and 5 rosequartz"); winners awarded nothing are omitted.
        /// </summary>
        public static Dictionary<string, string> AwardPotSplit(DiceBot diceBot, GameSession sesh,
            IList<string> winnersInOrder, IList<int> percents)
        {
            var rendered = new Dictionary<string, string>();
            if (!sesh.HasWagerStakes || winnersInOrder == null || winnersInOrder.Count == 0) return rendered;

            var totals = diceBot.WagerBank.CollectPot(sesh.GetWagerGameKey(), sesh.Players);
            sesh.WagerStakes.Clear();

            var awards = ComputePotSplit(totals, winnersInOrder, percents);
            foreach (var winnerBag in awards)
            {
                foreach (var entry in winnerBag.Value)
                    diceBot.WagerBank.PayWinner(winnerBag.Key, entry.Key, entry.Value);
                rendered[winnerBag.Key] = DisplayBag(winnerBag.Value);
            }
            return rendered;
        }

        /// <summary>
        /// Pure split math (no DB): distribute each currency column among the ordered winners by
        /// <paramref name="percents"/>, giving place 0 its share plus rounding dust. Extracted so
        /// the rounding/per-column behavior is unit-testable without a live DiceBot. Returns
        /// winner -> currency -> amount, omitting zero awards.
        /// </summary>
        public static Dictionary<string, Dictionary<string, int>> ComputePotSplit(
            Dictionary<string, int> totals, IList<string> winnersInOrder, IList<int> percents)
        {
            var awards = new Dictionary<string, Dictionary<string, int>>();
            if (totals == null || winnersInOrder == null || winnersInOrder.Count == 0) return awards;
            int splitCount = percents == null ? 0 : System.Math.Min(winnersInOrder.Count, percents.Count);

            foreach (var col in totals)
            {
                string currency = col.Key;
                int total = col.Value;
                if (total <= 0) continue;

                if (splitCount <= 1)
                {
                    // No real split defined: the whole column goes to first place.
                    AddAward(awards, winnersInOrder[0], currency, total);
                    continue;
                }

                int givenToRunnersUp = 0;
                for (int i = 1; i < splitCount; i++)
                {
                    int share = (int)System.Math.Floor(total * (double)percents[i] / 100);
                    if (share <= 0) continue;
                    AddAward(awards, winnersInOrder[i], currency, share);
                    givenToRunnersUp += share;
                }
                int firstShare = total - givenToRunnersUp; // first place takes its share + the dust
                if (firstShare > 0) AddAward(awards, winnersInOrder[0], currency, firstShare);
            }
            return awards;
        }

        private static void AddAward(Dictionary<string, Dictionary<string, int>> awards, string winner, string currency, int amount)
        {
            if (amount <= 0) return;
            if (!awards.ContainsKey(winner)) awards[winner] = new Dictionary<string, int>();
            awards[winner][currency] = (awards[winner].TryGetValue(currency, out int v) ? v : 0) + amount;
        }

        /// <summary>Return all committed stakes to whoever staked them, and clear wager state.</summary>
        public static void RefundAll(DiceBot diceBot, GameSession sesh)
        {
            if (sesh == null) return;
            diceBot.WagerBank.RefundPot(sesh.GetWagerGameKey(), sesh.Players);
            if (sesh.WagerStakes != null) sesh.WagerStakes.Clear();
            sesh.PendingWager = null;
        }

        // ---- helpers ----------------------------------------------------------------------

        private static GameSession FindPendingFor(BotMain bot, MessageAddress address)
        {
            string channelKey = address.GetChannelKey();
            return bot.DiceBot.GameSessions.FirstOrDefault(s =>
                s != null && s.PendingWager != null
                && s.GetChannelKey() == channelKey
                && string.Equals(s.PendingWager.AwaitingAcceptFrom, address.character, System.StringComparison.OrdinalIgnoreCase));
        }

        private static WagerStakeEntry ParseStake(BotCommandController cc, string[] terms, string[] rawTerms)
        {
            int amount = Utils.GetNumberFromInputs(terms);
            string currency = cc.GetIdentifierFromCommandTerms(rawTerms, "currency");
            return (amount > 0 && !string.IsNullOrEmpty(currency)) ? new WagerStakeEntry(currency, amount) : null;
        }

        private static void ApplyRules(IGame gametype, string player, GameSession sesh, BotMain bot, string[] terms, WagerStakeEntry stake)
        {
            // Let the game capture its own rules/options from the join terms (lives, growingtwos…).
            // The ante int is only used by legacy chip games; pass the stake amount for parity.
            string ignored;
            gametype.AddGameDataForPlayerJoin(player, sesh, bot, terms, stake != null ? stake.Amount : 0, out ignored);
        }

        private static string CannotAfford(string player, WagerStakeEntry stake)
        {
            return TextFormat.GetCharacterUserTags(player) + " can't cover a stake of " + stake.Display() + ".";
        }

        internal static string DisplayBag(Dictionary<string, int> totals)
        {
            var parts = totals.OrderBy(kv => kv.Key)
                .Select(kv => kv.Value + " " + kv.Key)
                .ToList();
            if (parts.Count == 0) return string.Empty;
            if (parts.Count == 1) return parts[0];
            if (parts.Count == 2) return parts[0] + " and " + parts[1];
            return string.Join(", ", parts.Take(parts.Count - 1)) + ", and " + parts[parts.Count - 1];
        }
    }
}
