using FChatDicebot;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for Utils.cs string manipulation functions
    /// </summary>
    public class UtilsStringManipulationTests
    {
        #region SanitizeInput Tests

        [Fact]
        public void SanitizeInput_RemovesSpecialCharacters()
        {
            string input = "hello{world}test";

            string result = Utils.SanitizeInput(input);

            Assert.Equal("helloworldtest", result);
        }

        [Fact]
        public void SanitizeInput_RemovesAllTargetedCharacters()
        {
            string input = "a{b}c\\d\"e'f|g,h";

            string result = Utils.SanitizeInput(input);

            Assert.Equal("abcdefgh", result);
        }

        [Fact]
        public void SanitizeInput_NoSpecialCharacters_ReturnsUnchanged()
        {
            string input = "normaltext";

            string result = Utils.SanitizeInput(input);

            Assert.Equal("normaltext", result);
        }

        [Fact]
        public void SanitizeInput_EmptyString_ReturnsEmpty()
        {
            string input = "";

            string result = Utils.SanitizeInput(input);

            Assert.Equal("", result);
        }

        #endregion

        #region CombineStringArray Tests

        [Fact]
        public void CombineStringArray_ConcatenatesAllStrings()
        {
            string[] input = { "Hello", "World", "Test" };

            string result = Utils.CombineStringArray(input);

            Assert.Equal("HelloWorldTest", result);
        }

        [Fact]
        public void CombineStringArray_EmptyArray_ReturnsEmpty()
        {
            string[] input = new string[0];

            string result = Utils.CombineStringArray(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void CombineStringArray_Null_ReturnsNull()
        {
            string[] input = null;

            string result = Utils.CombineStringArray(input);

            Assert.Null(result);
        }

        [Fact]
        public void CombineStringArray_WithEmptyStrings_IncludesEmpty()
        {
            string[] input = { "a", "", "b" };

            string result = Utils.CombineStringArray(input);

            Assert.Equal("ab", result);
        }

        #endregion

        #region GetFullStringOfInputs Tests

        [Fact]
        public void GetFullStringOfInputs_AddsSpacesBetweenElements()
        {
            string[] input = { "Hello", "World", "Test" };

            string result = Utils.GetFullStringOfInputs(input);

            Assert.Equal("Hello World Test", result);
        }

        [Fact]
        public void GetFullStringOfInputs_EmptyArray_ReturnsEmpty()
        {
            string[] input = new string[0];

            string result = Utils.GetFullStringOfInputs(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void GetFullStringOfInputs_Null_ReturnsEmpty()
        {
            string[] input = null;

            string result = Utils.GetFullStringOfInputs(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void GetFullStringOfInputs_SingleElement_ReturnsSingleElement()
        {
            string[] input = { "Solo" };

            string result = Utils.GetFullStringOfInputs(input);

            Assert.Equal("Solo", result);
        }

        #endregion

        #region Capitalize Tests

        [Fact]
        public void Capitalize_CapitalizesFirstLetter()
        {
            string input = "hello";

            string result = Utils.Capitalize(input);

            Assert.Equal("Hello", result);
        }

        [Fact]
        public void Capitalize_AlreadyCapitalized_RemainsUnchanged()
        {
            string input = "Hello";

            string result = Utils.Capitalize(input);

            Assert.Equal("Hello", result);
        }

        [Fact]
        public void Capitalize_AllUppercase_OnlyFirstLetterUppercase()
        {
            string input = "HELLO";

            string result = Utils.Capitalize(input);

            Assert.Equal("HELLO", result);
        }

        [Fact]
        public void Capitalize_SingleCharacter_Capitalizes()
        {
            string input = "a";

            string result = Utils.Capitalize(input);

            Assert.Equal("A", result);
        }

        #endregion

        #region LimitStringToNCharacters Tests

        [Fact]
        public void LimitStringToNCharacters_TruncatesLongString()
        {
            string input = "Hello World This Is A Test";

            string result = Utils.LimitStringToNCharacters(input, 11);

            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void LimitStringToNCharacters_ShortString_ReturnsUnchanged()
        {
            string input = "Hi";

            string result = Utils.LimitStringToNCharacters(input, 10);

            Assert.Equal("Hi", result);
        }

        [Fact]
        public void LimitStringToNCharacters_ExactLength_ReturnsUnchanged()
        {
            string input = "12345";

            string result = Utils.LimitStringToNCharacters(input, 5);

            Assert.Equal("12345", result);
        }

        [Fact]
        public void LimitStringToNCharacters_ZeroLimit_ReturnsEmpty()
        {
            string input = "Test";

            string result = Utils.LimitStringToNCharacters(input, 0);

            Assert.Equal("", result);
        }

        #endregion

        #region AnOrA Tests

        [Fact]
        public void AnOrA_VowelStart_ReturnsAn()
        {
            Assert.Equal("an", Utils.AnOrA("apple"));
            Assert.Equal("an", Utils.AnOrA("elephant"));
            Assert.Equal("an", Utils.AnOrA("igloo"));
            Assert.Equal("an", Utils.AnOrA("orange"));
            Assert.Equal("an", Utils.AnOrA("umbrella"));
        }

        [Fact]
        public void AnOrA_ConsonantStart_ReturnsA()
        {
            Assert.Equal("a", Utils.AnOrA("banana"));
            Assert.Equal("a", Utils.AnOrA("cat"));
            Assert.Equal("a", Utils.AnOrA("dog"));
            Assert.Equal("a", Utils.AnOrA("zebra"));
        }

        [Fact]
        public void AnOrA_UppercaseVowel_ReturnsAn()
        {
            Assert.Equal("an", Utils.AnOrA("Apple"));
            Assert.Equal("an", Utils.AnOrA("Elephant"));
        }

        [Fact]
        public void AnOrA_UppercaseConsonant_ReturnsA()
        {
            Assert.Equal("a", Utils.AnOrA("Banana"));
            Assert.Equal("a", Utils.AnOrA("Cat"));
        }

        #endregion

        #region GetChannelIdFromInputs Tests

        [Fact]
        public void GetChannelIdFromInputs_ValidSessionTag_ExtractsId()
        {
            string[] input = { "[session=test]123456789012345678901234[/session]" };

            string result = Utils.GetChannelIdFromInputs(input);

            Assert.Equal("123456789012345678901234", result);
        }

        [Fact]
        public void GetChannelIdFromInputs_NoSessionTag_ReturnsEmpty()
        {
            string[] input = { "no session here" };

            string result = Utils.GetChannelIdFromInputs(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void GetChannelIdFromInputs_EmptyArray_ReturnsEmpty()
        {
            string[] input = new string[0];

            string result = Utils.GetChannelIdFromInputs(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void GetChannelIdFromInputs_Null_ReturnsEmpty()
        {
            string[] input = null;

            string result = Utils.GetChannelIdFromInputs(input);

            Assert.Equal("", result);
        }

        #endregion

        #region GetCharacterUserTags Tests

        [Fact]
        public void GetCharacterUserTags_WrapsInUserTags()
        {
            string result = Utils.GetCharacterUserTags("TestCharacter");

            Assert.Equal("[user]TestCharacter[/user]", result);
        }

        #endregion

        #region GetCharacterIconTags Tests

        [Fact]
        public void GetCharacterIconTags_WrapsInIconTags()
        {
            string result = Utils.GetCharacterIconTags("TestCharacter");

            Assert.Equal("[icon]TestCharacter[/icon]", result);
        }

        #endregion

        #region GetCustomDeckName Tests

        [Fact]
        public void GetCustomDeckName_AppendsPostfix()
        {
            string result = Utils.GetCustomDeckName("Player1");

            Assert.Equal("Player1's deck", result);
        }

        #endregion

        #region GetStringOfNLength Tests

        [Fact]
        public void GetStringOfNLength_CreatesStringOfCorrectLength()
        {
            string result = Utils.GetStringOfNLength(5);

            Assert.Equal("11111", result);
            Assert.Equal(5, result.Length);
        }

        [Fact]
        public void GetStringOfNLength_Zero_ReturnsEmpty()
        {
            string result = Utils.GetStringOfNLength(0);

            Assert.Equal("", result);
        }

        [Fact]
        public void GetStringOfNLength_LargeNumber_CreatesLongString()
        {
            string result = Utils.GetStringOfNLength(100);

            Assert.Equal(100, result.Length);
            Assert.All(result, c => Assert.Equal('1', c));
        }

        #endregion

        #region RandomString Tests

        [Fact]
        public void RandomString_CreatesStringOfCorrectLength()
        {
            Random rnd = new Random(12345); // Seed for reproducibility

            string result = Utils.RandomString(rnd, 10);

            Assert.Equal(10, result.Length);
        }

        [Fact]
        public void RandomString_OnlyContainsValidCharacters()
        {
            Random rnd = new Random(54321);

            string result = Utils.RandomString(rnd, 50);

            Assert.All(result, c => Assert.Matches("[A-Z0-9]", c.ToString()));
        }

        [Fact]
        public void RandomString_ZeroLength_ReturnsEmpty()
        {
            Random rnd = new Random();

            string result = Utils.RandomString(rnd, 0);

            Assert.Equal("", result);
        }

        #endregion
    }
}
