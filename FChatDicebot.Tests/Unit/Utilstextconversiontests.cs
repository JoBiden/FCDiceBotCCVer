using FChatDicebot;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for Utils.cs text conversion/dictionary lookup functions
    /// </summary>
    public class UtilsTextConversionTests
    {
        #region TrainingToText Tests

        [Theory]
        [InlineData("anal", "comfortably take it up the ass")]
        [InlineData("corset", "wear a corset as tight as possible")]
        [InlineData("dance", "dance")]
        [InlineData("deepthroat", "deepthroat")]
        [InlineData("flight", "fly")]
        [InlineData("heel", "wear high heels")]
        [InlineData("instrument", "play an instrument")]
        [InlineData("magic", "use magic")]
        [InlineData("mathematics", "do mathematics")]
        [InlineData("obedience", "happily do as they're told")]
        [InlineData("ponygirl", "behave and dress as a pony")]
        public void TrainingToText_KnownTraining_ReturnsCorrectText(string training, string expected)
        {
            string result = Utils.TrainingToText(training);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void TrainingToText_UnknownTraining_ReturnsFallbackMessage()
        {
            string result = Utils.TrainingToText("unknowntraining");

            Assert.Contains("curiosity", result);
            Assert.Contains("unknowntraining", result);
        }

        #endregion

        #region ObjectToText Tests

        [Theory]
        [InlineData("book", "book")]
        [InlineData("building", "building")]
        [InlineData("clothing", "piece of clothing")]
        [InlineData("dildo", "dildo")]
        [InlineData("furniture", "piece of furniture")]
        [InlineData("onahole", "onahole")]
        [InlineData("orb", "orb")]
        [InlineData("painting", "painting")]
        [InlineData("statue", "statue")]
        [InlineData("tableware", "piece of tableware")]
        [InlineData("tool", "tool")]
        [InlineData("toy", "toy")]
        [InlineData("trash", "trash")]
        [InlineData("trinket", "trinket")]
        [InlineData("vehicle", "vehicle")]
        public void ObjectToText_KnownObject_ReturnsCorrectText(string objectType, string expected)
        {
            string result = Utils.ObjectToText(objectType);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ObjectToText_UnknownObject_ReturnsFallbackMessage()
        {
            string result = Utils.ObjectToText("unknownobject");

            Assert.Contains("curiosity", result);
            Assert.Contains("unknownobject", result);
        }

        #endregion

        #region JobToText Tests

        [Theory]
        [InlineData("adventurer", "Adventurer")]
        [InlineData("alchemist", "Alchemist")]
        [InlineData("armcandy", "Piece of Arm Candy")]
        [InlineData("artist", "Artist")]
        [InlineData("bartender", "Bartender")]
        [InlineData("bitch", "Bitch")]
        [InlineData("bodyguard", "Bodyguard")]
        [InlineData("breeder", "Breeder")]
        [InlineData("cook", "Cook")]
        [InlineData("dealer", "Dealer")]
        [InlineData("fucktoy", "Fuck Toy")]
        [InlineData("healer", "Healer")]
        [InlineData("maid", "Maid")]
        [InlineData("pet", "Pet")]
        [InlineData("slave", "Slave")]
        [InlineData("whore", "Whore")]
        public void JobToText_KnownJob_ReturnsCorrectText(string job, string expected)
        {
            string result = Utils.JobToText(job);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void JobToText_UnknownJob_ReturnsFallbackMessage()
        {
            string result = Utils.JobToText("unknownjob");

            Assert.Contains("Purveyor of New Professions", result);
            Assert.Contains("unknownjob", result);
        }

        #endregion

        #region AttireToText Tests

        [Theory]
        [InlineData("bottomless", "a bottomless outfit")]
        [InlineData("oriental", "oriental garb")]
        [InlineData("regal", "regal attire")]
        [InlineData("nude", "their birthday suit")]
        [InlineData("wedding", "wedding attire")]
        [InlineData("stripper", "stripper clothes")]
        [InlineData("piercings", "piercings and only piercings")]
        [InlineData("bondage", "some form of bondage")]
        [InlineData("bunnygirl", "a bunnygirl leotard")]
        [InlineData("maidoutfit", "a maid outfit")]
        [InlineData("swimsuit", "a swimsuit")]
        [InlineData("slutty", "something super slutty")]
        [InlineData("casual", "casual clothes")]
        [InlineData("lingerie", "sexy lingerie")]
        [InlineData("latex", "a latex outfit")]
        public void AttireToText_KnownAttire_ReturnsCorrectText(string attire, string expected)
        {
            string result = Utils.AttireToText(attire);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void AttireToText_UnknownAttire_ReturnsFallbackMessage()
        {
            string result = Utils.AttireToText("unknownattire");

            Assert.Contains("something ineffable", result);
            Assert.Contains("unknownattire", result);
        }

        #endregion

        #region SubstanceToText Tests

        [Theory]
        [InlineData("fire", "literal fire")]
        [InlineData("water", "water")]
        [InlineData("earth", "elemental earth")]
        [InlineData("air", "air")]
        [InlineData("golden", "golden fluid")]
        [InlineData("cum", "cum")]
        [InlineData("goo", "slimy goo")]
        [InlineData("pollen", "pollen")]
        [InlineData("sap", "sap")]
        [InlineData("pre", "precum")]
        [InlineData("saliva", "saliva")]
        [InlineData("sweat", "sweat")]
        [InlineData("drug", "sort of drug")]
        [InlineData("drink", "drink")]
        [InlineData("food", "food")]
        [InlineData("energy", "pure energy")]
        [InlineData("milk", "milk")]
        public void SubstanceToText_KnownSubstance_ReturnsCorrectText(string substance, string expected)
        {
            string result = Utils.SubstanceToText(substance);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void SubstanceToText_LustEssence_ReturnsColoredText()
        {
            string result = Utils.SubstanceToText("lustessence");

            Assert.Contains("[color=purple]", result);
            Assert.Contains("Lust Essence", result);
        }

        [Fact]
        public void SubstanceToText_UnknownSubstance_ReturnsFallbackMessage()
        {
            string result = Utils.SubstanceToText("unknownsubstance");

            Assert.Contains("something ineffable", result);
            Assert.Contains("unknownsubstance", result);
        }

        #endregion

        #region BodypartToText Tests

        [Fact]
        public void BodypartToText_LowerBack_ReturnsWithSpace()
        {
            string result = Utils.BodypartToText("lowerback");

            Assert.Equal("lower back", result);
        }

        [Fact]
        public void BodypartToText_OtherBodypart_ReturnsUnchanged()
        {
            string result = Utils.BodypartToText("chest");

            Assert.Equal("chest", result);
        }

        #endregion

        #region LocationToText Tests

        [Theory]
        [InlineData("magiclibrary", "Alice", "Bob", "in the magic library")]
        [InlineData("guestroom", "Alice", "Bob", "in a guest room")]
        [InlineData("queenstudy", "Alice", "Bob", "in the Queen's study")]
        [InlineData("cuddlepuddle", "Alice", "Bob", "in the cuddlepuddle")]
        [InlineData("storage", "Alice", "Bob", "in the storage room")]
        [InlineData("library", "Alice", "Bob", "in the library")]
        [InlineData("kitchen", "Alice", "Bob", "in the kitchen")]
        [InlineData("stables", "Alice", "Bob", "in the stables")]
        [InlineData("garden", "Alice", "Bob", "out in the garden")]
        public void LocationToText_KnownLocation_ReturnsCorrectText(string location, string initiator, string recipient, string expected)
        {
            string result = Utils.LocationToText(location, initiator, recipient);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void LocationToText_TheirRoom_UsesRecipientName()
        {
            string result = Utils.LocationToText("theirroom", "Alice", "Bob");

            Assert.Equal("in Bob's room", result);
        }

        [Fact]
        public void LocationToText_MyRoom_UsesInitiatorName()
        {
            string result = Utils.LocationToText("myroom", "Alice", "Bob");

            Assert.Equal("in Alice's room", result);
        }

        [Fact]
        public void LocationToText_TheirOffice_UsesRecipientName()
        {
            string result = Utils.LocationToText("theiroffice", "Alice", "Bob");

            Assert.Equal("in Bob's office", result);
        }

        [Fact]
        public void LocationToText_MyOffice_UsesInitiatorName()
        {
            string result = Utils.LocationToText("myoffice", "Alice", "Bob");

            Assert.Equal("in Alice's office", result);
        }

        [Fact]
        public void LocationToText_UnknownLocation_ReturnsFallbackMessage()
        {
            string result = Utils.LocationToText("unknownlocation", "Alice", "Bob");

            Assert.Contains("somewhere ineffable", result);
            Assert.Contains("unknownlocation", result);
        }

        #endregion

        #region BondToText Tests

        [Theory]
        [InlineData("marriage", true, "spouse")]
        [InlineData("marriage", false, "spouse")]
        [InlineData("offspring", true, "offspring")]
        [InlineData("offspring", false, "parent")]
        [InlineData("disciple", true, "disciple")]
        [InlineData("disciple", false, "mentor")]
        [InlineData("sibling", true, "sibling")]
        [InlineData("sibling", false, "sibling")]
        [InlineData("submission", true, "submissive")]
        [InlineData("submission", false, "dominant")]
        [InlineData("property", true, "property")]
        [InlineData("property", false, "owner")]
        [InlineData("pet", true, "pet")]
        [InlineData("pet", false, "leash holder")]
        public void BondToText_KnownBond_ReturnsCorrectText(string bond, bool initiatorPerspective, string expected)
        {
            string result = Utils.BondToText(bond, initiatorPerspective);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void BondToText_UnknownBond_ReturnsFallbackMessage()
        {
            string result = Utils.BondToText("unknownbond", true);

            Assert.Contains("a mysterious bond", result);
            Assert.Contains("unknownbond", result);
        }

        #endregion

        #region BondToPlural Tests

        [Theory]
        [InlineData("marriage", true, "spouses")]
        [InlineData("marriage", false, "spouses")]
        [InlineData("offspring", true, "offspring")]
        [InlineData("offspring", false, "parents")]
        [InlineData("disciple", true, "disciples")]
        [InlineData("disciple", false, "mentors")]
        [InlineData("sibling", true, "siblings")]
        [InlineData("sibling", false, "siblings")]
        [InlineData("submission", true, "submissives")]
        [InlineData("submission", false, "dominants")]
        [InlineData("property", true, "property")]
        [InlineData("property", false, "owners")]
        [InlineData("pet", true, "pets")]
        [InlineData("pet", false, "leash holders")]
        [InlineData("fuckbuddy", true, "fuckbuddies")]
        [InlineData("fuckbuddy", false, "fuckbuddies")]
        public void BondToPlural_KnownBond_ReturnsCorrectPlural(string bond, bool initiatorPerspective, string expected)
        {
            string result = Utils.BondToPlural(bond, initiatorPerspective);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void BondToPlural_UnknownBond_ReturnsFallbackMessage()
        {
            string result = Utils.BondToPlural("unknownbond", true);

            Assert.Contains("mysterious bonds", result);
            Assert.Contains("unknownbond", result);
        }

        #endregion

        //#region GetCharacterStringFromSpecialName Tests

        ////we're not concerned with dicebot functions for now
        //[Fact]
        //public void GetCharacterStringFromSpecialName_DealerAlias_ReturnsTheDealer()
        //{
        //    string result = Utils.GetCharacterStringFromSpecialName(DiceBot.DealerPlayerAlias);

        //    Assert.Equal("The dealer", result);
        //}

        //[Fact]
        //public void GetCharacterStringFromSpecialName_BurnAlias_ReturnsBurned()
        //{
        //    string result = Utils.GetCharacterStringFromSpecialName(DiceBot.BurnCardsPlayerAlias);

        //    Assert.Equal("Burned", result);
        //}

        //[Fact]
        //public void GetCharacterStringFromSpecialName_DiscardAlias_ReturnsDiscarded()
        //{
        //    string result = Utils.GetCharacterStringFromSpecialName(DiceBot.DiscardPlayerAlias);

        //    Assert.Equal("Discarded", result);
        //}

        //[Fact]
        //public void GetCharacterStringFromSpecialName_PotAlias_ReturnsThePot()
        //{
        //    string result = Utils.GetCharacterStringFromSpecialName(DiceBot.PotPlayerAlias);

        //    Assert.Equal("The pot", result);
        //}

        //[Fact]
        //public void GetCharacterStringFromSpecialName_HouseAlias_ReturnsTheHouse()
        //{
        //    string result = Utils.GetCharacterStringFromSpecialName(DiceBot.HousePlayerAlias);

        //    Assert.Equal("The house", result);
        //}

        //[Fact]
        //public void GetCharacterStringFromSpecialName_RegularName_ReturnsUserTags()
        //{
        //    string result = Utils.GetCharacterStringFromSpecialName("TestPlayer");

        //    Assert.Equal("[user]TestPlayer[/user]", result);
        //}

        //#endregion

        #region interactionToVerb Tests

        [Fact]
        public void InteractionToVerb_Dose_PastAndPresentTensesDiffer()
        {
            // Regression test for L15: both tenses returned "dosed", producing sentences
            // like "Pledged to dosed X" for the present/infinitive form.
            Assert.Equal("dose", Utils.interactionToVerb("dose", false));
            Assert.Equal("dosed", Utils.interactionToVerb("dose", true));
        }

        #endregion
    }
}
