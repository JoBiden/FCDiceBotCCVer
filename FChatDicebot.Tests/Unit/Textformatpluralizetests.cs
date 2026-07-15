using FChatDicebot;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for TextFormat.PluralizeNoun — added for feedback 6a5580c4, where the
    /// both-parties rate-limit sub-note rendered "their last kisss" by blindly appending
    /// an 's' to the interaction type.
    /// </summary>
    public class TextFormatPluralizeTests
    {
        [Theory]
        [InlineData("kiss", "kisses")]
        [InlineData("bully", "bullies")]
        [InlineData("cuddle", "cuddles")]
        [InlineData("lick", "licks")]
        [InlineData("lap", "laps")]
        [InlineData("sit", "sits")]
        [InlineData("boop", "boops")]
        [InlineData("toy", "toys")]     // vowel + y keeps the y
        [InlineData("boobhat", "boobhats")]
        public void PluralizeNoun_CommonInteractionTypes(string noun, string expected)
        {
            Assert.Equal(expected, TextFormat.PluralizeNoun(noun));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void PluralizeNoun_NullOrEmpty_ReturnsInputUnchanged(string noun)
        {
            Assert.Equal(noun, TextFormat.PluralizeNoun(noun));
        }
    }
}
