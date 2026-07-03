using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    [Collection("Database")]
    public class BreedProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly BreedProcessor _processor;
        private readonly Random _savedRng;

        public BreedProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new BreedProcessor(_database);

            // Default to a deterministic RNG that never triggers the rare-twin event
            // (Sample()=0.5 → Next(100)=50, never 0). Individual tests can override.
            _savedRng = BreedProcessor.Rng;
            BreedProcessor.Rng = new FixedSampleRandom(0.5);
        }

        public void Dispose()
        {
            BreedProcessor.Rng = _savedRng;
        }

        [Fact]
        public void InteractionType_ReturnsBreed()
        {
            Assert.Equal("breed", _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsCommitment()
        {
            Assert.Equal("commitment", _processor.InvestmentLevel);
        }

        [Fact]
        public void GetInteractionVerb_PastTense_ReturnsBred()
        {
            Assert.Equal("bred", _processor.GetInteractionVerb(InteractionProcessorBase.VerbTense.Past));
        }

        [Fact]
        public void ValidateInteraction_EmptyMonster_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_UnknownMonster_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var result = _processor.ValidateInteraction("Alice", "Bob", "unknownmonster");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_IdentifierMissingMonsterCategory_ReturnsFailure()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            // Identifier exists but isn't a monster (e.g. it's a scent) — should be rejected.
            _fixture.SeedIdentifier(new Identifier
            {
                type = "musk",
                description = "musk",
                categories = new[] { "scent" }
            });

            var result = _processor.ValidateInteraction("Alice", "Bob", "musk");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_KnownMonster_ReturnsSuccess()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "slime",
                description = "a viscous slime",
                categories = new[] { "monster" }
            });

            var result = _processor.ValidateInteraction("Alice", "Bob", "slime");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_DirectionLockActive_ReturnsFailure()
        {
            // Regression test for H4: without this recheck, a second !breed pending
            // against the same recipient (e.g. resolved via !consent all) could land the
            // same day and produce a second pregnancy.
            new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithTimer(BreedProcessor.DirectionTimerKey("Bob"), DateTime.UtcNow.AddHours(12))
                .BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier { type = "slime", description = "a viscous slime", categories = new[] { "monster" } });

            var result = _processor.ValidateInteraction("Alice", "Bob", "slime");

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_DirectionLockExpired_ReturnsSuccess()
        {
            new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithTimer(BreedProcessor.DirectionTimerKey("Bob"), DateTime.UtcNow.AddHours(-1))
                .BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier { type = "slime", description = "a viscous slime", categories = new[] { "monster" } });

            var result = _processor.ValidateInteraction("Alice", "Bob", "slime");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_DirectionLockIsPerRecipient_OtherRecipientUnaffected()
        {
            new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithTimer(BreedProcessor.DirectionTimerKey("Bob"), DateTime.UtcNow.AddHours(12))
                .BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier { type = "slime", description = "a viscous slime", categories = new[] { "monster" } });

            var result = _processor.ValidateInteraction("Alice", "Carol", "slime");

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ProcessInteraction_DirectionLockActive_NoOpsAndDropsPending()
        {
            // TOCTOU belt-and-suspenders: even if ValidateInteraction were bypassed,
            // ProcessInteraction itself must not add a second pregnancy.
            new ProfileBuilder()
                .WithUserName("Alice")
                .WithDisplayName("Alice")
                .WithTimer(BreedProcessor.DirectionTimerKey("Bob"), DateTime.UtcNow.AddHours(12))
                .BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier { type = "slime", description = "a viscous slime", categories = new[] { "monster" } });

            var pendingCommand = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction { initiator = "Alice", recipient = "Bob", type = "breed", identifier = "slime", investmentLevel = "commitment" }
            };
            _database.AddPendingCommand(pendingCommand);

            string result = _processor.ProcessInteraction(pendingCommand);

            Assert.Equal("NoInteraction", result);
            var bobProfile = _database.GetProfile("Bob");
            Assert.True(bobProfile.pregnancies == null || bobProfile.pregnancies.Count == 0);
        }

        [Fact]
        public void ProcessInteraction_AppendsPregnancyToRecipient()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "slime",
                description = "a viscous slime",
                categories = new[] { "monster" },
                gestationDays = 3,
                broodSizeMin = 2,
                broodSizeMax = 2
            });

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "slime");
            _database.AddPendingCommand(pendingCommand);

            DateTime before = DateTime.UtcNow;
            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            Assert.Single(bob.pregnancies);
            var pregnancy = bob.pregnancies.First();
            Assert.Equal("Alice", pregnancy.Initiator);
            Assert.Equal("slime", pregnancy.MonsterType);
            Assert.Equal(2, pregnancy.BroodSize);
            Assert.False(pregnancy.IsRareTwins); // not a twin event — min was already 2
            Assert.True(pregnancy.ConceivedAt >= before.AddSeconds(-1));
            Assert.True(pregnancy.ReadyAt >= pregnancy.ConceivedAt.AddDays(3).AddSeconds(-1));
            Assert.True(pregnancy.ReadyAt <= pregnancy.ConceivedAt.AddDays(3).AddSeconds(1));
            Assert.False(string.IsNullOrEmpty(pregnancy.Id));
        }

        [Fact]
        public void ProcessInteraction_StoresMonsterCategoriesOnPregnancy()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "lamia",
                categories = new[] { "monster", "snake", "beast", "mount" }
            });

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "lamia")));

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            Assert.NotNull(pregnancy.Categories);
            Assert.Equal(new[] { "monster", "snake", "beast", "mount" }, pregnancy.Categories.ToArray());
        }

        [Fact]
        public void ProcessInteraction_UnknownMonster_UsesDefaults()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "mystery");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            // Default brood is 1, but the twin RNG check runs here. With our fixture's
            // Sample()=0.5 RNG, Next(100) returns 50 → never triggers the twin event.
            Assert.Equal(1, pregnancy.BroodSize);
            Assert.False(pregnancy.IsRareTwins);
            Assert.True(pregnancy.ReadyAt <= pregnancy.ConceivedAt.AddDays(1).AddSeconds(1));
        }

        [Fact]
        public void ProcessInteraction_BroodSizeWithinRange()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "wasp",
                description = "a wasp brood",
                categories = new[] { "monster" },
                gestationDays = 1,
                broodSizeMin = 3,
                broodSizeMax = 7
            });

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "wasp");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            Assert.InRange(pregnancy.BroodSize, 3, 7);
        }

        [Fact]
        public void ProcessInteraction_GestationCappedAtSevenDays()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "elephant",
                description = "an elephant",
                categories = new[] { "monster" },
                gestationDays = 365,
                broodSizeMin = 1,
                broodSizeMax = 1
            });

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "elephant");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            Assert.True(pregnancy.ReadyAt <= pregnancy.ConceivedAt.AddDays(BreedProcessor.MaxGestationDays).AddSeconds(1));
        }

        [Fact]
        public void ProcessInteraction_SetsDirectionCooldownOnInitiatorOnly()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "slime");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            // Only the breeder (Alice) is locked against the bred resident (Bob).
            Assert.True(alice.timers.ContainsKey(BreedProcessor.DirectionTimerKey("Bob")));
            Assert.True(alice.timers[BreedProcessor.DirectionTimerKey("Bob")].timerEnd > DateTime.UtcNow);
            // Bob is NOT locked against Alice — he can breed her back the same day.
            Assert.False(bob.timers != null && bob.timers.ContainsKey(BreedProcessor.DirectionTimerKey("Alice")));
        }

        [Fact]
        public void ProcessInteraction_MultiplePregnanciesAccumulate()
        {
            // Multiple pregnancies accumulate via different breeders in the same day — the
            // per-direction lock only blocks the *same* breeder re-breeding the *same*
            // recipient (see ProcessInteraction_DirectionLockActive_NoOpsAndDropsPending
            // and H4 above for that case).
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Charlie").WithDisplayName("Charlie").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "slime")));
            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Charlie", "Bob", "wasp")));

            var bob = _database.GetProfile("Bob");
            Assert.Equal(2, bob.pregnancies.Count);
            Assert.Contains(bob.pregnancies, p => p.MonsterType == "slime");
            Assert.Contains(bob.pregnancies, p => p.MonsterType == "wasp");
        }

        [Fact]
        public void ProcessInteraction_CategoryFallback_SnakeOnly_UsesSnakeDefaults()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "basilisk",
                description = "a basilisk",
                categories = new[] { "snake" }
            });

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "basilisk");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            Assert.InRange(pregnancy.BroodSize, 4, 8);
            Assert.True(pregnancy.ReadyAt >= pregnancy.ConceivedAt.AddDays(3).AddSeconds(-1));
            Assert.True(pregnancy.ReadyAt <= pregnancy.ConceivedAt.AddDays(3).AddSeconds(1));
        }

        [Fact]
        public void ProcessInteraction_MultipleCategories_HighestPriorityWins()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "lamia",
                description = "lamia",
                categories = new[] { "monster", "snake", "beast", "mount", "carapace", "poison" }
            });

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "lamia");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            Assert.InRange(pregnancy.BroodSize, 4, 8);
            Assert.True(pregnancy.ReadyAt <= pregnancy.ConceivedAt.AddDays(3).AddSeconds(1));
        }

        [Fact]
        public void ProcessInteraction_InsectCategory_HasLargeBrood()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "bee",
                categories = new[] { "monster", "insect", "flight" }
            });

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "bee");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            Assert.InRange(pregnancy.BroodSize, 5, 15);
        }

        [Fact]
        public void ProcessInteraction_OnlyCosmeticCategories_UsesFallback()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "vial",
                categories = new[] { "poison", "cutting" }
            });

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "vial");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            Assert.Equal(1, pregnancy.BroodSize);
            Assert.True(pregnancy.ReadyAt <= pregnancy.ConceivedAt.AddDays(1).AddSeconds(1));
        }

        [Fact]
        public void ProcessInteraction_ExplicitGestationOverridesCategory_BroodInheritsFromCategory()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "dracoling",
                categories = new[] { "dragon" },
                gestationDays = 2
            });

            var pendingCommand = BuildPendingCommand("Alice", "Bob", "dracoling");
            _database.AddPendingCommand(pendingCommand);

            _processor.ProcessInteraction(pendingCommand);

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            Assert.True(pregnancy.ReadyAt <= pregnancy.ConceivedAt.AddDays(2).AddSeconds(1));
            Assert.Equal(1, pregnancy.BroodSize); // inherited from dragon (min=max=1) — twin chance disabled by RNG
        }

        [Fact]
        public void ResolveCategoryDefault_LamiaCategories_ReturnsSnakeEntry()
        {
            var identifier = new Identifier
            {
                type = "lamia",
                categories = new[] { "monster", "snake", "beast", "mount", "carapace", "poison" }
            };
            var resolved = BreedProcessor.ResolveCategoryDefault(identifier);
            Assert.True(resolved.HasValue);
            Assert.Equal("snake", resolved.Value.Category);
        }

        [Fact]
        public void ResolveCategoryDefault_NullIdentifier_ReturnsNull()
        {
            Assert.Null(BreedProcessor.ResolveCategoryDefault(null));
        }

        [Fact]
        public void ResolveCategoryDefault_EmptyCategories_ReturnsNull()
        {
            var identifier = new Identifier { type = "mystery", categories = new string[0] };
            Assert.Null(BreedProcessor.ResolveCategoryDefault(identifier));
        }

        // ─── Rare twin tests ──────────────────────────────────────────────────

        [Fact]
        public void RollBroodSize_MinMaxBothOne_NoTwinTrigger_ReturnsOne()
        {
            int result = BreedProcessor.RollBroodSize(1, 1, new FixedSampleRandom(0.5), out bool isTwins);
            Assert.Equal(1, result);
            Assert.False(isTwins);
        }

        [Fact]
        public void RollBroodSize_MinMaxBothOne_TwinTrigger_ReturnsTwoAndSetsFlag()
        {
            int result = BreedProcessor.RollBroodSize(1, 1, new FixedSampleRandom(0.0), out bool isTwins);
            Assert.Equal(2, result);
            Assert.True(isTwins);
        }

        [Fact]
        public void RollBroodSize_MinMaxBothTwo_NoTwinChanceFires()
        {
            // Twin chance is gated on min==max==1 specifically.
            int result = BreedProcessor.RollBroodSize(2, 2, new FixedSampleRandom(0.0), out bool isTwins);
            Assert.Equal(2, result);
            Assert.False(isTwins);
        }

        [Fact]
        public void ProcessInteraction_RareTwinTriggers_PregnancyFlaggedAndBroodIsTwo()
        {
            BreedProcessor.Rng = new FixedSampleRandom(0.0); // Next(100) → 0 → twin fires

            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "dragon",
                categories = new[] { "dragon" } // dragon → min=max=1
            });

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "dragon")));

            var bob = _database.GetProfile("Bob");
            var pregnancy = bob.pregnancies.Single();
            Assert.Equal(2, pregnancy.BroodSize);
            Assert.True(pregnancy.IsRareTwins);
        }

        // ─── Global counter tests ─────────────────────────────────────────────

        [Fact]
        public void ProcessInteraction_IncrementsGlobalPregnancyCounters()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "lamia",
                categories = new[] { "monster", "snake", "beast" }
            });

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "lamia")));

            var monsterStats = _database.GetMonsterStats(BreedProcessor.MonsterStatsKey("lamia"));
            Assert.NotNull(monsterStats);
            Assert.Equal(1, monsterStats.PregnancyCount);
            Assert.Equal(0, monsterStats.OffspringCount); // breed doesn't bump offspring

            Assert.Equal(1, _database.GetMonsterStats(BreedProcessor.CategoryStatsKey("monster")).PregnancyCount);
            Assert.Equal(1, _database.GetMonsterStats(BreedProcessor.CategoryStatsKey("snake")).PregnancyCount);
            Assert.Equal(1, _database.GetMonsterStats(BreedProcessor.CategoryStatsKey("beast")).PregnancyCount);
        }

        [Fact]
        public void ProcessInteraction_GlobalPregnancyCountersAccumulateAcrossBreeds()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").BuildAndSave(_database);
            _fixture.SeedIdentifier(new Identifier
            {
                type = "slime",
                categories = new[] { "slime" }
            });

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "slime")));
            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Carol", "slime")));

            Assert.Equal(2, _database.GetMonsterStats(BreedProcessor.MonsterStatsKey("slime")).PregnancyCount);
            Assert.Equal(2, _database.GetMonsterStats(BreedProcessor.CategoryStatsKey("slime")).PregnancyCount);
        }

        [Fact]
        public void ProcessInteraction_UnknownMonster_StillBumpsMonsterTypeCounter()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            // No identifier seeded — categories will be empty but the monster-type key
            // should still be incremented.
            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "mystery")));

            var stats = _database.GetMonsterStats(BreedProcessor.MonsterStatsKey("mystery"));
            Assert.NotNull(stats);
            Assert.Equal(1, stats.PregnancyCount);
        }

        [Fact]
        public void ProcessInteraction_PregnancyPersistsAcrossReloads()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            _processor.ProcessInteraction(SaveAndReturn(BuildPendingCommand("Alice", "Bob", "slime")));

            var reloaded = _database.GetProfile("Bob");
            Assert.Single(reloaded.pregnancies);
            Assert.Equal("slime", reloaded.pregnancies[0].MonsterType);
        }

        private PendingCommand BuildPendingCommand(string initiator, string recipient, string monster)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = "breed",
                    identifier = monster,
                    investmentLevel = "commitment"
                }
            };
        }

        private PendingCommand SaveAndReturn(PendingCommand pendingCommand)
        {
            _database.AddPendingCommand(pendingCommand);
            return pendingCommand;
        }

        /// <summary>
        /// A Random whose Sample() always returns the same fixed value. Because Random's
        /// public methods (Next(int), Next(int,int)) are implemented in terms of Sample,
        /// overriding it gives us deterministic outcomes for both single-arg and
        /// range-arg Next calls. Pass 0.5 for "middle of every range, never zero" or 0.0
        /// for "always lower bound / always trigger rare events keyed on Next(N)==0."
        /// </summary>
        private class FixedSampleRandom : Random
        {
            private readonly double _sample;
            public FixedSampleRandom(double sample) { _sample = sample; }
            protected override double Sample() => _sample;
        }
    }
}
