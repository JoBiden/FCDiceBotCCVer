using FChatDicebot.Database;
using FChatDicebot.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using Xunit;

namespace FChatDicebot.Tests.Fixtures
{
    /// <summary>
    /// xUnit collection fixture that manages the test database lifecycle.
    /// Shared across all tests in a collection to improve performance.
    /// </summary>
    public class TestDatabaseFixture : IDisposable
    {
        public IChateauDatabase Database { get; private set; }

        public TestDatabaseFixture()
        {
            // Create test database instance
            Database = new ChateauDatabase(
                TestConfiguration.TestConnectionString,
                TestConfiguration.TestDatabaseName
            );

            // Ensure clean slate
            Reset();
        }

        /// <summary>
        /// Resets the test database to a clean state.
        /// Call this in test constructors to ensure isolation.
        /// </summary>
        public void Reset()
        {
            // Clear all collections
            Database.ClearCollection("RegisteredProfiles");
            Database.ClearCollection("Interactions");
            Database.ClearCollection("PendingCommands");
            Database.ClearCollection("PendingDuties");
            Database.ClearCollection("Duties");
            Database.ClearCollection("Commands");
            Database.ClearCollection("Identifiers");
            Database.ClearCollection("ModMessages");
            Database.ClearCollection("Pledges");
        }

        /// <summary>
        /// Seeds the database with minimal required data for tests.
        /// Call this if your tests need baseline data.
        /// </summary>
        public void SeedBasicData()
        {
            // You can add common test data here if needed
            // For example, common identifiers, duties, etc.
        }

        /// <summary>
        /// Inserts an Identifier document directly into the Identifiers collection.
        /// Used by processor tests that need a specific identifier to exist in the DB
        /// (e.g. BreedProcessor reads gestation/brood fields off the monster identifier).
        /// </summary>
        public void SeedIdentifier(Identifier identifier)
        {
            if (identifier.Id == ObjectId.Empty)
            {
                identifier.Id = ObjectId.GenerateNewId();
            }
            var client = new MongoClient(TestConfiguration.TestConnectionString);
            var database = client.GetDatabase(TestConfiguration.TestDatabaseName);
            database.GetCollection<Identifier>("Identifiers").InsertOne(identifier);
        }

        public void Dispose()
        {
            // Clean up after all tests in the collection are done
            Reset();
        }
    }

    /// <summary>
    /// Collection definition for tests that need database access.
    /// All tests in this collection will share the same TestDatabaseFixture instance.
    /// </summary>
    [CollectionDefinition("Database")]
    public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
