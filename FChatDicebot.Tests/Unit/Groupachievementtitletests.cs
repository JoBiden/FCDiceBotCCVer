using FChatDicebot;
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
    /// Pure-lookup tests for the group-achievement title tables in ChateauSystemTitles:
    /// size-based titles (cumulative thresholds, per interaction type) and lapsit per-position
    /// titles. No database — these validate the title text and the threshold math directly.
    /// </summary>
    public class GroupTitleLookupTests
    {
        [Theory]
        // Below the first threshold → nothing yet.
        [InlineData("cuddle", 2)]
        [InlineData("kiss", 2)]
        [InlineData("boobhat", 3)] // boobhat's first tier is 4
        public void GetGroupSizeTitles_BelowFirstThreshold_IsEmpty(string type, int size)
        {
            Assert.Empty(ChateauSystemTitles.GetGroupSizeTitles(type, size));
        }

        [Fact]
        public void GetGroupSizeTitles_IsCumulative_GrantsEveryTierAtOrBelowSize()
        {
            Assert.Equal(new[] { "Cuddle Puddle" },
                ChateauSystemTitles.GetGroupSizeTitles("cuddle", 3));
            Assert.Equal(new[] { "Cuddle Puddle" },
                ChateauSystemTitles.GetGroupSizeTitles("cuddle", 4)); // 4 >= 3 but < 5
            Assert.Equal(new[] { "Cuddle Puddle", "Cuddle Pond", "Cuddle Lake" },
                ChateauSystemTitles.GetGroupSizeTitles("cuddle", 7));
            Assert.Equal(new[] { "Cuddle Puddle", "Cuddle Pond", "Cuddle Lake", "Cuddle Sea", "Cuddle Ocean" },
                ChateauSystemTitles.GetGroupSizeTitles("cuddle", 11));
        }

        [Fact]
        public void GetGroupSizeTitles_Kiss_ApexIsSpartakiss()
        {
            var titles = ChateauSystemTitles.GetGroupSizeTitles("kiss", 11);
            Assert.Equal(new[] { "Kiss Kiss", "Kisstacular", "Kissimanjaro", "Kisspocalypse", "Spartakiss" }, titles);
        }

        [Fact]
        public void GetGroupSizeTitles_Handhold_GeometricNames()
        {
            Assert.Equal(new[] { "Triangle", "Pentagon", "Septagon" },
                ChateauSystemTitles.GetGroupSizeTitles("handhold", 7));
        }

        [Fact]
        public void GetGroupSizeTitles_Spank_UsesItsOwn3_5_8_11Thresholds()
        {
            // "Seven Spanks" lands at size 8 (initiator + 7 spanked), not 7.
            Assert.Equal(new[] { "Echo", "Thunder Storm" },
                ChateauSystemTitles.GetGroupSizeTitles("spank", 7));
            Assert.Equal(new[] { "Echo", "Thunder Storm", "Seven Spanks" },
                ChateauSystemTitles.GetGroupSizeTitles("spank", 8));
            Assert.Equal(new[] { "Echo", "Thunder Storm", "Seven Spanks", "Strike!" },
                ChateauSystemTitles.GetGroupSizeTitles("spank", 11));
        }

        [Fact]
        public void GetGroupSizeTitles_Boobhat_StartsAtFourWithReassignedHatTrick()
        {
            Assert.Equal(new[] { "Hat Trick" },
                ChateauSystemTitles.GetGroupSizeTitles("boobhat", 4));
            Assert.Equal(new[] { "Hat Trick", "Mad Hatter", "Capping" },
                ChateauSystemTitles.GetGroupSizeTitles("boobhat", 11));
        }

        [Fact]
        public void GetGroupSizeTitles_Bully_And_Lick_FullLadders()
        {
            Assert.Equal(new[] { "Intimidating", "Hazing", "Demands Respect", "Unbullyvable", "Decibully" },
                ChateauSystemTitles.GetGroupSizeTitles("bully", 11));
            Assert.Equal(new[] { "Free Sample", "Indecisive", "Can Tie A Knot In A Cherry Stem", "Tongue In Chic", "Lick A Ton" },
                ChateauSystemTitles.GetGroupSizeTitles("lick", 11));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("objectify")] // not a group-size interaction
        [InlineData("bond")]
        public void GetGroupSizeTitles_UnknownOrNullType_IsEmpty(string type)
        {
            Assert.Empty(ChateauSystemTitles.GetGroupSizeTitles(type, 11));
        }

        [Fact]
        public void GetLapsitPositionTitles_BelowLadder_IsCumulative()
        {
            Assert.Empty(ChateauSystemTitles.GetLapsitPositionTitles(0, 0));
            Assert.Equal(new[] { "Elevated" }, ChateauSystemTitles.GetLapsitPositionTitles(2, 0));
            Assert.Equal(new[] { "Elevated", "Laps All The Way Down" },
                ChateauSystemTitles.GetLapsitPositionTitles(4, 0));
            Assert.Equal(new[] { "Elevated", "Laps All The Way Down", "Can See Their House From Here", "King Of The World", "Lap of Babel" },
                ChateauSystemTitles.GetLapsitPositionTitles(10, 0));
        }

        [Fact]
        public void GetLapsitPositionTitles_AboveLadder_IsCumulative()
        {
            Assert.Equal(new[] { "Shortstacked" }, ChateauSystemTitles.GetLapsitPositionTitles(0, 2));
            Assert.Equal(new[] { "Shortstacked", "Buried" }, ChateauSystemTitles.GetLapsitPositionTitles(0, 5));
            Assert.Equal(new[] { "Shortstacked", "Buried", "Foundational" },
                ChateauSystemTitles.GetLapsitPositionTitles(0, 10));
        }

        [Fact]
        public void GetLapsitPositionTitles_MidStack_EarnsBothBelowAndAbove()
        {
            // 2 people below AND 2 above: one of each ladder's first tier.
            Assert.Equal(new[] { "Elevated", "Shortstacked" },
                ChateauSystemTitles.GetLapsitPositionTitles(2, 2));
        }

        // ---- FormatGroupTitleNotification: one banner, grouped by title ----

        private static GroupTitleGrant Grant(string displayName, params string[] titles)
        {
            return new GroupTitleGrant { UserName = displayName, DisplayName = displayName, NewTitles = titles.ToList() };
        }

        private static int Occurrences(string haystack, string needle)
        {
            int count = 0, i = 0;
            while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { count++; i += needle.Length; }
            return count;
        }

        [Fact]
        public void FormatGroupTitleNotification_NoNewTitles_IsEmpty()
        {
            Assert.Equal(string.Empty, ChateauSystemTitles.FormatGroupTitleNotification(null));
            Assert.Equal(string.Empty, ChateauSystemTitles.FormatGroupTitleNotification(new List<GroupTitleGrant>()));
            Assert.Equal(string.Empty, ChateauSystemTitles.FormatGroupTitleNotification(
                new List<GroupTitleGrant> { Grant("Alice" /* no titles */) }));
        }

        [Fact]
        public void FormatGroupTitleNotification_SameTitleManyEarners_OneBannerSerialComma()
        {
            var grants = new List<GroupTitleGrant>
            {
                Grant("Alice", "Cuddle Puddle"),
                Grant("Bob", "Cuddle Puddle"),
                Grant("Carol", "Cuddle Puddle"),
            };

            string msg = ChateauSystemTitles.FormatGroupTitleNotification(grants);

            Assert.Equal(1, Occurrences(msg, "═══ Title Time! ═══"));
            Assert.Contains("Alice, Bob, and Carol earned the title [b]·Cuddle Puddle·[/b]!", msg);
            Assert.EndsWith("[sub]View all your titles with !titles[/sub]", msg);
        }

        [Fact]
        public void FormatGroupTitleNotification_DifferentTitles_GroupedByTitleInFirstSeenOrder()
        {
            // Lapsit-shaped: each rider's own below/above titles, granted in stack order.
            var grants = new List<GroupTitleGrant>
            {
                Grant("Alice", "Shortstacked", "Buried"),
                Grant("Bob", "Shortstacked"),
                Grant("Carol", "Elevated", "Shortstacked"),
            };

            string msg = ChateauSystemTitles.FormatGroupTitleNotification(grants);

            Assert.Equal(1, Occurrences(msg, "═══ Title Time! ═══"));
            Assert.Contains("Alice, Bob, and Carol earned the title [b]·Shortstacked·[/b]!", msg);
            Assert.Contains("Alice earned the title [b]·Buried·[/b]!", msg);
            Assert.Contains("Carol earned the title [b]·Elevated·[/b]!", msg);
            // First-seen title order: Shortstacked, then Buried, then Elevated.
            Assert.True(msg.IndexOf("·Shortstacked·", StringComparison.Ordinal)
                      < msg.IndexOf("·Buried·", StringComparison.Ordinal));
            Assert.True(msg.IndexOf("·Buried·", StringComparison.Ordinal)
                      < msg.IndexOf("·Elevated·", StringComparison.Ordinal));
        }

        [Fact]
        public void FormatGroupTitleNotification_SingleEarner_NoSerialComma()
        {
            string msg = ChateauSystemTitles.FormatGroupTitleNotification(
                new List<GroupTitleGrant> { Grant("Alice", "Echo") });

            Assert.Contains("Alice earned the title [b]·Echo·[/b]!", msg);
            Assert.DoesNotContain(",", msg);
        }
    }

    /// <summary>
    /// Grant-path tests: GroupInteractionResolver.CheckAndResolve hands off to the processor's
    /// GrantGroupTitles, which persists the size / position titles through the injected test
    /// database and reports them on the result. Mirrors the count-math resolver tests.
    /// </summary>
    [Collection("Database")]
    public class GroupAchievementTitleGrantTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public GroupAchievementTitleGrantTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        [Fact]
        public void Symmetric_ThreePersonCuddle_EveryParticipantEarnsCuddlePuddle()
        {
            SeedProfiles("Alice", "Bob", "Carol");
            var seats = CreateGroup("Alice", "cuddle", null, "Bob", "Carol");
            ConsentInOrder(seats);

            var result = GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            foreach (var name in new[] { "Alice", "Bob", "Carol" })
            {
                Assert.True(HasSystemTitle(name, "Cuddle Puddle"), $"{name} should have Cuddle Puddle");
            }
            Assert.Equal(3, result.GroupTitleGrants.Count);
            Assert.All(result.GroupTitleGrants, g => Assert.Equal(new[] { "Cuddle Puddle" }, g.NewTitles));
        }

        [Fact]
        public void Directional_ThreePersonSpank_OnlyInitiatorEarnsTheTitle()
        {
            SeedProfiles("Alice", "Bob", "Carol");
            var seats = CreateGroup("Alice", "spank", null, "Bob", "Carol");
            ConsentInOrder(seats);

            var result = GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            Assert.True(HasSystemTitle("Alice", "Echo"));
            Assert.False(HasSystemTitle("Bob", "Echo"));
            Assert.False(HasSystemTitle("Carol", "Echo"));

            var grant = Assert.Single(result.GroupTitleGrants);
            Assert.Equal("Alice", grant.UserName);
            Assert.Equal(new[] { "Echo" }, grant.NewTitles);
        }

        [Fact]
        public void Directional_FivePersonSpank_InitiatorEarnsCumulativeLadder()
        {
            SeedProfiles("Alice", "Bob", "Carol", "Dave", "Erin");
            var seats = CreateGroup("Alice", "spank", null, "Bob", "Carol", "Dave", "Erin"); // M = 5
            ConsentInOrder(seats);

            GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            Assert.True(HasSystemTitle("Alice", "Echo"));
            Assert.True(HasSystemTitle("Alice", "Thunder Storm"));
            Assert.False(HasSystemTitle("Alice", "Seven Spanks")); // needs size 8
        }

        [Fact]
        public void Lapsit_SixTallStack_PerPositionTitles()
        {
            SeedProfiles("Alice", "Bob", "Carol", "Dave", "Erin", "Frank");
            // !lap: Alice is the bottom; consenters stack above in consent order.
            var seats = CreateGroup("Alice", "lap", "lap", "Bob", "Carol", "Dave", "Erin", "Frank");
            ConsentInOrder(seats); // stack bottom->top: Alice,Bob,Carol,Dave,Erin,Frank (M=6)

            GroupInteractionResolver.CheckAndResolve(_database, seats[0].groupId);

            // Alice (bottom, 0 below / 5 above): Shortstacked + Buried, no below titles.
            Assert.True(HasSystemTitle("Alice", "Shortstacked"));
            Assert.True(HasSystemTitle("Alice", "Buried"));
            Assert.False(HasSystemTitle("Alice", "Elevated"));

            // Carol (2 below / 3 above): earns from both ladders.
            Assert.True(HasSystemTitle("Carol", "Elevated"));
            Assert.True(HasSystemTitle("Carol", "Shortstacked"));

            // Frank (top, 5 below / 0 above): below ladder only.
            Assert.True(HasSystemTitle("Frank", "Elevated"));
            Assert.True(HasSystemTitle("Frank", "Laps All The Way Down"));
            Assert.False(HasSystemTitle("Frank", "Shortstacked"));
        }

        [Fact]
        public void AlreadyHeld_IsNotRegrantedOrDoubleAnnounced()
        {
            SeedProfiles("Alice", "Bob", "Carol");

            // First 3-cuddle grants Cuddle Puddle to all.
            var first = CreateGroup("Alice", "cuddle", null, "Bob", "Carol");
            ConsentInOrder(first);
            GroupInteractionResolver.CheckAndResolve(_database, first[0].groupId);

            // A second identical 3-cuddle: no new titles to announce, no duplicates stored.
            var second = CreateGroup("Alice", "cuddle", null, "Bob", "Carol");
            ConsentInOrder(second);
            var result = GroupInteractionResolver.CheckAndResolve(_database, second[0].groupId);

            Assert.Empty(result.GroupTitleGrants);
            foreach (var name in new[] { "Alice", "Bob", "Carol" })
            {
                int count = _database.GetProfile(name).titles
                    .Count(t => t.IsSystemTitle && t.titleText == "Cuddle Puddle");
                Assert.Equal(1, count);
            }
        }

        // ---- Helpers (mirror GroupInteractionResolverTests) ----

        private bool HasSystemTitle(string userName, string titleText)
        {
            var profile = _database.GetProfile(userName);
            return profile.titles != null &&
                profile.titles.Any(t => t.IsSystemTitle && t.titleText == titleText);
        }

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
            foreach (var seat in seats) GroupInteractionResolver.MarkSeatConsented(_database, seat);
        }

        public void Dispose()
        {
        }
    }
}
