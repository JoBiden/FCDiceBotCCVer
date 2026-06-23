using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for the database additions backing the group/lifecycle features (B4/B5):
    /// the two new pending lookups and the +N rate-limited increment helper.
    /// </summary>
    [Collection("Database")]
    public class PendingCommandGroupDatabaseTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public PendingCommandGroupDatabaseTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        [Fact]
        public void GetPendingCommandsByGroupId_ReturnsOnlyThatGroupsSeats()
        {
            string groupId = Guid.NewGuid().ToString("N");
            _database.AddPendingCommand(MakeSeat("Alice", "Bob", groupId));
            _database.AddPendingCommand(MakeSeat("Alice", "Carol", groupId));
            _database.AddPendingCommand(MakeSeat("Alice", "Dave", null)); // 1:1, different request

            var seats = _database.GetPendingCommandsByGroupId(groupId);

            Assert.Equal(2, seats.Count);
            Assert.All(seats, s => Assert.Equal(groupId, s.groupId));
        }

        [Fact]
        public void GetPendingCommandsByInitiator_ReturnsAllSeatsFromThatInitiator()
        {
            string groupId = Guid.NewGuid().ToString("N");
            _database.AddPendingCommand(MakeSeat("Alice", "Bob", groupId));
            _database.AddPendingCommand(MakeSeat("Alice", "Carol", groupId));
            _database.AddPendingCommand(MakeSeat("Bob", "Alice", null));

            var aliceSeats = _database.GetPendingCommandsByInitiator("Alice");

            Assert.Equal(2, aliceSeats.Count);
            Assert.All(aliceSeats, s => Assert.Equal("Alice", s.pendingInteraction.initiator));
        }

        [Fact]
        public void ChangeCountByWithRateLimit_FirstCall_AppliesFullAmount()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);

            bool applied = _database.ChangeCountByWithRateLimit("Alice", "cuddle", 3, TimeSpan.FromMinutes(30));

            Assert.True(applied);
            Assert.Equal(3, _database.GetProfile("Alice").counts["cuddle"]);
        }

        [Fact]
        public void ChangeCountByWithRateLimit_SecondCallWhileLimited_AppliesNothing()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);

            _database.ChangeCountByWithRateLimit("Alice", "cuddle", 3, TimeSpan.FromMinutes(30));
            bool applied = _database.ChangeCountByWithRateLimit("Alice", "cuddle", 2, TimeSpan.FromMinutes(30));

            Assert.False(applied);
            Assert.Equal(3, _database.GetProfile("Alice").counts["cuddle"]); // unchanged
        }

        [Fact]
        public void UpdatePendingCommand_PersistsConsentStateAndOrder()
        {
            string groupId = Guid.NewGuid().ToString("N");
            var seat = MakeSeat("Alice", "Bob", groupId);
            _database.AddPendingCommand(seat);

            seat.consentState = PendingCommand.ConsentedState;
            seat.consentedOrder = 2;
            _database.UpdatePendingCommand(seat);

            var reloaded = _database.GetPendingCommandsByGroupId(groupId).Single();
            Assert.True(reloaded.HasConsented);
            Assert.Equal(2, reloaded.consentedOrder);
        }

        private static PendingCommand MakeSeat(string initiator, string recipient, string groupId)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = "cuddle",
                    identifier = null,
                    investmentLevel = "casual"
                },
                awaitingConsentFrom = recipient,
                groupId = groupId,
                consentState = PendingCommand.PendingConsentState
            };
        }

        public void Dispose()
        {
        }
    }
}
