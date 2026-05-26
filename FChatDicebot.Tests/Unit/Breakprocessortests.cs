using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    [Collection("Database")]
    public class BreakProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly BreakProcessor _processor;

        public BreakProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new BreakProcessor(_database);
            SeedBreakIdentifier("mouth");
            SeedBreakIdentifier("nose");  // not used in B-list but is break-tagged
        }

        public void Dispose() { }

        private void SeedBreakIdentifier(string name)
        {
            _fixture.SeedIdentifier(new Identifier
            {
                type = name,
                description = "stub bodypart for tests",
                categories = new string[] { "bodypart", "break" }
            });
        }

        // -------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------

        [Fact]
        public void InteractionType_ReturnsBreak()
        {
            Assert.Equal("break", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsConsequence()
        {
            Assert.Equal("consequence", _processor.InvestmentLevel);
        }

        // -------------------------------------------------------------------
        // ReadDays — defaults, clamps, fallback on malformed
        // -------------------------------------------------------------------

        [Fact]
        public void ReadDays_NoExtraParameters_ReturnsDefault()
        {
            var interaction = new Interaction { extraParameters = null };
            Assert.Equal(BreakProcessor.DefaultDays, BreakProcessor.ReadDays(interaction));
        }

        [Fact]
        public void ReadDays_BelowMin_ClampsToMin()
        {
            var interaction = new Interaction { extraParameters = new BsonArray { 0 } };
            Assert.Equal(BreakProcessor.MinDays, BreakProcessor.ReadDays(interaction));
        }

        [Fact]
        public void ReadDays_AboveMax_ClampsToMax()
        {
            var interaction = new Interaction { extraParameters = new BsonArray { 50 } };
            Assert.Equal(BreakProcessor.MaxDays, BreakProcessor.ReadDays(interaction));
        }

        [Fact]
        public void ReadDays_WithinRange_ReturnsAsTyped()
        {
            var interaction = new Interaction { extraParameters = new BsonArray { 7 } };
            Assert.Equal(7, BreakProcessor.ReadDays(interaction));
        }

        // -------------------------------------------------------------------
        // ValidateInteraction
        // -------------------------------------------------------------------

        [Fact]
        public void ValidateInteraction_MissingIdentifier_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_UnknownPart_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "nonexistent");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_NonBreakTaggedPart_ReturnsFailure()
        {
            _fixture.SeedIdentifier(new Identifier
            {
                type = "horn",
                description = "not break-tagged",
                categories = new string[] { "bodypart" }  // no "break" tag
            });
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "horn");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_BreakTaggedPart_ReturnsSuccess()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "mouth");
            Assert.True(result.IsValid);
        }

        // -------------------------------------------------------------------
        // ApplyBreak — direct unit test of the static helper
        // -------------------------------------------------------------------

        [Fact]
        public void ApplyBreak_NewPart_AddsBreakInstance()
        {
            var profile = new ProfileBuilder().WithUserName("Bob").Build();
            BreakProcessor.ApplyBreak(profile, "mouth", 3, "Alice");
            var entries = BreakInstance.LoadAll(profile);
            Assert.Single(entries);
            Assert.Equal("mouth", entries[0].Part);
            Assert.Equal(3, entries[0].Severity);
            Assert.Equal("Alice", entries[0].BrokenBy);
        }

        [Fact]
        public void ApplyBreak_LongerOverrideReplaces()
        {
            var profile = new ProfileBuilder().WithUserName("Bob").Build();
            BreakProcessor.ApplyBreak(profile, "mouth", 3, "Alice");
            BreakProcessor.ApplyBreak(profile, "mouth", 7, "Eve");
            var entries = BreakInstance.LoadAll(profile);
            Assert.Single(entries);
            Assert.Equal(7, entries[0].Severity);
            Assert.Equal("Eve", entries[0].BrokenBy);
        }

        [Fact]
        public void ApplyBreak_ShorterDoesNotReplace()
        {
            var profile = new ProfileBuilder().WithUserName("Bob").Build();
            BreakProcessor.ApplyBreak(profile, "mouth", 7, "Alice");
            BreakProcessor.ApplyBreak(profile, "mouth", 3, "Eve");
            var entries = BreakInstance.LoadAll(profile);
            Assert.Single(entries);
            Assert.Equal(7, entries[0].Severity);
            Assert.Equal("Alice", entries[0].BrokenBy);
        }

        // -------------------------------------------------------------------
        // ProcessInteraction — persistence + cooldown
        // -------------------------------------------------------------------

        [Fact]
        public void ProcessInteraction_PersistsBreakAndSetsCooldown()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "break",
                    identifier = "mouth",
                    investmentLevel = "consequence",
                    extraParameters = new BsonArray { 5 }
                }
            };
            _database.AddPendingCommand(pending);
            _processor.ProcessInteraction(pending);

            var bob = _database.GetProfile("Bob");
            var entries = BreakInstance.LoadAll(bob);
            Assert.Single(entries);
            Assert.Equal("mouth", entries[0].Part);
            Assert.Equal(5, entries[0].Severity);

            var alice = _database.GetProfile("Alice");
            string cooldownKey = BreakProcessor.CooldownTimerKey("mouth", "Bob");
            Assert.True(alice.timers.ContainsKey(cooldownKey));
            // 7-day cooldown
            Assert.True(alice.timers[cooldownKey].timerEnd > DateTime.UtcNow.AddDays(6));
        }

        [Fact]
        public void ProcessInteraction_DefaultDaysWhenNoExtraParameters()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "break",
                    identifier = "mouth",
                    investmentLevel = "consequence"
                }
            };
            _database.AddPendingCommand(pending);
            _processor.ProcessInteraction(pending);

            var bob = _database.GetProfile("Bob");
            var entries = BreakInstance.LoadAll(bob);
            Assert.Single(entries);
            Assert.Equal(BreakProcessor.DefaultDays, entries[0].Severity);
        }

        [Fact]
        public void ProcessInteraction_ClampsDaysAboveMax()
        {
            new ProfileBuilder().WithUserName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").BuildAndSave(_database);

            var pending = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "break",
                    identifier = "mouth",
                    investmentLevel = "consequence",
                    extraParameters = new BsonArray { 100 }
                }
            };
            _database.AddPendingCommand(pending);
            _processor.ProcessInteraction(pending);

            var bob = _database.GetProfile("Bob");
            var entries = BreakInstance.LoadAll(bob);
            Assert.Equal(BreakProcessor.MaxDays, entries[0].Severity);
        }
    }
}
