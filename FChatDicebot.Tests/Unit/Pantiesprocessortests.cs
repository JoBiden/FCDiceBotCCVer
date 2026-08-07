using FChatDicebot;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Tests for PantiesProcessor — the processor behind !panties and !givepanties, and the
    /// first non-bottle collectible.
    ///
    /// Two things worth pinning hardest. First, roles follow the typed verb rather than who
    /// typed it, so !givepanties puts the pair in the *recipient's* collection with the
    /// initiator's name on it. Second, almost nothing here is panties-specific: the serial, the
    /// frozen subject name, the transfer and the privacy rule are all inherited from
    /// Collectible, which is the claim the whole framework was built to make.
    /// </summary>
    [Collection("Database")]
    public class PantiesProcessorTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;
        private readonly PantiesProcessor _processor;

        public PantiesProcessorTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
            _processor = new PantiesProcessor(_database);

            new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);
        }

        public void Dispose()
        {
            _fixture.Reset();
        }

        private PendingCommand Pending(string initiator, string recipient, string typedVerb)
        {
            var pending = new PendingCommand
            {
                Id = ObjectId.GenerateNewId(),
                pendingInteraction = new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = typedVerb,
                    identifier = PantiesProcessor.ComposeIdentifier(typedVerb, 0),
                    investmentLevel = "involved",
                }
            };
            _database.AddPendingCommand(pending);
            return pending;
        }

        private static List<Panties> PantiesOf(Profile profile)
        {
            return CollectionInventory.OfType<Panties>(profile);
        }

        // -------------------------------------------------------------------
        // Identity and roles
        // -------------------------------------------------------------------

        [Fact]
        public void InteractionType_IsPantiesForBothVerbs()
        {
            Assert.Equal("panties", _processor.InteractionType);
            Assert.Equal("involved", _processor.InvestmentLevel);
        }

        [Fact]
        public void Roles_AreInvertible_AcrossBothVerbs()
        {
            Assert.True(_processor.Roles.IsInvertible);
            Assert.Equal(
                new[] { PantiesProcessor.PantiesType, PantiesProcessor.GivePantiesType },
                _processor.Roles.Verbs.ToArray());
        }

        [Fact]
        public void ResolveHolder_FollowsTheTypedVerb()
        {
            // !panties = "may I have yours", so the asker holds.
            Assert.Equal("Alice", PantiesProcessor.ResolveHolder(
                PantiesProcessor.PantiesType, "Alice", "Bob"));
            // !givepanties = "please take mine", so the recipient holds.
            Assert.Equal("Bob", PantiesProcessor.ResolveHolder(
                PantiesProcessor.GivePantiesType, "Alice", "Bob"));
        }

        [Fact]
        public void ResolveSubject_IsTheMirrorOfResolveHolder()
        {
            Assert.Equal("Bob", PantiesProcessor.ResolveSubject(
                PantiesProcessor.PantiesType, "Alice", "Bob"));
            Assert.Equal("Alice", PantiesProcessor.ResolveSubject(
                PantiesProcessor.GivePantiesType, "Alice", "Bob"));
        }

        // -------------------------------------------------------------------
        // Acquisition — both directions
        // -------------------------------------------------------------------

        [Fact]
        public void Panties_MintsIntoTheInitiatorsCollection_WithTheRecipientAsSubject()
        {
            _processor.ProcessInteraction(Pending("Alice", "Bob", PantiesProcessor.PantiesType));

            var held = Assert.Single(PantiesOf(_database.GetProfile("Alice")));
            Assert.Equal("Bob", held.subjectName);
            Assert.True(held.serial > 0);

            Assert.Empty(PantiesOf(_database.GetProfile("Bob")));
        }

        [Fact]
        public void Givepanties_MintsIntoTheRecipientsCollection_WithTheInitiatorAsSubject()
        {
            _processor.ProcessInteraction(Pending("Alice", "Bob", PantiesProcessor.GivePantiesType));

            var held = Assert.Single(PantiesOf(_database.GetProfile("Bob")));
            Assert.Equal("Alice", held.subjectName);
            Assert.True(held.serial > 0);

            Assert.Empty(PantiesOf(_database.GetProfile("Alice")));
        }

        [Fact]
        public void Counts_FollowTheRoles_NotWhoTyped()
        {
            // The collector takes "give", the one parted from takes "take" — matching
            // milkgive/milktake, where "give" marks the party who performed the act.
            _processor.ProcessInteraction(Pending("Alice", "Bob", PantiesProcessor.PantiesType));

            Assert.Equal(1, _database.GetProfile("Alice").counts["pantiesgive"]);
            Assert.Equal(1, _database.GetProfile("Bob").counts["pantiestake"]);
        }

        [Fact]
        public void Counts_Invert_WithTheGiveVerb()
        {
            _processor.ProcessInteraction(Pending("Alice", "Bob", PantiesProcessor.GivePantiesType));

            Assert.Equal(1, _database.GetProfile("Bob").counts["pantiesgive"]);
            Assert.Equal(1, _database.GetProfile("Alice").counts["pantiestake"]);
        }

        // -------------------------------------------------------------------
        // What comes free from Collectible
        // -------------------------------------------------------------------

        [Fact]
        public void Panties_AreTransferableButNotSellable()
        {
            var pair = new Panties { serial = 1, subjectName = "Bob", acquiredAt = DateTime.UtcNow };

            Assert.False(pair.IsSellable);
            Assert.True(pair.IsTransferable);
            Assert.Equal("pair of panties", pair.TypeLabel);
        }

        [Fact]
        public void Sell_NeverTakesPanties_EvenWhenTheyAreAllYouHold()
        {
            _processor.ProcessInteraction(Pending("Alice", "Bob", PantiesProcessor.PantiesType));
            Profile alice = _database.GetProfile("Alice");

            var result = FChatDicebot.BotCommands.ChateauSell.SellBottles(alice, null, null, 5);

            Assert.Equal(0, result.BottlesSold);
            Assert.Single(PantiesOf(alice));
        }

        [Fact]
        public void SerialSpace_IsSharedWithBottles()
        {
            // The property the whole shared-counter decision exists for: serial #1 is the oldest
            // *item* in the Chateau, so a bottle and a pair of panties can never both be "#1".
            Profile alice = _database.GetProfile("Alice");
            alice.collectibles.Add(new MilkBottle
            {
                serial = _database.ClaimCollectibleSerials(1),
                subjectName = "Bob",
                acquiredAt = DateTime.UtcNow,
                substance = "milk",
                quantity = 1,
            });
            _database.SetCollectibles("Alice", alice.collectibles);

            _processor.ProcessInteraction(Pending("Alice", "Bob", PantiesProcessor.PantiesType));

            var serials = _database.GetProfile("Alice").collectibles.Select(c => c.serial).ToList();
            Assert.Equal(2, serials.Count);
            Assert.Equal(serials.Count, serials.Distinct().Count());
        }

        [Fact]
        public void BottleLookupBySerial_IgnoresPanties()
        {
            // Serials are shared, so !drink #N and !pay ... bottle #N must not resolve a pair of
            // panties. "You don't have bottle #N" is the right answer.
            _processor.ProcessInteraction(Pending("Alice", "Bob", PantiesProcessor.PantiesType));
            Profile alice = _database.GetProfile("Alice");
            int serial = PantiesOf(alice).Single().serial;

            Assert.NotNull(CollectionInventory.FindBySerial(alice, serial));
            Assert.Null(BottleInventory.FindBySerial(alice, serial));
            Assert.True(BottleInventory.IsCollectionEmpty(alice));
        }

        [Fact]
        public void SubjectName_IsFrozen_AndResolvedThroughDisplayName()
        {
            _processor.ProcessInteraction(Pending("Alice", "Bob", PantiesProcessor.PantiesType));

            // Bob renames. The stored subjectName does not follow, exactly as a bottle's donor
            // doesn't — display has to resolve it.
            Profile bob = _database.GetProfile("Bob");
            bob.displayName = "Roberta";
            _database.SetProfile("Bob", bob);

            var held = Assert.Single(PantiesOf(_database.GetProfile("Alice")));
            Assert.Equal("Bob", held.subjectName);
            Assert.Equal("Roberta", _database.GetDisplayName(held.subjectName));
        }

        [Fact]
        public void StoredPanties_CarryTheirOwnDiscriminator()
        {
            _processor.ProcessInteraction(Pending("Alice", "Bob", PantiesProcessor.PantiesType));

            var reloaded = Assert.Single(_database.GetProfile("Alice").collectibles);
            Assert.IsType<Panties>(reloaded);
        }

        // -------------------------------------------------------------------
        // The per-direction daily lock
        // -------------------------------------------------------------------

        [Fact]
        public void Validate_BlocksASecondPairFromTheSameSubjectToday()
        {
            _processor.ProcessInteraction(Pending("Alice", "Bob", PantiesProcessor.PantiesType));

            var validation = _processor.ValidateInteraction(
                "Alice", "Bob", null, PantiesProcessor.PantiesType);

            Assert.False(validation.IsValid);
            Assert.Contains("Bob", validation.ErrorMessage);
        }

        [Fact]
        public void TheLockIsDirectional_SoTheOtherSideCanStillAsk()
        {
            _processor.ProcessInteraction(Pending("Alice", "Bob", PantiesProcessor.PantiesType));

            // Bob asking Alice is a different direction and must be unaffected.
            var validation = _processor.ValidateInteraction(
                "Bob", "Alice", null, PantiesProcessor.PantiesType);

            Assert.True(validation.IsValid, validation.ErrorMessage);
        }

        [Fact]
        public void TheLockFollowsTheHolder_NotTheInitiator()
        {
            // Alice gives Bob a pair: Bob holds, so Bob's lock against Alice is what's stamped.
            _processor.ProcessInteraction(Pending("Alice", "Bob", PantiesProcessor.GivePantiesType));

            // Alice offering again is blocked, because Bob already holds a pair of hers today...
            var blocked = _processor.ValidateInteraction(
                "Alice", "Bob", null, PantiesProcessor.GivePantiesType);
            Assert.False(blocked.IsValid);

            // ...but Alice asking Bob for a pair of *his* is a different holder and subject.
            var allowed = _processor.ValidateInteraction(
                "Alice", "Bob", null, PantiesProcessor.PantiesType);
            Assert.True(allowed.IsValid, allowed.ErrorMessage);
        }

        [Fact]
        public void TocTou_SecondPendingResolvesToNothing_AndTellsTheInitiatorPrivately()
        {
            // Two pendings queued before either consented; the second must not mint a pair.
            var first = Pending("Alice", "Bob", PantiesProcessor.PantiesType);
            var second = Pending("Alice", "Bob", PantiesProcessor.PantiesType);

            _processor.ProcessInteraction(first);
            _processor.ProcessInteraction(second);

            Assert.Single(PantiesOf(_database.GetProfile("Alice")));

            string privateNote = _processor.GetAndClearInitiatorPrivateMessage();
            Assert.False(string.IsNullOrEmpty(privateNote));
            Assert.Contains("Bob", privateNote);

            // And the channel says nothing rather than announcing a pair that wasn't handed over.
            Assert.Equal(string.Empty, _processor.GetCompletionMessage(
                _database.GetProfile("Alice"),
                _database.GetProfile("Bob"),
                second.pendingInteraction.identifier));
        }

        // -------------------------------------------------------------------
        // Validation
        // -------------------------------------------------------------------

        [Fact]
        public void Validate_RejectsSelfTarget()
        {
            var validation = _processor.ValidateInteraction(
                "Alice", "Alice", null, PantiesProcessor.PantiesType);

            Assert.False(validation.IsValid);
            Assert.Equal(PantiesProcessor.SelfTargetText, validation.ErrorMessage);
        }

        // -------------------------------------------------------------------
        // Messages
        // -------------------------------------------------------------------

        [Fact]
        public void CompletionMessage_NamesTheHolderAndTheSubject_ForBothVerbs()
        {
            Profile alice = _database.GetProfile("Alice");
            Profile bob = _database.GetProfile("Bob");

            string asked = _processor.GetCompletionMessage(alice, bob,
                PantiesProcessor.ComposeIdentifier(PantiesProcessor.PantiesType, 7));
            Assert.StartsWith("Alice tucks away the panties from Bob", asked);
            Assert.Contains("#7", asked);

            string given = _processor.GetCompletionMessage(alice, bob,
                PantiesProcessor.ComposeIdentifier(PantiesProcessor.GivePantiesType, 8));
            Assert.StartsWith("Bob tucks away the panties from Alice", given);
            Assert.Contains("#8", given);
        }

        [Fact]
        public void ConsentWarning_AsksOppositeQuestions_AndAlwaysOffersTheDecline()
        {
            Profile alice = _database.GetProfile("Alice");
            Profile bob = _database.GetProfile("Bob");

            string asked = _processor.GetConsentWarning(alice, bob, null, PantiesProcessor.PantiesType);
            Assert.Contains("wants a pair of panties from Bob", asked);
            Assert.Contains("parting with them", asked);

            string given = _processor.GetConsentWarning(alice, bob, null, PantiesProcessor.GivePantiesType);
            Assert.Contains("would like Bob to have a pair", given);
            Assert.Contains("keeping them", given);

            // The decline reminder is applied by the base entry point, never hand-written.
            Assert.Contains("(or !no)", asked);
            Assert.Contains("(or !no)", given);
        }

        [Fact]
        public void ConsentWarning_CarriesNoSeriousnessBlock()
        {
            // The bold "should not be taken lightly" block is the commitment/consequence shape.
            // This is an involved interaction, and !milk / !drinkfrom set the precedent of
            // stating neither the block nor the frequency in the prompt.
            string warning = _processor.GetConsentWarning(
                _database.GetProfile("Alice"), _database.GetProfile("Bob"), null, PantiesProcessor.PantiesType);

            Assert.DoesNotContain("[b]", warning);
            Assert.DoesNotContain("should not be taken lightly", warning);
        }

        [Fact]
        public void NoCooldownSpec_BecauseTheLockIsDirectional()
        {
            // CooldownSpec cannot express "per-direction" — its nearest value renders
            // "per pair", which would claim Alice taking Bob's pair also blocks Bob taking
            // Alice's. TheLockIsDirectional_SoTheOtherSideCanStillAsk proves it does not.
            Assert.Null(_processor.CooldownRule);
        }

        [Fact]
        public void NoUserFacingStringMentionsUtc()
        {
            Profile alice = _database.GetProfile("Alice");
            Profile bob = _database.GetProfile("Bob");

            var strings = new[]
            {
                _processor.GetConsentWarning(alice, bob, null, PantiesProcessor.PantiesType),
                _processor.GetConsentWarning(alice, bob, null, PantiesProcessor.GivePantiesType),
                _processor.GetCompletionMessage(alice, bob,
                    PantiesProcessor.ComposeIdentifier(PantiesProcessor.PantiesType, 3)),
                PantiesProcessor.DirectionLockMessage("Bob"),
                PantiesProcessor.SelfTargetText,
            };

            foreach (string s in strings)
            {
                Assert.DoesNotContain("UTC", s, StringComparison.OrdinalIgnoreCase);
                // House rule: no em-dashes in resident-facing text.
                Assert.DoesNotContain("—", s);
            }
        }
    }
}
