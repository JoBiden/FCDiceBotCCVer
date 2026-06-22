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
    /// Tests for CurseProcessor — validation, persistence shape, cooldown placement,
    /// Costume passive (random outfit if undressed), and consent wording. Disabler
    /// blocker / modifier fragment surfacing lives in CurseStatusContributorTests;
    /// cleanse lives in ChateauCleanseTests.
    /// </summary>
    [Collection("Database")]
    public class CurseProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly CurseProcessor _processor;

        public CurseProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new CurseProcessor(_database);

            // Descriptions mirror IdentifiersSnapshot.json — the consent warning surfaces
            // the description verbatim, so test seeds need to match production wording.
            _fixture.SeedIdentifier(new Identifier
            {
                type = "chastity",
                description = "A curse that prevents one from achieving !climax.",
                categories = new[] { CurseProcessor.CurseCategory },
            });
            _fixture.SeedIdentifier(new Identifier
            {
                type = "mooing",
                description = "A curse that will leave everyone wondering whether you're really a cow.",
                categories = new[] { CurseProcessor.CurseCategory },
            });
            _fixture.SeedIdentifier(new Identifier
            {
                type = "costume",
                description = "A curse that locks someone in costume.",
                categories = new[] { CurseProcessor.CurseCategory },
            });
            // A non-curse identifier to verify the category gate.
            _fixture.SeedIdentifier(new Identifier
            {
                type = "musk",
                description = "musk",
                categories = new[] { "scent" },
            });
            // An "attire" pool so the Costume passive has something to pick from.
            _fixture.SeedIdentifier(new Identifier
            {
                type = "wedding",
                description = "A wedding outfit.",
                categories = new[] { "attire" },
            });
            _fixture.SeedIdentifier(new Identifier
            {
                type = "bondage",
                description = "A bondage outfit.",
                categories = new[] { "attire" },
            });
            _fixture.SeedIdentifier(new Identifier
            {
                type = "nude",
                description = "The absence of attire.",
                categories = new[] { "attire" },
            });
        }

        public void Dispose() { }

        [Fact]
        public void InteractionType_ReturnsCurse()
        {
            Assert.Equal(CurseProcessor.CurseType, _processor.InteractionType);
        }

        [Fact]
        public void InvestmentLevel_ReturnsConsequence()
        {
            Assert.Equal("consequence", _processor.InvestmentLevel);
        }

        [Fact]
        public void ValidateInteraction_EmptyCurse_ReturnsFailure()
        {
            SeedPair();
            var result = _processor.ValidateInteraction("Alice", "Bob", "");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_UnknownCurse_ReturnsFailure()
        {
            SeedPair();
            var result = _processor.ValidateInteraction("Alice", "Bob", "ghostly-jinx");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_NonCurseCategory_ReturnsFailure()
        {
            // "musk" is a scent, not a curse — must be rejected.
            SeedPair();
            var result = _processor.ValidateInteraction("Alice", "Bob", "musk");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_KnownCurse_Succeeds()
        {
            SeedPair();
            var result = _processor.ValidateInteraction("Alice", "Bob", "chastity");
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateInteraction_AlreadyCursedSame_Rejects()
        {
            SeedPair();
            var bob = _database.GetProfile("Bob");
            CurseProcessor.ApplyCurse(bob, "chastity", "Alice");
            _database.SetProfile("Bob", bob);

            var result = _processor.ValidateInteraction("Alice", "Bob", "chastity");

            Assert.False(result.IsValid);
            Assert.Contains("chastity", result.ErrorMessage);
        }

        [Fact]
        public void ProcessInteraction_FirstCurse_PersistsAsInstance()
        {
            SeedPair();
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "chastity"));

            var bob = _database.GetProfile("Bob");
            var curses = CurseInstance.LoadAll(bob);
            Assert.Single(curses);
            Assert.Equal("chastity", curses[0].Curse);
            Assert.Equal("Alice", curses[0].AppliedBy);
        }

        [Fact]
        public void ProcessInteraction_SetsCooldownOnInitiatorByCurseAndRecipient()
        {
            SeedPair();
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "chastity"));

            var alice = _database.GetProfile("Alice");
            string key = CurseProcessor.CooldownTimerKey("chastity", "Bob");
            Assert.True(alice.timers.ContainsKey(key));
            Assert.True(alice.timers[key].timerEnd > DateTime.UtcNow.AddDays(6));
            Assert.True(alice.timers[key].timerEnd <= DateTime.UtcNow.AddDays(7).AddMinutes(1));
        }

        [Fact]
        public void ProcessInteraction_DifferentRecipient_DoesNotShareCooldown()
        {
            SeedPair();
            new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").BuildAndSave(_database);
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "chastity"));

            var alice = _database.GetProfile("Alice");
            Assert.True(alice.timers.ContainsKey(CurseProcessor.CooldownTimerKey("chastity", "Bob")));
            Assert.False(alice.timers.ContainsKey(CurseProcessor.CooldownTimerKey("chastity", "Carol")));
        }

        [Fact]
        public void ProcessInteraction_SelfCurse_PersistsOnSameProfile()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            _processor.ProcessInteraction(BuildPending("Alice", "Alice", "chastity"));

            var alice = _database.GetProfile("Alice");
            var curses = CurseInstance.LoadAll(alice);
            Assert.Single(curses);
            Assert.True(alice.timers.ContainsKey(CurseProcessor.CooldownTimerKey("chastity", "Alice")));
        }

        [Fact]
        public void ProcessInteraction_DeletesPendingCommand()
        {
            SeedPair();
            var pending = BuildPending("Alice", "Bob", "chastity");
            _database.AddPendingCommand(pending);

            _processor.ProcessInteraction(pending);

            Assert.Null(_database.GetPendingCommand(pending.Id));
        }

        [Fact]
        public void ProcessInteraction_CostumeCurseWhenUndressed_AssignsRandomOutfit()
        {
            SeedPair();
            var bob = _database.GetProfile("Bob");
            // Confirm undressed precondition.
            Assert.False(bob.characteristics.ContainsKey("attire"));

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "costume"));

            bob = _database.GetProfile("Bob");
            Assert.True(bob.characteristics.ContainsKey("attire"));
            string attire = bob.characteristics["attire"];
            // Passive picks from non-"nude" attire — wedding or bondage given our seed set.
            Assert.NotEqual("nude", attire);
            Assert.False(string.IsNullOrEmpty(attire));
        }

        [Fact]
        public void ProcessInteraction_CostumeCurseWhenAlreadyDressed_LeavesAttireUntouched()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob")
                .WithCharacteristic("attire", "regal")
                .BuildAndSave(_database);

            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "costume"));

            var bob = _database.GetProfile("Bob");
            Assert.Equal("regal", bob.characteristics["attire"]);
        }

        [Fact]
        public void ProcessInteraction_NonCostumeCurse_DoesNotTouchAttire()
        {
            SeedPair();
            _processor.ProcessInteraction(BuildPending("Alice", "Bob", "chastity"));

            var bob = _database.GetProfile("Bob");
            Assert.False(bob.characteristics.ContainsKey("attire"));
        }

        [Fact]
        public void CatalogMap_EveryEntry_HasNonRandomCurseCleanseCost()
        {
            // Defensive: cleanse must never recursively apply another curse.
            foreach (var kv in CurseProcessor.CatalogMap)
            {
                Assert.NotEqual(PurgeCostType.RandomCurse, kv.Value.CleanseCost);
            }
        }

        [Fact]
        public void CatalogMap_DisablersAllHaveBlocksOrAreSpecial()
        {
            // Poverty is the one disabler whose effect lives outside the contributor
            // (in !work / !volunteer); every other disabler must have at least one
            // BlockedInteractions entry so the contributor can emit a blocker.
            foreach (var kv in CurseProcessor.CatalogMap)
            {
                if (kv.Value.Bucket != CurseProcessor.CurseBucket.Disabler) continue;
                if (string.Equals(kv.Key, "poverty", StringComparison.OrdinalIgnoreCase)) continue;
                Assert.True(
                    kv.Value.BlockedInteractions != null && kv.Value.BlockedInteractions.Count > 0,
                    "Disabler '" + kv.Key + "' has no BlockedInteractions");
            }
        }

        [Fact]
        public void CatalogMap_ModifiersAllHaveTemplate()
        {
            foreach (var kv in CurseProcessor.CatalogMap)
            {
                if (kv.Value.Bucket != CurseProcessor.CurseBucket.Modifier) continue;
                Assert.False(string.IsNullOrEmpty(kv.Value.ModifierTemplate),
                    "Modifier '" + kv.Key + "' has no template");
                Assert.Contains("{subject}", kv.Value.ModifierTemplate);
            }
        }

        [Fact]
        public void GetConsentWarning_MentionsCurseCostAndCanonicalWarning()
        {
            SeedPair();
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string consent = _processor.GetConsentWarning(alice, bob, "chastity");

            Assert.Contains("[b]chastity[/b]", consent);
            // The chastity Identifier description must surface inside the warning.
            Assert.Contains("achieving !climax", consent);
            Assert.Contains("missing a day of work", consent);
            // Mirrors the standard Consequence-tier warning, now stating the real per-curse
            // cooldown and a recipient-framed cleanse note.
            Assert.Contains("This should not be taken lightly.", consent);
            Assert.Contains("can only afflict you with a given curse once per week", consent);
            Assert.Contains("you'll need to !cleanse", consent);
            Assert.Contains("Do you !consent", consent);
        }

        [Fact]
        public void GetConsentWarning_DoesNotExposeBucketVocabulary()
        {
            // Disabler/modifier is an implementation detail — players never see it.
            SeedPair();
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string disablerConsent = _processor.GetConsentWarning(alice, bob, "chastity");
            string modifierConsent = _processor.GetConsentWarning(alice, bob, "mooing");

            Assert.DoesNotContain("disabler", disablerConsent);
            Assert.DoesNotContain("modifier", modifierConsent);
        }

        [Fact]
        public void GetCompletionMessage_NamesBothPartiesAndCurse()
        {
            SeedPair();
            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");

            string completion = _processor.GetCompletionMessage(alice, bob, "chastity");

            Assert.Contains("Alice", completion);
            Assert.Contains("Bob", completion);
            Assert.Contains("[b]chastity[/b]", completion);
            Assert.Contains("Surely nothing ill will come of this", completion);
        }

        [Fact]
        public void ApplyCurse_AlreadyPresent_IsNoOp()
        {
            var bob = new ProfileBuilder().WithUserName("Bob").Build();
            CurseProcessor.ApplyCurse(bob, "chastity", "Alice");

            CurseProcessor.ApplyCurse(bob, "chastity", "Carol");

            var curses = CurseInstance.LoadAll(bob);
            Assert.Single(curses);
            // Original Alice-attributed instance is preserved.
            Assert.Equal("Alice", curses[0].AppliedBy);
        }

        private void SeedPair()
        {
            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
        }

        private static PendingCommand BuildPending(string initiator, string recipient, string curseName)
        {
            return new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = CurseProcessor.CurseType,
                    identifier = curseName,
                    investmentLevel = "consequence",
                    interactionTime = DateTime.UtcNow,
                },
            };
        }
    }
}
