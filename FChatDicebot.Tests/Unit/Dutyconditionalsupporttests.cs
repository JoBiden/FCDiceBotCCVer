using FChatDicebot.BotCommands.Support;
using FChatDicebot.Model;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Unit tests for DutyConditionalSupport, the shared prefix+key parser behind the
    /// duty-choice filtering in !work and !volunteer. Covers the formats present in the
    /// live Duties collection ("none", "None", "jobadventurer", "trndeepthroat",
    /// "monflight") plus the malformed shapes that used to crash or silently hide choices.
    /// </summary>
    public class DutyConditionalSupportTests
    {
        static Conditional C(string type, int value = 0)
        {
            return new Conditional { type = type, value = value };
        }

        // --- Kind: three-letter prefix, case-insensitive ---

        [Theory]
        [InlineData("none", "non")]
        [InlineData("None", "non")]   // live data contains both casings
        [InlineData("NONE", "non")]
        [InlineData("jobadventurer", "job")]
        [InlineData("Jobadventurer", "job")]
        [InlineData("trndeepthroat", "trn")]
        [InlineData("curgold", "cur")]
        [InlineData("monflight", "mon")]
        public void Kind_ParsesPrefixCaseInsensitively(string type, string expected)
        {
            Assert.Equal(expected, DutyConditionalSupport.Kind(C(type)));
        }

        [Fact]
        public void Kind_NullConditional_IsUnconditional()
        {
            Assert.Equal("non", DutyConditionalSupport.Kind(null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Kind_MissingType_IsUnconditional(string type)
        {
            Assert.Equal("non", DutyConditionalSupport.Kind(C(type)));
        }

        [Theory]
        [InlineData("no")]
        [InlineData("x")]
        public void Kind_TypeShorterThanPrefix_MatchesNoKind(string type)
        {
            Assert.Equal("", DutyConditionalSupport.Kind(C(type)));
        }

        [Fact]
        public void Kind_TrimsWhitespaceBeforeParsing()
        {
            Assert.Equal("job", DutyConditionalSupport.Kind(C("  jobmaid  ")));
        }

        // --- Key: everything after the prefix ---

        [Theory]
        [InlineData("jobadventurer", "adventurer")]
        [InlineData("trndeepthroat", "deepthroat")]
        [InlineData("curgold", "gold")]
        [InlineData("monflight", "flight")]
        [InlineData("  monflight  ", "flight")]
        public void Key_ReturnsEverythingAfterThePrefix(string type, string expected)
        {
            Assert.Equal(expected, DutyConditionalSupport.Key(C(type)));
        }

        [Theory]
        [InlineData("non")]
        [InlineData("")]
        [InlineData(null)]
        public void Key_NoKeyPortion_ReturnsEmpty(string type)
        {
            Assert.Equal("", DutyConditionalSupport.Key(C(type)));
        }

        [Fact]
        public void Key_IsMechanicalAfterPrefix_OnlyMeaningfulForKeyedKinds()
        {
            // Key() blindly returns everything after the 3-char prefix; callers only
            // consult it for job/trn/cur/mon kinds, so "none" yielding "e" is harmless.
            Assert.Equal("e", DutyConditionalSupport.Key(C("none")));
            Assert.Equal("non", DutyConditionalSupport.Kind(C("none")));
        }

        [Fact]
        public void Key_NullConditional_ReturnsEmpty()
        {
            Assert.Equal("", DutyConditionalSupport.Key(null));
        }
    }
}
