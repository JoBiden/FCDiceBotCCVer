using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Casual;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using Xunit;

namespace FChatDicebot.Tests.Integration
{
    /// <summary>
    /// Integration tests for the complete interaction flow.
    /// Tests the entire process from command initiation to completion.
    /// </summary>
    [Collection("Database")]
    public class InteractionFlowTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public InteractionFlowTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        [Fact]
        public void CompleteKissFlow_WithConsent_UpdatesProfilesCorrectly()
        {
            // Arrange
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice the Adventurer")
                .BuildAndSave(_database);

            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob the Bold")
                .BuildAndSave(_database);

            // Step 1: Create pending command (simulating !kiss command)
            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "kiss",
                    identifier = "",  // Casual interactions don't use identifiers
                    investmentLevel = "casual"
                },
                awaitingConsentFrom = "Bob",
                startTime = DateTime.UtcNow
            };

            _database.AddPendingCommand(pendingCommand);

            // Step 2: Process the interaction (simulating !consent)
            var processor = new KissProcessor(_database);
            string completionMessage = processor.ProcessInteraction(pendingCommand);

            // Assert - Check completion message
            Assert.NotNull(completionMessage);
            Assert.Equal("kiss", completionMessage); // Kiss processor returns just "kiss"

            // Assert - Casual interactions are NOT saved to Interactions collection
            // (They're unlimited and don't need to be tracked individually)
            var interactions = _database.GetInteractionsByType("kiss");
            Assert.Empty(interactions); // Should be empty for casual interactions

            // Assert - Check profiles were updated with counts
            var aliceAfter = _database.GetProfile("Alice");
            var bobAfter = _database.GetProfile("Bob");

            Assert.Equal(1, aliceAfter.counts["kiss"]);
            Assert.Equal(1, bobAfter.counts["kiss"]);

            // Assert - Check pending command was removed
            var pendingAfter = _database.GetPendingCommandAwaitingConsent("Bob");
            Assert.Null(pendingAfter);
        }

        [Fact]
        public void MultipleKisses_SameUsers_AccumulatesCountsCorrectly()
        {
            // Arrange
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var kissProcessor = new KissProcessor(_database);

            // Act - Multiple kisses
            for (int i = 0; i < 3; i++)
            {
                var kissCommand = new PendingCommand
                {
                    Id = ObjectId.GenerateNewId(),
                    pendingInteraction = new Interaction
                    {
                        initiator = "Alice",
                        recipient = "Bob",
                        type = "kiss",
                        identifier = "",
                        investmentLevel = "casual"
                    }
                };
                _database.AddPendingCommand(kissCommand);
                kissProcessor.ProcessInteraction(kissCommand);
            }

            // Act - Bob kisses Alice back twice
            for (int i = 0; i < 2; i++)
            {
                var kissCommand = new PendingCommand
                {
                    Id = ObjectId.GenerateNewId(),
                    pendingInteraction = new Interaction
                    {
                        initiator = "Bob",
                        recipient = "Alice",
                        type = "kiss",
                        identifier = "",
                        investmentLevel = "casual"
                    }
                };
                _database.AddPendingCommand(kissCommand);
                kissProcessor.ProcessInteraction(kissCommand);
            }

            // Assert
            var aliceProfile = _database.GetProfile("Alice");
            var bobProfile = _database.GetProfile("Bob");

            // Alice kissed Bob 3 times and was kissed 2 times = 5 total kisses
            Assert.Equal(5, aliceProfile.counts["kiss"]);

            // Bob kissed Alice 2 times and was kissed 3 times = 5 total kisses
            Assert.Equal(5, bobProfile.counts["kiss"]);

            // Casual interactions are not saved to database
            var kissInteractions = _database.GetInteractionsByType("kiss");
            Assert.Empty(kissInteractions);
        }

        [Fact]
        public void InteractionFlow_WithExpiredCommand_CanBeDetected()
        {
            // Arrange
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            // Create an expired pending command (started more than timeout ago)
            var expiredCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "kiss",
                    identifier = "",
                    investmentLevel = "casual"
                },
                awaitingConsentFrom = "Bob",
                startTime = DateTime.UtcNow.AddMinutes(-30) // 30 minutes ago
            };

            _database.AddPendingCommand(expiredCommand);

            // Act - Check if command is expired (typically timeout is 10 minutes)
            var retrievedCommand = _database.GetPendingCommandAwaitingConsent("Bob");
            bool isExpired = (DateTime.UtcNow - retrievedCommand.startTime).TotalMinutes > 10;

            // Assert
            Assert.True(isExpired);

            // Cleanup - In real code, expired commands would be automatically removed
            _database.DeletePendingCommand(expiredCommand.Id);
        }

        [Fact]
        public void CountingInteractions_BetweenTwoUsers_UsesProfileCounts()
        {
            // Arrange
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var charlie = new ProfileBuilder()
                .WithUserName("Charlie")
                .WithDisplayName("Charlie")
                .BuildAndSave(_database);

            var processor = new KissProcessor(_database);

            // Act - Alice kisses Bob 3 times
            for (int i = 0; i < 3; i++)
            {
                var cmd = new PendingCommand
                {
                    Id = ObjectId.GenerateNewId(),
                    pendingInteraction = new Interaction
                    {
                        initiator = "Alice",
                        recipient = "Bob",
                        type = "kiss",
                        identifier = "",
                        investmentLevel = "casual"
                    }
                };
                _database.AddPendingCommand(cmd);
                processor.ProcessInteraction(cmd);
            }

            // Bob kisses Alice 2 times
            for (int i = 0; i < 2; i++)
            {
                var cmd = new PendingCommand
                {
                    Id = ObjectId.GenerateNewId(),
                    pendingInteraction = new Interaction
                    {
                        initiator = "Bob",
                        recipient = "Alice",
                        type = "kiss",
                        identifier = "",
                        investmentLevel = "casual"
                    }
                };
                _database.AddPendingCommand(cmd);
                processor.ProcessInteraction(cmd);
            }

            // Charlie kisses Alice 1 time
            var charlieCmd = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Charlie",
                    recipient = "Alice",
                    type = "kiss",
                    identifier = "",
                    investmentLevel = "casual"
                }
            };
            _database.AddPendingCommand(charlieCmd);
            processor.ProcessInteraction(charlieCmd);

            // Assert - Check profile counts directly
            var aliceProfile = _database.GetProfile("Alice");
            var bobProfile = _database.GetProfile("Bob");
            var charlieProfile = _database.GetProfile("Charlie");

            // Alice: 3 with Bob + 2 with Bob + 1 with Charlie = 6 total
            Assert.Equal(6, aliceProfile.counts["kiss"]);

            // Bob: 3 with Alice + 2 with Alice = 5 total
            Assert.Equal(5, bobProfile.counts["kiss"]);

            // Charlie: 1 with Alice = 1 total
            Assert.Equal(1, charlieProfile.counts["kiss"]);

            // Note: For casual interactions, you can't query "between two users"
            // because interactions aren't saved. You'd need to track that separately
            // if needed, or use the profile counts as shown above.
        }

        [Fact]
        public void MultiplePendingCommands_CanExist()
        {
            // Arrange
            var alice = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var bob = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .BuildAndSave(_database);

            var charlie = new ProfileBuilder()
                .WithUserName("Charlie")
                .WithDisplayName("Charlie")
                .BuildAndSave(_database);

            // Act - Create multiple pending commands
            var pendingFromAlice = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "kiss",
                    identifier = "",
                    investmentLevel = "casual"
                },
                awaitingConsentFrom = "Bob",
                startTime = DateTime.UtcNow
            };

            var pendingFromCharlie = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Charlie",
                    recipient = "Bob",
                    type = "kiss",
                    identifier = "",
                    investmentLevel = "casual"
                },
                awaitingConsentFrom = "Bob",
                startTime = DateTime.UtcNow
            };

            _database.AddPendingCommand(pendingFromAlice);
            _database.AddPendingCommand(pendingFromCharlie);

            // Assert - Bob should have multiple pending commands
            var bobsPendingCommands = _database.GetPendingCommands("Bob");
            Assert.Equal(2, bobsPendingCommands.Count);
        }

        public void Dispose()
        {
            // Optional: cleanup specific to this test class if needed
        }
    }
}