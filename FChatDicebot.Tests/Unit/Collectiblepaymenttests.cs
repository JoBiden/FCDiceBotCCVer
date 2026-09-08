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
    /// Tests for collectible transfer through !pay.
    ///
    /// The property worth defending is exactness across the consent gap. A currency payment can
    /// settle with an atomic debit; a goods payment can't, so it promises specific serial numbers
    /// and refuses to settle for anything else — including the same bottle after it has been drunk.
    ///
    /// The second property, added when this stopped being bottle-only: <b>the path is type-blind</b>.
    /// Panties are `IsTransferable` and were documented as `!pay`-able from the day they shipped,
    /// but every selection resolved through `BottleInventory`, so a panties serial answered
    /// "Bottle #43 isn't in your collection". The panties cases below are what keeps that from
    /// being true again.
    /// </summary>
    [Collection("Database")]
    public class CollectiblePaymentTests
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private static readonly CollectionSection Bottles = new BottleCollectionSection();
        private static readonly CollectionSection PantiesSection = new PantiesCollectionSection();

        public CollectiblePaymentTests(TestDatabaseFixture fixture)
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
        [InlineData(new[] { "bottles" }, "bottles")]
        [InlineData(new[] { "bottle" }, "bottles")]
        [InlineData(new[] { "BOTTLES" }, "bottles")]
        [InlineData(new[] { "panties" }, "panties")]
        [InlineData(new[] { "PANTIES" }, "panties")]
        [InlineData(new[] { "gold" }, null)]
        [InlineData(new string[0], null)]
        public void SectionFor_RecognisesEveryTypeKeyword(string[] terms, string expectedToken)
        {
            CollectionSection section = CollectiblePayment.SectionFor(terms);

            Assert.Equal(expectedToken, section?.FilterToken);
        }

        [Fact]
        public void ParseSerials_TakesHashPrefixedNumbersInOrderWithoutDuplicates()
        {
            var serials = CollectiblePayment.ParseSerials(new[] { "bottles", "#12", "#7", "#12", "3" });

            Assert.Equal(new[] { 12, 7 }, serials.ToArray());
        }

        // -------------------------------------------------------------------
        // Selection — bottles
        // -------------------------------------------------------------------

        [Fact]
        public void Select_AmountForm_TakesNewestFullBottlesOnly()
        {
            var payer = new ProfileBuilder()
                .WithMilkBottle(Bottle(1, "cum", "Carol", hour: 1))
                .WithMilkBottle(Bottle(2, "cum", "Carol", hour: 5))
                .WithMilkBottle(Emptied(Bottle(3, "cum", "Carol", hour: 9)))
                .Build();

            var selection = CollectiblePayment.Select(payer, Bottles, new List<int>(), null, 2);

            Assert.True(selection.IsValid);
            // Newest-first among the full bottles; the newer empty is never swept in.
            Assert.Equal(new[] { 2, 1 }, selection.Items.Select(b => b.serial).ToArray());
        }

        [Fact]
        public void Select_AmountForm_ShortCollectionFails()
        {
            var payer = new ProfileBuilder()
                .WithMilkBottle(Bottle(1, "cum", "Carol", hour: 1))
                .Build();

            var selection = CollectiblePayment.Select(payer, Bottles, new List<int>(), null, 3);

            Assert.False(selection.IsValid);
            Assert.Equal(CollectiblePayment.NotEnoughText(Bottles), selection.PayerFacingError);
        }

        [Fact]
        public void Select_NamedSerial_CanMoveAnEmpty()
        {
            // A numbered empty is a keepsake worth handing over; naming it is the friction.
            var payer = new ProfileBuilder()
                .WithMilkBottle(Emptied(Bottle(3, "cum", "Carol", hour: 1)))
                .Build();

            var selection = CollectiblePayment.Select(payer, Bottles, new List<int> { 3 }, null, 1);

            Assert.True(selection.IsValid);
            Assert.True(((MilkBottle)selection.Items[0]).IsEmpty);
        }

        [Fact]
        public void Select_NamedSerial_NotHeldFails()
        {
            var payer = new ProfileBuilder()
                .WithMilkBottle(Bottle(1, "cum", "Carol", hour: 1))
                .Build();

            var selection = CollectiblePayment.Select(payer, Bottles, new List<int> { 99 }, null, 1);

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

            var selection = CollectiblePayment.Select(payer, Bottles, new List<int>(), "cum", 1);

            Assert.True(selection.IsValid);
            Assert.Equal(2, selection.Items[0].serial);
        }

        // -------------------------------------------------------------------
        // Selection — panties, the type that could not be paid at all
        // -------------------------------------------------------------------

        [Fact]
        public void Select_NamedPantiesSerial_Resolves()
        {
            // The whole bug: this used to answer "Bottle #43 isn't in your collection".
            var payer = new ProfileBuilder()
                .WithCollectible(Pair(43, "Carol", hour: 1))
                .Build();

            var selection = CollectiblePayment.Select(payer, PantiesSection, new List<int> { 43 }, null, 1);

            Assert.True(selection.IsValid, selection.PayerFacingError);
            Assert.IsType<Panties>(selection.Items[0]);
            Assert.Equal(43, selection.Items[0].serial);
        }

        [Fact]
        public void Select_PantiesAmountForm_TakesNewestFirst()
        {
            var payer = new ProfileBuilder()
                .WithCollectible(Pair(43, "Carol", hour: 1))
                .WithCollectible(Pair(44, "Carol", hour: 5))
                .WithCollectible(Pair(45, "Carol", hour: 3))
                .Build();

            var selection = CollectiblePayment.Select(payer, PantiesSection, new List<int>(), null, 2);

            Assert.True(selection.IsValid);
            Assert.Equal(new[] { 44, 45 }, selection.Items.Select(p => p.serial).ToArray());
        }

        [Fact]
        public void Select_PantiesIgnoreASubstanceFilter()
        {
            // A substance is a bottle question. Panties have none, so a request carrying one
            // isn't asking for them — the same rule the !collection readout follows.
            var payer = new ProfileBuilder()
                .WithCollectible(Pair(43, "Carol", hour: 1))
                .Build();

            var selection = CollectiblePayment.Select(payer, PantiesSection, new List<int>(), "cum", 1);

            Assert.False(selection.IsValid);
        }

        [Fact]
        public void Select_SerialOfTheWrongType_PointsAtTheRightKeyword()
        {
            var payer = new ProfileBuilder()
                .WithCollectible(Pair(43, "Carol", hour: 1))
                .Build();

            var selection = CollectiblePayment.Select(payer, Bottles, new List<int> { 43 }, null, 1);

            Assert.False(selection.IsValid);
            // Holding #43 but calling it a bottle is a different mistake from not holding it,
            // and the remedy is a different word rather than a different number.
            Assert.Contains("#43", selection.PayerFacingError);
            Assert.Contains("panties", selection.PayerFacingError);
            Assert.DoesNotContain("isn't in your collection", selection.PayerFacingError);
        }

        [Fact]
        public void Select_MixedParcelIsRefusedRatherThanSplit()
        {
            // One type per payment: the identifier slot holds one token and the completion
            // message names the goods from it, so a mixed parcel would make both of them lie.
            var payer = new ProfileBuilder()
                .WithMilkBottle(Bottle(12, "cum", "Carol", hour: 1))
                .WithCollectible(Pair(43, "Carol", hour: 2))
                .Build();

            var selection = CollectiblePayment.Select(payer, Bottles, new List<int> { 12, 43 }, null, 2);

            Assert.False(selection.IsValid);
        }

        // -------------------------------------------------------------------
        // Transfer and the consent gap
        // -------------------------------------------------------------------

        [Fact]
        public void TryTransfer_MovesBottlesIntactBetweenCollections()
        {
            SeedPair(Bottle(5, "cum", "Carol", hour: 1, tag: ChateauCurrency.CorruptTag));

            bool moved = Transfer(Bottles, Promise(5, false), out string failure);

            Assert.True(moved, failure);
            Assert.Empty(_database.GetProfile("Alice").collectibles);
            var received = Assert.Single(_database.GetProfile("Bob").Bottles());
            Assert.Equal(5, received.serial);
            Assert.Equal("Carol", received.subjectName);
            Assert.Equal(ChateauCurrency.CorruptTag, received.corruptionTag);
        }

        [Fact]
        public void TryTransfer_MovesPantiesIntactBetweenCollections()
        {
            SeedPair();
            var alice = _database.GetProfile("Alice");
            alice.collectibles.Add(Pair(43, "Carol", hour: 1));
            _database.SetCollectibles("Alice", alice.collectibles);

            bool moved = Transfer(PantiesSection, Promise(43, false), out string failure);

            Assert.True(moved, failure);
            Assert.Empty(_database.GetProfile("Alice").collectibles);
            var received = Assert.Single(_database.GetProfile("Bob").collectibles);
            // Serial and subject survive the handoff: a pair Bob is holding still remembers it
            // was Carol's, not Alice's.
            Assert.IsType<Panties>(received);
            Assert.Equal(43, received.serial);
            Assert.Equal("Carol", received.subjectName);
        }

        [Fact]
        public void TryTransfer_BottleSoldDuringConsentGap_Aborts()
        {
            SeedPair(Bottle(5, "cum", "Carol", hour: 1));
            // Alice sells it out from under the promise.
            _database.SetCollectibles("Alice", new List<Collectible>());

            bool moved = Transfer(Bottles, Promise(5, false), out string failure);

            Assert.False(moved);
            Assert.Contains("nothing changed hands", failure);
            Assert.Empty(_database.GetProfile("Bob").collectibles);
        }

        [Fact]
        public void TryTransfer_GoneMessageDescribesTheTypeThatWentMissing()
        {
            // "sold or enjoyed" is a story about bottles. The Chateau won't buy panties and
            // nobody drinks them, so the type owns the wording.
            SeedPair(Bottle(5, "cum", "Carol", hour: 1));
            _database.SetCollectibles("Alice", new List<Collectible>());

            Transfer(Bottles, Promise(5, false), out string bottleFailure);
            Transfer(PantiesSection, Promise(43, false), out string pantiesFailure);

            Assert.Contains("those bottles", bottleFailure);
            Assert.Contains("sold or enjoyed", bottleFailure);
            Assert.Contains("those panties", pantiesFailure);
            Assert.DoesNotContain("sold or enjoyed", pantiesFailure);
        }

        [Fact]
        public void TryTransfer_BottleDrunkDuringConsentGap_AbortsRatherThanDeliveringAnEmpty()
        {
            // The bottle is still there under the same number, but the recipient consented to a
            // full one. That's a mismatch, not a substitution.
            SeedPair(Bottle(5, "cum", "Carol", hour: 1));
            var alice = _database.GetProfile("Alice");
            alice.Bottles()[0].emptiedAt = DateTime.UtcNow;
            _database.SetCollectibles("Alice", alice.collectibles);

            bool moved = Transfer(Bottles, Promise(5, false), out string failure);

            Assert.False(moved);
            Assert.Contains("nothing changed hands", failure);
            Assert.Empty(_database.GetProfile("Bob").collectibles);
            Assert.Single(_database.GetProfile("Alice").collectibles);
        }

        [Fact]
        public void TryTransfer_PartialMismatch_MovesNothingAtAll()
        {
            SeedPair(Bottle(5, "cum", "Carol", hour: 1), Bottle(6, "cum", "Carol", hour: 2));
            var alice = _database.GetProfile("Alice");
            alice.collectibles.RemoveAll(b => b.serial == 6);
            _database.SetCollectibles("Alice", alice.collectibles);

            var promises = new List<KeyValuePair<int, bool>>
            {
                new KeyValuePair<int, bool>(5, false),
                new KeyValuePair<int, bool>(6, false),
            };
            bool moved = Transfer(Bottles, promises, out _);

            Assert.False(moved);
            // #5 was still available, but a partial delivery isn't what anyone agreed to.
            Assert.Empty(_database.GetProfile("Bob").collectibles);
            Assert.Single(_database.GetProfile("Alice").collectibles);
        }

        [Fact]
        public void EncodePromise_CarriesEmptyStateAlongsideTheSerial()
        {
            var full = Bottle(9, "cum", "Carol", hour: 1);
            var empty = Emptied(Bottle(9, "cum", "Carol", hour: 1));

            Assert.Equal(9, CollectiblePayment.EncodePromise(full));
            Assert.Equal(-9, CollectiblePayment.EncodePromise(empty));
            // A type with no spent state always encodes positive, which is why the consent-time
            // recheck compares the flag rather than reading meaning into it.
            Assert.Equal(43, CollectiblePayment.EncodePromise(Pair(43, "Carol", hour: 1)));
        }

        // -------------------------------------------------------------------
        // Wording
        // -------------------------------------------------------------------

        [Fact]
        public void Describe_Bottles_NamesCountSubstanceDonorAndTag()
        {
            var bottles = Items(
                Bottle(1, "cum", "Carol", hour: 2, tag: ChateauCurrency.CorruptTag),
                Bottle(2, "cum", "Carol", hour: 1, tag: ChateauCurrency.CorruptTag));

            string described = CollectiblePayment.Describe(_database, Bottles, bottles);

            Assert.Contains("[b]2 bottles[/b]", described);
            Assert.Contains("Carol", described);
            Assert.Contains("[b]corrupt[/b]", described);
            Assert.DoesNotContain("emptied", described);
        }

        [Fact]
        public void Describe_Bottles_FlagsEmptiesSoNobodyConsentsToOneUnknowingly()
        {
            var empty = Emptied(Bottle(1, "cum", "Carol", hour: 2));

            Assert.Contains("(already emptied)",
                CollectiblePayment.Describe(_database, Bottles, Items(empty)));

            // A mixed parcel says exactly how many rather than implying either.
            var mixed = Items(empty, Bottle(2, "cum", "Carol", hour: 1));
            Assert.Contains("1 of them already emptied", CollectiblePayment.Describe(_database, Bottles, mixed));
        }

        [Fact]
        public void Describe_Panties_NamesCountAndWhoseTheyWere()
        {
            string described = CollectiblePayment.Describe(
                _database, PantiesSection, Items(Pair(43, "Carol", hour: 2), Pair(44, "Carol", hour: 1)));

            Assert.Contains("[b]2 pairs of panties[/b]", described);
            Assert.Contains("two originally from Carol", described);
            // Nothing a pair doesn't have: no substance, no tag, no empty note.
            Assert.DoesNotContain("emptied", described);
            Assert.DoesNotContain("corrupt", described);
        }

        [Fact]
        public void Describe_Panties_SingularReadsAsAPair()
        {
            string described = CollectiblePayment.Describe(
                _database, PantiesSection, Items(Pair(43, "Carol", hour: 1)));

            Assert.Contains("[b]1 pair of panties[/b]", described);
        }

        [Fact]
        public void DescribesCollectibles_DistinguishesParcelsFromCurrencyAmounts()
        {
            Assert.True(CollectiblePayment.DescribesCollectibles(
                CollectiblePayment.Describe(_database, Bottles, Items(Bottle(1, "cum", "Carol", hour: 1)))));
            Assert.True(CollectiblePayment.DescribesCollectibles(
                CollectiblePayment.Describe(_database, PantiesSection, Items(Pair(43, "Carol", hour: 1)))));
            // The currency path passes bare "{amount} {currency}" with no markup at all.
            Assert.False(CollectiblePayment.DescribesCollectibles("100 gold"));
            Assert.False(CollectiblePayment.DescribesCollectibles(null));
        }

        [Theory]
        [InlineData("bottles", true)]
        [InlineData("panties", true)]
        [InlineData("gold", false)]
        [InlineData("nothing", false)]
        [InlineData(null, false)]
        public void IsCollectiblePayment_ReadsTheStoredTypeToken(string identifier, bool expected)
        {
            // "bottles" is what every payment completed before panties existed carries, which is
            // why a shipped token can't be renamed.
            Assert.Equal(expected, CollectiblePayment.IsCollectiblePayment(identifier));
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private bool Transfer(CollectionSection section, List<KeyValuePair<int, bool>> promises, out string failure)
        {
            return CollectiblePayment.TryTransfer(_database, section, "Alice", "Bob", promises, out failure);
        }

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

        private static List<Collectible> Items(params Collectible[] items)
        {
            return items.ToList();
        }

        private static MilkBottle Bottle(int serial, string substance, string source, int hour, string tag = null)
        {
            return BottleInventoryTests.NewBottle(serial, substance, source, hour, tag);
        }

        private static MilkBottle Emptied(MilkBottle bottle)
        {
            bottle.emptiedAt = bottle.acquiredAt.AddMinutes(30);
            return bottle;
        }

        private static Panties Pair(int serial, string subject, int hour)
        {
            return new Panties
            {
                serial = serial,
                subjectName = subject,
                acquiredAt = DateTime.UtcNow.Date.AddHours(hour),
            };
        }
    }
}
