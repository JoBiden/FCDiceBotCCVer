using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Regression tests for disposition #5 (Phase 7): a stale pre-refactor Profile document
    /// — one carrying a since-removed field, or an explicit null for a dictionary field added
    /// after that document was written — used to throw or NRE the first time anything touched
    /// it. Writes raw BsonDocuments directly (bypassing the C# model) to simulate documents
    /// this codebase's current Profile class was never used to create.
    /// </summary>
    [Collection("Database")]
    public class ProfileDeserializationTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ProfileDeserializationTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        public void Dispose()
        {
            _fixture.Reset();
        }

        private void InsertRawProfileDocument(BsonDocument doc)
        {
            var client = new MongoClient(TestConfiguration.TestConnectionString);
            var collection = client.GetDatabase(TestConfiguration.TestDatabaseName).GetCollection<BsonDocument>("RegisteredProfiles");
            collection.InsertOne(doc);
        }

        [Fact]
        public void GetProfile_DocumentWithUnknownExtraField_DoesNotThrow()
        {
            InsertRawProfileDocument(new BsonDocument
            {
                { "userName", "LegacyUser" },
                { "displayName", "LegacyUser" },
                { "counts", new BsonDocument() },
                { "characteristics", new BsonDocument() },
                { "lists", new BsonDocument() },
                { "timers", new BsonDocument() },
                { "currencies", new BsonDocument() },
                { "jobExperience", new BsonDocument() },
                // A field that no longer exists on the current Profile class.
                { "someRemovedLegacyField", "leftover data from a since-deleted feature" },
            });

            Profile profile = null;
            var exception = Record.Exception(() => profile = _database.GetProfile("LegacyUser"));

            Assert.Null(exception);
            Assert.NotNull(profile);
            Assert.Equal("LegacyUser", profile.userName);
        }

        [Fact]
        public void GetProfile_DocumentWithExplicitNullDictionary_ComesBackEmptyNotNull()
        {
            InsertRawProfileDocument(new BsonDocument
            {
                { "userName", "NullFieldUser" },
                { "displayName", "NullFieldUser" },
                { "counts", BsonNull.Value },
                { "currencies", BsonNull.Value },
                { "timers", BsonNull.Value },
            });

            Profile profile = _database.GetProfile("NullFieldUser");

            Assert.NotNull(profile);
            Assert.NotNull(profile.counts);
            Assert.Empty(profile.counts);
            Assert.NotNull(profile.currencies);
            Assert.Empty(profile.currencies);
            Assert.NotNull(profile.timers);
            Assert.Empty(profile.timers);
        }

        [Fact]
        public void GetProfile_MinimalLegacyDocument_MissingNewerFields_DoesNotThrow()
        {
            // Simulates a document written before several newer fields (escrow,
            // dailyMagnitudes, milkInventory, trainings, dailyClimaxCounts,
            // employeeEarnings) existed on Profile at all.
            InsertRawProfileDocument(new BsonDocument
            {
                { "userName", "MinimalUser" },
                { "displayName", "MinimalUser" },
            });

            Profile profile = null;
            var exception = Record.Exception(() => profile = _database.GetProfile("MinimalUser"));

            Assert.Null(exception);
            Assert.NotNull(profile);
            Assert.NotNull(profile.counts);
            Assert.NotNull(profile.milkInventory);
            Assert.NotNull(profile.trainings);
            Assert.NotNull(profile.employeeEarnings);
        }
    }
}
