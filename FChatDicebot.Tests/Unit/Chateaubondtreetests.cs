using FChatDicebot.BotCommands.Support;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Unit tests for BondTreeSupport.BuildBondTree, exercising traversal and rendering over a
    /// synthetic profile graph (an in-memory Func&lt;string, Profile&gt;), so no live DB is needed.
    /// </summary>
    public class ChateauBondtreeTests
    {
        // ---- Graph helpers -------------------------------------------------

        private readonly Dictionary<string, Profile> _graph =
            new Dictionary<string, Profile>(StringComparer.OrdinalIgnoreCase);

        private Func<string, Profile> Lookup =>
            u => _graph.TryGetValue(u ?? string.Empty, out Profile p) ? p : null;

        /// <summary>
        /// Add a person to the graph. bonds entries are (listLabel, neighborUserName) pairs, e.g.
        /// ("bondpetinitiated", "Bob"). The display name defaults to the userName.
        /// </summary>
        private Profile AddPerson(string userName, string displayName = null, params (string list, string neighbor)[] bonds)
        {
            var builder = new ProfileBuilder()
                .WithUserName(userName)
                .WithDisplayName(displayName ?? userName);
            foreach (var b in bonds)
            {
                builder.WithListItem(b.list, b.neighbor);
            }
            Profile p = builder.Build();
            _graph[userName] = p;
            return p;
        }

        private static int Count(string haystack, string needle)
        {
            int count = 0, index = 0;
            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }
            return count;
        }

        // ---- Empty state ---------------------------------------------------

        [Fact]
        public void NoBonds_BondTree_ReturnsEmptyState()
        {
            AddPerson("Alice", "Alice");

            string result = BondTreeSupport.BuildBondTree("Alice", 2, familyOnly: false, Lookup);

            Assert.Equal("Alice has no bonds yet.", result);
        }

        [Fact]
        public void NoBonds_FamilyTree_ReturnsFamilyEmptyState()
        {
            AddPerson("Alice", "Alice");

            string result = BondTreeSupport.BuildBondTree("Alice", 2, familyOnly: true, Lookup);

            Assert.Equal("Alice has no family bonds yet.", result);
        }

        [Fact]
        public void OnlyNonFamilyBonds_FamilyTree_ReturnsFamilyEmptyState()
        {
            AddPerson("Alice", "Alice", ("bondpetinitiated", "Bob"));
            AddPerson("Bob", "Bob");

            string result = BondTreeSupport.BuildBondTree("Alice", 2, familyOnly: true, Lookup);

            Assert.Equal("Alice has no family bonds yet.", result);
        }

        // ---- Direct bonds + perspective ------------------------------------

        [Fact]
        public void DirectBonds_RendersRolesFromBothPerspectives()
        {
            // Alice initiated a pet bond with Bob (Bob is Alice's pet), and received a submission
            // bond from Dom (Dom is Alice's dominant).
            AddPerson("Alice", "Alice",
                ("bondpetinitiated", "Bob"),
                ("bondsubmissionreceived", "Dom"));
            AddPerson("Bob", "Bob");
            AddPerson("Dom", "Dom");

            string result = BondTreeSupport.BuildBondTree("Alice", 1, familyOnly: false, Lookup);

            Assert.Contains("Bond tree for Alice, up to 1 degree:", result);
            Assert.Contains("1st degree:", result);
            Assert.Contains("Bob (Alice's pet)", result);     // initiated → BondToText(type, true)
            Assert.Contains("Dom (Alice's dominant)", result); // received  → BondToText(type, false)
        }

        // ---- Second degree + dedup to shallowest ---------------------------

        [Fact]
        public void SecondDegree_PersonReachableAtBothDepths_AppearsOnceAtShallowest()
        {
            // Alice—Bob (sibling) and Alice—Carol (ally) are both 1st degree.
            // Bob—Carol (sibling) would put Carol at 2nd degree too, but she must stay at 1st.
            // Bob—Dave (sibling) is a genuine 2nd-degree node.
            AddPerson("Alice", "Alice",
                ("bondsiblinginitiated", "Bob"),
                ("bondallyinitiated", "Carol"));
            AddPerson("Bob", "Bob",
                ("bondsiblinginitiated", "Carol"),
                ("bondsiblinginitiated", "Dave"));
            AddPerson("Carol", "Carol");
            AddPerson("Dave", "Dave");

            string result = BondTreeSupport.BuildBondTree("Alice", 2, familyOnly: false, Lookup);

            Assert.Contains("Carol (Alice's ally)", result);  // reached at 1st degree, via Alice
            Assert.DoesNotContain("Carol (Bob's", result);    // never the 2nd-degree label
            Assert.Equal(1, Count(result, "Carol ("));        // appears exactly once
            Assert.Contains("2nd degree:", result);
            Assert.Contains("Dave (Bob's sibling)", result);  // genuine 2nd-degree node
        }

        // ---- Cycles --------------------------------------------------------

        [Fact]
        public void Cycle_Terminates_AndListsEachPersonOnce()
        {
            // Triangle A→B→C→A, with the symmetric initiated/received records the bond
            // interaction writes on both ends of every edge.
            AddPerson("A", "A",
                ("bondallyinitiated", "B"),   // A initiated with B
                ("bondallyreceived", "C"));   // C initiated with A
            AddPerson("B", "B",
                ("bondallyreceived", "A"),    // A→B
                ("bondallyinitiated", "C"));  // B initiated with C
            AddPerson("C", "C",
                ("bondallyreceived", "B"),    // B→C
                ("bondallyinitiated", "A"));  // C→A

            string result = BondTreeSupport.BuildBondTree("A", 3, familyOnly: false, Lookup);

            // Each non-root node appears exactly once; the root is never listed as a connection.
            Assert.Equal(1, Count(result, "B (A's ally)"));
            Assert.Equal(1, Count(result, "C (A's ally)"));
            Assert.DoesNotContain("A (", result);
        }

        // ---- Family-only filter --------------------------------------------

        [Fact]
        public void FamilyOnly_IncludesFamilyBonds_ExcludesOthers()
        {
            AddPerson("Alice", "Alice",
                ("bondsiblinginitiated", "Sib"),
                ("bondpetinitiated", "Pet"),
                ("bondrivalreceived", "Riv"));
            AddPerson("Sib", "Sib");
            AddPerson("Pet", "Pet");
            AddPerson("Riv", "Riv");

            string familyResult = BondTreeSupport.BuildBondTree("Alice", 2, familyOnly: true, Lookup);
            Assert.Contains("Sib", familyResult);
            Assert.DoesNotContain("Pet", familyResult);
            Assert.DoesNotContain("Riv", familyResult);

            // Sanity: the all-bonds variant includes all three.
            string allResult = BondTreeSupport.BuildBondTree("Alice", 2, familyOnly: false, Lookup);
            Assert.Contains("Sib", allResult);
            Assert.Contains("Pet", allResult);
            Assert.Contains("Riv", allResult);
        }

        // ---- Node cap ------------------------------------------------------

        [Fact]
        public void NodeCap_StopsAt100_AppendsLimitReached()
        {
            var bonds = new List<(string, string)>();
            for (int i = 0; i < 150; i++)
            {
                string name = "U" + i;
                bonds.Add(("bondallyinitiated", name));
                AddPerson(name, name);
            }
            AddPerson("Root", "Root", bonds.ToArray());

            string result = BondTreeSupport.BuildBondTree("Root", 1, familyOnly: false, Lookup);

            Assert.Contains("limit reached", result);
            Assert.Equal(BondTreeSupport.MaxNodes, Count(result, "(Root's ally)"));
        }

        // ---- Depth clamp ---------------------------------------------------

        [Fact]
        public void DepthClamp_AboveMax_BehavesAsThreeDegrees()
        {
            AddPerson("Alpha", "Alpha", ("bondallyinitiated", "Bravo"));
            AddPerson("Bravo", "Bravo", ("bondallyinitiated", "Charlie"));
            AddPerson("Charlie", "Charlie", ("bondallyinitiated", "Delta"));
            AddPerson("Delta", "Delta", ("bondallyinitiated", "Echo"));
            AddPerson("Echo", "Echo");

            string result = BondTreeSupport.BuildBondTree("Alpha", 5, familyOnly: false, Lookup);

            Assert.Contains("up to 3 degrees:", result);
            Assert.Contains("Bravo", result);
            Assert.Contains("Charlie", result);
            Assert.Contains("Delta", result);
            Assert.DoesNotContain("Echo", result); // 4th degree, beyond the clamped max

            // Identical to an explicit N=3 request.
            string atThree = BondTreeSupport.BuildBondTree("Alpha", 3, familyOnly: false, Lookup);
            Assert.Equal(atThree, result);
        }

        // ---- Missing neighbor profile --------------------------------------

        [Fact]
        public void MissingNeighborProfile_FallsBackToUserName_TraversalContinues()
        {
            // Ghost is referenced in Alice's bond list but has no profile in the graph.
            AddPerson("Alice", "Alice",
                ("bondpetinitiated", "Ghost"),
                ("bondsiblinginitiated", "Bob"));
            AddPerson("Bob", "Bob");

            string result = BondTreeSupport.BuildBondTree("Alice", 2, familyOnly: false, Lookup);

            Assert.Contains("Ghost (Alice's pet)", result); // falls back to the stored userName
            Assert.Contains("Bob (Alice's sibling)", result); // traversal still reaches the rest
        }
    }
}
