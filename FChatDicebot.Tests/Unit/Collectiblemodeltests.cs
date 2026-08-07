using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for the Collectible base class, its polymorphic storage, and the migration
    /// contract that storage depends on.
    ///
    /// The migration scripts themselves (scripts/backfill-collectibles.js and its rollback)
    /// are mongosh one-offs verified by their DRY_RUN output, following the same convention as
    /// scripts/backfill-bottle-serials.js. What these tests pin is the *shape contract* the
    /// scripts have to produce, and — more importantly — the two failure modes that make the
    /// migration mandatory rather than optional.
    /// </summary>
    [Collection("Database")]
    public class CollectibleModelTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public CollectibleModelTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        public void Dispose()
        {
            _fixture.Reset();
        }

        private static IMongoCollection<BsonDocument> RawProfiles()
        {
            var client = new MongoClient(TestConfiguration.TestConnectionString);
            return client.GetDatabase(TestConfiguration.TestDatabaseName)
                .GetCollection<BsonDocument>("RegisteredProfiles");
        }

        private void InsertRawProfileDocument(BsonDocument doc)
        {
            RawProfiles().InsertOne(doc);
        }

        // -------------------------------------------------------------------
        // Polymorphic storage
        // -------------------------------------------------------------------

        [Fact]
        public void Collectibles_RoundTrip_PreserveRuntimeTypeAndEveryField()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var acquired = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc);

            _database.SetCollectibles("Alice", new List<Collectible>
            {
                new MilkBottle
                {
                    serial = 12,
                    subjectName = "Bob",
                    acquiredAt = acquired,
                    substance = "cum",
                    quantity = 1,
                    corruptionTag = ChateauCurrency.CorruptTag,
                },
            });

            var stored = Assert.Single(_database.GetProfile("Alice").collectibles);

            var bottle = Assert.IsType<MilkBottle>(stored);
            Assert.Equal(12, bottle.serial);
            Assert.Equal("Bob", bottle.subjectName);
            Assert.Equal(acquired, bottle.acquiredAt);
            Assert.Equal("cum", bottle.substance);
            Assert.Equal(1, bottle.quantity);
            Assert.Equal(ChateauCurrency.CorruptTag, bottle.corruptionTag);
            Assert.False(bottle.IsEmpty);
        }

        [Fact]
        public void StoredCollectible_CarriesTheDiscriminator()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            _database.SetCollectibles("Alice", new List<Collectible>
            {
                new MilkBottle { serial = 1, subjectName = "Bob", acquiredAt = DateTime.UtcNow, quantity = 1 },
            });

            var raw = RawProfiles().Find(Builders<BsonDocument>.Filter.Eq("userName", "Alice")).First();
            var element = raw["collectibles"].AsBsonArray[0].AsBsonDocument;

            // The migration script writes this same value by hand. If the driver's discriminator
            // ever stops being the bare class name, the script has to change with it.
            Assert.Equal("MilkBottle", element["_t"].AsString);
        }

        [Fact]
        public void StoredCollectible_UsesTheMigratedElementKeys()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            _database.SetCollectibles("Alice", new List<Collectible>
            {
                new MilkBottle { serial = 1, subjectName = "Bob", acquiredAt = DateTime.UtcNow, quantity = 1 },
            });

            var raw = RawProfiles().Find(Builders<BsonDocument>.Filter.Eq("userName", "Alice")).First();
            var element = raw["collectibles"].AsBsonArray[0].AsBsonDocument;

            // These are exactly the renames backfill-collectibles.js performs. The test exists so
            // the script and the model can't drift apart silently.
            Assert.True(element.Contains("subjectName"));
            Assert.True(element.Contains("acquiredAt"));
            Assert.False(element.Contains("sourceName"));
            Assert.False(element.Contains("milkedAt"));
        }

        // -------------------------------------------------------------------
        // Why the migration is mandatory — failure mode 1 (loud)
        // -------------------------------------------------------------------

        [Fact]
        public void CollectibleWithoutDiscriminator_FailsToLoad()
        {
            // A half-migrated document: renamed to the new field, but the _t stamp was missed.
            // An abstract declared type has nothing to construct, so this throws rather than
            // guessing. Pinned so nobody later "simplifies" the discriminator away.
            InsertRawProfileDocument(new BsonDocument
            {
                { "userName", "HalfMigrated" },
                { "displayName", "HalfMigrated" },
                {
                    "collectibles", new BsonArray
                    {
                        new BsonDocument
                        {
                            { "serial", 3 },
                            { "subjectName", "Bob" },
                            { "substance", "milk" },
                        },
                    }
                },
            });

            Assert.ThrowsAny<Exception>(() => _database.GetProfile("HalfMigrated"));
        }

        // -------------------------------------------------------------------
        // Why the migration is mandatory — failure mode 2 (silent, and worse)
        // -------------------------------------------------------------------

        [Fact]
        public void UnmigratedProfile_LoadsWithAnEmptyCollection_WhichIsWhyTheStartupGuardExists()
        {
            // The dangerous one. The driver ignores fields the model doesn't declare, so a
            // profile still carrying `milkInventory` does NOT throw — it comes back looking like
            // a resident who owns nothing. The next write would persist that emptiness and the
            // bottles would be gone for real.
            //
            // This test documents the behavior rather than wishing it away; the actual defense
            // is CollectiblesMigrationCheck, asserted below.
            InsertRawProfileDocument(new BsonDocument
            {
                { "userName", "Unmigrated" },
                { "displayName", "Unmigrated" },
                {
                    "milkInventory", new BsonArray
                    {
                        new BsonDocument
                        {
                            { "serial", 7 },
                            { "sourceName", "Bob" },
                            { "substance", "milk" },
                            { "milkedAt", DateTime.UtcNow },
                            { "quantity", 1 },
                        },
                    }
                },
            });

            Profile loaded = _database.GetProfile("Unmigrated");

            Assert.NotNull(loaded);
            Assert.Empty(loaded.collectibles);
        }

        [Fact]
        public void MigrationCheck_Throws_WhenAnyProfileStillCarriesMilkInventory()
        {
            InsertRawProfileDocument(new BsonDocument
            {
                { "userName", "Unmigrated" },
                { "displayName", "Unmigrated" },
                { "milkInventory", new BsonArray() },
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => CollectiblesMigrationCheck.AssertMigrated(_database));

            // The message has to be actionable — it is the only thing the operator sees.
            Assert.Contains("backfill-collectibles.js", exception.Message);
        }

        [Fact]
        public void MigrationCheck_Passes_OnAMigratedDatabase()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            _database.SetCollectibles("Alice", new List<Collectible>
            {
                new MilkBottle { serial = 1, subjectName = "Bob", acquiredAt = DateTime.UtcNow, quantity = 1 },
            });

            CollectiblesMigrationCheck.AssertMigrated(_database);
        }

        [Fact]
        public void MigrationCheck_Passes_OnAFreshDatabase()
        {
            // No profiles at all — a brand new install must not be told to run a migration.
            CollectiblesMigrationCheck.AssertMigrated(_database);
        }

        // -------------------------------------------------------------------
        // Base-class rules
        // -------------------------------------------------------------------

        [Fact]
        public void MilkBottle_IsSellable_OnlyWhileFull()
        {
            var full = new MilkBottle { serial = 1, quantity = 1 };
            var empty = new MilkBottle { serial = 2, quantity = 1, emptiedAt = DateTime.UtcNow };

            Assert.True(full.IsSellable);
            Assert.False(empty.IsSellable);
        }

        [Fact]
        public void MilkBottle_IsTransferable_EvenWhenEmpty()
        {
            // Empties move by explicit serial — the numbered empty is the keepsake.
            var empty = new MilkBottle { serial = 2, quantity = 1, emptiedAt = DateTime.UtcNow };

            Assert.True(empty.IsTransferable);
        }

        [Fact]
        public void MilkBottle_TypeLabel_IsBottle()
        {
            Assert.Equal("bottle", new MilkBottle().TypeLabel);
        }

        // -------------------------------------------------------------------
        // Shared serial space
        // -------------------------------------------------------------------

        [Fact]
        public void ClaimCollectibleSerials_ReadsTheHistoricalBottleCounterDocument()
        {
            // The C# surface was renamed; the stored _id must not be. A rename applied to code
            // but missed in data restarts at 1 and collides with every existing bottle.
            Assert.Equal("bottleSerial", ChateauDatabase.CollectibleSerialCounterId);

            _database.ClaimCollectibleSerials(3);

            var client = new MongoClient(TestConfiguration.TestConnectionString);
            var counters = client.GetDatabase(TestConfiguration.TestDatabaseName)
                .GetCollection<BsonDocument>("Counters");
            var doc = counters.Find(Builders<BsonDocument>.Filter.Eq("_id", "bottleSerial")).FirstOrDefault();

            Assert.NotNull(doc);
            Assert.Equal(3, doc["value"].ToInt32());
        }

        // -------------------------------------------------------------------
        // Selection over a mixed list
        // -------------------------------------------------------------------

        [Fact]
        public void FindBySerial_IsSharedAcrossTypes_ButBottleLookupStaysTyped()
        {
            var profile = new ProfileBuilder().WithUserName("Alice").Build();
            profile.collectibles = new List<Collectible>
            {
                new MilkBottle { serial = 5, subjectName = "Bob", acquiredAt = DateTime.UtcNow, quantity = 1 },
            };

            Assert.Equal(5, CollectionInventory.FindBySerial(profile, 5).serial);
            Assert.Equal(5, BottleInventory.FindBySerial(profile, 5).serial);

            // Serial 0 is the pre-backfill sentinel, never a referenceable number.
            Assert.Null(CollectionInventory.FindBySerial(profile, 0));
            Assert.Null(BottleInventory.FindBySerial(profile, 99));
        }

        [Fact]
        public void HasNone_IsPerType_NotWholeCollection()
        {
            // The distinction that matters once there is more than one type: "holds no bottles"
            // and "holds nothing" are different questions, and !bottles asks the first.
            var empty = new ProfileBuilder().WithUserName("Alice").Build();
            Assert.True(CollectionInventory.HasNone<MilkBottle>(empty));
            Assert.True(CollectionInventory.IsEmpty(empty));

            empty.collectibles = new List<Collectible>
            {
                new MilkBottle { serial = 1, subjectName = "Bob", acquiredAt = DateTime.UtcNow, quantity = 1 },
            };
            Assert.False(CollectionInventory.HasNone<MilkBottle>(empty));
            Assert.False(CollectionInventory.IsEmpty(empty));
        }

        [Fact]
        public void OfType_ToleratesNullProfileAndNullCollection()
        {
            Assert.Empty(CollectionInventory.OfType<MilkBottle>(null));
            Assert.Empty(CollectionInventory.OfType<MilkBottle>(new Profile { collectibles = null }));
            Assert.True(CollectionInventory.HasNone<MilkBottle>(new Profile { collectibles = null }));
        }
    }
}
