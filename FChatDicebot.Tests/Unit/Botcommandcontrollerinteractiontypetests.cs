using FChatDicebot;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for BotCommandController.GetInteractionTypeFromCommandTerms method.
    ///
    /// Terms are passed the way production passes them: BotMain.SeparateCommandTerms strips
    /// the command name before dispatch, so the array starts at the first argument. These
    /// tests used to include a leading "!pledge" term that no caller ever sends, which is
    /// what hid the single-word-name bug (a one-slot [user] tag left the interaction type in
    /// the position the parser was skipping as "the command name").
    /// </summary>
    public class BotCommandControllerInteractionTypeTests
    {
        private readonly BotCommandController _controller;

        public BotCommandControllerInteractionTypeTests()
        {
            // We don't need a real BotMain instance for these tests
            _controller = new BotCommandController(null);
        }

        #region GetInteractionTypeFromCommandTerms Tests

        [Fact]
        public void GetInteractionTypeFromCommandTerms_SimpleCase_ReturnsInteractionType()
        {
            string[] terms = { "[user]Bob[/user]", "feed" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("feed", result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_MultiWordUsername_ReturnsInteractionType()
        {
            string[] terms = { "[user]Bob", "Smith[/user]", "mark" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("mark", result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_AfterQuotedText_ReturnsInteractionType()
        {
            string[] terms = { "\"some", "quoted", "text\"", "feed" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("feed", result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_AfterEIcon_ReturnsInteractionType()
        {
            string[] terms = { "[eicon]heart[/eicon]", "entitle" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("entitle", result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_MixedCase_ReturnsLowercase()
        {
            string[] terms = { "[user]Bob[/user]", "FEED" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("feed", result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_OnlyUserTag_ReturnsNull()
        {
            string[] terms = { "[user]Bob[/user]" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Null(result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_NullTerms_ReturnsNull()
        {
            string[] terms = null;

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Null(result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_EmptyArray_ReturnsNull()
        {
            string[] terms = { };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Null(result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_ComplexCase_ReturnsFirstPlainText()
        {
            string[] terms = { "[user]Alice[/user]", "monsterize" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("monsterize", result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_WithMultipleUserTags_ReturnsFirstAfterTags()
        {
            string[] terms = { "[user]Bob[/user]", "[user]Alice[/user]", "consume" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("consume", result);
        }

        #endregion
    }
}
