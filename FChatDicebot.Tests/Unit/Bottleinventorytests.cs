using FChatDicebot;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for BottleInventory — the shared selection and grouping that !sell, !drink,
    /// !bottles and bottle transfer all read from. Pure functions over a Profile, so no
    /// database fixture is needed.
    ///
    /// The two contracts under test: newest-first ordering (so what !bottles shows at the top
    /// is what !sell and !drink act on next), and empties never appearing in an implicit
    /// selection.
    /// </summary>
    public class BottleInventoryTests
    {
        // -------------------------------------------------------------------
        // Selection
        // -------------------------------------------------------------------

        [Fact]
        public void SelectFull_NullProfile_ReturnsEmptyList()
        {
            Assert.Empty(BottleInventory.SelectFull(null, null, null));
        }

        [Fact]
        public void SelectFull_OrdersNewestFirst()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle(1, "cum", "Bob", hour: 1))
                .WithMilkBottle(NewBottle(2, "cum", "Bob", hour: 5))
                .WithMilkBottle(NewBottle(3, "cum", "Bob", hour: 3))
                .Build();

            var ordered = BottleInventory.SelectFull(profile, null, null);

            Assert.Equal(new[] { 2, 3, 1 }, ordered.Select(b => b.serial).ToArray());
        }

        [Fact]
        public void SelectFull_ExcludesEmpties()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle(1, "cum", "Bob", hour: 1))
                .WithMilkBottle(Emptied(NewBottle(2, "cum", "Bob", hour: 9)))
                .Build();

            var full = BottleInventory.SelectFull(profile, null, null);

            Assert.Single(full);
            Assert.Equal(1, full[0].serial);
        }

        [Fact]
        public void SelectEmpty_ReturnsOnlyEmpties()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle(1, "cum", "Bob", hour: 1))
                .WithMilkBottle(Emptied(NewBottle(2, "cum", "Bob", hour: 2)))
                .WithMilkBottle(Emptied(NewBottle(3, "milk", "Alice", hour: 3)))
                .Build();

            var empties = BottleInventory.SelectEmpty(profile, null, null);

            Assert.Equal(2, empties.Count);
            Assert.DoesNotContain(empties, b => b.serial == 1);
        }

        [Fact]
        public void SelectFull_AppliesSubstanceAndSourceFilters()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle(1, "milk", "Alice", hour: 5))
                .WithMilkBottle(NewBottle(2, "cum", "Alice", hour: 4))
                .WithMilkBottle(NewBottle(3, "cum", "Bob", hour: 3))
                .Build();

            var bySubstance = BottleInventory.SelectFull(profile, "cum", null);
            Assert.Equal(new[] { 2, 3 }, bySubstance.Select(b => b.serial).ToArray());

            var bySource = BottleInventory.SelectFull(profile, "cum", "Bob");
            Assert.Single(bySource);
            Assert.Equal(3, bySource[0].serial);
        }

        // -------------------------------------------------------------------
        // Serial lookup
        // -------------------------------------------------------------------

        [Fact]
        public void FindBySerial_FindsFullAndEmptyAlike()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle(11, "cum", "Bob", hour: 1))
                .WithMilkBottle(Emptied(NewBottle(12, "cum", "Bob", hour: 2)))
                .Build();

            Assert.Equal(11, BottleInventory.FindBySerial(profile, 11).serial);
            Assert.Equal(12, BottleInventory.FindBySerial(profile, 12).serial);
            Assert.Null(BottleInventory.FindBySerial(profile, 99));
        }

        [Fact]
        public void FindBySerial_ZeroNeverMatches()
        {
            // Serial 0 is the "predates the backfill" sentinel, not a referenceable number.
            var profile = new ProfileBuilder()
                .WithMilkBottle(NewBottle(0, "cum", "Bob", hour: 1))
                .Build();

            Assert.Null(BottleInventory.FindBySerial(profile, 0));
        }

        // -------------------------------------------------------------------
        // Grouping and display
        // -------------------------------------------------------------------

        [Fact]
        public void Group_CollapsesMatchingSubstanceSourceAndTag()
        {
            var bottles = new[]
            {
                NewBottle(1, "milk", "Alice", hour: 3),
                NewBottle(2, "milk", "Alice", hour: 2),
                NewBottle(3, "milk", "Bob", hour: 1),
            };

            var groups = BottleInventory.Group(bottles);

            Assert.Equal(2, groups.Count);
            Assert.Equal(2, groups[0].Count);
            Assert.Equal(new[] { 1, 2 }, groups[0].Serials.ToArray());
            Assert.Single(groups[1].Serials);
        }

        [Fact]
        public void Group_SeparatesByCorruptionTag()
        {
            var bottles = new[]
            {
                NewBottle(1, "milk", "Alice", hour: 3, tag: ChateauCurrency.CorruptTag),
                NewBottle(2, "milk", "Alice", hour: 2),
            };

            Assert.Equal(2, BottleInventory.Group(bottles).Count);
        }

        [Fact]
        public void FormatSerials_CapsWithMoreTail()
        {
            string text = CollectionInventory.FormatSerials(new[] { 1, 2, 3, 4, 5 }, cap: 3);

            Assert.Equal("#1, #2, #3, and 2 more", text);
        }

        [Fact]
        public void FormatSerials_UnderCapHasNoTail()
        {
            Assert.Equal("#1, #2", CollectionInventory.FormatSerials(new[] { 1, 2 }, cap: 8));
        }

        [Fact]
        public void FormatSerials_UnnumberedRendersAsQuestionMark()
        {
            // A pre-backfill bottle shouldn't claim to be "#0".
            Assert.Equal("#?", CollectionInventory.FormatSerials(new[] { 0 }, cap: 8));
        }

        [Fact]
        public void CountsBySubstance_OrdersByCountThenName()
        {
            var bottles = new[]
            {
                NewBottle(1, "cum", "Bob", hour: 1),
                NewBottle(2, "milk", "Alice", hour: 2),
                NewBottle(3, "milk", "Alice", hour: 3),
                NewBottle(4, "milk", "Bob", hour: 4),
            };

            var counts = BottleInventory.CountsBySubstance(bottles);

            Assert.Equal("milk", counts[0].Key);
            Assert.Equal(3, counts[0].Value);
            Assert.Equal("cum", counts[1].Key);
            Assert.Equal(1, counts[1].Value);
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        internal static MilkBottle NewBottle(int serial, string substance, string source, int hour, string tag = null)
        {
            return new MilkBottle
            {
                serial = serial,
                substance = substance,
                subjectName = source,
                quantity = 1,
                corruptionTag = tag,
                acquiredAt = DateTime.UtcNow.Date.AddHours(hour),
            };
        }

        internal static MilkBottle Emptied(MilkBottle bottle)
        {
            bottle.emptiedAt = bottle.acquiredAt.AddMinutes(30);
            return bottle;
        }
    }
}
