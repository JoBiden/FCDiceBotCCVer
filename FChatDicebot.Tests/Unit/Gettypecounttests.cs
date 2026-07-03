using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Fixtures;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Regression test for M4: GetTypeCount lowercased identifierType before querying, but
    /// Interaction.type is stored with its canonical case (e.g. "paymentGive") and Mongo
    /// string equality is case-sensitive, so camelCase types could never match — silently
    /// zeroing out any specialist count derived from them.
    /// </summary>
    [Collection("Database")]
    public class GetTypeCountTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public GetTypeCountTests(TestDatabaseFixture fixture)
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
        public void GetTypeCount_CamelCaseType_MatchesStoredInteractions()
        {
            _database.AddInteraction(new Interaction
            {
                initiator = "Alice",
                recipient = "Bob",
                type = "paymentGive",
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow
            });

            long count = _database.GetTypeCount("Alice", "paymentGive", "initiator");

            Assert.Equal(1, count);
        }

        [Fact]
        public void GetTypeCount_LowercaseTypeAgainstCamelCaseStorage_WouldHaveMissed()
        {
            _database.AddInteraction(new Interaction
            {
                initiator = "Alice",
                recipient = "Bob",
                type = "paymentGive",
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow
            });

            // Sanity check that Mongo string equality really is case-sensitive here — if
            // this ever starts passing, GetTypeCount's own case-preservation is moot.
            long lowercasedQuery = _database.GetTypeCount("Alice", "paymentgive", "initiator");

            Assert.Equal(0, lowercasedQuery);
        }
    }
}
