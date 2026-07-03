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
        public void ProcessInteraction_InitiatorLacksCurrency_FailsWithoutMintingOrOverdrawing()
        {
            // Regression test for C2: a payer with no balance at all (not even a zero
            // entry) must not be driven negative. Replaces a prior test that asserted an
            // overdraft to -10 as "correct" behavior.
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
            string result = _processor.ProcessInteraction(pendingCommand);

            // Assert
            Assert.Equal("NoInteraction", result);

            var aliceProfile = _database.GetProfile("Alice");
            var bobProfile = _database.GetProfile("Bob");

            Assert.False(aliceProfile.currencies.ContainsKey("silver"));
            Assert.False(bobProfile.currencies.ContainsKey("silver"));
        }

        [Fact]
        public void ProcessInteraction_SelfPay_LeavesBalanceUnchanged()
        {
            // Regression test for C1: initiator == recipient used to double-load the same
            // profile and net a free mint on save. The atomic guarded debit + credit nets
            // to zero instead.
            var selfPayer = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCurrency("gold", 100)
                .BuildAndSave(_database);

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Alice",
                    type = "paymentGive",
                    identifier = "gold",
                    investmentLevel = "involved",
                    extraParameters = new BsonArray { 30 }
                }
            };

            _database.AddPendingCommand(pendingCommand);

            string result = _processor.ProcessInteraction(pendingCommand);

            Assert.Equal("paymentGive", result);
            Assert.Equal(100, _database.GetProfile("Alice").currencies["gold"]);
        }

        [Fact]
        public void ProcessInteraction_TwoQueuedPaymentsRaceSameBalance_ExactlyOneSucceedsAndSumIsConserved()
        {
            // Regression test for C2: two pending payments both drawing on the same 100
            // gold. Whichever consents first drains the balance to 0; the second must fail
            // cleanly rather than overdraw, and total currency in the system is conserved.
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").WithCurrency("gold", 100).BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").BuildAndSave(_database);

            var pendingToBob = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "paymentGive",
                    identifier = "gold",
                    investmentLevel = "involved",
                    extraParameters = new BsonArray { 100 }
                }
            };
            var pendingToCarol = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Carol",
                    type = "paymentGive",
                    identifier = "gold",
                    investmentLevel = "involved",
                    extraParameters = new BsonArray { 100 }
                }
            };
            _database.AddPendingCommand(pendingToBob);
            _database.AddPendingCommand(pendingToCarol);

            string firstResult = _processor.ProcessInteraction(pendingToBob);
            string secondResult = _processor.ProcessInteraction(pendingToCarol);

            Assert.Equal("paymentGive", firstResult);
            Assert.Equal("NoInteraction", secondResult);

            var aliceCurrencies = _database.GetProfile("Alice").currencies;
            var bobCurrencies = _database.GetProfile("Bob").currencies;
            var carolCurrencies = _database.GetProfile("Carol").currencies;
            int aliceGold = aliceCurrencies.ContainsKey("gold") ? aliceCurrencies["gold"] : 0;
            int bobGold = bobCurrencies.ContainsKey("gold") ? bobCurrencies["gold"] : 0;
            int carolGold = carolCurrencies.ContainsKey("gold") ? carolCurrencies["gold"] : 0;

            Assert.Equal(0, aliceGold);
            Assert.Equal(100, bobGold);
            Assert.Equal(0, carolGold);
            Assert.Equal(100, aliceGold + bobGold + carolGold); // conserved, nothing minted
        }

        [Fact]
        public void ProcessInteraction_IouCurrency_MayGoNegative()
        {
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithCurrency(CurrencyRules.IouCurrency, 0)
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
                    identifier = CurrencyRules.IouCurrency,
                    investmentLevel = "involved",
                    extraParameters = new BsonArray { 50 }
                }
            };
            _database.AddPendingCommand(pendingCommand);

            string result = _processor.ProcessInteraction(pendingCommand);

            Assert.Equal("paymentGive", result);
            Assert.Equal(-50, _database.GetProfile("Alice").currencies[CurrencyRules.IouCurrency]);
            Assert.Equal(50, _database.GetProfile("Bob").currencies[CurrencyRules.IouCurrency]);
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
