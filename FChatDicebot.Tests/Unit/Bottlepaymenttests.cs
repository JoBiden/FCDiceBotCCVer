using FChatDicebot;
using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for bottle transfer through !pay.
    ///
    /// The property worth defending here is exactness across the consent gap. A currency payment
    /// can settle with an atomic debit; a bottle payment can't, so it promises specific serial
    /// numbers and refuses to settle for anything else — including the same bottle after it has
    /// been drunk.
    /// </summary>
    [Collection("Database")]
    public class BottlePaymentTests
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public BottlePaymentTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").BuildAndSave(_database);
        }

        // -------------------------------------------------------------------
        // Parsing
        // -------------------------------------------------------------------

        [Theory]
        [InlineData(new[] { "bottles" }, true)]
        [InlineData(new[] { "bottle" }, true)]
        [InlineData(new[] { "BOTTLES" }, true)]
        [InlineData(new[] { "gold" }, false)]
        [InlineData(new string[0], false)]
        public void MentionsBottles_RecognisesTheKeyword(string[] terms, bool expected)
        {
            Assert.Equal(expected, BottlePayment.MentionsBottles(terms));
        }

        [Fact]
        public void ParseSerials_TakesHashPrefixedNumbersInOrderWithoutDuplicates()
        {
            var serials = BottlePayment.ParseSerials(new[] { "bottles", "#12", "#7", "#12", "3" });

            Assert.Equal(new[] { 12, 7 }, serials.ToArray());
        }

        // -------------------------------------------------------------------
        // Selection
        // -------------------------------------------------------------------

        [Fact]
        public void Select_AmountForm_TakesNewestFullBottlesOnly()
        {
            var empty = Bottle(3, "cum", "Carol", hour: 9);
            empty.emptiedAt = DateTime.UtcNow;
            var payer = new ProfileBuilder()
                .WithMilkBottle(Bottle(1, "cum", "Carol", hour: 1))
                .WithMilkBottle(Bottle(2, "cum", "Carol", hour: 5))
                .WithMilkBottle(empty)
                .Build();

            var selection = BottlePayment.Select(payer, new List<int>(), null, 2);

            Assert.True(selection.IsValid);
            // Newest-first among the full bottles; the newer empty is never swept in.
            Assert.Equal(new[] { 2, 1 }, selection.Bottles.Select(b => b.serial).ToArray());
        }

        [Fact]
        public void Select_AmountForm_ShortCollectionFails()
        {
            var payer = new ProfileBuilder()
                .WithMilkBottle(Bottle(1, "cum", "Carol", hour: 1))
                .Build();

            var selection = BottlePayment.Select(payer, new List<int>(), null, 3);

            Assert.False(selection.IsValid);
            Assert.Equal(BottlePayment.NotEnoughText, selection.PayerFacingError);
        }

        [Fact]
        public void Select_NamedSerial_CanMoveAnEmpty()
        {
            // A numbered empty is a keepsake worth handing over; naming it is the friction.
            var empty = Bottle(3, "cum", "Carol", hour: 1);
            empty.emptiedAt = DateTime.UtcNow;
            var payer = new ProfileBuilder().WithMilkBottle(empty).Build();

            var selection = BottlePayment.Select(payer, new List<int> { 3 }, null, 1);

            Assert.True(selection.IsValid);
            Assert.True(selection.Bottles[0].IsEmpty);
        }

        [Fact]
        public void Select_NamedSerial_NotHeldFails()
        {
            var payer = new ProfileBuilder()
                .WithMilkBottle(Bottle(1, "cum", "Carol", hour: 1))
                .Build();

            var selection = BottlePayment.Select(payer, new List<int> { 99 }, null, 1);

            Assert.False(selection.IsValid);
            Assert.Contains("#99", selection.PayerFacingError);
        }

        [Fact]
        public void Select_SubstanceFilterNarrowsTheAmountForm()
        {
            var payer = new ProfileBuilder()
                .WithMilkBottle(Bottle(1, "milk", "Carol", hour: 5))
                .WithMilkBottle(Bottle(2, "cum", "Carol", hour: 1))
                .Build();

            var selection = BottlePayment.Select(payer, new List<int>(), "cum", 1);

            Assert.True(selection.IsValid);
            Assert.Equal(2, selection.Bottles[0].serial);
        }

        // -------------------------------------------------------------------
        // Transfer and the consent gap
        // -------------------------------------------------------------------

        [Fact]
        public void TryTransfer_MovesBottlesIntactBetweenCollections()
        {
            SeedPair(Bottle(5, "cum", "Carol", hour: 1, tag: ChateauCurrency.CorruptTag));

            bool moved = BottlePayment.TryTransfer(_database, "Alice", "Bob", Promise(5, false), out string failure);

            Assert.True(moved, failure);
            Assert.Empty(_database.GetProfile("Alice").milkInventory);
            var received = Assert.Single(_database.GetProfile("Bob").milkInventory);
            Assert.Equal(5, received.serial);
            Assert.Equal("Carol", received.sourceName);
            Assert.Equal(ChateauCurrency.CorruptTag, received.corruptionTag);
        }

        [Fact]
        public void TryTransfer_BottleSoldDuringConsentGap_Aborts()
        {
            SeedPair(Bottle(5, "cum", "Carol", hour: 1));
            // Alice sells it out from under the promise.
            _database.SetMilkInventory("Alice", new List<MilkBottle>());

            bool moved = BottlePayment.TryTransfer(_database, "Alice", "Bob", Promise(5, false), out string failure);

            Assert.False(moved);
            Assert.Contains("nothing changed hands", failure);
            Assert.Empty(_database.GetProfile("Bob").milkInventory);
        }

        [Fact]
        public void TryTransfer_BottleDrunkDuringConsentGap_AbortsRatherThanDeliveringAnEmpty()
        {
            // The bottle is still there under the same number, but the recipient consented to a
            // full one. That's a mismatch, not a substitution.
            SeedPair(Bottle(5, "cum", "Carol", hour: 1));
            var alice = _database.GetProfile("Alice");
            alice.milkInventory[0].emptiedAt = DateTime.UtcNow;
            _database.SetMilkInventory("Alice", alice.milkInventory);

            bool moved = BottlePayment.TryTransfer(_database, "Alice", "Bob", Promise(5, false), out string failure);

            Assert.False(moved);
            Assert.Contains("nothing changed hands", failure);
            Assert.Empty(_database.GetProfile("Bob").milkInventory);
            Assert.Single(_database.GetProfile("Alice").milkInventory);
        }

        [Fact]
        public void TryTransfer_PartialMismatch_MovesNothingAtAll()
        {
            SeedPair(Bottle(5, "cum", "Carol", hour: 1), Bottle(6, "cum", "Carol", hour: 2));
            var alice = _database.GetProfile("Alice");
            alice.milkInventory.RemoveAll(b => b.serial == 6);
            _database.SetMilkInventory("Alice", alice.milkInventory);

            var promises = new List<KeyValuePair<int, bool>>
            {
                new KeyValuePair<int, bool>(5, false),
                new KeyValuePair<int, bool>(6, false),
            };
            bool moved = BottlePayment.TryTransfer(_database, "Alice", "Bob", promises, out _);

            Assert.False(moved);
            // #5 was still available, but a partial delivery isn't what anyone agreed to.
            Assert.Empty(_database.GetProfile("Bob").milkInventory);
            Assert.Single(_database.GetProfile("Alice").milkInventory);
        }

        [Fact]
        public void EncodePromise_CarriesEmptyStateAlongsideTheSerial()
        {
            var full = Bottle(9, "cum", "Carol", hour: 1);
            var empty = Bottle(9, "cum", "Carol", hour: 1);
            empty.emptiedAt = DateTime.UtcNow;

            Assert.Equal(9, BottlePayment.EncodePromise(full));
            Assert.Equal(-9, BottlePayment.EncodePromise(empty));
        }

        // -------------------------------------------------------------------
        // Wording
        // -------------------------------------------------------------------

        [Fact]
        public void Describe_NamesCountSubstanceDonorAndTag()
        {
            var bottles = new List<MilkBottle>
            {
                Bottle(1, "cum", "Carol", hour: 2, tag: ChateauCurrency.CorruptTag),
                Bottle(2, "cum", "Carol", hour: 1, tag: ChateauCurrency.CorruptTag),
            };

            string described = BottlePayment.Describe(_database, bottles);

            Assert.Contains("[b]2 bottles[/b]", described);
            Assert.Contains("Carol", described);
            Assert.Contains("[b]corrupt[/b]", described);
            Assert.DoesNotContain("emptied", described);
        }

        [Fact]
        public void Describe_FlagsEmptiesSoNobodyConsentsToOneUnknowingly()
        {
            var empty = Bottle(1, "cum", "Carol", hour: 2);
            empty.emptiedAt = DateTime.UtcNow;

            Assert.Contains("(already emptied)",
                BottlePayment.Describe(_database, new List<MilkBottle> { empty }));

            // A mixed parcel says exactly how many rather than implying either.
            var mixed = new List<MilkBottle> { empty, Bottle(2, "cum", "Carol", hour: 1) };
            Assert.Contains("1 of them already emptied", BottlePayment.Describe(_database, mixed));
        }

        [Fact]
        public void DescribesBottles_DistinguishesBottleSummariesFromCurrencyAmounts()
        {
            var bottles = new List<MilkBottle> { Bottle(1, "cum", "Carol", hour: 1) };

            Assert.True(BottlePayment.DescribesBottles(BottlePayment.Describe(_database, bottles)));
            // The currency path passes bare "{amount} {currency}" with no markup at all.
            Assert.False(BottlePayment.DescribesBottles("100 gold"));
            Assert.False(BottlePayment.DescribesBottles(null));
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private void SeedPair(params MilkBottle[] aliceBottles)
        {
            var builder = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice");
            foreach (var bottle in aliceBottles) builder.WithMilkBottle(bottle);
            builder.BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
        }

        private static List<KeyValuePair<int, bool>> Promise(int serial, bool wasEmpty)
        {
            return new List<KeyValuePair<int, bool>> { new KeyValuePair<int, bool>(serial, wasEmpty) };
        }

        private static MilkBottle Bottle(int serial, string substance, string source, int hour, string tag = null)
        {
            return BottleInventoryTests.NewBottle(serial, substance, source, hour, tag);
        }
    }
}
