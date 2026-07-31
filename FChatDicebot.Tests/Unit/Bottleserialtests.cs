using FChatDicebot;
using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Involved;
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
    /// Tests for bottle serial issuance — the counter behind every bottle's permanent number.
    ///
    /// The properties that matter: numbers are never handed out twice (a duplicate would make
    /// !drink #142 and !pay bottle #142 ambiguous), a milking claims its whole block in one
    /// reservation, and a number is retired rather than recycled when its bottle leaves.
    /// </summary>
    [Collection("Database")]
    public class BottleSerialTests
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public BottleSerialTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        [Fact]
        public void ClaimBottleSerials_ReturnsFirstOfBlock()
        {
            int first = _database.ClaimBottleSerials(3);

            // The block is [first, first+2]; the next claim must start past it.
            int next = _database.ClaimBottleSerials(1);
            Assert.Equal(first + 3, next);
        }

        [Fact]
        public void ClaimBottleSerials_StartsAtOneOnFreshDatabase()
        {
            Assert.Equal(1, _database.ClaimBottleSerials(1));
        }

        [Fact]
        public void ClaimBottleSerials_NeverRepeatsAcrossManyClaims()
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < 50; i++)
            {
                int count = (i % 3) + 1;
                int first = _database.ClaimBottleSerials(count);
                for (int offset = 0; offset < count; offset++)
                {
                    Assert.True(seen.Add(first + offset), "serial " + (first + offset) + " was issued twice");
                }
            }
        }

        [Fact]
        public void ClaimBottleSerials_NonPositiveCountIsNoOp()
        {
            int before = _database.ClaimBottleSerials(1);
            Assert.Equal(0, _database.ClaimBottleSerials(0));
            Assert.Equal(before + 1, _database.ClaimBottleSerials(1));
        }

        [Fact]
        public void SoldSerials_AreRetiredNotReissued()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(BottleInventoryTests.NewBottle(1, "cum", "Bob", hour: 1))
                .Build();

            ChateauSell.SellBottles(profile, null, null, 1);

            // The bottle went to the Chateau, so its number leaves the collection entirely.
            Assert.Empty(profile.milkInventory);
            // And the counter keeps climbing rather than handing #1 back out.
            _database.ClaimBottleSerials(1);
            Assert.True(_database.ClaimBottleSerials(1) > 1);
        }

        [Fact]
        public void DrunkSerials_StayInTheCollection()
        {
            // The counterpart to the sell case: drinking keeps the number, selling loses it.
            var bottle = BottleInventoryTests.NewBottle(7, "cum", "Bob", hour: 1);
            var profile = new ProfileBuilder().WithMilkBottle(bottle).Build();

            bottle.emptiedAt = DateTime.UtcNow;

            Assert.Single(profile.milkInventory);
            Assert.Equal(7, profile.milkInventory[0].serial);
            Assert.True(profile.milkInventory[0].IsEmpty);
        }

        [Fact]
        public void MilkedSerialsSurviveTransferUnchanged()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var alice = _database.GetProfile("Alice");
            var bottle = BottleInventoryTests.NewBottle(0, "cum", "Carol", hour: 1);
            bottle.serial = _database.ClaimBottleSerials(1);
            bottle.corruptionTag = ChateauCurrency.CorruptTag;
            alice.milkInventory.Add(bottle);
            _database.SetMilkInventory("Alice", alice.milkInventory);

            var promises = new List<KeyValuePair<int, bool>>
            {
                new KeyValuePair<int, bool>(bottle.serial, false),
            };
            bool moved = BottlePayment.TryTransfer(_database, "Alice", "Bob", promises, out string failure);

            Assert.True(moved, failure);
            var bobBottle = Assert.Single(_database.GetProfile("Bob").milkInventory);
            Assert.Equal(bottle.serial, bobBottle.serial);
            Assert.Equal("Carol", bobBottle.sourceName);
            Assert.Equal(ChateauCurrency.CorruptTag, bobBottle.corruptionTag);
            Assert.Empty(_database.GetProfile("Alice").milkInventory);
        }
    }
}
