using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Identifier names are unique only WITHIN a category — the live catalog carries both an
    /// attire "bimbo" and a curse "bimbo" — so any caller that means "the X named N" has to
    /// say which X. These tests pin the category-scoped lookup that makes that possible, and
    /// the !curse regression that made the collision visible: the name-only fetch returned the
    /// attire document, so the consent prompt quoted the wrong description and the recipient's
    /// !consent failed the curse category gate with "requires an identifier of type curse".
    /// </summary>
    [Collection("Database")]
    public class IdentifierCategoryLookupTests
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        private const string AttireDescription =
            "Like, there's a lot of like, pink and stuff for bimbo at- att- clothes. Probably plastic surgery too.";
        private const string CurseDescription = "This curse, like... umm... yeah!";

        public IdentifierCategoryLookupTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;

            // Seeded in catalog order: the attire "bimbo" predates the curse system, so it is
            // the document a name-only lookup finds first.
            _fixture.SeedIdentifier(new Identifier
            {
                type = "bimbo",
                description = AttireDescription,
                categories = new[] { "attire" },
            });
            _fixture.SeedIdentifier(new Identifier
            {
                type = "bimbo",
                description = CurseDescription,
                categories = new[] { CurseProcessor.CurseCategory },
            });
        }

        [Fact]
        public void GetIdentifier_WithCategory_ReturnsTheDocumentInThatCategory()
        {
            Assert.Equal(CurseDescription, _database.GetIdentifier("bimbo", CurseProcessor.CurseCategory).description);
            Assert.Equal(AttireDescription, _database.GetIdentifier("bimbo", "attire").description);
        }

        [Fact]
        public void GetIdentifier_WithCategory_ReturnsNullWhenTheNameExistsInAnotherCategoryOnly()
        {
            Assert.Null(_database.GetIdentifier("bimbo", "scent"));
            Assert.Null(_database.GetIdentifier("nosuchidentifier", CurseProcessor.CurseCategory));
        }

        [Fact]
        public void ValidateInteraction_CurseNameSharedWithAnotherCategory_IsAccepted()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var result = new CurseProcessor(_database).ValidateInteraction("Alice", "Bob", "bimbo");

            Assert.True(result.IsValid, "collision-shadowed curse should validate: " + result.ErrorMessage);
        }

        [Fact]
        public void ConsentWarning_CurseNameSharedWithAnotherCategory_QuotesTheCurseDescription()
        {
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            string warning = new CurseProcessor(_database).GetConsentWarning(
                _database.GetProfile("Alice"), _database.GetProfile("Bob"), "bimbo");

            Assert.Contains(CurseDescription, warning);
            Assert.DoesNotContain(AttireDescription, warning);
        }
    }
}
