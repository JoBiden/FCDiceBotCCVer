using FChatDicebot;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for Utils.cs date and time functions
    /// </summary>
    public class UtilsDateTimeTests
    {
        #region GetTimeSpanPrint Tests

        [Fact]
        public void GetTimeSpanPrint_FormatsTimeSpanCorrectly()
        {
            TimeSpan timeSpan = new TimeSpan(2, 3, 15, 30); // 2 days, 3 hours, 15 minutes, 30 seconds

            string result = Utils.GetTimeSpanPrint(timeSpan);

            Assert.Equal("2 days, 3 hours, 15 minutes, 30 seconds", result);
        }

        [Fact]
        public void GetTimeSpanPrint_ZeroTimeSpan_FormatsCorrectly()
        {
            TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0);

            string result = Utils.GetTimeSpanPrint(timeSpan);

            Assert.Equal("0 days, 0 hours, 0 minutes, 0 seconds", result);
        }

        [Fact]
        public void GetTimeSpanPrint_OnlySeconds_FormatsCorrectly()
        {
            TimeSpan timeSpan = new TimeSpan(0, 0, 0, 45);

            string result = Utils.GetTimeSpanPrint(timeSpan);

            Assert.Equal("0 days, 0 hours, 0 minutes, 45 seconds", result);
        }

        [Fact]
        public void GetTimeSpanPrint_LargeTimeSpan_FormatsCorrectly()
        {
            TimeSpan timeSpan = new TimeSpan(100, 23, 59, 59);

            string result = Utils.GetTimeSpanPrint(timeSpan);

            Assert.Equal("100 days, 23 hours, 59 minutes, 59 seconds", result);
        }

        #endregion

        #region AddDateToFileName Tests

        [Fact]
        public void AddDateToFileName_AddsDateToSimpleFilename()
        {
            // Note: This test depends on the current date, so we'll just verify the format
            string fileName = "logfile.txt";

            string result = Utils.AddDateToFileName(fileName);

            // Should be in format: logfile_M_D_YYYY.txt
            Assert.StartsWith("logfile_", result);
            Assert.EndsWith(".txt", result);
            Assert.Contains("_", result);
        }

        [Fact]
        public void AddDateToFileName_NoExtension_AddsDateAndDefaultExtension()
        {
            string fileName = "logfile";

            string result = Utils.AddDateToFileName(fileName);

            // Should be in format: logfile_M_D_YYYY.txt
            Assert.StartsWith("logfile_", result);
            Assert.EndsWith(".txt", result);
        }

        [Fact]
        public void AddDateToFileName_DifferentExtension_PreservesExtension()
        {
            string fileName = "data.json";

            string result = Utils.AddDateToFileName(fileName);

            // Should preserve the .json extension
            Assert.StartsWith("data_", result);
            Assert.EndsWith(".json", result);
        }

        [Fact]
        public void AddDateToFileName_ContainsDateComponents()
        {
            DateTime now = DateTime.Now.Date;
            string fileName = "test.txt";

            string result = Utils.AddDateToFileName(fileName);

            // Verify it contains the month, day, and year
            Assert.Contains(now.Month.ToString(), result);
            Assert.Contains(now.Day.ToString(), result);
            Assert.Contains(now.Year.ToString(), result);
        }

        #endregion

        #region GetDaySuffix Tests

        [Theory]
        [InlineData(1, "st")]
        [InlineData(21, "st")]
        [InlineData(31, "st")]
        public void GetDaySuffix_EndsInOne_ReturnsSt(int day, string expected)
        {
            string result = Utils.GetDaySuffix(day);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(2, "nd")]
        [InlineData(22, "nd")]
        public void GetDaySuffix_EndsInTwo_ReturnsNd(int day, string expected)
        {
            string result = Utils.GetDaySuffix(day);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(3, "rd")]
        [InlineData(23, "rd")]
        public void GetDaySuffix_EndsInThree_ReturnsRd(int day, string expected)
        {
            string result = Utils.GetDaySuffix(day);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(4, "th")]
        [InlineData(5, "th")]
        [InlineData(6, "th")]
        [InlineData(7, "th")]
        [InlineData(8, "th")]
        [InlineData(9, "th")]
        [InlineData(10, "th")]
        [InlineData(11, "th")]
        [InlineData(12, "th")]
        [InlineData(13, "th")]
        [InlineData(14, "th")]
        [InlineData(15, "th")]
        [InlineData(16, "th")]
        [InlineData(17, "th")]
        [InlineData(18, "th")]
        [InlineData(19, "th")]
        [InlineData(20, "th")]
        [InlineData(24, "th")]
        [InlineData(25, "th")]
        [InlineData(26, "th")]
        [InlineData(27, "th")]
        [InlineData(28, "th")]
        [InlineData(29, "th")]
        [InlineData(30, "th")]
        public void GetDaySuffix_OtherDays_ReturnsTh(int day, string expected)
        {
            string result = Utils.GetDaySuffix(day);

            Assert.Equal(expected, result);
        }

        #endregion
    }
}
