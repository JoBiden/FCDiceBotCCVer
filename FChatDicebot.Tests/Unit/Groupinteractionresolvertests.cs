using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
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
    /// Unit tests for the B4 hybrid-group flow: GroupInteractionResolver applies the symmetric
    /// / directional / lapsit count math over whoever consented, emits one combined message,
    /// and clears the group. Drives MarkSeatConsented + CheckAndResolve directly against the
    /// test database (the same path ChateauConsent/ChateauRefuse use at runtime).
    /// </summary>
    [Collection("Database")]
    public class GroupInteractionResolverTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public GroupInteractionResolverTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        // ---- Symmetric (cuddle): every participant gets +(M-1) ----

        [Fact]
        public void Symmetric_ThreePersonCuddle_EachGetsMMinus1()
        {
            SeedProfiles("Alice", "Bob", "Carol");
            var seats = CreateGroup("Alice", "cuddle", null, "Bob", "Carol");
            ConsentInOrder(seats); // Bob then Carol

            var result = GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            Assert.True(result.Resolved);
            Assert.Equal(2, _database.GetProfile("Alice").counts["cuddle"]);
            Assert.Equal(2, _database.GetProfile("Bob").counts["cuddle"]);
            Assert.Equal(2, _database.GetProfile("Carol").counts["cuddle"]);
            Assert.Empty(_database.GetPendingCommandsByGroupId(seats[0].groupId));
        }

        [Fact]
        public void Symmetric_CompletionMessage_UsesSerialComma()
        {
            SeedProfiles("Alice", "Bob", "Carol");
            var seats = CreateGroup("Alice", "cuddle", null, "Bob", "Carol");
            ConsentInOrder(seats);

            var result = GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            Assert.Contains("Alice, Bob, and Carol", result.ChannelMessage);
            Assert.Contains("cuddle up together", result.ChannelMessage);
        }

        // ---- Directional (lick): initiator +R, each recipient +1 ----

        [Fact]
        public void Directional_GroupLick_InitiatorGetsR_RecipientsGet1()
        {
            SeedProfiles("Alice", "Bob", "Carol", "Dave");
            var seats = CreateGroup("Alice", "lick", null, "Bob", "Carol", "Dave");
            ConsentInOrder(seats);

            var result = GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            Assert.True(result.Resolved);
            Assert.Equal(3, _database.GetProfile("Alice").counts["lickgive"]);
            Assert.Equal(1, _database.GetProfile("Bob").counts["licktake"]);
            Assert.Equal(1, _database.GetProfile("Carol").counts["licktake"]);
            Assert.Equal(1, _database.GetProfile("Dave").counts["licktake"]);
            Assert.Contains("Alice licks Bob, Carol, and Dave.", result.ChannelMessage);
        }

        // ---- Partial consent: fire with whoever consented ----

        [Fact]
        public void PartialConsent_FiresWithConsentedSubset_ExpiredSeatDropped()
        {
            SeedProfiles("Alice", "Bob", "Carol", "Dave");
            var seats = CreateGroup("Alice", "lick", null, "Bob", "Carol", "Dave");

            ConsentSeat(seats[0]); // Bob
            ConsentSeat(seats[1]); // Carol
            ExpireSeat(seats[2]);  // Dave never responds and times out

            var result = GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            Assert.True(result.Resolved);
            Assert.Equal(2, _database.GetProfile("Alice").counts["lickgive"]); // R = 2
            Assert.Equal(1, _database.GetProfile("Bob").counts["licktake"]);
            Assert.Equal(1, _database.GetProfile("Carol").counts["licktake"]);
            Assert.False(_database.GetProfile("Dave").counts.ContainsKey("licktake"));
        }

        // ---- Enforcement spine (H2, group side): a re-validation failure drops the seat ----

        [Fact]
        public void ConsentedSeat_FailsRevalidation_DroppedFromMomentButOthersStillResolve()
        {
            SeedProfiles("Alice", "Bob", "Carol");
            var seats = CreateGroup("Alice", "cuddle", null, "Bob", "Carol");

            var carolProfile = _database.GetProfile("Carol");
            BreakInstance.SaveAll(carolProfile, new List<BreakInstance>
            {
                new BreakInstance { Part = "torso", Severity = 5, BrokenBy = "TestSetter", BrokenAt = DateTime.UtcNow, LastTickedAt = DateTime.UtcNow.Date }
            });
            _database.SetProfile("Carol", carolProfile);

            ConsentInOrder(seats); // Bob then Carol

            var result = GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            Assert.True(result.Resolved); // Bob's seat still completes
            Assert.Single(result.Dropped);
            Assert.Equal("Carol", result.Dropped[0].Participant);
            Assert.Contains("torso", result.Dropped[0].Reason);

            // Only Alice + Bob counted (M=2) — Carol excluded entirely from the math.
            Assert.Equal(1, _database.GetProfile("Alice").counts["cuddle"]);
            Assert.Equal(1, _database.GetProfile("Bob").counts["cuddle"]);
            Assert.False(_database.GetProfile("Carol").counts.ContainsKey("cuddle"));
            Assert.Empty(_database.GetPendingCommandsByGroupId(seats[0].groupId));
        }

        [Fact]
        public void ZeroConsent_AllExpired_GroupDiesSilently()
        {
            SeedProfiles("Alice", "Bob", "Carol");
            var seats = CreateGroup("Alice", "cuddle", null, "Bob", "Carol");
            ExpireSeat(seats[0]);
            ExpireSeat(seats[1]);

            var result = GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            Assert.False(result.Resolved);
            Assert.Empty(_database.GetPendingCommandsByGroupId(seats[0].groupId));
            Assert.False(_database.GetProfile("Alice").counts.ContainsKey("cuddle"));
            Assert.False(_database.GetProfile("Bob").counts.ContainsKey("cuddle"));
        }

        [Fact]
        public void StillWaiting_PendingSeatRemains_DoesNotResolve()
        {
            SeedProfiles("Alice", "Bob", "Carol");
            var seats = CreateGroup("Alice", "cuddle", null, "Bob", "Carol");
            ConsentSeat(seats[0]); // only Bob; Carol still Pending and not expired

            var result = GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            Assert.False(result.Resolved);
            Assert.Equal(2, _database.GetPendingCommandsByGroupId(seats[0].groupId).Count);
            Assert.False(_database.GetProfile("Alice").counts.ContainsKey("cuddle"));
        }

        // ---- Lapsit per-position math ----

        [Fact]
        public void Lapsit_Lap_PositionsByConsentOrder()
        {
            SeedProfiles("Alice", "Bob", "Carol");
            // !lap: initiator is the bottom (position 0); consenters stack above in order.
            var seats = CreateGroup("Alice", "lap", "lap", "Bob", "Carol");
            ConsentInOrder(seats); // Bob then Carol → stack Alice(0), Bob(1), Carol(2), M=3

            GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            var carol = _database.GetProfile("Carol");

            Assert.Equal(2, alice.counts["lapsittake"]);
            Assert.False(alice.counts.ContainsKey("lapsitgive"));
            Assert.Equal(1, bob.counts["lapsittake"]);
            Assert.Equal(1, bob.counts["lapsitgive"]);
            Assert.False(carol.counts.ContainsKey("lapsittake"));
            Assert.Equal(2, carol.counts["lapsitgive"]);
        }

        [Fact]
        public void Lapsit_Sit_FirstConsenterClaimsBottom()
        {
            SeedProfiles("Alice", "Bob", "Carol");
            // !sit: first consenter claims the open bottom, initiator sits at position 1.
            var seats = CreateGroup("Alice", "sit", "sit", "Bob", "Carol");
            ConsentInOrder(seats); // Bob then Carol → stack Bob(0), Alice(1), Carol(2), M=3

            GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            var alice = _database.GetProfile("Alice");
            var bob = _database.GetProfile("Bob");
            var carol = _database.GetProfile("Carol");

            Assert.Equal(2, bob.counts["lapsittake"]);
            Assert.False(bob.counts.ContainsKey("lapsitgive"));
            Assert.Equal(1, alice.counts["lapsittake"]);
            Assert.Equal(1, alice.counts["lapsitgive"]);
            Assert.False(carol.counts.ContainsKey("lapsittake"));
            Assert.Equal(2, carol.counts["lapsitgive"]);
        }

        [Fact]
        public void Lapsit_Lap_TwoPerson_DegenerateCase()
        {
            SeedProfiles("Alice", "Bob");
            var seats = CreateGroup("Alice", "lap", "lap", "Bob");
            ConsentInOrder(seats); // stack Alice(0), Bob(1), M=2

            GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            Assert.Equal(1, _database.GetProfile("Alice").counts["lapsittake"]);
            Assert.Equal(1, _database.GetProfile("Bob").counts["lapsitgive"]);
        }

        [Fact]
        public void Lapsit_TallStack_CompletionMessageRendersHeight()
        {
            SeedProfiles("Alice", "Bob", "Carol", "David", "Erwin");
            var seats = CreateGroup("Alice", "lap", "lap", "Bob", "Carol", "David", "Erwin");
            ConsentInOrder(seats);

            var result = GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            Assert.Contains("Alice pulls Bob onto their lap.", result.ChannelMessage);
            Assert.Contains("Then Carol takes a seat, then David, then Erwin", result.ChannelMessage);
            Assert.Contains("forming a lap stack 5 people tall!", result.ChannelMessage);
        }

        // ---- Helpers ----

        private void SeedProfiles(params string[] names)
        {
            foreach (var name in names)
            {
                new ProfileBuilder().WithUserName(name).WithDisplayName(name).BuildAndSave(_database);
            }
        }

        private List<PendingCommand> CreateGroup(string initiator, string type, string identifier, params string[] recipients)
        {
            string groupId = Guid.NewGuid().ToString("N");
            var seats = new List<PendingCommand>();
            foreach (var recipient in recipients)
            {
                var seat = new PendingCommand
                {
                    Id = ObjectId.GenerateNewId(),
                    pendingInteraction = new Interaction
                    {
                        initiator = initiator,
                        recipient = recipient,
                        type = type,
                        identifier = identifier,
                        investmentLevel = "casual"
                    },
                    awaitingConsentFrom = recipient,
                    groupId = groupId,
                    consentState = PendingCommand.PendingConsentState
                };
                _database.AddPendingCommand(seat);
                seats.Add(seat);
            }
            return seats;
        }

        private void ConsentInOrder(List<PendingCommand> seats)
        {
            foreach (var seat in seats) ConsentSeat(seat);
        }

        private void ConsentSeat(PendingCommand seat)
        {
            GroupInteractionResolver.MarkSeatConsented(_database, seat);
        }

        private void ExpireSeat(PendingCommand seat)
        {
            seat.startTime = DateTime.UtcNow.AddMinutes(-(GroupInteractionResolver.PendingMinutesKeep + 5));
            _database.UpdatePendingCommand(seat);
        }

        public void Dispose()
        {
        }
    }
}
