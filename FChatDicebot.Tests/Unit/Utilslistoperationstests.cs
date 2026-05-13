using FChatDicebot;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for Utils.cs list and array operation functions
    /// </summary>
    public class UtilsListOperationsTests
    {
        #region PrintList Tests

        [Fact]
        public void PrintList_StringArray_ReturnsCommaSeparated()
        {
            string[] input = { "apple", "banana", "cherry" };

            string result = Utils.PrintList(input);

            Assert.Equal("apple, banana, cherry", result);
        }

        [Fact]
        public void PrintList_StringArray_EmptyArray_ReturnsEmpty()
        {
            string[] input = new string[0];

            string result = Utils.PrintList(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void PrintList_StringArray_Null_ReturnsEmpty()
        {
            string[] input = null;

            string result = Utils.PrintList(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void PrintList_StringArray_SingleItem_ReturnsItem()
        {
            string[] input = { "solo" };

            string result = Utils.PrintList(input);

            Assert.Equal("solo", result);
        }

        [Fact]
        public void PrintList_StringList_ReturnsCommaSeparated()
        {
            List<string> input = new List<string> { "red", "green", "blue" };

            string result = Utils.PrintList(input);

            Assert.Equal("red, green, blue", result);
        }

        [Fact]
        public void PrintList_StringList_EmptyList_ReturnsEmpty()
        {
            List<string> input = new List<string>();

            string result = Utils.PrintList(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void PrintList_StringList_Null_ReturnsEmpty()
        {
            List<string> input = null;

            string result = Utils.PrintList(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void PrintList_CharArray_ReturnsCommaSeparated()
        {
            char[] input = { 'a', 'b', 'c' };

            string result = Utils.PrintList(input);

            Assert.Equal("a, b, c", result);
        }

        [Fact]
        public void PrintList_CharArray_EmptyArray_ReturnsEmpty()
        {
            char[] input = new char[0];

            string result = Utils.PrintList(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void PrintList_CharArray_Null_ReturnsEmpty()
        {
            char[] input = null;

            string result = Utils.PrintList(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void PrintList_IntList_ReturnsCommaSeparated()
        {
            List<int> input = new List<int> { 1, 2, 3, 10, 100 };

            string result = Utils.PrintList(input);

            Assert.Equal("1, 2, 3, 10, 100", result);
        }

        [Fact]
        public void PrintList_IntList_EmptyList_ReturnsEmpty()
        {
            List<int> input = new List<int>();

            string result = Utils.PrintList(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void PrintList_IntList_Null_ReturnsEmpty()
        {
            List<int> input = null;

            string result = Utils.PrintList(input);

            Assert.Equal("", result);
        }

        #endregion

        #region CopyList Tests

        [Fact]
        public void CopyList_CreatesIndependentCopy()
        {
            List<int> original = new List<int> { 1, 2, 3 };

            List<int> copy = Utils.CopyList(original);

            Assert.Equal(original, copy);
            copy.Add(4);
            Assert.NotEqual(original.Count, copy.Count);
        }

        [Fact]
        public void CopyList_EmptyList_ReturnsEmptyList()
        {
            List<int> original = new List<int>();

            List<int> copy = Utils.CopyList(original);

            Assert.NotNull(copy);
            Assert.Empty(copy);
        }

        [Fact]
        public void CopyList_Null_ReturnsNull()
        {
            List<int> original = null;

            List<int> copy = Utils.CopyList(original);

            Assert.Null(copy);
        }

        #endregion

        #region LowercaseStrings Tests

        [Fact]
        public void LowercaseStrings_ConvertsAllToLower()
        {
            string[] input = { "HELLO", "WoRLd", "test" };

            string[] result = Utils.LowercaseStrings(input);

            Assert.Equal(new[] { "hello", "world", "test" }, result);
        }

        [Fact]
        public void LowercaseStrings_EmptyArray_ReturnsEmptyArray()
        {
            string[] input = new string[0];

            string[] result = Utils.LowercaseStrings(input);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void LowercaseStrings_Null_ReturnsNull()
        {
            string[] input = null;

            string[] result = Utils.LowercaseStrings(input);

            Assert.Null(result);
        }

        #endregion

        #region TrimStringsAndRemoveEmpty Tests

        [Fact]
        public void TrimStringsAndRemoveEmpty_TrimsAndRemovesEmpty()
        {
            string[] input = { "  hello  ", "", "world", null, "  test" };

            string[] result = Utils.TrimStringsAndRemoveEmpty(input);

            Assert.Equal(new[] { "hello", "world", "test" }, result);
        }

        [Fact]
        public void TrimStringsAndRemoveEmpty_AllEmpty_ReturnsEmpty()
        {
            string[] input = { "", null, "" };

            string[] result = Utils.TrimStringsAndRemoveEmpty(input);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void TrimStringsAndRemoveEmpty_NoEmptyStrings_TrimsAll()
        {
            string[] input = { "  a  ", " b ", "c" };

            string[] result = Utils.TrimStringsAndRemoveEmpty(input);

            Assert.Equal(new[] { "a", "b", "c" }, result);
        }

        [Fact]
        public void TrimStringsAndRemoveEmpty_Null_ReturnsNull()
        {
            string[] input = null;

            string[] result = Utils.TrimStringsAndRemoveEmpty(input);

            Assert.Null(result);
        }

        #endregion

        #region FixComparators Tests

        [Fact]
        public void FixComparators_ReplacesHtmlEntities()
        {
            string[] input = { "5&gt;3", "2&lt;10", "both&gt;&lt;symbols" };

            string[] result = Utils.FixComparators(input);

            Assert.Equal(new[] { "5>3", "2<10", "both><symbols" }, result);
        }

        [Fact]
        public void FixComparators_NoEntities_ReturnsUnchanged()
        {
            string[] input = { "hello", "world" };

            string[] result = Utils.FixComparators(input);

            Assert.Equal(input, result);
        }

        [Fact]
        public void FixComparators_Null_ReturnsNull()
        {
            string[] input = null;

            string[] result = Utils.FixComparators(input);

            Assert.Null(result);
        }

        #endregion

        #region SortedListDisplayText Tests

        [Fact]
        public void SortedListDisplayText_SortsAlphabetically()
        {
            List<string> input = new List<string> { "zebra", "apple", "mango", "banana" };

            string result = Utils.sortedListDisplayText(input);

            Assert.Equal("apple, banana, mango, zebra", result);
        }

        [Fact]
        public void SortedListDisplayText_EmptyList_ReturnsEmpty()
        {
            List<string> input = new List<string>();

            string result = Utils.sortedListDisplayText(input);

            Assert.Equal("", result);
        }

        [Fact]
        public void SortedListDisplayText_SingleItem_ReturnsSingleItem()
        {
            List<string> input = new List<string> { "solo" };

            string result = Utils.sortedListDisplayText(input);

            Assert.Equal("solo", result);
        }

        #endregion
    }
}
