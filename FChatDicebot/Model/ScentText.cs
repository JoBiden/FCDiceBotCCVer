using System;

namespace FChatDicebot.Model
{
    /// <summary>
    /// Renders a scent as a user-facing noun phrase. Categorization is data-driven so
    /// admins can tag new scents in the Identifiers collection without code changes:
    ///
    /// - <c>personal</c> (e.g. musk) → "{appliedBy}'s {scent}" — "Alice's musk"
    /// - <c>scentof</c>  (e.g. lemonade, liquor, perfume) → "a scent of {scent}"
    /// - anything else → "a {scent} scent" — "a wood scent"
    ///
    /// The same helper feeds the !odorize consent/completion text, the !wash output,
    /// and the OdorizeStatusContributor descriptors so the rendering stays consistent
    /// across every place a scent shows up.
    /// </summary>
    public static class ScentText
    {
        public const string PersonalCategory = "personal";
        public const string ScentOfCategory = "scentof";

        public static string ScentPhrase(Identifier scentIdentifier, string scentName, string appliedBy)
        {
            string name = !string.IsNullOrEmpty(scentName)
                ? scentName
                : (scentIdentifier?.type ?? string.Empty);

            if (scentIdentifier?.categories != null)
            {
                foreach (var category in scentIdentifier.categories)
                {
                    if (string.Equals(category, PersonalCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        string owner = string.IsNullOrEmpty(appliedBy) ? "someone" : appliedBy;
                        return owner + "'s " + name;
                    }
                    if (string.Equals(category, ScentOfCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        return "a scent of " + name;
                    }
                }
            }

            return "a " + name + " scent";
        }

        /// <summary>
        /// Uppercase the first character of a phrase so it can start a sentence. Leaves the
        /// rest of the phrase alone, so "Alice's musk" stays "Alice's musk".
        /// </summary>
        public static string Capitalize(string phrase)
        {
            if (string.IsNullOrEmpty(phrase)) return phrase;
            if (char.IsUpper(phrase[0])) return phrase;
            return char.ToUpper(phrase[0]) + phrase.Substring(1);
        }
    }
}
