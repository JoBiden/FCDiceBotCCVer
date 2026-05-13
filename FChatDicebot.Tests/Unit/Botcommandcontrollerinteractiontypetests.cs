using FChatDicebot;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for BotCommandController.GetInteractionTypeFromCommandTerms method
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
            string[] terms = { "!pledge", "[user]Bob[/user]", "feed" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("feed", result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_MultiWordUsername_ReturnsInteractionType()
        {
            string[] terms = { "!pledge", "[user]Bob", "Smith[/user]", "mark" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("mark", result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_AfterQuotedText_ReturnsInteractionType()
        {
            string[] terms = { "!command", "\"some", "quoted", "text\"", "feed" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("feed", result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_AfterEIcon_ReturnsInteractionType()
        {
            string[] terms = { "!command", "[eicon]heart[/eicon]", "entitle" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("entitle", result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_MixedCase_ReturnsLowercase()
        {
            string[] terms = { "!pledge", "[user]Bob[/user]", "FEED" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("feed", result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_NotEnoughTerms_ReturnsNull()
        {
            string[] terms = { "!pledge", "[user]Bob[/user]" };

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
            string[] terms = { "!fulfill", "[user]Alice[/user]", "monsterize" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("monsterize", result);
        }

        [Fact]
        public void GetInteractionTypeFromCommandTerms_WithMultipleUserTags_ReturnsFirstAfterTags()
        {
            string[] terms = { "!command", "[user]Bob[/user]", "[user]Alice[/user]", "consume" };

            string result = _controller.GetInteractionTypeFromCommandTerms(terms);

            Assert.Equal("consume", result);
        }

        #endregion
    }
}
