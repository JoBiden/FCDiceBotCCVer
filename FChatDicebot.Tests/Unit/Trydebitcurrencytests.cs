using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for the atomic guarded-debit primitive (FIX_SPEC.md Phase 1a) and the
    /// targeted milkInventory write it accompanies (Phase 1e). These are the choke-point
    /// tests: everything that routes currency through TryDebitCurrency inherits the
    /// zero-floor and no-mint guarantees asserted here.
    /// </summary>
    [Collection("Database")]
    public class TryDebitCurrencyTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public TryDebitCurrencyTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        public void Dispose()
        {
            _fixture.Reset();
        }

        [Fact]
        public void TryDebitCurrency_SufficientBalance_DebitsAndReturnsTrue()
        {
            var profile = new ProfileBuilder().WithCurrency("gold", 100).BuildAndSave(_database);

            bool result = _database.TryDebitCurrency(profile.userName, "gold", 30, allowNegative: false);

            Assert.True(result);
            Assert.Equal(70, _database.GetProfile(profile.userName).currencies["gold"]);
        }

        [Fact]
        public void TryDebitCurrency_InsufficientBalance_DoesNotFloorNegativeAndReturnsFalse()
        {
            var profile = new ProfileBuilder().WithCurrency("gold", 10).BuildAndSave(_database);

            bool result = _database.TryDebitCurrency(profile.userName, "gold", 30, allowNegative: false);

            Assert.False(result);
            Assert.Equal(10, _database.GetProfile(profile.userName).currencies["gold"]);
        }

        [Fact]
        public void TryDebitCurrency_MissingCurrencyField_FailsRatherThanGoingNegative()
        {
            // No WithCurrency call: the "gold" key is entirely absent from the document,
            // not merely zero. $gte must not match a missing field.
            var profile = new ProfileBuilder().BuildAndSave(_database);

            bool result = _database.TryDebitCurrency(profile.userName, "gold", 1, allowNegative: false);

            Assert.False(result);
            Assert.False(_database.GetProfile(profile.userName).currencies.ContainsKey("gold"));
        }

        [Fact]
        public void TryDebitCurrency_IousAllowNegative_DebitsBelowZero()
        {
            var profile = new ProfileBuilder().WithCurrency(CurrencyRules.IouCurrency, 0).BuildAndSave(_database);

            bool result = _database.TryDebitCurrency(profile.userName, CurrencyRules.IouCurrency, 50, allowNegative: true);

            Assert.True(result);
            Assert.Equal(-50, _database.GetProfile(profile.userName).currencies[CurrencyRules.IouCurrency]);
        }

        [Fact]
        public void TryDebitCurrency_ZeroOrNegativeAmount_IsRejected()
        {
            var profile = new ProfileBuilder().WithCurrency("gold", 100).BuildAndSave(_database);

            Assert.False(_database.TryDebitCurrency(profile.userName, "gold", 0, allowNegative: false));
            Assert.False(_database.TryDebitCurrency(profile.userName, "gold", -5, allowNegative: false));
            Assert.Equal(100, _database.GetProfile(profile.userName).currencies["gold"]);
        }

        [Fact]
        public void CurrencyRules_AllowsNegative_OnlyForIousAndNothing()
        {
            Assert.True(CurrencyRules.AllowsNegative("ious"));
            Assert.True(CurrencyRules.AllowsNegative("nothing"));
            Assert.False(CurrencyRules.AllowsNegative("gold"));
            Assert.False(CurrencyRules.AllowsNegative("copper"));
        }

        [Fact]
        public void SetMilkInventory_DoesNotRevertConcurrentCurrencyChange()
        {
            // Regression test for M6: a whole-profile SetProfile call sitting between a
            // command's initial GetProfile and its final persistence used to revert any
            // atomic $inc that landed on this profile in between. SetMilkInventory re-fetches
            // fresh at call time instead, so it must not revert the concurrent credit below.
            var profile = new ProfileBuilder().WithCurrency("gold", 10).BuildAndSave(_database);

            // Simulates another command's atomic credit landing after this command loaded
            // its own profile snapshot but before it persists its own (unrelated) change.
            _database.ChangeCurrency(profile.userName, "gold", 5);

            _database.SetMilkInventory(profile.userName, new System.Collections.Generic.List<MilkBottle>
            {
                new MilkBottle { substance = "milk", quantity = 1, sourceName = "Someone", milkedAt = DateTime.UtcNow }
            });

            var reloaded = _database.GetProfile(profile.userName);
            Assert.Equal(15, reloaded.currencies["gold"]);
            Assert.Single(reloaded.milkInventory);
        }
    }
}
