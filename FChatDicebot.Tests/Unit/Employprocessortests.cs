using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    [Collection("Database")]
    public class EmployProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly EmployProcessor _processor;

        public EmployProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new EmployProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsEmploy()
        {
            Assert.Equal("employ", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCommitment()
        {
            Assert.Equal("commitment", _processor.InvestmentLevel);
        }

        [Fact]
        public void ValidateInteraction_RecipientEmployedTooRecently_ReturnsFailure()
        {
            // Regression test for M9: the recheck was never wired, so a recipient could be
            // re-employed (job/employer swapped) an unlimited number of times per day.
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithTimer("employ", DateTime.UtcNow.AddHours(12))
                .BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "butler");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_RecipientEmployCooldownExpired_ReturnsSuccess()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithTimer("employ", DateTime.UtcNow.AddHours(-1))
                .BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "butler");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ProcessInteraction_SetsJobAndEmployer()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "employ",
                    identifier = "butler",
                    investmentLevel = "consequence"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.Equal("butler", bobProfile.characteristics["job"]);
            Assert.Equal("Alice", bobProfile.characteristics["employer"]);
        }

        [Fact]
        public void ProcessInteraction_SetsCooldown()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "employ",
                    identifier = "butler",
                    investmentLevel = "consequence"
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var bobProfile = _database.GetProfile("Bob");
            Assert.True(bobProfile.timers.ContainsKey("employ"));
        }

        public void Dispose()
        {
        }
    }
}
