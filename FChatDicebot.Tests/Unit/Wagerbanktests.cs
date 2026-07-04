using FChatDicebot.Database;
using FChatDicebot.DiceFunctions.Wager;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    [Collection("Database")]
    public class WagerBankTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly ChateauWagerBank _bank;

        private const string Game = "chan1";

        public WagerBankTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _bank = new ChateauWagerBank(_database);
        }

        private void Seed(string name, string currency, int amount)
        {
            new ProfileBuilder()
                .WithUserName(name)
                .WithDisplayName(name)
                .WithCurrency(currency, amount)
                .BuildAndSave(_database);
        }

        [Fact]
        public void BalanceOf_ReturnsHeldAmount_AndZeroForMissing()
        {
            Seed("Alice", "copper", 40);
            Assert.Equal(40, _bank.BalanceOf("Alice", "copper"));
            Assert.Equal(0, _bank.BalanceOf("Alice", "gold"));
        }

        [Fact]
        public void Commit_DebitsWallet_AndCreditsEscrow()
        {
            Seed("Alice", "copper", 40);

            bool ok = _bank.Commit("Alice", Game, "copper", 30);

            Assert.True(ok);
            var profile = _database.GetProfile("Alice");
            Assert.Equal(10, profile.currencies["copper"]);
            Assert.Equal(30, profile.escrow[WagerEscrow.Key(Game, "copper")]);
        }

        [Fact]
        public void Commit_InsufficientFunds_MovesNothing_ReturnsFalse()
        {
            Seed("Alice", "copper", 5);

            bool ok = _bank.Commit("Alice", Game, "copper", 30);

            Assert.False(ok);
            var profile = _database.GetProfile("Alice");
            Assert.Equal(5, profile.currencies["copper"]);
            Assert.False(profile.escrow.ContainsKey(WagerEscrow.Key(Game, "copper")));
        }

        [Fact]
        public void Refund_ReturnsStakeFromEscrowToWallet()
        {
            Seed("Alice", "copper", 40);
            _bank.Commit("Alice", Game, "copper", 30);

            _bank.Refund("Alice", Game, "copper", 30);

            var profile = _database.GetProfile("Alice");
            Assert.Equal(40, profile.currencies["copper"]);
            Assert.Equal(0, profile.escrow[WagerEscrow.Key(Game, "copper")]);
        }

        [Fact]
        public void AwardPot_MixedCurrencies_WinnerSweepsTheWholeBag()
        {
            Seed("Alice", "copper", 40);
            Seed("Bob", "rosequartz", 50);
            _bank.Commit("Alice", Game, "copper", 30);
            _bank.Commit("Bob", Game, "rosequartz", 20);

            var totals = _bank.AwardPot(Game, new[] { "Alice", "Bob" }, "Alice");

            Assert.Equal(30, totals["copper"]);
            Assert.Equal(20, totals["rosequartz"]);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            // Alice gets her own 30 copper back plus Bob's 20 rosequartz.
            Assert.Equal(40, alice.currencies["copper"]);     // 40 - 30 committed + 30 awarded
            Assert.Equal(20, alice.currencies["rosequartz"]); // 0 + 20 awarded
            Assert.Equal(30, bob.currencies["rosequartz"]);   // 50 - 20 committed, nothing back
            // Escrow fully drained for both.
            Assert.Equal(0, alice.escrow[WagerEscrow.Key(Game, "copper")]);
            Assert.Equal(0, bob.escrow[WagerEscrow.Key(Game, "rosequartz")]);
        }

        [Fact]
        public void AwardPot_SameCurrency_WinnerGetsTheSum()
        {
            Seed("Alice", "copper", 40);
            Seed("Bob", "copper", 40);
            _bank.Commit("Alice", Game, "copper", 25);
            _bank.Commit("Bob", Game, "copper", 25);

            var totals = _bank.AwardPot(Game, new[] { "Alice", "Bob" }, "Bob");

            Assert.Equal(50, totals["copper"]);
            var bob = _database.GetProfile("Bob");
            Assert.Equal(65, bob.currencies["copper"]); // 40 - 25 committed + 50 won
        }

        [Fact]
        public void RefundPot_ReturnsEachPlayersStake()
        {
            Seed("Alice", "copper", 40);
            Seed("Bob", "rosequartz", 50);
            _bank.Commit("Alice", Game, "copper", 30);
            _bank.Commit("Bob", Game, "rosequartz", 20);

            _bank.RefundPot(Game, new[] { "Alice", "Bob" });

            Assert.Equal(40, _database.GetProfile("Alice").currencies["copper"]);
            Assert.Equal(50, _database.GetProfile("Bob").currencies["rosequartz"]);
        }

        [Fact]
        public void CollectPot_DrainsAllStakes_WithoutCreditingAnyone()
        {
            Seed("Alice", "copper", 40);
            Seed("Bob", "rosequartz", 50);
            _bank.Commit("Alice", Game, "copper", 30);
            _bank.Commit("Bob", Game, "rosequartz", 20);

            var totals = _bank.CollectPot(Game, new[] { "Alice", "Bob" });

            Assert.Equal(30, totals["copper"]);
            Assert.Equal(20, totals["rosequartz"]);
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            // Escrow drained, but nobody credited (the caller distributes).
            Assert.Equal(0, alice.escrow[WagerEscrow.Key(Game, "copper")]);
            Assert.Equal(10, alice.currencies["copper"]);
            Assert.Equal(0, bob.escrow[WagerEscrow.Key(Game, "rosequartz")]);
            Assert.Equal(30, bob.currencies["rosequartz"]);
        }

        [Fact]
        public void Drain_IsScopedToGameKey_OtherGamesUntouched()
        {
            Seed("Alice", "copper", 100);
            _bank.Commit("Alice", "gameA", "copper", 30);
            _bank.Commit("Alice", "gameB", "copper", 40);

            // Awarding gameA must not touch the gameB stake.
            _bank.AwardPot("gameA", new[] { "Alice" }, "Alice");

            var alice = _database.GetProfile("Alice");
            Assert.Equal(0, alice.escrow[WagerEscrow.Key("gameA", "copper")]);
            Assert.Equal(40, alice.escrow[WagerEscrow.Key("gameB", "copper")]);
        }

        [Fact]
        public void House_Loss_BurnsStake()
        {
            Seed("Alice", "copper", 40);

            int payout = WagerHouse.Resolve(_bank, "Alice", "copper", 10, 0); // multiplier 0 == loss

            Assert.Equal(0, payout);
            Assert.Equal(30, _database.GetProfile("Alice").currencies["copper"]); // stake gone
        }

        [Fact]
        public void House_Win_MintsStakeTimesMultiplier()
        {
            Seed("Alice", "copper", 40);

            int payout = WagerHouse.Resolve(_bank, "Alice", "copper", 10, 36); // single-number roulette = 36x

            Assert.Equal(360, payout);
            // 40 - 10 staked + 360 minted = 390 (net +350).
            Assert.Equal(390, _database.GetProfile("Alice").currencies["copper"]);
        }

        [Fact]
        public void ReconcileAllEscrow_RefundsEveryStrandedStake()
        {
            Seed("Alice", "copper", 40);
            Seed("Bob", "rosequartz", 50);
            _bank.Commit("Alice", Game, "copper", 30);
            _bank.Commit("Bob", "someOtherGame", "rosequartz", 20);

            int refunded = _bank.ReconcileAllEscrow();

            Assert.Equal(2, refunded);
            Assert.Equal(40, _database.GetProfile("Alice").currencies["copper"]);
            Assert.Equal(50, _database.GetProfile("Bob").currencies["rosequartz"]);
            Assert.Equal(0, _database.GetProfile("Alice").escrow[WagerEscrow.Key(Game, "copper")]);
        }

        [Fact]
        public void SlotsJackpot_PersistsPerMachine_AsPerCurrencyAmounts()
        {
            Assert.Empty(_database.GetSlotsJackpotAmounts("Fancy")); // unset

            _database.SetSlotsJackpotAmounts("Fancy", new System.Collections.Generic.Dictionary<string, int> { { "copper", 500 } });
            Assert.Equal(500, _database.GetSlotsJackpotAmounts("Fancy")["copper"]);

            // Updates in place, and other currencies on the same machine coexist in one document.
            _database.SetSlotsJackpotAmounts("Fancy", new System.Collections.Generic.Dictionary<string, int> { { "copper", 750 }, { "gold", 30 } });
            var amounts = _database.GetSlotsJackpotAmounts("Fancy");
            Assert.Equal(750, amounts["copper"]);
            Assert.Equal(30, amounts["gold"]);

            Assert.Empty(_database.GetSlotsJackpotAmounts("OtherMachine")); // other machine isolated
        }

        public void Dispose()
        {
        }
    }
}
