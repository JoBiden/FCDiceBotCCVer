using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Tests for InfestProcessor — validation, persistence shape, cooldown placement,
    /// per-parasite cost lookup, and consent wording. Spread behavior lives in
    /// ParasiteSpreadEffectTests; purge lives in ChateauPurgeTests.
    /// </summary>
    [Collection("Database")]
    public class InfestProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly InfestProcessor _processor;

        public InfestProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new InfestProcessor(_database);

            _fixture.SeedIdentifier(new Identifier
            {
                type = "paraslime",
                description = "These parasitic slimes make their home inside your skull.",
                categories = new[] { InfestProcessor.ParasiteCategory },
            });
            _fixture.SeedIdentifier(new Identifier
            {
                type = "tentacles",
                description = "Squirmy wriggling creatures.",
                categories = new[] { InfestProcessor.ParasiteCategory },
            });
            _fixture.SeedIdentifier(new Identifier
            {
                type = "musk",
                description = "musk",
                categories = new[] { "scent" },
            });
        }

        public void Dispose() { }

        [Fact]
        public void InteractionType_ReturnsInfest()
        {
            Assert.Equal(InfestProcessor.InfestType, _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsConsequence()
        {
            Assert.Equal("consequence", _processor.InvestmentLevel);
        }

        [Fact]
        public void ValidateInteraction_EmptyParasite_ReturnsFailure()
        {
            SeedPair();
            var result = _processor.ValidateInteraction("Alice", "Bob", "");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_UnknownParasite_ReturnsFailure()
        {
            SeedPair();
            var result = _processor.ValidateInteraction("Alice", "Bob", "ghost-worm");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_NonParasiteCategory_ReturnsFailure()
        {
            // "musk" is a scent, not a parasite — must be rejected.
            SeedPair();
            var result = _processor.ValidateInteraction("Alice", "Bob", "musk");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_KnownParasite_Succeeds()
        {
            SeedPair();
            var result = _processor.ValidateInteraction("Alice", "Bob", "paraslime");
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_AlreadyInfestedSameParasite_Rejects()
        {
            SeedPair();
            var bob = _database.GetProfile("Bob");
            InfestProcessor.ApplyInfestation(bob, "paraslime", "Alice",
                spreadFromContact: false, gracePeriod: TimeSpan.Zero);
            _database.SetProfile("Bob", bob);

            var result = _processor.ValidateInteraction("Alice", "Bob", "paraslime");

            Assert.False(result.IsValid);
            Assert.Contains("already a willing host of paraslime", result.ErrorMessage);
        }

        [Fact]
        public void ProcessInteraction_FirstInfest_PersistsAsDirect()
        {
            SeedPair();
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "paraslime"));

            var bob = _database.GetProfile("Bob");
            var parasites = ParasiteInstance.LoadAll(bob);
            Assert.Single(parasites);
            Assert.Equal("paraslime", parasites[0].Parasite);
            Assert.Equal("Alice", parasites[0].InfestedBy);
            Assert.False(parasites[0].SpreadFromContact);
        }

        [Fact]
        public void ProcessInteraction_SetsCooldownOnInitiatorByParasiteAndRecipient()
        {
            SeedPair();
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "paraslime"));

            var alice = _database.GetProfile("Alice");
            string key = InfestProcessor.CooldownTimerKey("paraslime", "Bob");
            Assert.True(alice.timers.ContainsKey(key));
            Assert.True(alice.timers[key].timerEnd > DateTime.UtcNow.AddDays(6));
            Assert.True(alice.timers[key].timerEnd <= DateTime.UtcNow.AddDays(7).AddMinutes(1));
        }

        [Fact]
        public void ProcessInteraction_DifferentRecipient_DoesNotShareCooldown()
        {
            SeedPair();
            new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").BuildAndSave(_database);
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "paraslime"));

            var alice = _database.GetProfile("Alice");
            Assert.True(alice.timers.ContainsKey(InfestProcessor.CooldownTimerKey("paraslime", "Bob")));
            Assert.False(alice.timers.ContainsKey(InfestProcessor.CooldownTimerKey("paraslime", "Carol")));
        }

        [Fact]
        public void ProcessInteraction_SelfInfest_PersistsOnSameProfile()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            _processor.ProcessInteraction(BuildPending("Alice", "Alice", "paraslime"));

            var alice = _database.GetProfile("Alice");
            var parasites = ParasiteInstance.LoadAll(alice);
            Assert.Single(parasites);
            Assert.True(alice.timers.ContainsKey(InfestProcessor.CooldownTimerKey("paraslime", "Alice")));
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            SeedPair();
            var pending = BuildPending("Alice", "Bob", "paraslime");
            _database.AddPendingCommand(pending);

            _processor.ProcessInteraction(pending);

            Assert.Null(_database.GetPendingCommand(pending.Id));
        }

        [Fact]
        public void PurgeCostFor_KnownParasite_ReturnsMappedCost()
        {
            Assert.Equal(PurgeCostType.LostTrainingPoint, InfestProcessor.PurgeCostFor("bimboslime"));
            Assert.Equal(PurgeCostType.RandomBreak, InfestProcessor.PurgeCostFor("tentacles"));
            Assert.Equal(PurgeCostType.RandomCurse, InfestProcessor.PurgeCostFor("lustleeches"));
        }

        [Fact]
        public void PurgeCostFor_UnknownParasite_FallsBackToDefault()
        {
            Assert.Equal(InfestProcessor.DefaultPurgeCost, InfestProcessor.PurgeCostFor("unmapped-parasite"));
        }

        [Fact]
        public void PurgeCostFor_CaseInsensitive()
        {
            Assert.Equal(PurgeCostType.LostTrainingPoint, InfestProcessor.PurgeCostFor("BIMBOSLIME"));
        }

        [Fact]
        public void GetConsentWarning_MentionsParasiteCostAndGrace()
        {
            SeedPair();
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string consent = _processor.GetConsentWarning(alice, bob, "paraslime");

            // Includes the parasite name preceded by an article ("a paraslime"), the cost
            // phrase, and the grace-period advertisement.
            Assert.Contains("a paraslime", consent);
            Assert.Contains("missing work to recover", consent);
            Assert.Contains("24-hour", consent);
            Assert.Contains("Do you !consent to being made into a new host?", consent);
        }

        [Fact]
        public void GetConsentWarning_PluralParasite_NoArticle()
        {
            SeedPair();
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string consent = _processor.GetConsentWarning(alice, bob, "tentacles");

            // Plural-shaped name — no article, no "a tentacles".
            Assert.Contains("with tentacles!", consent);
            Assert.DoesNotContain("with a tentacles", consent);
        }

        [Fact]
        public void GetCompletionMessage_DirectInfest_UsesArticleAndFlavor()
        {
            SeedPair();
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string completion = _processor.GetCompletionMessage(alice, bob, "paraslime");

            Assert.Contains("Alice has infested Bob with a paraslime", completion);
            Assert.Contains("Enjoy your future as a host", completion);
        }

        [Fact]
        public void GetCompletionMessage_InitiatorAlreadyCarries_SwitchesToSpreadFlavor()
        {
            SeedPair();
            var alice = _database.GetProfile("Alice");
            // Alice is already a host — the completion should frame the infest as a direct
            // spread instead of a fresh introduction.
            InfestProcessor.ApplyInfestation(alice, "paraslime", "Carol",
                spreadFromContact: false, gracePeriod: TimeSpan.Zero);
            _database.SetProfile("Alice", alice);
            alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string completion = _processor.GetCompletionMessage(alice, bob, "paraslime");

            Assert.Contains("Alice spreads their paraslime directly to Bob", completion);
            Assert.Contains("just like Alice has~", completion);
        }

        [Fact]
        public void ApplyInfestation_DirectInfest_GraceUntilEqualsNow()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").Build();
            DateTime before = DateTime.UtcNow;

            InfestProcessor.ApplyInfestation(bob, "paraslime", "Alice",
                spreadFromContact: false, gracePeriod: TimeSpan.FromHours(24));

            var parasites = ParasiteInstance.LoadAll(bob);
            // Direct infestations don't get a grace window; we use InfestedAt for GraceUntil
            // so a check against now is always false.
            Assert.False(parasites[0].SpreadFromContact);
            Assert.True(parasites[0].GraceUntil <= DateTime.UtcNow);
            Assert.True(parasites[0].GraceUntil >= before);
        }

        [Fact]
        public void ApplyInfestation_SpreadInfest_GraceUntilHonorsPeriod()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").Build();

            InfestProcessor.ApplyInfestation(bob, "paraslime", "Alice",
                spreadFromContact: true, gracePeriod: TimeSpan.FromHours(24));

            var parasites = ParasiteInstance.LoadAll(bob);
            Assert.True(parasites[0].SpreadFromContact);
            Assert.True(parasites[0].GraceUntil > DateTime.UtcNow.AddHours(23));
            Assert.True(parasites[0].GraceUntil < DateTime.UtcNow.AddHours(25));
        }

        [Fact]
        public void ApplyInfestation_AlreadyPresent_IsNoOp()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").Build();
            InfestProcessor.ApplyInfestation(bob, "paraslime", "Alice",
                spreadFromContact: false, gracePeriod: TimeSpan.Zero);

            InfestProcessor.ApplyInfestation(bob, "paraslime", "Carol",
                spreadFromContact: true, gracePeriod: TimeSpan.FromHours(24));

            var parasites = ParasiteInstance.LoadAll(bob);
            Assert.Single(parasites);
            // The original Alice-attributed direct infestation stays put.
            Assert.Equal("Alice", parasites[0].InfestedBy);
            Assert.False(parasites[0].SpreadFromContact);
        }

        private void SeedPair()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
        }

        private static PendingCommand BuildPending(string initiator, string recipient, string parasite)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = InfestProcessor.InfestType,
                    identifier = parasite,
                    investmentLevel = "consequence",
                    interactionTime = DateTime.UtcNow,
                },
            };
        }
    }
}
