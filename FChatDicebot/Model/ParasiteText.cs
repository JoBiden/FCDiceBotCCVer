using System;

namespace FChatDicebot.Model
{
    /// <summary>
    /// User-facing rendering helpers for parasite names. Centralizes the per-parasite
    /// styling (colors, spacing) and the a/an article inflection used in consent and
    /// completion messages.
    ///
    /// Special-case rendering lives here because the underlying identifier name is a single
    /// lowercase token (<c>lustleeches</c>) that we want to surface as a styled phrase
    /// (<c>[color=purple]lust leeches[/color]</c>). Most parasites render as their bare
    /// identifier name — only the explicit cases below override.
    /// </summary>
    public static class ParasiteText
    {
        /// <summary>
        /// Render a parasite name with any per-parasite styling. Returns the bare name when
        /// no override applies. Null/empty inputs return <see cref="string.Empty"/>.
        /// </summary>
        public static string ParasiteName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            if (string.Equals(name, "lustleeches", StringComparison.OrdinalIgnoreCase))
            {
                return "[color=purple]lust leeches[/color]";
            }
            return name;
        }

        /// <summary>
        /// Render a parasite name preceded by the appropriate indefinite article
        /// ("a"/"an"). Plural-shaped names (those ending in 's' — tentacles, nymites,
        /// lustleeches) take no article. Vowel-initial names take "an"; otherwise "a".
        /// </summary>
        public static string ParasiteNameWithArticle(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            string rendered = ParasiteName(name);
            if (TreatAsPlural(name)) return rendered;
            string article = StartsWithVowelSound(name) ? "an " : "a ";
            return article + rendered;
        }

        /// <summary>
        /// True when the parasite name should be treated as a plural for grammar purposes —
        /// drives both article suppression ("a tentacles" reads wrong) and verb agreement
        /// ("tentacles have" vs "paraslime has") in flavor/spread fragments.
        /// </summary>
        public static bool TreatAsPlural(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.EndsWith("s", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verb agreement helper for parasite name as subject: "have" for plural-shaped
        /// names, "has" otherwise.
        /// </summary>
        public static string HasOrHave(string name)
        {
            return TreatAsPlural(name) ? "have" : "has";
        }

        private static bool StartsWithVowelSound(string name)
        {
            char first = char.ToLowerInvariant(name[0]);
            return first == 'a' || first == 'e' || first == 'i' || first == 'o' || first == 'u';
        }
    }
}
