using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Casual;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Tests for personal bodypart eicons (<c>!seteicon ass …</c>): storage on the profile,
    /// token resolution in the command, which interactions render whose part, and the
    /// identifier-eicon readout in <c>!whatis</c>.
    /// </summary>
    [Collection("Database")]
    public class BodypartEiconTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public BodypartEiconTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        private void SeedBodypart(string type)
        {
            _fixture.SeedIdentifier(new Identifier
            {
                type = type,
                description = "A part of the body.",
                categories = new[] { "bodypart" },
            });
        }

        // ---- 1. Storage round-trip / clear / null-safety ----

        [Fact]
        public void SetBodypartEicon_ThenGet_Roundtrips()
        {
            var p = new ProfileBuilder().Build();
            InteractionEiconSupport.SetBodypartEicon(p, "ass", "[eicon]MyBooty[/eicon]");
            Assert.Equal("[eicon]MyBooty[/eicon]", InteractionEiconSupport.GetBodypartEicon(p, "ass"));
        }

        [Fact]
        public void SetBodypartEicon_IsCaseInsensitiveOnThePart()
        {
            var p = new ProfileBuilder().Build();
            InteractionEiconSupport.SetBodypartEicon(p, "Ass", "[eicon]MyBooty[/eicon]");
            Assert.Equal("[eicon]MyBooty[/eicon]", InteractionEiconSupport.GetBodypartEicon(p, "ass"));
        }

        [Fact]
        public void ClearBodypartEicon_RemovesIt()
        {
            var p = new ProfileBuilder().Build();
            InteractionEiconSupport.SetBodypartEicon(p, "ass", "[eicon]MyBooty[/eicon]");
            InteractionEiconSupport.ClearBodypartEicon(p, "ass");
            Assert.Equal(string.Empty, InteractionEiconSupport.GetBodypartEicon(p, "ass"));
        }

        [Fact]
        public void BodypartEiconAccessors_AreNullSafe()
        {
            Assert.Equal(string.Empty, InteractionEiconSupport.GetBodypartEicon(null, "ass"));
            Assert.Equal(string.Empty, InteractionEiconSupport.GetBodypartEicon(new ProfileBuilder().Build(), null));
            InteractionEiconSupport.SetBodypartEicon(null, "ass", "[eicon]x[/eicon]");   // no throw
            InteractionEiconSupport.ClearBodypartEicon(null, "ass");                     // no throw
            Assert.Empty(InteractionEiconSupport.GetAllBodypartEicons(null));
        }

        // ---- 2. Key namespace never collides with an interaction key ----

        [Fact]
        public void BodypartKey_IsPrefixed_AndNeverCollidesWithAnyInteractionKey()
        {
            var p = new ProfileBuilder().Build();
            InteractionEiconSupport.SetBodypartEicon(p, "ass", "[eicon]MyBooty[/eicon]");
            Assert.True(p.characteristics.ContainsKey("eicon_part_ass"));

            // Set every canonical interaction eicon; none of them may land in the bodypart
            // namespace, and none of them may overwrite the stored part.
            foreach (string token in InteractionEiconSupport.CanonicalTokensInOrder)
            {
                Assert.True(InteractionEiconSupport.TryResolveTokenToVerbKeys(token, out var verbKeys));
                foreach (string verbKey in verbKeys)
                {
                    InteractionEiconSupport.SetInteractionEicon(p, verbKey, "[eicon]iv[/eicon]");
                }
            }

            foreach (var key in p.characteristics.Keys)
            {
                if (key == "eicon_part_ass") continue;
                Assert.False(key.StartsWith(InteractionEiconSupport.BodypartKeyPrefix, StringComparison.Ordinal));
            }
            Assert.Equal("[eicon]MyBooty[/eicon]", InteractionEiconSupport.GetBodypartEicon(p, "ass"));
        }

        // ---- 3. Token resolution in !seteicon ----

        [Fact]
        public void ResolveBodypart_FindsBodypartIdentifier()
        {
            SeedBodypart("ass");
            Assert.Equal("ass", ChateauSeteicon.ResolveBodypart("ass"));
            Assert.Equal("ass", ChateauSeteicon.ResolveBodypart("ASS"));
        }

        [Fact]
        public void ResolveBodypart_RejectsNonBodypartIdentifier()
        {
            _fixture.SeedIdentifier(new Identifier
            {
                type = "milk",
                description = "A substance.",
                categories = new[] { "substance" },
            });
            Assert.Null(ChateauSeteicon.ResolveBodypart("milk"));
        }

        [Fact]
        public void ResolveBodypart_RejectsUnknownToken()
        {
            Assert.Null(ChateauSeteicon.ResolveBodypart("nonsense"));
        }

        [Fact]
        public void InteractionTokenStillWins_OverAnySameNamedBodypart()
        {
            // Precedence guard: if a bodypart identifier ever shared a name with an
            // interaction, the interaction resolution runs first and claims the token.
            SeedBodypart("kiss");
            Assert.True(InteractionEiconSupport.TryResolveTokenToVerbKeys("kiss", out var keys));
            Assert.Equal(new[] { "kiss" }, keys);
        }

        // ---- 4. FromIdentifier rules: whose part is it ----

        [Fact]
        public void Mark_AppendsRecipientPartEicon()
        {
            var initiator = BuildWithPart("Alice", "ass", "[eicon]AliceAss[/eicon]");
            // MarkProcessor reads the marker's own mark eicon; every marker has one set.
            InteractionEiconSupport.SetInteractionEicon(initiator, "mark", "[eicon]TheMark[/eicon]");
            _database.SetProfile("Alice", initiator);
            var recipient = BuildWithPart("Bob", "ass", "[eicon]BobAss[/eicon]");

            string message = new MarkProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "ass", "mark");

            Assert.Contains("[eicon]BobAss[/eicon]", message);
            Assert.DoesNotContain("[eicon]AliceAss[/eicon]", message);
        }

        [Fact]
        public void Golden_AppendsRecipientPartEicon()
        {
            var initiator = BuildWithPart("Alice", "face", "[eicon]AliceFace[/eicon]");
            var recipient = BuildWithPart("Bob", "face", "[eicon]BobFace[/eicon]");

            string message = new GoldenProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "face", "golden");

            Assert.Contains("[eicon]BobFace[/eicon]", message);
            Assert.DoesNotContain("[eicon]AliceFace[/eicon]", message);
        }

        [Fact]
        public void Break_AppendsRecipientPartEicon()
        {
            var initiator = BuildWithPart("Alice", "arm", "[eicon]AliceArm[/eicon]");
            var recipient = BuildWithPart("Bob", "arm", "[eicon]BobArm[/eicon]");

            string message = new BreakProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "arm", "break");

            Assert.Contains("[eicon]BobArm[/eicon]", message);
            Assert.DoesNotContain("[eicon]AliceArm[/eicon]", message);
        }

        [Fact]
        public void Break_NonBodypartIdentifier_AppendsNothing()
        {
            var initiator = BuildWithPart("Alice", "arm", "[eicon]AliceArm[/eicon]");
            var recipient = BuildWithPart("Bob", "arm", "[eicon]BobArm[/eicon]");

            // "mind" is in the break category but isn't a bodypart, so there's simply no
            // stored eicon to find. No error, no icon.
            string message = new BreakProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "mind", "break");

            Assert.DoesNotContain("[eicon]", message);
        }

        [Fact]
        public void Consume_AppendsInitiatorPartEicon()
        {
            var initiator = BuildWithPart("Alice", "mouth", "[eicon]AliceMouth[/eicon]");
            var recipient = BuildWithPart("Bob", "mouth", "[eicon]BobMouth[/eicon]");

            string message = new ConsumeProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "mouth", "consume");

            Assert.Contains("[eicon]AliceMouth[/eicon]", message);
            Assert.DoesNotContain("[eicon]BobMouth[/eicon]", message);
        }

        [Fact]
        public void PartWithNoStoredEicon_AppendsNothing()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var recipient = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").BuildAndSave(_database);

            string message = new GoldenProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "face", "golden");

            Assert.DoesNotContain("[eicon]", message);
        }

        // ---- 5. Mark: interaction eicon still suppressed, bodypart eicon still appended ----

        [Fact]
        public void Mark_InteractionEiconStaysSelfRendered_ButPartEiconAppends()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            InteractionEiconSupport.SetInteractionEicon(initiator, "mark", "[eicon]TheMark[/eicon]");
            _database.SetProfile("Alice", initiator);

            var recipient = BuildWithPart("Bob", "ass", "[eicon]BobAss[/eicon]");

            string message = new MarkProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "ass", "mark");

            // The mark eicon is woven into MarkProcessor's own sentence, so the generic
            // suffix must not add it a second time — but the marked part's icon still shows.
            Assert.Contains("[eicon]BobAss[/eicon]", message);
            Assert.Single(CountOccurrences(message, "[eicon]TheMark[/eicon]"));
        }

        // ---- 6. Fixed rules ----

        [Fact]
        public void Spank_AppendsRecipientAssEicon()
        {
            var initiator = BuildWithPart("Alice", "ass", "[eicon]AliceAss[/eicon]");
            var recipient = BuildWithPart("Bob", "ass", "[eicon]BobAss[/eicon]");

            string message = new SpankProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "", "spank");

            Assert.Contains("[eicon]BobAss[/eicon]", message);
            Assert.DoesNotContain("[eicon]AliceAss[/eicon]", message);
        }

        [Fact]
        public void Feed_AppendsRecipientMouthEicon()
        {
            var initiator = BuildWithPart("Alice", "mouth", "[eicon]AliceMouth[/eicon]");
            var recipient = BuildWithPart("Bob", "mouth", "[eicon]BobMouth[/eicon]");

            // The typed identifier is the substance, not a part — the fixed rule still fires.
            string message = new FeedProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "milk", "feed");

            Assert.Contains("[eicon]BobMouth[/eicon]", message);
            Assert.DoesNotContain("[eicon]AliceMouth[/eicon]", message);
        }

        [Fact]
        public void Lick_AppendsInitiatorTongueEicon()
        {
            var initiator = BuildWithPart("Alice", "tongue", "[eicon]AliceTongue[/eicon]");
            var recipient = BuildWithPart("Bob", "tongue", "[eicon]BobTongue[/eicon]");

            string message = new LickProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "", "lick");

            Assert.Contains("[eicon]AliceTongue[/eicon]", message);
            Assert.DoesNotContain("[eicon]BobTongue[/eicon]", message);
        }

        [Fact]
        public void Boobhat_AppendsInitiatorBreastEicon()
        {
            var initiator = BuildWithPart("Alice", "breast", "[eicon]AliceBreast[/eicon]");
            var recipient = BuildWithPart("Bob", "breast", "[eicon]BobBreast[/eicon]");

            string message = new BoobhatProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "", "boobhat");

            Assert.Contains("[eicon]AliceBreast[/eicon]", message);
            Assert.DoesNotContain("[eicon]BobBreast[/eicon]", message);
        }

        [Fact]
        public void Milk_AppendsRecipientBreastEicon()
        {
            var initiator = BuildWithPart("Alice", "breast", "[eicon]AliceBreast[/eicon]");
            var recipient = BuildWithPart("Bob", "breast", "[eicon]BobBreast[/eicon]");

            string message = new MilkProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "milk|2", "milk");

            Assert.Contains("[eicon]BobBreast[/eicon]", message);
            Assert.DoesNotContain("[eicon]AliceBreast[/eicon]", message);
        }

        [Fact]
        public void Handhold_AppendsBothHandEicons_InitiatorFirst()
        {
            var initiator = BuildWithPart("Alice", "hand", "[eicon]AliceHand[/eicon]");
            var recipient = BuildWithPart("Bob", "hand", "[eicon]BobHand[/eicon]");

            string message = new HandholdProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "", "handhold");

            Assert.EndsWith("[eicon]AliceHand[/eicon] [eicon]BobHand[/eicon]", message);
        }

        [Fact]
        public void Handhold_SelfInteraction_ShowsOneHandEicon()
        {
            var alice = BuildWithPart("Alice", "hand", "[eicon]AliceHand[/eicon]");

            string message = new HandholdProcessor(_database)
                .GetCompletionMessageWithStatusEffects(alice, alice, "", "handhold");

            Assert.Single(CountOccurrences(message, "[eicon]AliceHand[/eicon]"));
        }

        // ---- 7. Ordering: bodypart eicons come after interaction eicons ----

        [Fact]
        public void BodypartEicon_ComesAfterInteractionEicon()
        {
            var initiator = new ProfileBuilder()
                .WithUserName("Alice").WithDisplayName("Alice")
                .WithCharacteristic("eicon_spank", "[eicon]AliceHand[/eicon]")
                .BuildAndSave(_database);
            var recipient = BuildWithPart("Bob", "ass", "[eicon]BobAss[/eicon]");

            string message = new SpankProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, recipient, "", "spank");

            Assert.EndsWith("[eicon]AliceHand[/eicon] [eicon]BobAss[/eicon]", message);
        }

        // ---- 8. Group path ----

        [Fact]
        public void GroupSpank_AppendsEveryConsentersAssEicon_InOrder_NoDedup()
        {
            var initiator = BuildWithPart("Alice", "ass", "[eicon]AliceAss[/eicon]");
            var c1 = BuildWithPart("Bob", "ass", "[eicon]SharedAss[/eicon]");
            var c2 = BuildWithPart("Carol", "ass", "[eicon]SharedAss[/eicon]");

            string suffix = new SpankProcessor(_database)
                .GetGroupEiconSuffix("spank", initiator, new List<Profile> { c1, c2 }, "");

            // No de-dup: two residents who picked the same booty icon get two icons. The
            // spanker's own ass is not what's in play, so it doesn't appear.
            Assert.Equal(" [eicon]SharedAss[/eicon] [eicon]SharedAss[/eicon]", suffix);
        }

        [Fact]
        public void GroupBoobhat_InitiatorOwnedRule_AppendsOnce()
        {
            var initiator = BuildWithPart("Alice", "breast", "[eicon]AliceBreast[/eicon]");
            var c1 = BuildWithPart("Bob", "breast", "[eicon]BobBreast[/eicon]");
            var c2 = BuildWithPart("Carol", "breast", "[eicon]CarolBreast[/eicon]");

            string suffix = new BoobhatProcessor(_database)
                .GetGroupEiconSuffix("boobhat", initiator, new List<Profile> { c1, c2 }, "");

            Assert.Equal(" [eicon]AliceBreast[/eicon]", suffix);
        }

        [Fact]
        public void GroupLapsit_TotemPole_UnchangedByBodypartEicons()
        {
            var initiator = new ProfileBuilder()
                .WithUserName("Alice")
                .WithCharacteristic("eicon_lap", "[eicon]LAP[/eicon]")
                .WithCharacteristic("eicon_part_ass", "[eicon]AliceAss[/eicon]")
                .Build();
            var c1 = new ProfileBuilder()
                .WithUserName("Bob")
                .WithCharacteristic("eicon_sit", "[eicon]C1SIT[/eicon]")
                .WithCharacteristic("eicon_part_ass", "[eicon]BobAss[/eicon]")
                .Build();

            string suffix = new LapsitProcessor(_database)
                .GetGroupEiconSuffix("lap", initiator, new List<Profile> { c1 }, "");

            Assert.Equal("\n[eicon]C1SIT[/eicon]\n[eicon]LAP[/eicon]", suffix);
        }

        // ---- 9. Queen Contract qcass migration ----

        [Fact]
        public void Spank_QueenContractWithPartEicon_ReproducesTheOldEasterEgg()
        {
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var queen = BuildWithPart("Queen Contract", "ass", "[eicon]qcass[/eicon]");

            string oneToOne = new SpankProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, queen, "", "spank");
            string group = new SpankProcessor(_database)
                .GetGroupEiconSuffix("spank", initiator, new List<Profile> { queen }, "");

            Assert.EndsWith("[eicon]qcass[/eicon]", oneToOne);
            Assert.Equal(" [eicon]qcass[/eicon]", group);
        }

        [Fact]
        public void Spank_QueenContractWithoutPartEicon_ShowsNothing()
        {
            // The icon is no longer hardcoded to her name — it comes from her own !seteicon.
            var initiator = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").BuildAndSave(_database);
            var queen = new ProfileBuilder().WithUserName("Queen Contract").WithDisplayName("Queen Contract").BuildAndSave(_database);

            string message = new SpankProcessor(_database)
                .GetCompletionMessageWithStatusEffects(initiator, queen, "", "spank");

            Assert.DoesNotContain("[eicon]", message);
        }

        // ---- 10. !seteicon bare-list readout ----

        [Fact]
        public void ListMessage_ShowsBothSections()
        {
            var p = new ProfileBuilder()
                .WithCharacteristic("eicon_kiss", "[eicon]Lips[/eicon]")
                .WithCharacteristic("eicon_part_lowerback", "[eicon]MyBack[/eicon]")
                .Build();

            string message = ChateauSeteicon.BuildListMessage(p);

            Assert.Contains("Here are the interaction eicons you've set:\n[b]!kiss[/b]: [eicon]Lips[/eicon]", message);
            // lowerback renders through Utils.BodypartToText, so it reads as two words.
            Assert.Contains("Here are the bodypart eicons you've set:\n[b]lower back[/b]: [eicon]MyBack[/eicon]", message);
        }

        [Fact]
        public void ListMessage_OmitsBodypartSectionWhenNoneSet()
        {
            var p = new ProfileBuilder().WithCharacteristic("eicon_kiss", "[eicon]Lips[/eicon]").Build();

            string message = ChateauSeteicon.BuildListMessage(p);

            Assert.Contains("interaction eicons", message);
            Assert.DoesNotContain("bodypart eicons", message);
        }

        [Theory]
        [InlineData("kiss", "whenever you kiss someone")]
        [InlineData("hug", "whenever you hug someone")]
        // !pet's eicon belongs to the one being petted, so the confirmation has to be
        // phrased from that side rather than the petter's.
        [InlineData("pet", "whenever someone pets you")]
        [InlineData("PET", "whenever someone pets you")]
        public void SetConfirmation_IsPhrasedFromTheSideWhoseEiconShows(string token, string expected)
        {
            Assert.Equal(expected, ChateauSeteicon.SetConfirmationClause(token));
        }

        [Fact]
        public void ListMessage_NothingSet_PromptsForBoth()
        {
            string message = ChateauSeteicon.BuildListMessage(new ProfileBuilder().Build());

            Assert.Contains("You haven't set any eicons yet.", message);
        }

        // ---- 11. !whatis readout ----

        [Fact]
        public void WhatIs_ShowsIdentifierEiconOnTitleLine()
        {
            var identifier = new Identifier
            {
                type = "ass",
                description = "The rear end.",
                categories = new[] { "bodypart" },
                eicon = "[eicon]stockbooty[/eicon]",
            };

            string message = ChateauIdentifier.BuildIdentifierMessage(identifier, "Nobody");

            Assert.StartsWith(ReadoutText.Title("ass") + " [eicon]stockbooty[/eicon]\nThe rear end.", message);
        }

        [Fact]
        public void WhatIs_NoIdentifierEicon_TitleLineUnchanged()
        {
            var identifier = new Identifier
            {
                type = "ass",
                description = "The rear end.",
                categories = new[] { "bodypart" },
            };

            string message = ChateauIdentifier.BuildIdentifierMessage(identifier, "Nobody");

            Assert.StartsWith(ReadoutText.Title("ass") + "\nThe rear end.", message);
        }

        [Fact]
        public void WhatIs_Bodypart_ShowsAskersPersonalEicon()
        {
            BuildWithPart("Alice", "ass", "[eicon]MyBooty[/eicon]");
            var identifier = new Identifier
            {
                type = "ass",
                description = "The rear end.",
                categories = new[] { "bodypart" },
            };

            string message = ChateauIdentifier.BuildIdentifierMessage(identifier, "Alice");

            Assert.EndsWith("\n" + ReadoutText.Small("Your personal eicon: [eicon]MyBooty[/eicon]"), message);
        }

        [Fact]
        public void WhatIs_NonBodypart_NoPersonalEiconLine()
        {
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice")
                .WithCharacteristic("eicon_part_milk", "[eicon]Nope[/eicon]")
                .BuildAndSave(_database);
            var identifier = new Identifier
            {
                type = "milk",
                description = "A substance.",
                categories = new[] { "substance" },
            };

            string message = ChateauIdentifier.BuildIdentifierMessage(identifier, "Alice");

            Assert.DoesNotContain("Your personal eicon", message);
        }

        [Fact]
        public void WhatIs_UnknownIdentifier_SaysSo()
        {
            Assert.Contains("not found in our records", ChateauIdentifier.BuildIdentifierMessage(null, "Alice"));
        }

        // ---- 12. Identifier eicon storage (the admin setter's DB path) ----

        [Fact]
        public void SetIdentifierEicon_SetsAndClears()
        {
            SeedBodypart("ass");

            Assert.True(_database.SetIdentifierEicon("ass", "[eicon]stockbooty[/eicon]"));
            Assert.Equal("[eicon]stockbooty[/eicon]", _database.GetIdentifier("ass").eicon);

            Assert.True(_database.SetIdentifierEicon("ass", null));
            Assert.Null(_database.GetIdentifier("ass").eicon);
        }

        [Fact]
        public void SetIdentifierEicon_UnknownIdentifier_ReturnsFalse()
        {
            Assert.False(_database.SetIdentifierEicon("nonsense", "[eicon]x[/eicon]"));
        }

        [Fact]
        public void SetIdentifierEiconCommand_IsAdminGated()
        {
            var command = new ChateauSetidentifiereicon();
            Assert.True(command.RequireBotAdmin);
            Assert.Equal("Admin", command.Category);
        }

        // ---- helpers ----

        private Profile BuildWithPart(string userName, string part, string eicon)
        {
            return new ProfileBuilder()
                .WithUserName(userName)
                .WithDisplayName(userName)
                .WithCharacteristic(InteractionEiconSupport.BodypartKeyPrefix + part, eicon)
                .BuildAndSave(_database);
        }

        /// <summary>One entry per occurrence of <paramref name="needle"/> in <paramref name="haystack"/>.</summary>
        private static List<bool> CountOccurrences(string haystack, string needle)
        {
            var hits = new List<bool>();
            int index = haystack.IndexOf(needle, StringComparison.Ordinal);
            while (index >= 0)
            {
                hits.Add(true);
                index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
            }
            return hits;
        }

        public void Dispose()
        {
        }
    }
}
