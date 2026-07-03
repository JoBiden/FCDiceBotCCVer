using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Regression test for L15's "break" case: GetInteractionDescription rendered
    /// "X utterly broke ." — missing both the recipient's name and the broken bodypart.
    /// </summary>
    [Collection("Database")]
    public class GetInteractionDescriptionTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public GetInteractionDescriptionTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        public void Dispose()
        {
            _fixture.Reset();
        }

        [Fact]
        public void GetInteractionDescription_Break_IncludesRecipientAndBodypart()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var interaction = new Interaction
            {
                initiator = "Alice",
                recipient = "Bob",
                type = "break",
                identifier = "arm",
                investmentLevel = "consequence",
                interactionTime = DateTime.UtcNow
            };

            string result = Utils.GetInteractionDescription(interaction);

            Assert.Contains("Alice", result);
            Assert.Contains("Bob", result);
            Assert.Contains("arm", result);
        }
    }
}
