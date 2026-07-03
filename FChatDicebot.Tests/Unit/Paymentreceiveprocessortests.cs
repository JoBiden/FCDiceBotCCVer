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
    public class PaymentReceiveProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly PaymentReceiveProcessor _processor;

        public PaymentReceiveProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new PaymentReceiveProcessor(_database);
        }

        [Fact]
        public void InteractionType_ReturnsPaymentReceive()
        {
            Assert.Equal("paymentReceive", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsInvolved()
        {
            Assert.Equal("involved", _processor.InvestmentLevel);
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
        public void ProcessInteraction_TransfersCurrencyFromRecipientToInitiator()
        {
            // paymentReceive means the initiator is billing the recipient, i.e. the
            // recipient pays. ChateauPay only ever creates this type with a negative
            // paymentAmount (the "requesting funds" branch), so the real invocation always
            // passes a negative magnitude here — Alice (initiator) is requesting 30 gold
            // from Bob (recipient).
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCurrency("gold", 50)
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithCurrency("gold", 100)
                .BuildAndSave(_database);

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "paymentReceive",
                    identifier = "gold",
                    investmentLevel = "involved",
                    extraParameters = new BsonArray { -30 }
                }
            };

            _database.AddPendingCommand(pendingCommand);

            // Act
            string result = _processor.ProcessInteraction(pendingCommand);

            // Assert
            Assert.Equal("paymentReceive", result);
            var aliceProfile = _database.GetProfile("Alice");
            var bobProfile = _database.GetProfile("Bob");

            Assert.Equal(80, aliceProfile.currencies["gold"]); // 50 + 30
            Assert.Equal(70, bobProfile.currencies["gold"]); // 100 - 30
        }

        [Fact]
        public void ProcessInteraction_BilledPartyLacksFunds_FailsWithoutMintingOrOverdrawing()
        {
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .BuildAndSave(_database);

            var recipient = new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithCurrency("gold", 10)
                .BuildAndSave(_database);

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "paymentReceive",
                    identifier = "gold",
                    investmentLevel = "involved",
                    extraParameters = new BsonArray { -30 }
                }
            };
            _database.AddPendingCommand(pendingCommand);

            string result = _processor.ProcessInteraction(pendingCommand);

            Assert.Equal("NoInteraction", result);
            Assert.False(_database.GetProfile("Alice").currencies.ContainsKey("gold"));
            Assert.Equal(10, _database.GetProfile("Bob").currencies["gold"]);
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
                .WithCurrency("gold", 100)
                .BuildAndSave(_database);

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "paymentReceive",
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
            Assert.Contains(interactions, i => i.type == "paymentReceive");
        }

        public void Dispose()
        {
        }
    }
}
