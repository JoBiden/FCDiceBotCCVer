using System.Collections.Generic;
using FChatDicebot;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for RecipientResolver — bare-name targeting, which lets residents type
    /// "!cuddle bob smith" instead of reaching for a [user] tag.
    /// </summary>
    public class RecipientResolverTests
    {
        private static List<ProfileName> Roster(params string[] userNames)
        {
            var names = new List<ProfileName>();
            foreach (string userName in userNames)
                names.Add(new ProfileName { userName = userName, displayName = userName });
            return names;
        }

        private static string[] Terms(params string[] terms)
        {
            return terms;
        }

        #region Leaving well enough alone

        [Fact]
        public void Resolve_WithExplicitUserTag_LeavesTermsUntouched()
        {
            string[] raw = Terms("[user]Bob", "Smith[/user]", "lust");

            var result = RecipientResolver.Resolve(raw, new[] { "lust" }, Roster("Bob Smith", "Queen Contract"));

            Assert.Null(result.ResolvedName);
            Assert.Same(raw, result.Terms);
        }

        [Fact]
        public void Resolve_WithUnknownName_LeavesTermsUntouched()
        {
            string[] raw = Terms("nobody", "here");

            var result = RecipientResolver.Resolve(raw, null, Roster("Bob Smith"));

            Assert.Null(result.ResolvedName);
            Assert.Same(raw, result.Terms);
        }

        [Fact]
        public void Resolve_WithNoTerms_LeavesTermsUntouched()
        {
            var result = RecipientResolver.Resolve(Terms(), null, Roster("Bob Smith"));

            Assert.Null(result.ResolvedName);
        }

        [Fact]
        public void Resolve_WithEmptyRoster_LeavesTermsUntouched()
        {
            string[] raw = Terms("bob");

            var result = RecipientResolver.Resolve(raw, null, new List<ProfileName>());

            Assert.Null(result.ResolvedName);
            Assert.Same(raw, result.Terms);
        }

        #endregion

        #region Splicing the tag in

        [Fact]
        public void Resolve_WithBareSingleWordName_SplicesUserTag()
        {
            var result = RecipientResolver.Resolve(Terms("bob"), null, Roster("Bob"));

            Assert.Equal("Bob", result.ResolvedName);
            Assert.Equal(new[] { "[user]Bob[/user]" }, result.Terms);
        }

        [Fact]
        public void Resolve_WithBareMultiWordName_TokenizesTagTheWayFChatDoes()
        {
            var result = RecipientResolver.Resolve(Terms("bob", "smith"), null, Roster("Bob Smith"));

            Assert.Equal("Bob Smith", result.ResolvedName);
            Assert.Equal(new[] { "[user]Bob", "Smith[/user]" }, result.Terms);
        }

        [Fact]
        public void Resolve_WithWrongCasing_ResolvesToCanonicalCasing()
        {
            // The whole point: MonDB.getProfile is an exact match, so the spliced tag has to
            // carry the registered spelling rather than what the resident typed.
            var result = RecipientResolver.Resolve(Terms("QUEEN", "contract"), null, Roster("Queen Contract"));

            Assert.Equal("Queen Contract", result.ResolvedName);
            Assert.Equal(new[] { "[user]Queen", "Contract[/user]" }, result.Terms);
        }

        [Fact]
        public void Resolve_PreservesSurroundingTerms()
        {
            var result = RecipientResolver.Resolve(
                Terms("bob", "smith", "50", "gold"),
                new[] { "gold", "nothing" },
                Roster("Bob Smith"));

            Assert.Equal(new[] { "[user]Bob", "Smith[/user]", "50", "gold" }, result.Terms);
        }

        [Fact]
        public void Resolve_WithNameAfterOtherArguments_SplicesInPlace()
        {
            // !sell 5 milk bob smith
            var result = RecipientResolver.Resolve(
                Terms("5", "milk", "bob", "smith"),
                new[] { "milk" },
                Roster("Bob Smith"));

            Assert.Equal(new[] { "5", "milk", "[user]Bob", "Smith[/user]" }, result.Terms);
        }

        #endregion

        #region Not-a-name terms

        [Fact]
        public void Resolve_IgnoresIdentifierTerms()
        {
            // !dose bob smith lust — "lust" is a vice, not part of the name.
            var result = RecipientResolver.Resolve(
                Terms("bob", "smith", "lust"),
                new[] { "lust", "sloth" },
                Roster("Bob Smith"));

            Assert.Equal("Bob Smith", result.ResolvedName);
            Assert.Equal(new[] { "[user]Bob", "Smith[/user]", "lust" }, result.Terms);
        }

        [Fact]
        public void Resolve_IgnoresQuotedText()
        {
            // !entitle bob smith "Best Boy"
            var result = RecipientResolver.Resolve(
                Terms("bob", "smith", "\"Best", "Boy\""),
                null,
                Roster("Bob Smith"));

            Assert.Equal("Bob Smith", result.ResolvedName);
            Assert.Equal(new[] { "[user]Bob", "Smith[/user]", "\"Best", "Boy\"" }, result.Terms);
        }

        [Fact]
        public void Resolve_IgnoresEiconSpans()
        {
            var result = RecipientResolver.Resolve(
                Terms("[eicon]heart[/eicon]", "bob"),
                null,
                Roster("Bob"));

            Assert.Equal("Bob", result.ResolvedName);
            Assert.Equal(new[] { "[eicon]heart[/eicon]", "[user]Bob[/user]" }, result.Terms);
        }

        [Fact]
        public void Resolve_IgnoresReservedKeywords()
        {
            // A resident registered as "Random" must not swallow "!breed Bob random".
            var result = RecipientResolver.Resolve(Terms("random"), null, Roster("Random"));

            Assert.Null(result.ResolvedName);
        }

        [Fact]
        public void Resolve_DoesNotJoinAcrossNonNameTerms()
        {
            // "ann" and "bell" sit either side of an amount, so they are not one name — an
            // implementation that joined every leftover would resolve "Ann Bell" here.
            var result = RecipientResolver.Resolve(Terms("ann", "50", "bell"), null, Roster("Ann Bell", "Ann"));

            Assert.Equal("Ann", result.ResolvedName);
            Assert.Equal(new[] { "[user]Ann[/user]", "50", "bell" }, result.Terms);
        }

        #endregion

        #region Display names and prefixes

        [Fact]
        public void Resolve_MatchesRenamedResidentByCurrentDisplayName()
        {
            var roster = new List<ProfileName>
            {
                new ProfileName { userName = "Bob", displayName = "[s]Bob[/s] Steve" },
            };

            var result = RecipientResolver.Resolve(Terms("steve"), null, roster);

            // Resolves to the login name, which is what every downstream lookup keys on.
            Assert.Equal("Bob", result.ResolvedName);
        }

        [Fact]
        public void Resolve_MatchesUniquePrefix()
        {
            var result = RecipientResolver.Resolve(Terms("queen"), null, Roster("Queen Contract", "Bob Smith"));

            Assert.Equal("Queen Contract", result.ResolvedName);
        }

        [Fact]
        public void Resolve_PrefersWholeNameOverPrefixOfAnother()
        {
            var result = RecipientResolver.Resolve(Terms("bob"), null, Roster("Bobby Tables", "Bob"));

            Assert.Equal("Bob", result.ResolvedName);
        }

        [Fact]
        public void Resolve_WithShortPrefix_DoesNotGuess()
        {
            var result = RecipientResolver.Resolve(Terms("qu"), null, Roster("Queen Contract"));

            Assert.Null(result.ResolvedName);
        }

        [Fact]
        public void Resolve_WithAmbiguousPrefix_ReportsCandidatesAndLeavesTermsUntouched()
        {
            string[] raw = Terms("queen");

            var result = RecipientResolver.Resolve(raw, null, Roster("Queen Contract", "Queen Bee"));

            Assert.True(result.IsAmbiguous);
            Assert.Equal("queen", result.TypedText);
            Assert.Equal(new[] { "Queen Bee", "Queen Contract" }, result.Candidates.ToArray());
            Assert.Same(raw, result.Terms);
        }

        [Fact]
        public void Resolve_WithTwoResidentsSharingADisplayName_IsAmbiguous()
        {
            var roster = new List<ProfileName>
            {
                new ProfileName { userName = "Bob", displayName = "[s]Bob[/s] Kitten" },
                new ProfileName { userName = "Alice", displayName = "[s]Alice[/s] Kitten" },
            };

            var result = RecipientResolver.Resolve(Terms("kitten"), null, roster);

            Assert.True(result.IsAmbiguous);
            Assert.Equal(2, result.Candidates.Count);
        }

        #endregion

        #region Group casuals

        [Fact]
        public void Resolve_WithSeveralBareNames_SplicesATagForEach()
        {
            // !cuddle bob smith jane doe
            var result = RecipientResolver.Resolve(
                Terms("bob", "smith", "jane", "doe"),
                null,
                Roster("Bob Smith", "Jane Doe"));

            Assert.Equal(new[] { "Bob Smith", "Jane Doe" }, result.ResolvedNames.ToArray());
            Assert.Equal(
                new[] { "[user]Bob", "Smith[/user]", "[user]Jane", "Doe[/user]" },
                result.Terms);
        }

        [Fact]
        public void Resolve_WithSeveralBareNames_RequiresLaterOnesToBeComplete()
        {
            // A prefix is a convenience for naming your target; it shouldn't quietly pull an
            // extra person into a group interaction.
            var result = RecipientResolver.Resolve(Terms("bob", "jane"), null, Roster("Bob", "Jane Doe"));

            Assert.Equal(new[] { "Bob" }, result.ResolvedNames.ToArray());
            Assert.Equal(new[] { "[user]Bob[/user]", "jane" }, result.Terms);
        }

        #endregion

        #region Names we can't place

        [Fact]
        public void Resolve_WithUnplaceableWords_ReportsWhatWasTyped()
        {
            var result = RecipientResolver.Resolve(Terms("bbo", "smith"), null, Roster("Bob Smith"));

            Assert.Null(result.ResolvedName);
            Assert.Equal("bbo smith", result.UnresolvedText);
        }

        [Fact]
        public void Resolve_WithNoLeftoverWords_ReportsNothingUnresolved()
        {
            // "!dossier" on its own, or "!sell 5 milk" — no name was attempted.
            var result = RecipientResolver.Resolve(Terms("5", "milk"), new[] { "milk" }, Roster("Bob Smith"));

            Assert.Null(result.UnresolvedText);
        }

        [Fact]
        public void Resolve_WithOneNameFound_DoesNotReportTrailingWordsAsUnresolved()
        {
            var result = RecipientResolver.Resolve(Terms("bob", "smith", "gibberish"), null, Roster("Bob Smith"));

            Assert.Equal("Bob Smith", result.ResolvedName);
            Assert.Null(result.UnresolvedText);
        }

        [Fact]
        public void SuggestSimilarNames_FindsTheLikelyTypo()
        {
            var suggestions = RecipientResolver.SuggestSimilarNames(
                "bbo smith", Roster("Bob Smith", "Queen Contract"), 3);

            Assert.Equal(new[] { "Bob Smith" }, suggestions.ToArray());
        }

        [Fact]
        public void SuggestSimilarNames_LeavesOutDistantNames()
        {
            var suggestions = RecipientResolver.SuggestSimilarNames(
                "zzzzzzzz", Roster("Bob Smith", "Queen Contract"), 3);

            Assert.Empty(suggestions);
        }

        #endregion

        #region StripDisplayName

        [Theory]
        [InlineData("[s]Bob[/s] Steve", "Steve")]
        [InlineData("[b]Queen Contract[/b]", "Queen Contract")]
        [InlineData("Plain Name", "Plain Name")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void StripDisplayName_ReducesToPlainTypableText(string stored, string expected)
        {
            Assert.Equal(expected, RecipientResolver.StripDisplayName(stored));
        }

        #endregion
    }
}
