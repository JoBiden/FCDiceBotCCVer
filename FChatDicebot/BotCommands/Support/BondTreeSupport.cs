using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FChatDicebot.BotCommands.Support
{
    /// <summary>
    /// Shared bond-graph traversal behind !bondtree and !familytree. Both commands walk the
    /// same graph and differ only by which bond types are included, so the whole traversal +
    /// rendering lives in one pure-ish static builder over an injected profile lookup
    /// (testable without a live DB).
    ///
    /// The bond interaction writes both directions as profile lists: on the initiator
    /// "bond{type}initiated" → recipient userNames, and on the recipient "bond{type}received"
    /// → initiator userNames (see BondProcessor). A person's neighbors are therefore the union,
    /// over every bond type, of those two list families; the graph is symmetric and walkable
    /// one profile at a time. No DB-layer change — this rides existing GetProfile reads.
    /// </summary>
    public static class BondTreeSupport
    {
        /// <summary>
        /// Family subset (B10.1). "family" and "kin" are the same bond — bond type "family"
        /// renders as the role "kin" — so this is four bond types, not five. Shared so any
        /// future family-only feature filters identically.
        /// </summary>
        public static readonly string[] FamilyBondTypes = { "marriage", "offspring", "sibling", "family" };

        // B10.2 tuning: default 2 degrees, hard max of 3, hard cap of 100 distinct people rendered.
        public const int DefaultDegrees = 2;
        public const int MaxDegrees = 3;
        public const int MaxNodes = 100;

        // Past this assembled length the body is tucked behind a [spoiler] so the PM doesn't
        // arrive as a wall of text (and stays clear of the F-Chat 4096-char message cap).
        public const int SpoilerThreshold = 1900;

        /// <summary>
        /// Pull the optional depth argument out of the parsed command terms: the first term that
        /// parses as an integer wins, otherwise <see cref="DefaultDegrees"/>. Clamping to the
        /// [1, MaxDegrees] range is left to <see cref="BuildBondTree"/> so it happens in one place.
        /// </summary>
        public static int ParseDegrees(string[] terms)
        {
            if (terms != null)
            {
                foreach (string term in terms)
                {
                    if (int.TryParse(term, out int parsed))
                    {
                        return parsed;
                    }
                }
            }
            return DefaultDegrees;
        }

        /// <summary>
        /// Walk the bond graph outward from <paramref name="rootUserName"/> and render everyone
        /// within <paramref name="degrees"/> degrees, grouped by degree. <paramref name="familyOnly"/>
        /// restricts the walk to <see cref="FamilyBondTypes"/> (the !familytree variant).
        /// <paramref name="getProfile"/> is the only DB dependency, injected so tests can supply a
        /// synthetic graph.
        /// </summary>
        public static string BuildBondTree(string rootUserName, int degrees, bool familyOnly,
                                           Func<string, Profile> getProfile)
        {
            degrees = Math.Max(1, Math.Min(degrees, MaxDegrees));

            Profile rootProfile = getProfile(rootUserName);
            string rootDisplay = ResolveDisplay(rootProfile, rootUserName);

            // BFS so each person is first reached — and therefore recorded — at their shallowest
            // degree. visited is keyed case-insensitively so cycles (mutual / diamond bonds)
            // terminate even when a name's casing differs between the two ends of an edge.
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootUserName };
            var byDegree = new SortedDictionary<int, List<Connection>>();
            var frontier = new Queue<FrontierNode>();
            frontier.Enqueue(new FrontierNode(rootUserName, 0));

            int rendered = 0;
            bool capReached = false;

            while (frontier.Count > 0 && !capReached)
            {
                FrontierNode node = frontier.Dequeue();
                if (node.Degree >= degrees) continue; // reached the depth limit; don't expand further

                Profile profile = getProfile(node.UserName);
                if (profile == null || profile.lists == null) continue; // missing profile: can't expand
                string connectorDisplay = ResolveDisplay(profile, node.UserName);

                foreach (BondEdge edge in EnumerateBondEdges(profile, familyOnly))
                {
                    if (visited.Contains(edge.NeighborUserName)) continue;
                    if (rendered >= MaxNodes) { capReached = true; break; }

                    visited.Add(edge.NeighborUserName);
                    int neighborDegree = node.Degree + 1;

                    Profile neighborProfile = getProfile(edge.NeighborUserName);
                    string neighborDisplay = ResolveDisplay(neighborProfile, edge.NeighborUserName);

                    // Role is the neighbor's relationship to the person they were reached from,
                    // mirroring BondProcessor's perspective: reached via the parent's *initiated*
                    // list → the neighbor is the parent's BondToText(type, true); via *received* →
                    // BondToText(type, false).
                    string role = Utils.BondToText(edge.BondType, edge.ViaInitiated);

                    if (!byDegree.ContainsKey(neighborDegree))
                        byDegree[neighborDegree] = new List<Connection>();
                    byDegree[neighborDegree].Add(new Connection(neighborDisplay, connectorDisplay, role));
                    rendered++;

                    frontier.Enqueue(new FrontierNode(edge.NeighborUserName, neighborDegree));
                }
            }

            return Render(rootDisplay, degrees, familyOnly, byDegree, capReached);
        }

        /// <summary>
        /// Yield every bond neighbor of a profile as (neighbor, bondType, viaInitiated) by
        /// scanning its "bond{type}initiated" / "bond{type}received" lists, restricted to
        /// <see cref="FamilyBondTypes"/> when <paramref name="familyOnly"/>.
        /// </summary>
        private static IEnumerable<BondEdge> EnumerateBondEdges(Profile profile, bool familyOnly)
        {
            foreach (KeyValuePair<string, List<string>> kv in profile.lists)
            {
                string key = kv.Key;
                if (kv.Value == null || kv.Value.Count == 0) continue;
                if (!key.StartsWith("bond")) continue;

                bool viaInitiated;
                string bondType;
                if (key.EndsWith("initiated"))
                {
                    viaInitiated = true;
                    bondType = key.Substring(4, key.Length - 4 - "initiated".Length);
                }
                else if (key.EndsWith("received"))
                {
                    viaInitiated = false;
                    bondType = key.Substring(4, key.Length - 4 - "received".Length);
                }
                else
                {
                    continue;
                }

                if (string.IsNullOrEmpty(bondType)) continue;
                if (familyOnly && Array.IndexOf(FamilyBondTypes, bondType) < 0) continue;

                foreach (string neighbor in kv.Value)
                {
                    if (string.IsNullOrEmpty(neighbor)) continue;
                    yield return new BondEdge(neighbor, bondType, viaInitiated);
                }
            }
        }

        private static string Render(string rootDisplay, int degrees, bool familyOnly,
                                     SortedDictionary<int, List<Connection>> byDegree, bool capReached)
        {
            bool anyConnections = byDegree.Any(kv => kv.Value.Count > 0);
            if (!anyConnections)
            {
                // Empty-state wording pending owner review.
                return familyOnly
                    ? rootDisplay + " has no family bonds yet."
                    : rootDisplay + " has no bonds yet.";
            }

            string treeLabel = familyOnly ? "Family tree" : "Bond tree";
            string header = "[b]" + treeLabel + " for " + rootDisplay + ", up to " + degrees +
                            (degrees == 1 ? " degree:" : " degrees:") + "[/b]";

            StringBuilder body = new StringBuilder();
            foreach (KeyValuePair<int, List<Connection>> kv in byDegree)
            {
                if (kv.Value.Count == 0) continue;
                body.Append("\n[u]").Append(kv.Key).Append(Utils.GetDaySuffix(kv.Key)).Append(" degree:[/u]");
                foreach (Connection c in kv.Value.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                {
                    body.Append("\n  ").Append(c.Name).Append(" (").Append(c.ConnectorName)
                        .Append("'s ").Append(c.Role).Append(")");
                }
            }
            if (capReached)
            {
                body.Append("\n…and more (limit reached)");
            }

            string bodyText = body.ToString();
            if (header.Length + bodyText.Length > SpoilerThreshold)
            {
                return header + "\n[spoiler]" + bodyText.TrimStart('\n') + "[/spoiler]";
            }
            return header + bodyText;
        }

        private static string ResolveDisplay(Profile profile, string fallbackUserName)
        {
            return profile != null && !string.IsNullOrEmpty(profile.displayName)
                ? profile.displayName
                : fallbackUserName;
        }

        private struct FrontierNode
        {
            public readonly string UserName;
            public readonly int Degree;
            public FrontierNode(string userName, int degree) { UserName = userName; Degree = degree; }
        }

        private struct BondEdge
        {
            public readonly string NeighborUserName;
            public readonly string BondType;
            public readonly bool ViaInitiated;
            public BondEdge(string neighbor, string bondType, bool viaInitiated)
            {
                NeighborUserName = neighbor;
                BondType = bondType;
                ViaInitiated = viaInitiated;
            }
        }

        private struct Connection
        {
            public readonly string Name;
            public readonly string ConnectorName;
            public readonly string Role;
            public Connection(string name, string connectorName, string role)
            {
                Name = name;
                ConnectorName = connectorName;
                Role = role;
            }
        }
    }
}
