using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FChatDicebot.Model
{
    /// <summary>
    /// Which family of information a readout section belongs to. Drives the section-header
    /// colour so the same kind of fact wears the same colour in every readout — a resident
    /// who learns that red means "something is currently on you" in <c>!dossier</c> reads
    /// <c>!statistics</c> the same way without being taught twice.
    /// </summary>
    public enum ReadoutDomain
    {
        /// <summary>No colour. Neutral sections and anything historical or narrative.</summary>
        None,
        /// <summary>Things currently afflicting someone: curses, parasites, breaks, scents, corruption.</summary>
        Affliction,
        /// <summary>Connections between residents: bonds, marks, employment rosters, populations.</summary>
        Relationship,
        /// <summary>Tallies and history: interaction counts, offspring, experience, lifetime totals.</summary>
        Record,
        /// <summary>Money, goods and work: vaults, collections, payroll, workforce.</summary>
        Economy,
        /// <summary>Documentation fields in <c>!help</c> and the identifier lookups.</summary>
        Reference
    }

    /// <summary>
    /// Single source of truth for the BBCode grammar shared by every informative readout
    /// (<c>!dossier</c>, <c>!bank</c>, <c>!statistics</c>, the drill-downs, <c>!help</c>, ...).
    ///
    /// Before this, each readout was written on its own and drifted: <c>!dossier</c> alone
    /// carried three section-header shapes (<c>[b]Active Curses:[/b]</c>,
    /// <c>Currently known [b]Marks[/b]:</c>, <c>Days of [b]Experience[/b] working as...</c>),
    /// <c>!bank</c> bolded the value where <c>!dossier</c> bolded the label, and four different
    /// inline separators were in use across five commands. Route new readout text through the
    /// helpers here rather than hand-assembling tags.
    ///
    /// Colour note: F-Chat resolves <c>[color=x]</c> to a fixed hex regardless of theme
    /// (<c>scss/_flist_derived.scss</c>; only <c>blue</c> is redefined per theme). Colours are
    /// therefore chosen once here and not per call site. Black and white are never used —
    /// each is invisible on one of the two themes.
    /// </summary>
    public static class ReadoutText
    {
        /// <summary>Gap between cells in a one-line section. The only inline separator in use.</summary>
        public const string InlineSeparator = "   ";

        /// <summary>Leading indent for rows sitting under a section header.</summary>
        public const string RowIndent = "  ";

        /// <summary>Row count above which a multi-line section collapses into a spoiler.</summary>
        public const int SpoilerLineThreshold = 6;

        /// <summary>
        /// The colour a section header wears for a given domain, or null for no colour.
        /// </summary>
        public static string DomainColor(ReadoutDomain domain)
        {
            switch (domain)
            {
                case ReadoutDomain.Affliction: return "red";
                case ReadoutDomain.Relationship: return "purple";
                case ReadoutDomain.Record: return "blue";
                case ReadoutDomain.Economy: return "brown";
                case ReadoutDomain.Reference: return "purple";
                default: return null;
            }
        }

        /// <summary>
        /// The readout's opening line: <c>[b][u]Title[/u][/b]</c>. Exactly one per readout, and
        /// the only place bold-and-underline is used, so it can't be confused with a section.
        /// </summary>
        public static string Title(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return "[b][u]" + text + "[/u][/b]";
        }

        /// <summary>
        /// A section header: <c>[b][color=x]Header:[/color][/b]</c>. The colon is applied here
        /// rather than by callers, which is what kept it drifting inside and outside the tag.
        /// Pass <paramref name="header"/> without trailing punctuation.
        /// </summary>
        public static string Section(string header, ReadoutDomain domain)
        {
            if (string.IsNullOrEmpty(header)) return string.Empty;
            string color = DomainColor(domain);
            string inner = header.TrimEnd(':', ' ') + ":";
            if (!string.IsNullOrEmpty(color))
            {
                inner = "[color=" + color + "]" + inner + "[/color]";
            }
            return "[b]" + inner + "[/b]";
        }

        /// <summary>
        /// A row label: <c>[u]Label:[/u]</c>. Pass <paramref name="label"/> without the colon.
        /// </summary>
        public static string Label(string label)
        {
            if (string.IsNullOrEmpty(label)) return string.Empty;
            return "[u]" + label.TrimEnd(':', ' ') + ":[/u]";
        }

        /// <summary>
        /// The number a line exists to report. The unit stays outside the tag, so
        /// <c>Num(340) + " gold"</c> gives "[b]340[/b] gold" rather than bolding the whole
        /// phrase — the value is what the eye should catch.
        ///
        /// Deliberately does not apply thousands separators: <see cref="TextFormat.ApplyNumberCommas"/>
        /// does that downstream when the channel opts in, and it already knows to skip digits
        /// inside identifier-bearing tags.
        /// </summary>
        public static string Num(long value)
        {
            return "[b]" + value + "[/b]";
        }

        /// <summary>Overload for values a caller has already formatted (e.g. "8,140" or "3 copper").</summary>
        public static string Num(string formatted)
        {
            if (string.IsNullOrEmpty(formatted)) return string.Empty;
            return "[b]" + formatted + "[/b]";
        }

        /// <summary>A full row: <c>[u]Label:[/u] value</c>.</summary>
        public static string Row(string label, string value)
        {
            return Label(label) + " " + value;
        }

        /// <summary>
        /// Capitalises the first actual letter, stepping over any leading BBCode tags.
        ///
        /// Row labels are Title Case, but some <c>Utils.*ToText</c> helpers hand back a
        /// colour-wrapped phrase rather than a bare word — <c>SubstanceToText("lustessence")</c>
        /// returns "[color=purple]Lust Essence[/color]" while <c>SubstanceToText("milk")</c>
        /// returns "milk". A plain <c>char.ToUpper(s[0])</c> would uppercase the "[" and leave
        /// the word untouched, so the two rendered with different casing side by side.
        ///
        /// Deliberately not folded into <see cref="Label"/>: plenty of labels are display names
        /// or userName fallbacks, which must be shown exactly as stored.
        /// </summary>
        public static string CapitalizePastTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            int i = 0;
            while (i < text.Length && text[i] == '[')
            {
                int close = text.IndexOf(']', i);
                if (close < 0) return text; // malformed; leave it alone
                i = close + 1;
            }
            if (i >= text.Length || !char.IsLetter(text[i]) || char.IsUpper(text[i])) return text;

            return text.Substring(0, i) + char.ToUpper(text[i]) + text.Substring(i + 1);
        }

        /// <summary>Small print. Bookkeeping notes, parentheticals and command pointers.</summary>
        public static string Small(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return "[sub]" + text + "[/sub]";
        }

        /// <summary>
        /// A section's closing pointer at sibling commands, on its own line in small print.
        /// </summary>
        public static string Footer(string text)
        {
            return Small(text);
        }

        /// <summary>
        /// An empty slot. Small print rather than body weight so a resident with no pledges
        /// doesn't read a screenful of "None" as loudly as real data.
        /// </summary>
        public static string None()
        {
            return Small("None");
        }

        /// <summary>
        /// Renders a one-line section: a header followed by cells on the same line, separated
        /// by <see cref="InlineSeparator"/>. Returns empty for an empty cell list so callers
        /// can append unconditionally. Used wherever a row is a short label and one number.
        /// </summary>
        public static string InlineSection(string header, ReadoutDomain domain, IList<string> cells)
        {
            if (cells == null || cells.Count == 0) return string.Empty;
            return Section(header, domain) + " " + string.Join(InlineSeparator, cells) + "\n";
        }

        /// <summary>
        /// Renders a multi-line section: one indented row per line under the header. Past
        /// <see cref="SpoilerLineThreshold"/> rows the body collapses into a spoiler with
        /// <paramref name="summary"/> (e.g. "23 residents across 11 roles") left visible beside
        /// the header, so a collapsed block still reports its own size.
        /// </summary>
        public static string LineSection(string header, ReadoutDomain domain, string summary, IList<string> rows)
        {
            if (rows == null || rows.Count == 0) return string.Empty;

            StringBuilder sb = new StringBuilder();
            sb.Append(Section(header, domain));

            IEnumerable<string> indented = rows.Select(r => RowIndent + r);

            if (rows.Count > SpoilerLineThreshold)
            {
                if (!string.IsNullOrEmpty(summary))
                {
                    sb.Append(' ').Append(summary);
                }
                sb.Append(" [spoiler]\n");
                sb.Append(string.Join("\n", indented));
                sb.Append("[/spoiler]\n");
            }
            else
            {
                sb.Append('\n');
                sb.Append(string.Join("\n", indented));
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Joins the section groups that actually produced content, separating each with a
        /// blank line. Skipping empties is what stops a resident with no active curses from
        /// getting a stray gap where the affliction group would have been.
        /// </summary>
        public static string JoinClusters(params string[] clusters)
        {
            return string.Join("\n", clusters.Where(c => !string.IsNullOrEmpty(c)));
        }
    }
}
