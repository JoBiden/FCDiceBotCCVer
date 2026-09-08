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
    /// Tests for the !collection rendering. Uses the database fixture only to resolve subject
    /// userNames into displayNames, which is the one thing the view can't do from the profile
    /// alone and the one thing it must never skip.
    ///
    /// The bottle assertions are carried over from the !bottles suite this replaces: the rows
    /// themselves did not change when the readout became cross-type, only the framing around
    /// them, so these are the regression proof that the refactor kept the bottle view intact.
    /// </summary>
    [Collection("Database")]
    public class ChateauCollectionTests
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ChateauCollectionTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bobby").BuildAndSave(_database);
        }

        // -------------------------------------------------------------------
        // Bottles
        // -------------------------------------------------------------------

        [Fact]
        public void BuildCollectionText_EmptyCollection_PointsAtBothWaysToStartOne()
        {
            var profile = new ProfileBuilder().Build();

            string text = Build(profile);

            Assert.Equal(ChateauCollection.EmptyCollectionText, text);
        }

        [Fact]
        public void BuildCollectionText_ShowsCountSerialsAndPrice()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2))
                .WithMilkBottle(Bottle(12, "cum", "Bob", hour: 1))
                .Build();

            string text = Build(profile);

            Assert.Contains("[b]2[/b] bottles", text);
            Assert.Contains("#11, #12", text);
            Assert.Contains(ReadoutText.Num(ChateauCurrency.StandardBottlePrice) + " " + ChateauCurrency.SellPayoutCurrency + " each", text);
        }

        [Fact]
        public void BuildCollectionText_RendersSubjectDisplayNameNotUserName()
        {
            // subjectName is a stored userName; a resident should never see the raw handle.
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 1))
                .Build();

            Assert.Contains("Bobby", Build(profile));
        }

        [Fact]
        public void BuildCollectionText_TotalValueCountsFullBottlesOnly()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2))
                .WithMilkBottle(Emptied(Bottle(12, "cum", "Bob", hour: 1)))
                .Build();

            string text = Build(profile);

            Assert.Contains("[b]1[/b] bottle", text);
            Assert.Contains(ReadoutText.Num(ChateauCurrency.StandardBottlePrice) + " "
                + ChateauCurrency.SellPayoutCurrency + " if you choose to !sell", text);
        }

        [Fact]
        public void BuildCollectionText_OnlyEmpties_DoesNotOpenWithZeroBottles()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Emptied(Bottle(12, "cum", "Bob", hour: 1)))
                .Build();

            string text = Build(profile);

            Assert.DoesNotContain("0 bottles", text);
            Assert.Contains("[b]1[/b] empty", text);
            Assert.Contains("#12", text);
        }

        [Fact]
        public void BuildCollectionText_OnlyEmpties_OffersNoSale()
        {
            // Nothing the Chateau would buy, so quoting a price of zero would be noise.
            var profile = new ProfileBuilder()
                .WithMilkBottle(Emptied(Bottle(12, "cum", "Bob", hour: 1)))
                .Build();

            Assert.DoesNotContain("!sell", Build(profile));
        }

        [Fact]
        public void BuildCollectionText_ListsEmptiesUnderTheBottles()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2))
                .WithMilkBottle(Emptied(Bottle(12, "cum", "Bob", hour: 1)))
                .Build();

            Assert.Contains(ReadoutText.Row("Empties", "#12"), Build(profile));
        }

        [Fact]
        public void BuildCollectionText_NoEmpties_OmitsTheEmptiesRow()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 1))
                .Build();

            Assert.DoesNotContain("Empties", Build(profile));
        }

        [Fact]
        public void BuildCollectionText_TagsAreLabelled()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2, tag: ChateauCurrency.CorruptTag))
                .WithMilkBottle(Bottle(12, "milk", "Bob", hour: 1, tag: ChateauCurrency.PurifiedTag))
                .Build();

            string text = Build(profile);

            Assert.Contains("[b]corrupt[/b]", text);
            Assert.Contains("[b]pure[/b]", text);
        }

        [Fact]
        public void BuildCollectionText_GroupsSameSubstanceSourceAndTag()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 3))
                .WithMilkBottle(Bottle(12, "cum", "Bob", hour: 2))
                .WithMilkBottle(Bottle(13, "cum", "Bob", hour: 1))
                .Build();

            string text = Build(profile);

            // Three separate milkings collapse into one readable line.
            // Title, holdings line, section header, the one group row, footer.
            Assert.Contains("#11, #12, #13", text);
            Assert.Equal(5, text.Split('\n').Length);
        }

        // -------------------------------------------------------------------
        // Panties — the type that had nowhere to be seen before this command
        // -------------------------------------------------------------------

        [Fact]
        public void BuildCollectionText_ShowsPanties()
        {
            var profile = new ProfileBuilder()
                .WithCollectible(Pair(43, "Bob", hour: 1))
                .Build();

            string text = Build(profile);

            Assert.Contains("[b]1[/b] pair of panties", text);
            Assert.Contains("Bobby", text);
            Assert.Contains("#43", text);
        }

        [Fact]
        public void BuildCollectionText_GroupsPantiesBySubject()
        {
            var profile = new ProfileBuilder()
                .WithCollectible(Pair(43, "Bob", hour: 2))
                .WithCollectible(Pair(44, "Bob", hour: 1))
                .Build();

            string text = Build(profile);

            Assert.Contains("[b]2[/b] pairs of panties", text);
            Assert.Contains("#43, #44", text);
        }

        [Fact]
        public void BuildCollectionText_PantiesAreNotOfferedToTheChateau()
        {
            var profile = new ProfileBuilder()
                .WithCollectible(Pair(43, "Bob", hour: 1))
                .Build();

            string text = Build(profile);

            Assert.Contains("won't buy panties", text);
            Assert.DoesNotContain("if you choose to !sell", text);
            // Not sellable, but transferable — and !pay really does move them now, which it
            // didn't when panties first shipped.
            Assert.Contains("!pay", text);
        }

        // -------------------------------------------------------------------
        // Both at once
        // -------------------------------------------------------------------

        [Fact]
        public void BuildCollectionText_ListsEveryTypeHeld()
        {
            var profile = MixedCollection();

            string text = Build(profile);

            Assert.Contains(ReadoutText.Section("Bottles", ReadoutDomain.Economy), text);
            Assert.Contains(ReadoutText.Section("Panties", ReadoutDomain.Economy), text);
            Assert.Contains("[b]1[/b] bottle and [b]1[/b] pair of panties", text);
        }

        [Fact]
        public void BuildCollectionText_HoldingsLineIsPunctuatedAsOneList()
        {
            // Each section contributes cells rather than a joined phrase, so a three-cell line
            // reads "a, b and c" instead of stacking two "and"s.
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 3))
                .WithMilkBottle(Emptied(Bottle(12, "cum", "Bob", hour: 2)))
                .WithCollectible(Pair(43, "Bob", hour: 1))
                .Build();

            Assert.Contains("[b]1[/b] bottle, [b]1[/b] empty and [b]1[/b] pair of panties", Build(profile));
        }

        [Fact]
        public void BuildCollectionText_SectionsPrintInRegisteredOrder()
        {
            string text = Build(MixedCollection());

            Assert.True(
                text.IndexOf(ReadoutText.Section("Bottles", ReadoutDomain.Economy), StringComparison.Ordinal)
                < text.IndexOf(ReadoutText.Section("Panties", ReadoutDomain.Economy), StringComparison.Ordinal),
                "Sections should print in CollectionSections order (bottles first).");
        }

        // -------------------------------------------------------------------
        // Filters
        // -------------------------------------------------------------------

        [Fact]
        public void BuildCollectionText_TypeFilter_ShowsOnlyThatType()
        {
            string text = Build(MixedCollection(), new CollectionFilter { TypeToken = "panties" });

            Assert.Contains(ReadoutText.Section("Panties", ReadoutDomain.Economy), text);
            Assert.DoesNotContain(ReadoutText.Section("Bottles", ReadoutDomain.Economy), text);
        }

        [Fact]
        public void BuildCollectionText_SubstanceFilter_LeavesPantiesOut()
        {
            // A substance is a bottle question. Panties have no substance to match, so they are
            // reported as nothing rather than shown regardless.
            string text = Build(MixedCollection(), new CollectionFilter { Substance = "cum" });

            Assert.Contains(ReadoutText.Section("Bottles", ReadoutDomain.Economy), text);
            Assert.DoesNotContain(ReadoutText.Section("Panties", ReadoutDomain.Economy), text);
        }

        [Fact]
        public void BuildCollectionText_TypeFilterWithNoneHeld_PointsAtHowToGetOne()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 1))
                .Build();

            // Forty bottles doesn't make "your filter matched nothing" the useful answer to
            // "show me my panties".
            string text = Build(profile, new CollectionFilter { TypeToken = "panties" });

            Assert.Equal(new PantiesCollectionSection().EmptyText, text);
        }

        [Fact]
        public void BuildCollectionText_BottleTypeFilterWithNoneHeld_ReusesTheDrinkWording()
        {
            var profile = new ProfileBuilder()
                .WithCollectible(Pair(43, "Bob", hour: 1))
                .Build();

            string text = Build(profile, new CollectionFilter { TypeToken = "bottles" });

            Assert.Equal(BottleCollectionSection.NoBottlesText, text);
        }

        [Fact]
        public void BuildCollectionText_FilterMiss_DistinguishedFromEmptyCollection()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 1))
                .Build();

            string text = Build(profile, new CollectionFilter { Substance = "milk" });

            Assert.NotEqual(ChateauCollection.EmptyCollectionText, text);
            Assert.Contains("!collection on its own", text);
        }

        [Fact]
        public void BuildCollectionText_SubjectFilter_NarrowsEveryType()
        {
            new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").BuildAndSave(_database);
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 3))
                .WithMilkBottle(Bottle(12, "cum", "Carol", hour: 2))
                .WithCollectible(Pair(43, "Carol", hour: 1))
                .Build();

            string text = Build(profile, new CollectionFilter { Subject = "Carol" });

            Assert.Contains("#12", text);
            Assert.Contains("#43", text);
            Assert.DoesNotContain("#11", text);
        }

        // -------------------------------------------------------------------
        // Argument parsing
        // -------------------------------------------------------------------

        [Theory]
        [InlineData("bottles", "bottles")]
        [InlineData("panties", "panties")]
        [InlineData("cum", null)]
        [InlineData("bobby", null)]
        public void ParseTypeToken_RecognisesOnlyTypeWords(string term, string expected)
        {
            Assert.Equal(expected, ChateauCollection.ParseTypeToken(new[] { term }));
        }

        [Fact]
        public void TypeWords_AreDeclaredAsArgumentKeywords()
        {
            // Without this, bare-name resolution treats "panties" as a resident it can't place
            // and refuses to run the command at all.
            var command = new ChateauCollection();

            Assert.Contains("bottles", command.ArgumentKeywords);
            Assert.Contains("panties", command.ArgumentKeywords);
        }

        // -------------------------------------------------------------------
        // !bank's one-line summary, which shares this data
        // -------------------------------------------------------------------

        [Fact]
        public void BuildCollectionLine_ZeroBottles_IsOmittedFromBank()
        {
            var profile = new ProfileBuilder().Build();

            Assert.Equal(string.Empty, ChateauBank.BuildCollectionLine(profile, ownAccount: true));
        }

        [Fact]
        public void BuildCollectionLine_CountsFullAndEmptySeparately()
        {
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2))
                .WithMilkBottle(Emptied(Bottle(12, "cum", "Bob", hour: 1)))
                .Build();

            string line = ChateauBank.BuildCollectionLine(profile, ownAccount: true);

            Assert.Contains("[b]1[/b] bottle", line);
            Assert.Contains("[b]1[/b] empty", line);
            Assert.Contains("!collection", line);
        }

        // -------------------------------------------------------------------
        // Source eicons — the icon on an item belongs to whoever it came from
        // -------------------------------------------------------------------

        [Fact]
        public void BuildCollectionText_BottleRow_CarriesTheDonorsOwnMilkEicon()
        {
            SaveWithEicon("Bob", "Bobby", "eicon_milk", "[eicon]bmilk[/eicon]");

            var profile = new ProfileBuilder().WithMilkBottle(Bottle(11, "cum", "Bob", hour: 1)).Build();

            Assert.Contains("Bobby [eicon]bmilk[/eicon]", Build(profile));
        }

        [Fact]
        public void BuildCollectionText_PantiesRow_CarriesTheSubjectsOwnPantiesEicon()
        {
            SaveWithEicon("Bob", "Bobby", "eicon_panties", "[eicon]bsilk[/eicon]");

            var profile = new ProfileBuilder().WithCollectible(Pair(43, "Bob", hour: 1)).Build();

            string text = Build(profile);

            // Outside the label: the underline is the name's, not the icon's.
            Assert.Contains(ReadoutText.Label("Bobby") + " [eicon]bsilk[/eicon]", text);
        }

        [Fact]
        public void BuildCollectionText_EachTypeReadsItsOwnSlot()
        {
            // One person, two icons, one for each thing of theirs you can be holding.
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bobby")
                .WithCharacteristic("eicon_milk", "[eicon]bmilk[/eicon]")
                .WithCharacteristic("eicon_panties", "[eicon]bsilk[/eicon]")
                .BuildAndSave(_database);

            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2))
                .WithCollectible(Pair(43, "Bob", hour: 1))
                .Build();

            string text = Build(profile);

            Assert.Contains("[eicon]bmilk[/eicon]", text);
            Assert.Contains("[eicon]bsilk[/eicon]", text);
        }

        [Fact]
        public void BuildCollectionText_SubjectWithNoEicon_RowsAreUnchanged()
        {
            // Bob is saved without one by the constructor.
            var profile = new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2))
                .WithCollectible(Pair(43, "Bob", hour: 1))
                .Build();

            Assert.DoesNotContain("[eicon]", Build(profile));
        }

        [Fact]
        public void BuildCollectionText_UsesTheSubjectsEicon_NotTheHoldersOwn()
        {
            // The holder's own icons are what people see on *their* milk and panties. They have
            // no business decorating a row of someone else's.
            SaveWithEicon("Bob", "Bobby", "eicon_milk", "[eicon]bmilk[/eicon]");

            var profile = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithCharacteristic("eicon_milk", "[eicon]amilk[/eicon]")
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 1))
                .Build();

            string text = Build(profile);

            Assert.Contains("[eicon]bmilk[/eicon]", text);
            Assert.DoesNotContain("[eicon]amilk[/eicon]", text);
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private void SaveWithEicon(string userName, string displayName, string key, string eicon)
        {
            new ProfileBuilder().WithUserName(userName).WithDisplayName(displayName)
                .WithCharacteristic(key, eicon).BuildAndSave(_database);
        }


        private string Build(Profile profile, CollectionFilter filter = null)
        {
            return ChateauCollection.BuildCollectionText(_database, profile, filter);
        }

        private static Profile MixedCollection()
        {
            return new ProfileBuilder()
                .WithMilkBottle(Bottle(11, "cum", "Bob", hour: 2))
                .WithCollectible(Pair(43, "Bob", hour: 1))
                .Build();
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
