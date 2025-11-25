using FChatDicebot;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for Utils.TimeDifferenceText method
    /// </summary>
    public class UtilsTimeDifferenceTests
    {
        #region TimeDifferenceText Tests

        [Fact]
        public void TimeDifferenceText_LessThanOneMinute_ReturnsCorrectText()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 1, 1, 12, 0, 30); // 30 seconds

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("less than a minute", result);
        }

        [Fact]
        public void TimeDifferenceText_OneMinute_ReturnsSingular()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 1, 1, 12, 1, 0); // 1 minute

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("1 minute", result);
        }

        [Fact]
        public void TimeDifferenceText_MultipleMinutes_ReturnsPlural()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 1, 1, 12, 15, 0); // 15 minutes

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("15 minutes", result);
        }

        [Fact]
        public void TimeDifferenceText_OneHour_ReturnsSingular()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 1, 1, 13, 0, 0); // 1 hour

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("1 hour", result);
        }

        [Fact]
        public void TimeDifferenceText_MultipleHours_ReturnsPlural()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 1, 1, 17, 0, 0); // 5 hours

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("5 hours", result);
        }

        [Fact]
        public void TimeDifferenceText_OneDay_ReturnsSingular()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 1, 2, 12, 0, 0); // 1 day

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("1 day", result);
        }

        [Fact]
        public void TimeDifferenceText_MultipleDays_ReturnsPlural()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 1, 5, 12, 0, 0); // 4 days

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("4 days", result);
        }

        [Fact]
        public void TimeDifferenceText_OneWeek_ReturnsSingular()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 1, 8, 12, 0, 0); // 7 days = 1 week

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("1 week", result);
        }

        [Fact]
        public void TimeDifferenceText_MultipleWeeks_ReturnsPlural()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 1, 22, 12, 0, 0); // 21 days = 3 weeks

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("3 weeks", result);
        }

        [Fact]
        public void TimeDifferenceText_OneMonth_ReturnsSingular()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 2, 1, 12, 0, 0); // ~31 days = 1 month

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("1 month", result);
        }

        [Fact]
        public void TimeDifferenceText_MultipleMonths_ReturnsPlural()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 4, 1, 12, 0, 0); // ~91 days = 3 months

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("3 months", result);
        }

        [Fact]
        public void TimeDifferenceText_OneYear_ReturnsSingular()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2025, 1, 1, 12, 0, 0); // 365 days = 1 year

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("1 year", result);
        }

        [Fact]
        public void TimeDifferenceText_MultipleYears_ReturnsPlural()
        {
            DateTime start = new DateTime(2020, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 1, 1, 12, 0, 0); // 4 years

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("4 years", result);
        }

        [Fact]
        public void TimeDifferenceText_UsesLargestUnit_Days()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 1, 3, 15, 30, 0); // 2 days, 3.5 hours

            string result = Utils.TimeDifferenceText(start, end);

            // Should use days, not hours
            Assert.Equal("2 days", result);
        }

        [Fact]
        public void TimeDifferenceText_UsesLargestUnit_Months()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 3, 15, 12, 0, 0); // ~2.5 months

            string result = Utils.TimeDifferenceText(start, end);

            // Should use months, not weeks or days
            Assert.Equal("2 months", result);
        }

        [Fact]
        public void TimeDifferenceText_ZeroDifference_ReturnsLessThanMinute()
        {
            DateTime start = new DateTime(2024, 1, 1, 12, 0, 0);
            DateTime end = new DateTime(2024, 1, 1, 12, 0, 0); // 0 difference

            string result = Utils.TimeDifferenceText(start, end);

            Assert.Equal("less than a minute", result);
        }

        #endregion
    }
}
