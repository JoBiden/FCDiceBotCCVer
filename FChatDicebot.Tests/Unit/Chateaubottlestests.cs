using FChatDicebot;
using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for the !bottles rendering. Uses the database fixture only to resolve donor
    /// userNames into displayNames, which is the one thing the view can't do from the profile
    /// alone and the one thing it must never skip.
    /// </summary>
    [Collection("Database")]
    public class ChateauBottlesTests
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ChateauBottlesTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bobby").BuildAndSave(_database);
        }

        [Fact]
        public void BuildCollectionText_EmptyCollection_PointsAtMilk()
        {
            var profile = new ProfileBuilder().Build();

            string text = ChateauBottles.BuildCollectionText(_database, profile, null, null);

            Assert.Equal(ChateauBottles.EmptyCollectionText, text);
        }

        [Fact]
        public void BuildCollectionText_ShowsCountSerialsAndPrice()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2))
                .WithMilkBottle(Bottle(12, "cum", "Bob", hour: 1))
                .Build();

            string text = ChateauBottles.BuildCollectionText(_database, profile, null, null);

            Assert.Contains("[b]2 bottles[/b]", text);
            Assert.Contains("#11, #12", text);
            Assert.Contains(ChateauCurrency.StandardBottlePrice + " " + ChateauCurrency.SellPayoutCurrency + " each", text);
        }

        [Fact]
        public void BuildCollectionText_RendersDonorDisplayNameNotUserName()
        {
            // sourceName is a stored userName; a resident should never see the raw handle.
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 1))
                .Build();

            string text = ChateauBottles.BuildCollectionText(_database, profile, null, null);

            Assert.Contains("Bobby", text);
        }

        [Fact]
        public void BuildCollectionText_TotalValueCountsFullBottlesOnly()
        {
            var empty = Bottle(12, "cum", "Bob", hour: 1);
            empty.emptiedAt = DateTime.UtcNow;
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2))
                .WithMilkBottle(empty)
                .Build();

            string text = ChateauBottles.BuildCollectionText(_database, profile, null, null);

            Assert.Contains("[b]1 bottle[/b]", text);
            Assert.Contains("[b]" + ChateauCurrency.StandardBottlePrice + " "
                + ChateauCurrency.SellPayoutCurrency + "[/b] if you choose to !sell", text);
        }

        [Fact]
        public void BuildCollectionText_OnlyEmpties_DoesNotOpenWithZeroBottles()
        {
            var empty = Bottle(12, "cum", "Bob", hour: 1);
            empty.emptiedAt = DateTime.UtcNow;
            var profile = new ProfileBuilder().WithMilkBottle(empty).Build();

            string text = ChateauBottles.BuildCollectionText(_database, profile, null, null);

            Assert.DoesNotContain("0 bottles", text);
            Assert.Contains("[b]1 empty[/b]", text);
            Assert.Contains("#12", text);
        }

        [Fact]
        public void BuildCollectionText_ListsEmptiesSeparately()
        {
            var empty = Bottle(12, "cum", "Bob", hour: 1);
            empty.emptiedAt = DateTime.UtcNow;
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2))
                .WithMilkBottle(empty)
                .Build();

            string text = ChateauBottles.BuildCollectionText(_database, profile, null, null);

            Assert.Contains("Empties: #12", text);
        }

        [Fact]
        public void BuildCollectionText_NoEmpties_OmitsTheEmptiesLine()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 1))
                .Build();

            Assert.DoesNotContain("Empties:", ChateauBottles.BuildCollectionText(_database, profile, null, null));
        }

        [Fact]
        public void BuildCollectionText_TagsAreLabelled()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2, tag: ChateauCurrency.CorruptTag))
                .WithMilkBottle(Bottle(12, "milk", "Bob", hour: 1, tag: ChateauCurrency.PurifiedTag))
                .Build();

            string text = ChateauBottles.BuildCollectionText(_database, profile, null, null);

            Assert.Contains("[b]corrupt[/b]", text);
            Assert.Contains("[b]pure[/b]", text);
        }

        [Fact]
        public void BuildCollectionText_FilterMiss_DistinguishedFromEmptyCollection()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 1))
                .Build();

            string text = ChateauBottles.BuildCollectionText(_database, profile, "milk", null);

            Assert.NotEqual(ChateauBottles.EmptyCollectionText, text);
            Assert.Contains("!bottles on its own", text);
        }

        [Fact]
        public void BuildCollectionText_GroupsSameSubstanceSourceAndTag()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 3))
                .WithMilkBottle(Bottle(12, "cum", "Bob", hour: 2))
                .WithMilkBottle(Bottle(13, "cum", "Bob", hour: 1))
                .Build();

            string text = ChateauBottles.BuildCollectionText(_database, profile, null, null);

            // Three separate milkings collapse into one readable line.
            Assert.Contains("#11, #12, #13", text);
            Assert.Equal(3, text.Split('\n').Length);
        }

        [Fact]
        public void BuildCollectionLine_ZeroBottles_IsOmittedFromBank()
        {
            var profile = new ProfileBuilder().Build();

            Assert.Equal(string.Empty, ChateauBank.BuildCollectionLine(profile, ownAccount: true));
        }

        [Fact]
        public void BuildCollectionLine_CountsFullAndEmptySeparately()
        {
            var empty = Bottle(12, "cum", "Bob", hour: 1);
            empty.emptiedAt = DateTime.UtcNow;
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2))
                .WithMilkBottle(empty)
                .Build();

            string line = ChateauBank.BuildCollectionLine(profile, ownAccount: true);

            Assert.Contains("[b]1 bottle[/b]", line);
            Assert.Contains("[b]1 empty[/b]", line);
            Assert.Contains("!bottles", line);
        }

        private static MilkBottle Bottle(int serial, string substance, string source, int hour, string tag = null)
        {
            return BottleInventoryTests.NewBottle(serial, substance, source, hour, tag);
        }
    }
}
