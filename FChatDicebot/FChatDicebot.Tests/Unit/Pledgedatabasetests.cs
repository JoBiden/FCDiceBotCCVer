using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for pledge database operations
    /// </summary>
    [Collection("Database")]
    public class PledgeDatabaseTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public PledgeDatabaseTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        public void Dispose()
        {
            _fixture.Reset();
        }

        #region AddPledge Tests

        [Fact]
        public void AddPledge_ValidPledge_SavesSuccessfully()
        {
            // Arrange
            var pledge = new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                identifier = "cake",
                investmentLevel = "involved",
                pledgeTime = DateTime.UtcNow,
                status = "active"
            };

            // Act
            _database.AddPledge(pledge);

            // Assert
            var retrieved = _database.GetPledgesByPledger("Alice").FirstOrDefault();
            Assert.NotNull(retrieved);
            Assert.Equal("Alice", retrieved.pledger);
            Assert.Equal("Bob", retrieved.pledgee);
            Assert.Equal("feed", retrieved.interactionType);
            Assert.Equal("active", retrieved.status);
        }

        #endregion

        #region GetPledge Tests

        [Fact]
        public void GetPledge_ExistingPledge_ReturnsCorrectPledge()
        {
            // Arrange
            var pledge = new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "mark",
                investmentLevel = "commitment"
            };
            _database.AddPledge(pledge);

            // Act
            var result = _database.GetPledgesByPledger("Alice").FirstOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Bob", result.pledgee);
            Assert.Equal("Alice", result.pledger);
            Assert.Equal("mark", result.interactionType);
        }

        [Fact]
        public void GetPledge_NonExistentPledge_ReturnsNull()
        {
            // Arrange
            var fakeId = ObjectId.GenerateNewId();

            // Act
            var result = _database.GetPledge(fakeId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetPledgesByPledger Tests

        [Fact]
        public void GetPledgesByPledger_MultiplePledges_ReturnsAllForPledger()
        {
            // Arrange
            _database.AddPledge(new Pledge { pledger = "Alice", pledgee = "Bob", interactionType = "feed", investmentLevel = "involved" });
            _database.AddPledge(new Pledge { pledger = "Alice", pledgee = "Charlie", interactionType = "mark", investmentLevel = "commitment" });
            _database.AddPledge(new Pledge { pledger = "Bob", pledgee = "Alice", interactionType = "entitle", investmentLevel = "commitment" });

            // Act
            var result = _database.GetPledgesByPledger("Alice");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal("Alice", p.pledger));
        }

        [Fact]
        public void GetPledgesByPledger_NoPledges_ReturnsEmptyList()
        {
            // Act
            var result = _database.GetPledgesByPledger("NonExistent");

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetPledgesByPledgee Tests

        [Fact]
        public void GetPledgesByPledgee_MultiplePledges_ReturnsAllForPledgee()
        {
            // Arrange
            _database.AddPledge(new Pledge { pledger = "Alice", pledgee = "Bob", interactionType = "feed", investmentLevel = "involved" });
            _database.AddPledge(new Pledge { pledger = "Charlie", pledgee = "Bob", interactionType = "mark", investmentLevel = "commitment" });
            _database.AddPledge(new Pledge { pledger = "Bob", pledgee = "Alice", interactionType = "entitle", investmentLevel = "commitment" });

            // Act
            var result = _database.GetPledgesByPledgee("Bob");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal("Bob", p.pledgee));
        }

        #endregion

        #region GetActivePledges Tests

        [Fact]
        public void GetActivePledges_MatchingActivePledge_ReturnsPledge()
        {
            // Arrange
            _database.AddPledge(new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                investmentLevel = "involved",
                status = "active"
            });

            // Act
            var result = _database.GetActivePledges("Alice", "Bob", "feed");

            // Assert
            Assert.Single(result);
            Assert.Equal("active", result[0].status);
        }

        [Fact]
        public void GetActivePledges_FulfilledPledge_ReturnsEmpty()
        {
            // Arrange
            _database.AddPledge(new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                investmentLevel = "involved",
                status = "fulfilled"
            });

            // Act
            var result = _database.GetActivePledges("Alice", "Bob", "feed");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetActivePledges_MultipleMatchingActive_ReturnsAll()
        {
            // Arrange
            _database.AddPledge(new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                investmentLevel = "involved",
                status = "active"
            });
            _database.AddPledge(new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                investmentLevel = "involved",
                status = "active"
            });

            // Act
            var result = _database.GetActivePledges("Alice", "Bob", "feed");

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetActivePledges_DifferentInteractionType_ReturnsEmpty()
        {
            // Arrange
            _database.AddPledge(new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                investmentLevel = "involved",
                status = "active"
            });

            // Act
            var result = _database.GetActivePledges("Alice", "Bob", "mark");

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region UpdatePledge Tests

        [Fact]
        public void UpdatePledge_ModifiesExistingPledge()
        {
            // Arrange
            var pledge = new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                investmentLevel = "involved",
                pledgeTime = DateTime.UtcNow,
                status = "active"
            };
            _database.AddPledge(pledge);

            // Retrieve the pledge with its generated Id
            var savedPledge = _database.GetPledgesByPledger("Alice").FirstOrDefault();
            Assert.NotNull(savedPledge); // Verify it was saved

            // Act
            savedPledge.status = "fulfilled";
            savedPledge.fulfilledTime = DateTime.UtcNow;
            savedPledge.pledgeHonored = true;
            _database.UpdatePledge(savedPledge);

            // Assert
            var updated = _database.GetPledgesByPledger("Alice").FirstOrDefault();
            Assert.NotNull(updated);
            Assert.Equal("fulfilled", updated.status);
            Assert.NotNull(updated.fulfilledTime);
            Assert.True(updated.pledgeHonored);
        }

        #endregion

        #region DeletePledge Tests

        [Fact]
        public void DeletePledge_RemovesPledge()
        {
            // Arrange
            var pledge = new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                investmentLevel = "involved"
            };
            _database.AddPledge(pledge);

            // Act
            _database.DeletePledge(pledge.Id);

            // Assert
            var result = _database.GetPledge(pledge.Id);
            Assert.Null(result);
        }

        #endregion

        #region Pledge Model Tests

        [Fact]
        public void PledgeIsActive_ActiveStatus_ReturnsTrue()
        {
            var pledge = new Pledge { status = "active" };

            Assert.True(pledge.IsActive);
        }

        [Fact]
        public void PledgeIsActive_FulfilledStatus_ReturnsFalse()
        {
            var pledge = new Pledge { status = "fulfilled" };

            Assert.False(pledge.IsActive);
        }

        [Fact]
        public void PledgeIsActive_CancelledStatus_ReturnsFalse()
        {
            var pledge = new Pledge { status = "cancelled" };

            Assert.False(pledge.IsActive);
        }

        #endregion
    }
}
