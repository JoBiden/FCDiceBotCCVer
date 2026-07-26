using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FChatDicebot
{
    /// <summary>
    /// A registered resident's two names, projected out of RegisteredProfiles. Deliberately
    /// tiny — this is loaded for *every* resident to back bare-name targeting, so it must not
    /// drag whole <see cref="Model.Profile"/> documents across the wire.
    /// </summary>
    public class ProfileName
    {
        public string userName { get; set; }
        public string displayName { get; set; }
    }

    /// <summary>
    /// Outcome of trying to infer a recipient from terms that weren't wrapped in a
    /// <c>[user]</c> tag. <see cref="Terms"/> is what the command should actually be
    /// dispatched with — either the original terms untouched, or a copy with a synthetic
    /// <c>[user]…[/user]</c> tag spliced in where the bare name was typed.
    /// </summary>
    public class RecipientResolution
    {
        public string[] Terms;

        /// <summary>First canonical userName spliced in, or null when nothing was inferred.</summary>
        public string ResolvedName;

        /// <summary>
        /// Every name spliced in, in the order typed. More than one only for the group
        /// casuals ("!cuddle bob smith jane doe"); commands that take a single recipient read
        /// the first tag and ignore the rest, exactly as they do with typed-out tags.
        /// </summary>
        public List<string> ResolvedNames = new List<string>();

        /// <summary>The bare text the resident actually typed, for error wording.</summary>
        public string TypedText;

        /// <summary>Populated only when the typed text matched more than one resident.</summary>
        public List<string> Candidates;

        /// <summary>
        /// Set when the resident clearly typed a name but we couldn't place it — nothing else
        /// in the command accounted for those words, and no resident matched them. The command
        /// is going to fail its own recipient lookup anyway, so this is our chance to say
        /// something more useful than "the user  could not be found".
        /// </summary>
        public string UnresolvedText;

        public bool IsAmbiguous { get { return Candidates != null && Candidates.Count > 1; } }
    }

    /// <summary>
    /// Lets residents target each other by typing a bare name instead of a <c>[user]</c> tag
    /// (<c>!cuddle bob smith</c>, <c>!dose queen contract lust</c>).
    ///
    /// The approach is normalization, not re-parsing: everything a command recognizes by shape
    /// is already position-independent (identifiers, integers, quoted text, eicons), so we strip
    /// those, treat what's left as a name, and — if it matches a real registered resident —
    /// rewrite the terms to contain the <c>[user]</c> tag the resident didn't type. Every
    /// existing parser downstream (<c>GetUserNameFromCommandTerms</c>, the group-casual
    /// multi-target parse, <c>GetInteractionTypeFromCommandTerms</c>) then works unchanged.
    ///
    /// Two properties keep this safe:
    /// <list type="bullet">
    ///   <item>A real <c>[user]</c> tag always wins, so nothing that works today changes.</item>
    ///   <item>The rewrite only happens when the leftover text matches a registered resident,
    ///         so unrecognized arguments (<c>!breed X random</c>, <c>!no all</c>) fall through
    ///         to the command's own parsing exactly as before.</item>
    /// </list>
    /// </summary>
    public static class RecipientResolver
    {
        /// <summary>
        /// Bare words that are arguments to some command rather than names. Without this a
        /// resident who registered as "Random" would swallow <c>!breed X random</c>.
        /// </summary>
        private static readonly HashSet<string> ReservedTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "all", "random", "mixed", "self", "me", "none", "list", "help",
        };

        /// <summary>
        /// Shortest bare text we'll accept for a *prefix* match. Exact and display-name matches
        /// have no minimum — if someone's name really is "Jo", typing "jo" should find them —
        /// but "a" resolving to whoever happens to sort first would be obnoxious.
        /// </summary>
        private const int MinimumPrefixLength = 3;

        /// <summary>
        /// Ceiling on how many names one command can name bare. Well above the biggest group
        /// casual anyone types, and it guarantees the resolve loop terminates.
        /// </summary>
        private const int MaximumRecipients = 8;

        public static RecipientResolution Resolve(
            string[] rawTerms,
            IEnumerable<string> identifierTypes,
            IEnumerable<ProfileName> knownNames)
        {
            var unchanged = new RecipientResolution { Terms = rawTerms };

            if (rawTerms == null || rawTerms.Length == 0)
                return unchanged;

            // An explicit [user] tag is authoritative — never second-guess it.
            if (rawTerms.Any(t => !string.IsNullOrEmpty(t) && t.StartsWith("[user]")))
                return unchanged;

            var names = knownNames == null
                ? new List<ProfileName>()
                : knownNames.Where(n => n != null && !string.IsNullOrWhiteSpace(n.userName)).ToList();
            if (names.Count == 0)
                return unchanged;

            var identifiers = new HashSet<string>(
                (identifierTypes ?? Enumerable.Empty<string>()).Where(t => !string.IsNullOrWhiteSpace(t)),
                StringComparer.OrdinalIgnoreCase);

            string[] working = rawTerms;
            var resolvedNames = new List<string>();
            string firstTyped = null;

            for (int pass = 0; pass < MaximumRecipients; pass++)
            {
                // Only the first name may be matched on a prefix. Beyond that we're adding
                // people to a group interaction they didn't obviously ask for, so a partial
                // word isn't enough — spell the rest out or tag them.
                RecipientResolution outcome = ResolveOne(working, identifiers, names, allowPrefix: pass == 0);
                if (outcome == null)
                    break;

                if (outcome.Candidates != null)
                {
                    // Ambiguity on the very first name is worth stopping for. Ambiguity on a
                    // later one isn't — we already have a recipient, so run with it rather
                    // than refusing the whole command over a trailing word.
                    if (resolvedNames.Count == 0)
                        return outcome;
                    break;
                }

                working = outcome.Terms;
                resolvedNames.Add(outcome.ResolvedName);
                if (firstTyped == null)
                    firstTyped = outcome.TypedText;
            }

            if (resolvedNames.Count == 0)
            {
                // Words we couldn't account for and couldn't place. Report the longest run of
                // them so the caller can say who we didn't recognize.
                List<int> leftovers = FindLeftoverTermIndices(rawTerms, identifiers);
                if (leftovers.Count == 0)
                    return unchanged;

                var firstRun = ContiguousSpans(leftovers).First();
                unchanged.UnresolvedText = string.Join(" ", firstRun.Select(i => rawTerms[i]));
                return unchanged;
            }

            return new RecipientResolution
            {
                Terms = working,
                ResolvedName = resolvedNames[0],
                ResolvedNames = resolvedNames,
                TypedText = firstTyped,
            };
        }

        /// <summary>
        /// One resolution pass: find the best span of unaccounted-for words that names exactly
        /// one resident and splice the tag in. Returns null when nothing matched.
        /// </summary>
        private static RecipientResolution ResolveOne(
            string[] terms, HashSet<string> identifiers, List<ProfileName> names, bool allowPrefix)
        {
            List<int> leftovers = FindLeftoverTermIndices(terms, identifiers);
            if (leftovers.Count == 0)
                return null;

            var spans = ContiguousSpans(leftovers).ToList();

            // Longest spans first: "bob smith" should win over "bob" when both resolve.
            foreach (var span in spans)
            {
                string typed = string.Join(" ", span.Select(i => terms[i]));

                List<string> matches = MatchByName(typed, names);
                if (matches.Count == 1)
                    return Splice(terms, span, matches[0], typed);
                if (matches.Count > 1)
                    return Ambiguous(terms, typed, matches);
            }

            if (!allowPrefix)
                return null;

            // Nothing matched a whole name — fall back to prefixes ("!cuddle queen" for
            // "Queen Contract"). Deliberately a second pass: a complete name anywhere in the
            // input beats a prefix match on a longer span.
            foreach (var span in spans)
            {
                string typed = string.Join(" ", span.Select(i => terms[i]));
                if (typed.Length < MinimumPrefixLength)
                    continue;

                List<string> matches = MatchByPrefix(typed, names);
                if (matches.Count == 1)
                    return Splice(terms, span, matches[0], typed);
                if (matches.Count > 1)
                    return Ambiguous(terms, typed, matches);
            }

            return null;
        }

        /// <summary>
        /// Residents whose name is a plausible typo of what was typed, best first. Used to turn
        /// "we couldn't find them" into "did you mean…?" — by the time this runs, exact,
        /// case-insensitive, display-name and prefix matching have all already missed.
        /// </summary>
        public static List<string> SuggestSimilarNames(
            string typedText, IEnumerable<ProfileName> knownNames, int maximumSuggestions)
        {
            if (string.IsNullOrWhiteSpace(typedText) || knownNames == null)
                return new List<string>();

            // Scale the tolerance to the length of what was typed, or short names would match
            // half the roster.
            int tolerance = typedText.Length <= 4 ? 1 : (typedText.Length <= 8 ? 2 : 3);

            return knownNames
                .Where(n => n != null && !string.IsNullOrWhiteSpace(n.userName))
                .Select(n => new
                {
                    n.userName,
                    distance = Math.Min(
                        EditDistance(typedText, n.userName),
                        EditDistance(typedText, StripDisplayName(n.displayName))),
                })
                .Where(x => x.distance <= tolerance)
                .OrderBy(x => x.distance)
                .ThenBy(x => x.userName, StringComparer.OrdinalIgnoreCase)
                .Take(maximumSuggestions)
                .Select(x => x.userName)
                .ToList();
        }

        private static int EditDistance(string left, string right)
        {
            if (string.IsNullOrEmpty(left)) return string.IsNullOrEmpty(right) ? 0 : right.Length;
            if (string.IsNullOrEmpty(right)) return left.Length;

            left = left.ToLowerInvariant();
            right = right.ToLowerInvariant();

            // Two rolling rows rather than the full matrix — this runs over every resident.
            var previous = new int[right.Length + 1];
            var current = new int[right.Length + 1];

            for (int j = 0; j <= right.Length; j++)
                previous[j] = j;

            for (int i = 1; i <= left.Length; i++)
            {
                current[0] = i;
                for (int j = 1; j <= right.Length; j++)
                {
                    int substitution = previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1);
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), substitution);
                }

                int[] swap = previous;
                previous = current;
                current = swap;
            }

            return previous[right.Length];
        }

        /// <summary>
        /// Indices of terms that aren't accounted for by any other argument shape the command
        /// could be parsing — i.e. the candidate name words. Mirrors the tag/quote/eicon state
        /// machine the other term parsers in <see cref="BotCommandController"/> use.
        /// </summary>
        private static List<int> FindLeftoverTermIndices(string[] rawTerms, HashSet<string> identifiers)
        {
            var leftovers = new List<int>();
            bool inUserTag = false;
            bool inQuotes = false;
            bool inEIcon = false;

            for (int i = 0; i < rawTerms.Length; i++)
            {
                string term = rawTerms[i];
                if (string.IsNullOrWhiteSpace(term))
                    continue;

                // Tags spliced by an earlier pass are settled recipients, not candidate words.
                if (inUserTag)
                {
                    if (term.EndsWith("[/user]")) inUserTag = false;
                    continue;
                }
                if (inEIcon)
                {
                    if (term.EndsWith("[/eicon]")) inEIcon = false;
                    continue;
                }
                if (inQuotes)
                {
                    if (term.EndsWith("\"")) inQuotes = false;
                    continue;
                }

                if (term.StartsWith("[user]"))
                {
                    if (!term.EndsWith("[/user]")) inUserTag = true;
                    continue;
                }
                if (term.StartsWith("[eicon]"))
                {
                    if (!term.EndsWith("[/eicon]")) inEIcon = true;
                    continue;
                }
                if (term.StartsWith("\""))
                {
                    if (!(term.Length > 1 && term.EndsWith("\""))) inQuotes = true;
                    continue;
                }

                // Amounts, day counts, tree depths, pending-seat numbers.
                int ignoredNumber;
                if (int.TryParse(term, out ignoredNumber))
                    continue;

                if (identifiers.Contains(term))
                    continue;
                if (ReservedTerms.Contains(term))
                    continue;

                // Any other bbcode the resident pasted in isn't part of a name.
                if (term.StartsWith("["))
                    continue;

                leftovers.Add(i);
            }

            return leftovers;
        }

        /// <summary>
        /// Every run of adjacent leftover terms, sub-divided into spans and ordered longest
        /// first. Adjacency matters: a name is typed as consecutive words, so terms sitting on
        /// either side of an amount ("bob 50 smith") are not one name.
        /// </summary>
        private static IEnumerable<List<int>> ContiguousSpans(List<int> leftovers)
        {
            var runs = new List<List<int>>();
            var current = new List<int>();

            foreach (int index in leftovers)
            {
                if (current.Count > 0 && index != current[current.Count - 1] + 1)
                {
                    runs.Add(current);
                    current = new List<int>();
                }
                current.Add(index);
            }
            if (current.Count > 0)
                runs.Add(current);

            var spans = new List<List<int>>();
            foreach (var run in runs)
            {
                for (int start = 0; start < run.Count; start++)
                {
                    for (int end = start; end < run.Count; end++)
                    {
                        spans.Add(run.GetRange(start, end - start + 1));
                    }
                }
            }

            return spans.OrderByDescending(s => s.Count).ThenBy(s => s[0]);
        }

        /// <summary>
        /// Whole-name match, best tier first: exact userName, then case-insensitive userName,
        /// then display name. Tiers don't blend — a case-exact hit is never put to a vote
        /// against someone whose display name happens to collide.
        /// </summary>
        private static List<string> MatchByName(string typed, List<ProfileName> names)
        {
            var exact = names.Where(n => string.Equals(n.userName, typed, StringComparison.Ordinal))
                             .Select(n => n.userName).ToList();
            if (exact.Count > 0)
                return Distinct(exact);

            var caseInsensitive = names.Where(n => string.Equals(n.userName, typed, StringComparison.OrdinalIgnoreCase))
                                       .Select(n => n.userName).ToList();
            if (caseInsensitive.Count > 0)
                return Distinct(caseInsensitive);

            var byDisplay = names.Where(n => string.Equals(StripDisplayName(n.displayName), typed, StringComparison.OrdinalIgnoreCase))
                                 .Select(n => n.userName).ToList();
            return Distinct(byDisplay);
        }

        private static List<string> MatchByPrefix(string typed, List<ProfileName> names)
        {
            var matches = names.Where(n =>
                    n.userName.StartsWith(typed, StringComparison.OrdinalIgnoreCase)
                    || StripDisplayName(n.displayName).StartsWith(typed, StringComparison.OrdinalIgnoreCase))
                .Select(n => n.userName)
                .ToList();
            return Distinct(matches);
        }

        private static List<string> Distinct(List<string> matches)
        {
            return matches
                .GroupBy(m => m, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Reduce a stored displayName to the plain text people would actually type. Renamed
        /// residents carry their old name struck through ("[s]Bob[/s] Steve"); that half isn't
        /// what anyone calls them any more, and it still resolves through the userName tiers.
        /// </summary>
        public static string StripDisplayName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return string.Empty;

            string working = displayName;
            while (true)
            {
                int open = working.IndexOf("[s]", StringComparison.OrdinalIgnoreCase);
                if (open < 0) break;
                int close = working.IndexOf("[/s]", open, StringComparison.OrdinalIgnoreCase);
                if (close < 0) break;
                working = working.Remove(open, (close + "[/s]".Length) - open);
            }

            var plain = new StringBuilder();
            bool inTag = false;
            foreach (char character in working)
            {
                if (character == '[') { inTag = true; continue; }
                if (character == ']') { inTag = false; continue; }
                if (!inTag) plain.Append(character);
            }

            return plain.ToString().Trim();
        }

        /// <summary>
        /// Replace the matched bare-name terms with the <c>[user]</c> tag the resident would
        /// have typed, tokenized exactly the way F-Chat sends one (the tag opens on the first
        /// word of the name and closes on the last).
        /// </summary>
        private static RecipientResolution Splice(string[] rawTerms, List<int> span, string resolvedName, string typed)
        {
            string[] nameWords = resolvedName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameWords.Length == 0)
                return new RecipientResolution { Terms = rawTerms };

            var tagTerms = new List<string>();
            for (int i = 0; i < nameWords.Length; i++)
            {
                string word = nameWords[i];
                if (i == 0) word = "[user]" + word;
                if (i == nameWords.Length - 1) word = word + "[/user]";
                tagTerms.Add(word);
            }

            int first = span[0];
            int last = span[span.Count - 1];

            var rewritten = new List<string>();
            rewritten.AddRange(rawTerms.Take(first));
            rewritten.AddRange(tagTerms);
            rewritten.AddRange(rawTerms.Skip(last + 1));

            return new RecipientResolution
            {
                Terms = rewritten.ToArray(),
                ResolvedName = resolvedName,
                ResolvedNames = new List<string> { resolvedName },
                TypedText = typed,
            };
        }

        private static RecipientResolution Ambiguous(string[] rawTerms, string typed, List<string> matches)
        {
            return new RecipientResolution
            {
                Terms = rawTerms,
                TypedText = typed,
                Candidates = matches,
            };
        }
    }
}
