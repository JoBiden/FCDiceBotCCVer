using FChatDicebot;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Covers <see cref="TextFormat.ApplyNumberCommas"/>, which every private message now
    /// runs through unconditionally (BotMain.SendPrivateMessage). It used to comma-ise every
    /// digit run in the message via a bare \d+, which corrupted anything that merely contained
    /// digits — eicon names, character names, dates. These tests pin the "standalone numbers
    /// only" rule that makes it safe to apply everywhere.
    /// </summary>
    public class TextFormatNumberCommasTests
    {
        [Theory]
        [InlineData("14837 gold", "14,837 gold")]
        [InlineData("You have 1000 chips.", "You have 1,000 chips.")]
        [InlineData("Total: 1234567", "Total: 1,234,567")]
        public void FormatsStandaloneNumbers(string input, string expected)
        {
            Assert.Equal(expected, TextFormat.ApplyNumberCommas(input));
        }

        [Theory]
        [InlineData("999")]
        [InlineData("Titles earned: 34")]
        [InlineData("")]
        public void LeavesShortNumbersUnchanged(string input)
        {
            Assert.Equal(input, TextFormat.ApplyNumberCommas(input));
        }

        [Theory]
        // An eicon whose name contains digits must survive intact or the icon breaks.
        [InlineData("[eicon]kanna1000[/eicon]")]
        // F-Chat character names may contain digits.
        [InlineData("[user]Robot2000[/user] arrived.")]
        // Dates would otherwise render as "2,026-05-26".
        [InlineData("2026-05-26: goblin brood of 3")]
        // Decimals and URLs.
        [InlineData("a rate of 1.5000 per day")]
        [InlineData("see example.com/page/1234 for more")]
        // Digits inside a BBCode tag's attributes.
        [InlineData("[color=1234]text[/color]")]
        // A name that is entirely numeric still has to survive — the digits touch only
        // brackets, so the standalone rule can't tell it from a figure on its own.
        [InlineData("[eicon]1000[/eicon]")]
        [InlineData("[user]4815162342[/user]")]
        [InlineData("[Eicon]1000[/Eicon]")]
        public void LeavesEmbeddedDigitsUnchanged(string input)
        {
            Assert.Equal(input, TextFormat.ApplyNumberCommas(input));
        }

        [Fact]
        public void StillFormatsNumbersInsideFormattingTags()
        {
            // [b] and friends wrap presentation, not an identifier, so a figure inside one
            // is a real number and should format.
            Assert.Equal("[b]14,837[/b] gold", TextFormat.ApplyNumberCommas("[b]14837[/b] gold"));
        }

        [Fact]
        public void FormatsStandaloneNumberAlongsideProtectedText()
        {
            // The protected constructs and a real figure coexist in the same message.
            string input = "[user]Robot2000[/user] earned 14837 gold on 2026-05-26.";
            string expected = "[user]Robot2000[/user] earned 14,837 gold on 2026-05-26.";

            Assert.Equal(expected, TextFormat.ApplyNumberCommas(input));
        }

        [Fact]
        public void OverlongDigitRunIsLeftAloneRatherThanThrowing()
        {
            // Longer than a long can hold; previously long.Parse threw and the catch-all
            // discarded formatting for the entire message.
            string input = "count 12345678901234567890 here and 9999 there";

            Assert.Equal("count 12345678901234567890 here and 9,999 there",
                TextFormat.ApplyNumberCommas(input));
        }
    }
}
