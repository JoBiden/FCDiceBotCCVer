using FChatDicebot.Model;
using Xunit;

namespace FChatDicebot.Tests.Unit.Model
{
    /// <summary>
    /// Tests for the data-driven scent rendering helper. Each scent's noun-phrase form is
    /// chosen from the Identifier's categories, so admins can add new scents in the
    /// database with the right tag and get correct rendering without code changes.
    /// </summary>
    public class ScentTextTests
    {
        [Fact]
        public void ScentPhrase_PersonalCategory_UsesAppliedByPossessive()
        {
            var ident = new Identifier { type = "musk", categories = new[] { "personal", "scent" } };

            string phrase = ScentText.ScentPhrase(ident, "musk", "Alice");

            Assert.Equal("Alice's musk", phrase);
        }

        [Fact]
        public void ScentPhrase_PersonalCategory_NullAppliedBy_UsesSomeone()
        {
            var ident = new Identifier { type = "musk", categories = new[] { "personal", "scent" } };

            string phrase = ScentText.ScentPhrase(ident, "musk", appliedBy: null);

            Assert.Equal("someone's musk", phrase);
        }

        [Fact]
        public void ScentPhrase_ScentOfCategory_UsesAScentOf()
        {
            var ident = new Identifier { type = "lemonade", categories = new[] { "scentof", "scent" } };

            string phrase = ScentText.ScentPhrase(ident, "lemonade", "Alice");

            Assert.Equal("a scent of lemonade", phrase);
        }

        [Fact]
        public void ScentPhrase_DefaultCategory_UsesAScentSuffix()
        {
            var ident = new Identifier { type = "wood", categories = new[] { "scent" } };

            string phrase = ScentText.ScentPhrase(ident, "wood", "Alice");

            Assert.Equal("a wood scent", phrase);
        }

        [Fact]
        public void ScentPhrase_NullIdentifier_FallsBackToDefault()
        {
            string phrase = ScentText.ScentPhrase(scentIdentifier: null, scentName: "wood", appliedBy: "Alice");

            Assert.Equal("a wood scent", phrase);
        }

        [Fact]
        public void ScentPhrase_NullCategories_FallsBackToDefault()
        {
            var ident = new Identifier { type = "wood", categories = null };

            string phrase = ScentText.ScentPhrase(ident, "wood", "Alice");

            Assert.Equal("a wood scent", phrase);
        }

        [Fact]
        public void ScentPhrase_PersonalTakesPriorityOverScentOf()
        {
            // Hypothetical mis-tagged identifier: personal wins (iteration order honored).
            var ident = new Identifier { type = "weird", categories = new[] { "personal", "scentof", "scent" } };

            string phrase = ScentText.ScentPhrase(ident, "weird", "Alice");

            Assert.Equal("Alice's weird", phrase);
        }

        [Fact]
        public void ScentPhrase_FallsBackToIdentifierType_WhenScentNameMissing()
        {
            var ident = new Identifier { type = "musk", categories = new[] { "personal", "scent" } };

            string phrase = ScentText.ScentPhrase(ident, scentName: null, appliedBy: "Alice");

            Assert.Equal("Alice's musk", phrase);
        }

        [Theory]
        [InlineData("alice's musk", "Alice's musk")]
        [InlineData("a scent of lemonade", "A scent of lemonade")]
        [InlineData("a wood scent", "A wood scent")]
        public void Capitalize_LowercaseFirst_BecomesUpper(string input, string expected)
        {
            Assert.Equal(expected, ScentText.Capitalize(input));
        }

        [Fact]
        public void Capitalize_AlreadyUpper_Unchanged()
        {
            Assert.Equal("Alice's musk", ScentText.Capitalize("Alice's musk"));
        }

        [Fact]
        public void Capitalize_Empty_Unchanged()
        {
            Assert.Equal(string.Empty, ScentText.Capitalize(string.Empty));
        }

        [Fact]
        public void Capitalize_Null_ReturnsNull()
        {
            Assert.Null(ScentText.Capitalize(null));
        }
    }
}
