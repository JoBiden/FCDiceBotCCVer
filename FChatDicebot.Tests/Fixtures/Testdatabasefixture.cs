using FChatDicebot.Database;
using FChatDicebot.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using Xunit;

// InteractionProcessorRegistry and MonDB are both process-wide static singletons: the registry
// constructs every processor exactly once (on first GetProcessor call) bound to whatever
// MonDB.GetDatabase() returns AT THAT MOMENT, and that binding can't be changed afterwards. Xunit
// runs different test collections in parallel by default, so a collection with no [Collection]
// attribute (e.g. one that only touches the registry, never the database) can race ahead of the
// "Database" collection's fixture and lock every processor onto MonDB's lazy production-database
// default before TestDatabaseFixture ever calls MonDB.Initialize. Disabling parallelization makes
// that ordering deterministic instead of a flaky race.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

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
            // Also point the static MonDB adapter at the test database. Code obtained through
            // InteractionProcessorRegistry (e.g. GroupInteractionResolver, ChateauConsent) is
            // constructed via processors' legacy parameterless constructor, which reads its
            // IChateauDatabase from MonDB.GetDatabase() rather than from this fixture's Database
            // property. Without this, that path would silently fall back to MonDB's lazy
            // production-database default and never see any data seeded through Database below.
            FChatDicebot.MonDB.Initialize(TestConfiguration.TestConnectionString, TestConfiguration.TestDatabaseName);

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
            Database.ClearCollection("MonsterStats");
            Database.ClearCollection("Feedback");
            Database.ClearCollection("SlotsJackpots");
            Database.ClearCollection("RandomEvents");
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
