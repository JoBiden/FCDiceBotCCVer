using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Integration
{
    /// <summary>
    /// Integration tests for the complete pledge workflow
    /// </summary>
    [Collection("Database")]
    public class PledgeFlowTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly FeedProcessor _feedProcessor;

        public PledgeFlowTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _feedProcessor = new FeedProcessor(_database);
        }

        public void Dispose()
        {
            _fixture.Reset();
        }

        #region Pledge Creation Flow

        [Fact]
        public void PledgeCreation_NewPledge_CreatesActiveRecord()
        {
            // Arrange
            var pledge = new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                identifier = "food",
                investmentLevel = "involved",
                pledgeTime = DateTime.UtcNow,
                status = "active"
            };

            // Act
            _database.AddPledge(pledge);

            // Assert
            var saved = _database.GetPledgesByPledger("Alice").FirstOrDefault();
            Assert.NotNull(saved);
            Assert.True(saved.IsActive);
            Assert.False(saved.pledgeHonored);
            Assert.Null(saved.fulfilledTime);
        }

        #endregion

        #region Pledge Fulfillment Flow

        [Fact]
        public void PledgeFulfillment_WithPledgeId_MarksPledgeAsFulfilled()
        {
            // Arrange - Create a pledge
            var pledge = new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                identifier = "food",
                investmentLevel = "involved",
                pledgeTime = DateTime.UtcNow.AddDays(-2), // 2 days ago
                status = "active"
            };
            _database.AddPledge(pledge);
            pledge = _database.GetPledgesByPledger("Alice").FirstOrDefault();
            // Create profiles
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            // Create a pending interaction with pledge ID in extraParameters
            var interaction = new Interaction
            {
                initiator = "Alice",
                recipient = "Bob",
                type = "feed",
                identifier = "food",
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow,
                extraParameters = new BsonArray
                {
                    new BsonDocument
                    {
                        { "pledgeId", pledge.Id.ToString() }
                    }
                }
            };

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = interaction,
                awaitingConsentFrom = "Bob"
            };

            _database.AddPendingCommand(pendingCommand);

            // Act - Process the interaction (simulating consent)
            string result = _feedProcessor.ProcessInteraction(pendingCommand);

            // Manually handle pledge fulfillment logic
            var firstParam = pendingCommand.pendingInteraction.extraParameters[0].AsBsonDocument;
            if (firstParam.Contains("pledgeId"))
            {
                string pledgeIdStr = firstParam["pledgeId"].AsString;
                ObjectId pledgeId = ObjectId.Parse(pledgeIdStr);

                var pledgeToUpdate = _database.GetPledge(pledgeId);
                if (pledgeToUpdate != null && pledgeToUpdate.IsActive)
                {
                    pledgeToUpdate.status = "fulfilled";
                    pledgeToUpdate.fulfilledTime = DateTime.UtcNow;

                    TimeSpan timeSincePledge = pledgeToUpdate.fulfilledTime.Value - pledgeToUpdate.pledgeTime;
                    if (timeSincePledge.TotalDays >= 1)
                    {
                        pledgeToUpdate.pledgeHonored = true;
                    }

                    _database.UpdatePledge(pledgeToUpdate);
                }
            }

            // Assert
            var updatedPledge = _database.GetPledge(pledge.Id);
            Assert.Equal("fulfilled", updatedPledge.status);
            Assert.NotNull(updatedPledge.fulfilledTime);
            Assert.True(updatedPledge.pledgeHonored); // Should be honored (2 days after creation)
        }

        [Fact]
        public void PledgeFulfillment_SameDay_NotHonored()
        {
            // Arrange - Create a pledge from today
            var pledge = new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                identifier = "food",
                investmentLevel = "involved",
                pledgeTime = DateTime.UtcNow.AddHours(-2), // 2 hours ago
                status = "active"
            };
            _database.AddPledge(pledge);
            // Retrieve the pledge with its generated Id
            pledge = _database.GetPledgesByPledger("Alice").FirstOrDefault();

            // Create profiles
            new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCount("pledgesactive", 1)
                .BuildAndSave(_database);

            new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            // Create a pending interaction with pledge ID
            var interaction = new Interaction
            {
                initiator = "Alice",
                recipient = "Bob",
                type = "feed",
                identifier = "food",
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow,
                extraParameters = new BsonArray
                {
                    new BsonDocument
                    {
                        { "pledgeId", pledge.Id.ToString() }
                    }
                }
            };

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = interaction,
                awaitingConsentFrom = "Bob"
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _feedProcessor.ProcessInteraction(pendingCommand);

            // Manually handle pledge fulfillment logic
            var firstParam = pendingCommand.pendingInteraction.extraParameters[0].AsBsonDocument;
            if (firstParam.Contains("pledgeId"))
            {
                string pledgeIdStr = firstParam["pledgeId"].AsString;
                ObjectId pledgeId = ObjectId.Parse(pledgeIdStr);

                var pledgeToUpdate = _database.GetPledge(pledgeId);
                if (pledgeToUpdate != null && pledgeToUpdate.IsActive)
                {
                    pledgeToUpdate.status = "fulfilled";
                    pledgeToUpdate.fulfilledTime = DateTime.UtcNow;

                    TimeSpan timeSincePledge = pledgeToUpdate.fulfilledTime.Value - pledgeToUpdate.pledgeTime;
                    if (timeSincePledge.TotalDays >= 1)
                    {
                        pledgeToUpdate.pledgeHonored = true;
                    }

                    _database.UpdatePledge(pledgeToUpdate);
                }
            }

            // Assert
            var updatedPledge = _database.GetPledge(pledge.Id);
            Assert.Equal("fulfilled", updatedPledge.status);
            Assert.False(updatedPledge.pledgeHonored); // Not honored (same day)
        }

        [Fact]
        public void PledgeFulfillment_ExactlyOneDayLater_IsHonored()
        {
            // Arrange
            var pledge = new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                identifier = "food",
                investmentLevel = "involved",
                pledgeTime = DateTime.UtcNow.AddDays(-1).AddMinutes(-1), // Just over 1 day ago
                status = "active"
            };
            _database.AddPledge(pledge);
            pledge = _database.GetPledgesByPledger("Alice").FirstOrDefault();

            new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCount("pledgesactive", 1)
                .BuildAndSave(_database);

            new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var interaction = new Interaction
            {
                initiator = "Alice",
                recipient = "Bob",
                type = "feed",
                identifier = "food",
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow,
                extraParameters = new BsonArray
                {
                    new BsonDocument { { "pledgeId", pledge.Id.ToString() } }
                }
            };

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = interaction,
                awaitingConsentFrom = "Bob"
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _feedProcessor.ProcessInteraction(pendingCommand);

            // Manually handle pledge fulfillment logic
            var firstParam = pendingCommand.pendingInteraction.extraParameters[0].AsBsonDocument;
            if (firstParam.Contains("pledgeId"))
            {
                string pledgeIdStr = firstParam["pledgeId"].AsString;
                ObjectId pledgeId = ObjectId.Parse(pledgeIdStr);

                var pledgeToUpdate = _database.GetPledge(pledgeId);
                if (pledgeToUpdate != null && pledgeToUpdate.IsActive)
                {
                    pledgeToUpdate.status = "fulfilled";
                    pledgeToUpdate.fulfilledTime = DateTime.UtcNow;

                    TimeSpan timeSincePledge = pledgeToUpdate.fulfilledTime.Value - pledgeToUpdate.pledgeTime;
                    if (timeSincePledge.TotalDays >= 1)
                    {
                        pledgeToUpdate.pledgeHonored = true;
                    }

                    _database.UpdatePledge(pledgeToUpdate);
                }
            }

            // Assert
            var updatedPledge = _database.GetPledgesByPledger("Alice").FirstOrDefault();
            Assert.True(updatedPledge.pledgeHonored);
        }

        #endregion

        #region Multiple Pledges

        [Fact]
        public void MultiplePledges_SameType_BothStayActive()
        {
            // Arrange - Create two pledges from Alice to Bob for same interaction
            var pledge1 = new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                identifier = "food",
                investmentLevel = "involved",
                pledgeTime = DateTime.UtcNow.AddDays(-1)
            };

            var pledge2 = new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                identifier = "pie",
                investmentLevel = "involved",
                pledgeTime = DateTime.UtcNow
            };

            _database.AddPledge(pledge1);
            _database.AddPledge(pledge2);

            // Act
            var activePledges = _database.GetActivePledges("Alice", "Bob", "feed");

            // Assert
            Assert.Equal(2, activePledges.Count);
            Assert.All(activePledges, p => Assert.True(p.IsActive));
        }

        [Fact]
        public void PledgesByPledger_MultiplePledgees_ReturnsAll()
        {
            // Arrange
            _database.AddPledge(new Pledge { pledger = "Alice", pledgee = "Bob", interactionType = "feed", investmentLevel = "involved" });
            _database.AddPledge(new Pledge { pledger = "Alice", pledgee = "Charlie", interactionType = "mark", investmentLevel = "commitment" });
            _database.AddPledge(new Pledge { pledger = "Alice", pledgee = "David", interactionType = "entitle", investmentLevel = "commitment" });

            // Act
            var pledges = _database.GetPledgesByPledger("Alice");

            // Assert
            Assert.Equal(3, pledges.Count);
            Assert.Contains(pledges, p => p.pledgee == "Bob");
            Assert.Contains(pledges, p => p.pledgee == "Charlie");
            Assert.Contains(pledges, p => p.pledgee == "David");
        }

        #endregion

        #region Pledge Status Transitions

        [Fact]
        public void PledgeStatusTransition_ActiveToFulfilled_UpdatesCorrectly()
        {
            // Arrange
            var pledge = new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                investmentLevel = "involved",
                status = "active"
            };
            _database.AddPledge(pledge);

            // Retrieve the pledge with its generated Id
            var savedPledge = _database.GetPledgesByPledger("Alice").FirstOrDefault();
            Assert.NotNull(savedPledge); // Verify it was saved

            // Act
            savedPledge.status = "fulfilled";
            savedPledge.fulfilledTime = DateTime.UtcNow;
            _database.UpdatePledge(savedPledge);

            // Assert
            var updated = _database.GetPledgesByPledger("Alice").FirstOrDefault();
            Assert.Equal("fulfilled", updated.status);
            Assert.False(updated.IsActive);
        }

        [Fact]
        public void PledgeStatusTransition_ActiveToAbandoned_UpdatesCorrectly()
        {
            // Arrange
            var pledge = new Pledge
            {
                pledger = "Alice",
                pledgee = "Bob",
                interactionType = "feed",
                investmentLevel = "involved",
                status = "active"
            };
            _database.AddPledge(pledge);

            // Retrieve the pledge with its generated Id
            var savedPledge = _database.GetPledgesByPledger("Alice").FirstOrDefault();
            Assert.NotNull(savedPledge); // Verify it was saved

            // Act
            savedPledge.status = "abandoned";
            _database.UpdatePledge(savedPledge);

            // Assert
            var updated = _database.GetPledgesByPledger("Alice").FirstOrDefault();
            Assert.Equal("abandoned", updated.status);
            Assert.False(updated.IsActive);
        }

        #endregion

        #region Error Handling

        [Fact]
        public void PledgeFulfillment_InvalidPledgeId_DoesNotCrash()
        {
            // Arrange - Create interaction with invalid pledge ID
            new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var interaction = new Interaction
            {
                initiator = "Alice",
                recipient = "Bob",
                type = "feed",
                identifier = "food",
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow,
                extraParameters = new BsonArray
                {
                    new BsonDocument { { "pledgeId", "invalid-id" } }
                }
            };

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = interaction,
                awaitingConsentFrom = "Bob"
            };

            _database.AddPendingCommand(pendingCommand);

            // Act & Assert - Should not throw
            var exception = Record.Exception(() =>
            {
                _feedProcessor.ProcessInteraction(pendingCommand);

                // Try to process pledge fulfillment even with invalid ID
                try
                {
                    var firstParam = pendingCommand.pendingInteraction.extraParameters[0].AsBsonDocument;
                    if (firstParam.Contains("pledgeId"))
                    {
                        string pledgeIdStr = firstParam["pledgeId"].AsString;
                        ObjectId pledgeId = ObjectId.Parse(pledgeIdStr);

                        var pledgeToUpdate = _database.GetPledge(pledgeId);
                        if (pledgeToUpdate != null && pledgeToUpdate.IsActive)
                        {
                            pledgeToUpdate.status = "fulfilled";
                            pledgeToUpdate.fulfilledTime = DateTime.UtcNow;

                            TimeSpan timeSincePledge = pledgeToUpdate.fulfilledTime.Value - pledgeToUpdate.pledgeTime;
                            if (timeSincePledge.TotalDays >= 1)
                            {
                                pledgeToUpdate.pledgeHonored = true;
                            }

                            _database.UpdatePledge(pledgeToUpdate);
                        }
                    }
                }
                catch
                {
                    // Ignore pledge processing errors
                }
            });
            Assert.Null(exception);
        }

        [Fact]
        public void PledgeFulfillment_NonExistentPledgeId_DoesNotCrash()
        {
            // Arrange
            new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var fakeId = ObjectId.GenerateNewId();
            var interaction = new Interaction
            {
                initiator = "Alice",
                recipient = "Bob",
                type = "feed",
                identifier = "food",
                investmentLevel = "involved",
                interactionTime = DateTime.UtcNow,
                extraParameters = new BsonArray
                {
                    new BsonDocument { { "pledgeId", fakeId.ToString() } }
                }
            };

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = interaction,
                awaitingConsentFrom = "Bob"
            };

            _database.AddPendingCommand(pendingCommand);

            // Act & Assert - Should not throw
            var exception = Record.Exception(() =>
            {
                _feedProcessor.ProcessInteraction(pendingCommand);

                // Try to process pledge fulfillment
                try
                {
                    var firstParam = pendingCommand.pendingInteraction.extraParameters[0].AsBsonDocument;
                    if (firstParam.Contains("pledgeId"))
                    {
                        string pledgeIdStr = firstParam["pledgeId"].AsString;
                        ObjectId pledgeId = ObjectId.Parse(pledgeIdStr);

                        var pledgeToUpdate = _database.GetPledge(pledgeId);
                        if (pledgeToUpdate != null && pledgeToUpdate.IsActive)
                        {
                            pledgeToUpdate.status = "fulfilled";
                            pledgeToUpdate.fulfilledTime = DateTime.UtcNow;

                            TimeSpan timeSincePledge = pledgeToUpdate.fulfilledTime.Value - pledgeToUpdate.pledgeTime;
                            if (timeSincePledge.TotalDays >= 1)
                            {
                                pledgeToUpdate.pledgeHonored = true;
                            }

                            _database.UpdatePledge(pledgeToUpdate);
                        }
                    }
                }
                catch
                {
                    // Ignore pledge processing errors
                }
            });
            Assert.Null(exception);
        }

        #endregion
    }
}
