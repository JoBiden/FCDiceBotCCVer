using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Casual;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Tests for the global custom-eicon feature (!seteicon): storage/mapping in
    /// InteractionEiconSupport, the directional-vs-symmetric completion suffix, the lapsit
    /// per-position rule, and the birth special-case.
    /// </summary>
    [Collection("Database")]
    public class InteractionEiconTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public InteractionEiconTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        // ---- InteractionEiconSupport storage / mapping ----

        [Fact]
        public void SetInteractionEicon_ThenGet_Roundtrips()
        {
            var p = new ProfileBuilder().Build();
            InteractionEiconSupport.SetInteractionEicon(p, "kiss", "[eicon]lips[/eicon]");
            Assert.Equal("[eicon]lips[/eicon]", InteractionEiconSupport.GetInteractionEicon(p, "kiss"));
        }

        [Fact]
        public void SetInteractionEicon_Mark_UsesLegacyCharacteristicSlot()
        {
            var p = new ProfileBuilder().Build();
            InteractionEiconSupport.SetInteractionEicon(p, InteractionEiconSupport.MarkVerbKey, "[eicon]qcmark[/eicon]");

            // Mark keeps its historical slot so the dossier + existing marks keep working.
            Assert.Equal("[eicon]qcmark[/eicon]", p.characteristics["mark"]);
            Assert.False(p.characteristics.ContainsKey("eicon_mark"));
        }

        [Fact]
        public void ClearInteractionEicon_RemovesIt()
        {
            var p = new ProfileBuilder().Build();
            InteractionEiconSupport.SetInteractionEicon(p, "spank", "[eicon]hand[/eicon]");
            InteractionEiconSupport.ClearInteractionEicon(p, "spank");
            Assert.Equal(string.Empty, InteractionEiconSupport.GetInteractionEicon(p, "spank"));
        }

        [Theory]
        [InlineData("hug", new[] { "cuddle" })]
        [InlineData("dress", new[] { "dressup" })]
        [InlineData("hire", new[] { "employ" })]
        [InlineData("purify", new[] { "purify" })]
        [InlineData("sit", new[] { "sit" })]
        [InlineData("pay", new[] { "paymentGive", "paymentReceive" })]
        public void TryResolveTokenToVerbKeys_MapsTokens(string token, string[] expected)
        {
            Assert.True(InteractionEiconSupport.TryResolveTokenToVerbKeys(token, out var keys));
            Assert.Equal(expected, keys);
        }

        [Theory]
        [InlineData("rest")]
        [InlineData("roll")]
        [InlineData("")]
        [InlineData("notacommand")]
        public void TryResolveTokenToVerbKeys_RejectsUnsupported(string token)
        {
            Assert.False(InteractionEiconSupport.TryResolveTokenToVerbKeys(token, out _));
        }

        [Fact]
        public void IsSelfRendered_OnlyMark()
        {
            Assert.True(InteractionEiconSupport.IsSelfRendered("mark"));
            Assert.False(InteractionEiconSupport.IsSelfRendered("kiss"));
        }

        // ---- Directionality of the completion suffix ----

        [Fact]
        public void GroupEiconSuffix_Directional_InitiatorOnly()
        {
            var initiator = new ProfileBuilder().WithCharacteristic("eicon_spank", "[eicon]ihand[/eicon]").Build();
            var recipient = new ProfileBuilder().WithCharacteristic("eicon_spank", "[eicon]rbutt[/eicon]").Build();
            var processor = new SpankProcessor(_database);

            string suffix = processor.GetGroupEiconSuffix("spank", initiator, new List<Profile> { recipient });

            Assert.Contains("[eicon]ihand[/eicon]", suffix);
            Assert.DoesNotContain("[eicon]rbutt[/eicon]", suffix);
        }

        [Fact]
        public void GroupEiconSuffix_Symmetric_AllParticipants_NoDedup()
        {
            var initiator = new ProfileBuilder().WithCharacteristic("eicon_kiss", "[eicon]lips[/eicon]").Build();
            var c1 = new ProfileBuilder().WithCharacteristic("eicon_kiss", "[eicon]heart[/eicon]").Build();
            var c2 = new ProfileBuilder().WithCharacteristic("eicon_kiss", "[eicon]lips[/eicon]").Build(); // same icon as initiator
            var processor = new KissProcessor(_database);

            string suffix = processor.GetGroupEiconSuffix("kiss", initiator, new List<Profile> { c1, c2 });

            // No de-dup: the duplicate "lips" shows once per participant that set it.
            Assert.Equal(" [eicon]lips[/eicon] [eicon]heart[/eicon] [eicon]lips[/eicon]", suffix);
        }

        [Fact]
        public void GroupEiconSuffix_Mark_IsSelfRendered_ReturnsEmpty()
        {
            var initiator = new ProfileBuilder().Build();
            InteractionEiconSupport.SetInteractionEicon(initiator, "mark", "[eicon]qcmark[/eicon]");
            var recipient = new ProfileBuilder().Build();
            var processor = new MarkProcessor(_database);

            // Mark renders its own reveal inline; the generic suffix must not double it.
            Assert.Equal(string.Empty, processor.GetGroupEiconSuffix("mark", initiator, new List<Profile> { recipient }));
        }

        // ---- Lapsit per-position rule (bottom shows lap, riders show sit) ----

        [Fact]
        public void LapsitGroupEiconSuffix_Lap_BottomShowsLap_RidersShowSit()
        {
            // !lap: the initiator is the bottom, consenters stack above in order.
            var initiator = new ProfileBuilder()
                .WithCharacteristic("eicon_lap", "[eicon]LAP[/eicon]")
                .WithCharacteristic("eicon_sit", "[eicon]ISIT[/eicon]")
                .Build();
            var c1 = new ProfileBuilder().WithCharacteristic("eicon_sit", "[eicon]C1SIT[/eicon]").Build();
            var c2 = new ProfileBuilder().WithCharacteristic("eicon_sit", "[eicon]C2SIT[/eicon]").Build();
            var processor = new LapsitProcessor(_database);

            string suffix = processor.GetGroupEiconSuffix("lap", initiator, new List<Profile> { c1, c2 });

            // Bottom (initiator) uses their lap eicon; riders use their sit eicon.
            Assert.Equal(" [eicon]LAP[/eicon] [eicon]C1SIT[/eicon] [eicon]C2SIT[/eicon]", suffix);
        }

        [Fact]
        public void LapsitGroupEiconSuffix_Sit_FirstConsenterIsBottom()
        {
            // !sit: the first consenter claims the bottom, the initiator sits at position 1.
            var initiator = new ProfileBuilder().WithCharacteristic("eicon_sit", "[eicon]ISIT[/eicon]").Build();
            var c1 = new ProfileBuilder().WithCharacteristic("eicon_lap", "[eicon]C1LAP[/eicon]").Build();
            var c2 = new ProfileBuilder().WithCharacteristic("eicon_sit", "[eicon]C2SIT[/eicon]").Build();
            var processor = new LapsitProcessor(_database);

            string suffix = processor.GetGroupEiconSuffix("sit", initiator, new List<Profile> { c1, c2 });

            // Bottom is c1 (their lap eicon); initiator + c2 ride (their sit eicons).
            Assert.Equal(" [eicon]C1LAP[/eicon] [eicon]ISIT[/eicon] [eicon]C2SIT[/eicon]", suffix);
        }

        // ---- 1:1 completion path actually appends the eicon (symmetric shows both) ----

        [Fact]
        public void CompletionWithStatusEffects_Symmetric_AppendsBothEicons()
        {
            var initiator = new ProfileBuilder()
                .WithUserName("Alice").WithDisplayName("Alice")
                .WithCharacteristic("eicon_kiss", "[eicon]aa[/eicon]")
                .BuildAndSave(_database);
            var recipient = new ProfileBuilder()
                .WithUserName("Bob").WithDisplayName("Bob")
                .WithCharacteristic("eicon_kiss", "[eicon]bb[/eicon]")
                .BuildAndSave(_database);
            var processor = new KissProcessor(_database);

            string message = processor.GetCompletionMessageWithStatusEffects(initiator, recipient, "", "kiss");

            Assert.Contains("[eicon]aa[/eicon]", message);
            Assert.Contains("[eicon]bb[/eicon]", message);
        }

        // ---- Birth special-case ----

        [Fact]
        public void Birth_AppendsCarrierBirthEicon()
        {
            var carrier = new ProfileBuilder()
                .WithUserName("Carrier")
                .WithDisplayName("Carrier")
                .WithCharacteristic("eicon_birth", "[eicon]stork[/eicon]")
                .BuildAndSave(_database);

            carrier.pregnancies = new List<Pregnancy>
            {
                new Pregnancy
                {
                    Initiator = "Sire",
                    MonsterType = "slime",
                    ConceivedAt = DateTime.UtcNow.AddDays(-2),
                    ReadyAt = DateTime.UtcNow.AddDays(-1),
                    BroodSize = 1,
                    Categories = new List<string>(),
                }
            };
            _database.SetProfile("Carrier", carrier);

            var result = ChateauBirth.ExecuteBirth(_database, "Carrier", new string[] { });

            Assert.Contains("[eicon]stork[/eicon]", result.ChannelMessage);
        }

        public void Dispose()
        {
        }
    }
}
