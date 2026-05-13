using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.BotCommands
{
    [Collection("Database")]
    public class ChateauBirthTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public ChateauBirthTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        [Fact]
        public void ExecuteBirth_NoPregnancies_ReturnsError()
        {
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            string result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0], out string error);

            Assert.Null(result);
            Assert.NotNull(error);
            Assert.Contains("no pregnancies", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExecuteBirth_PregnancyNotReady_ReturnsErrorWithTimeRemaining()
        {
            CreateCarrierWithPregnancies("Bob",
                new Pregnancy
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Initiator = "Alice",
                    MonsterType = "slime",
                    ConceivedAt = DateTime.UtcNow,
                    ReadyAt = DateTime.UtcNow.AddDays(3),
                    BroodSize = 1
                });

            string result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0], out string error);

            Assert.Null(result);
            Assert.NotNull(error);
            Assert.Contains("ready", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExecuteBirth_NoIndex_BirthsOldestReady()
        {
            CreateCarrierWithPregnancies("Bob",
                new Pregnancy
                {
                    Id = "first",
                    Initiator = "Alice",
                    MonsterType = "slime",
                    ConceivedAt = DateTime.UtcNow.AddDays(-5),
                    ReadyAt = DateTime.UtcNow.AddDays(-2),
                    BroodSize = 1
                },
                new Pregnancy
                {
                    Id = "second",
                    Initiator = "Carol",
                    MonsterType = "wasp",
                    ConceivedAt = DateTime.UtcNow.AddDays(-3),
                    ReadyAt = DateTime.UtcNow.AddDays(-1),
                    BroodSize = 4
                });

            string result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0], out string error);

            Assert.Null(error);
            Assert.NotNull(result);
            Assert.Contains("slime", result);

            var bob = _database.GetProfile("Bob");
            Assert.Single(bob.pregnancies);
            Assert.Equal("second", bob.pregnancies[0].Id);
        }

        [Fact]
        public void ExecuteBirth_ExplicitIndex_BirthsThatPregnancy()
        {
            CreateCarrierWithPregnancies("Bob",
                new Pregnancy
                {
                    Id = "first",
                    Initiator = "Alice",
                    MonsterType = "slime",
                    ConceivedAt = DateTime.UtcNow.AddDays(-5),
                    ReadyAt = DateTime.UtcNow.AddDays(-2),
                    BroodSize = 1
                },
                new Pregnancy
                {
                    Id = "second",
                    Initiator = "Carol",
                    MonsterType = "wasp",
                    ConceivedAt = DateTime.UtcNow.AddDays(-3),
                    ReadyAt = DateTime.UtcNow.AddDays(-1),
                    BroodSize = 4
                });

            string result = ChateauBirth.ExecuteBirth(_database, "Bob", new[] { "2" }, out string error);

            Assert.Null(error);
            Assert.NotNull(result);
            Assert.Contains("wasp", result);

            var bob = _database.GetProfile("Bob");
            Assert.Single(bob.pregnancies);
            Assert.Equal("first", bob.pregnancies[0].Id);
        }

        [Fact]
        public void ExecuteBirth_IndexOutOfRange_ReturnsError()
        {
            CreateCarrierWithPregnancies("Bob",
                new Pregnancy
                {
                    Id = "only",
                    Initiator = "Alice",
                    MonsterType = "slime",
                    ConceivedAt = DateTime.UtcNow.AddDays(-2),
                    ReadyAt = DateTime.UtcNow.AddDays(-1),
                    BroodSize = 1
                });

            string result = ChateauBirth.ExecuteBirth(_database, "Bob", new[] { "5" }, out string error);

            Assert.Null(result);
            Assert.NotNull(error);
            Assert.Contains("out of range", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExecuteBirth_OnlyCountsReadyPregnanciesForIndex()
        {
            CreateCarrierWithPregnancies("Bob",
                new Pregnancy
                {
                    Id = "notready",
                    Initiator = "Alice",
                    MonsterType = "slime",
                    ConceivedAt = DateTime.UtcNow,
                    ReadyAt = DateTime.UtcNow.AddDays(3),
                    BroodSize = 1
                },
                new Pregnancy
                {
                    Id = "ready",
                    Initiator = "Carol",
                    MonsterType = "wasp",
                    ConceivedAt = DateTime.UtcNow.AddDays(-2),
                    ReadyAt = DateTime.UtcNow.AddDays(-1),
                    BroodSize = 1
                });

            string result = ChateauBirth.ExecuteBirth(_database, "Bob", new[] { "1" }, out string error);

            Assert.Null(error);
            Assert.NotNull(result);
            Assert.Contains("wasp", result);

            var bob = _database.GetProfile("Bob");
            Assert.Single(bob.pregnancies);
            Assert.Equal("notready", bob.pregnancies[0].Id);
        }

        [Fact]
        public void ExecuteBirth_AppendsOffspringListEntryAndBumpsBirthCount()
        {
            CreateCarrierWithPregnancies("Bob",
                new Pregnancy
                {
                    Id = "only",
                    Initiator = "Alice",
                    MonsterType = "slime",
                    ConceivedAt = DateTime.UtcNow.AddDays(-2),
                    ReadyAt = DateTime.UtcNow.AddDays(-1),
                    BroodSize = 3
                });

            ChateauBirth.ExecuteBirth(_database, "Bob", new string[0], out string error);
            Assert.Null(error);

            var bob = _database.GetProfile("Bob");
            Assert.True(bob.lists.ContainsKey("offspring"));
            var entry = bob.lists["offspring"].Single();
            Assert.Contains("slime", entry);
            Assert.Contains("3", entry);
            Assert.Contains("Alice", entry);
            Assert.Equal(1, bob.counts["birth"]);
        }

        [Fact]
        public void ExecuteBirth_BroodSizeOnePhrasing_UsesAnOrA()
        {
            CreateCarrierWithPregnancies("Bob",
                new Pregnancy
                {
                    Id = "only",
                    Initiator = "Alice",
                    MonsterType = "imp",
                    ConceivedAt = DateTime.UtcNow.AddDays(-2),
                    ReadyAt = DateTime.UtcNow.AddDays(-1),
                    BroodSize = 1
                });

            string result = ChateauBirth.ExecuteBirth(_database, "Bob", new string[0], out string error);

            Assert.Null(error);
            Assert.Contains("imp", result);
            Assert.DoesNotContain("brood of", result);
        }

        [Fact]
        public void ExecuteBirth_UnknownUser_ReturnsError()
        {
            string result = ChateauBirth.ExecuteBirth(_database, "Ghost", new string[0], out string error);
            Assert.Null(result);
            Assert.NotNull(error);
        }

        private void CreateCarrierWithPregnancies(string userName, params Pregnancy[] pregnancies)
        {
            var carrier = new ProfileBuilder()
                .WithUserName(userName)
                .WithDisplayName(userName)
                .BuildAndSave(_database);

            carrier.pregnancies = new List<Pregnancy>(pregnancies);
            _database.SetProfile(userName, carrier);
        }

        public void Dispose()
        {
        }
    }
}
