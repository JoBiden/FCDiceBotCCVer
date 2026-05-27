using FChatDicebot.Model;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for the ParasiteText rendering helpers — name styling overrides, a/an article
    /// inflection, and verb agreement.
    /// </summary>
    public class ParasiteTextTests
    {
        [Theory]
        [InlineData("paraslime", "paraslime")]
        [InlineData("bimboslime", "bimboslime")]
        [InlineData("love", "love")]
        [InlineData("tentacles", "tentacles")]
        [InlineData("nymites", "nymites")]
        public void ParasiteName_DefaultParasites_RenderAsIs(string input, string expected)
        {
            Assert.Equal(expected, ParasiteText.ParasiteName(input));
        }

        [Fact]
        public void ParasiteName_LustLeeches_RendersStyledPhrase()
        {
            Assert.Equal("[color=purple]lust leeches[/color]", ParasiteText.ParasiteName("lustleeches"));
        }

        [Fact]
        public void ParasiteName_LustLeeches_CaseInsensitive()
        {
            Assert.Equal("[color=purple]lust leeches[/color]", ParasiteText.ParasiteName("LustLeeches"));
        }

        [Fact]
        public void ParasiteName_NullOrEmpty_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, ParasiteText.ParasiteName(null));
            Assert.Equal(string.Empty, ParasiteText.ParasiteName(""));
        }

        [Theory]
        [InlineData("paraslime", "a paraslime")]      // consonant-initial, singular
        [InlineData("bimboslime", "a bimboslime")]    // consonant-initial, singular
        [InlineData("love", "a love")]                 // consonant-initial, singular
        public void ParasiteNameWithArticle_Singular_GetsArticle(string input, string expected)
        {
            Assert.Equal(expected, ParasiteText.ParasiteNameWithArticle(input));
        }

        [Theory]
        [InlineData("tentacles", "tentacles")]
        [InlineData("nymites", "nymites")]
        public void ParasiteNameWithArticle_PluralShaped_NoArticle(string input, string expected)
        {
            Assert.Equal(expected, ParasiteText.ParasiteNameWithArticle(input));
        }

        [Fact]
        public void ParasiteNameWithArticle_LustLeeches_NoArticleAndStyled()
        {
            // Plural-shaped AND special-cased name — no article, styled phrase only.
            Assert.Equal("[color=purple]lust leeches[/color]", ParasiteText.ParasiteNameWithArticle("lustleeches"));
        }

        [Fact]
        public void ParasiteNameWithArticle_VowelInitialSingular_GetsAn()
        {
            // No vowel-initial parasites exist in the catalog yet, but the helper should be
            // ready for a future "ootheca" or similar custom addition.
            Assert.Equal("an ootheca", ParasiteText.ParasiteNameWithArticle("ootheca"));
        }

        [Theory]
        [InlineData("paraslime", "has")]
        [InlineData("love", "has")]
        [InlineData("tentacles", "have")]
        [InlineData("nymites", "have")]
        [InlineData("lustleeches", "have")]
        public void HasOrHave_FollowsPlurality(string input, string expected)
        {
            Assert.Equal(expected, ParasiteText.HasOrHave(input));
        }
    }
}
