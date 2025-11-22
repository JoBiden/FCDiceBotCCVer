using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    [Collection("Database")]
    public class PaymentGiveProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly PaymentGiveProcessor _processor;

        public PaymentGiveProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new PaymentGiveProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsPaymentGive()
        {
            Assert.Equal("paymentGive", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsTransaction()
        {
            Assert.Equal("transaction", _processor.InvestmentLevel);
        }

        [Fact]
        public void ValidateInteraction_NoCurrencyProvided_ReturnsFailure()
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

            // Act
            var result = _processor.ValidateInteraction("Alice", "Bob", "");

            // Assert
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ProcessInteraction_TransfersCurrencyFromInitiatorToRecipient()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCurrency("gold", 100)
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithCurrency("gold", 50)
                .BuildAndSave(_database);

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "paymentGive",
                    identifier = "gold",
                    investmentLevel = "involved",
                    extraParameters = new BsonArray { 30 }
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var aliceProfile = _database.GetProfile("Alice");
            var bobProfile = _database.GetProfile("Bob");

            Assert.Equal(70, aliceProfile.currencies["gold"]); // 100 - 30
            Assert.Equal(80, bobProfile.currencies["gold"]); // 50 + 30
        }

        [Fact]
        public void ProcessInteraction_InitializesCurrencyIfNotExists()
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
                    type = "paymentGive",
                    identifier = "silver",
                    investmentLevel = "involved",
                    extraParameters = new BsonArray { 10 }
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var aliceProfile = _database.GetProfile("Alice");
            var bobProfile = _database.GetProfile("Bob");

            Assert.True(aliceProfile.currencies.ContainsKey("silver"));
            Assert.True(bobProfile.currencies.ContainsKey("silver"));
            Assert.Equal(-10, aliceProfile.currencies["silver"]); // 0 - 10
            Assert.Equal(10, bobProfile.currencies["silver"]); // 0 + 10
        }

        [Fact]
        public void ProcessInteraction_SavesInteractionToHistory()
        {
            // Arrange
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCurrency("gold", 100)
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
                    type = "paymentGive",
                    identifier = "gold",
                    investmentLevel = "involved",
                    extraParameters = new BsonArray { 30 }
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            _processor.ProcessInteraction(pendingCommand);

            // Assert
            var interactions = _database.GetInteractionsByInitiator("Alice");
            Assert.Contains(interactions, i => i.type == "paymentGive");
        }

        public void Dispose()
        {
        }
    }
}
