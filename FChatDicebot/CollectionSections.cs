using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot
{
    /// <summary>
    /// Every <see cref="CollectionSection"/> <c>!collection</c> prints, in the order it prints
    /// them.
    ///
    /// <para>
    /// Deliberately a plain static array rather than a registry with an <c>Initialize</c> call.
    /// The interaction and status-effect registries need one because their contents depend on a
    /// live database; sections don't, and an initialize step is a thing to get wrong in exactly
    /// the way <c>MonDB.Initialize</c> already is. A new type is one line here, and
    /// <c>CollectionSectionTests</c> fails if a <see cref="Collectible"/> subclass ships without
    /// one — which is the same guarantee the <c>!help</c> listing gets from
    /// <c>ChatBotCommand.Category</c>.
    /// </para>
    /// </summary>
    public static class CollectionSections
    {
        // Print order. Bottles first: they are the oldest type, the only sellable one, and the
        // one the footer's sell-value line is about.
        private static readonly CollectionSection[] Sections =
        {
            new BottleCollectionSection(),
            new PantiesCollectionSection(),
        };

        /// <summary>Every section, in print order.</summary>
        public static IReadOnlyList<CollectionSection> InPrintOrder => Sections;

        /// <summary>
        /// The section a resident named, or null if the word isn't a type. Used to tell
        /// "!collection panties" apart from a substance filter or a bare name, and to route
        /// "!pay ... panties #43" to the right kind of item.
        /// </summary>
        public static CollectionSection ByKeyword(string word)
        {
            if (string.IsNullOrEmpty(word)) return null;
            return Sections.FirstOrDefault(s => s.Answers(word));
        }

        /// <summary>
        /// The section a set of typed terms names, or null if none of them is a type word. This
        /// is what decides whether <c>!pay</c> is moving goods or currency.
        /// </summary>
        public static CollectionSection ByKeywordIn(IEnumerable<string> terms)
        {
            if (terms == null) return null;
            foreach (string term in terms)
            {
                CollectionSection section = ByKeyword(term);
                if (section != null) return section;
            }
            return null;
        }

        /// <summary>
        /// Every word that means a type, across all of them. Handed to <c>!collection</c> and
        /// <c>!pay</c> as argument keywords so bare-name resolution accounts for them instead of
        /// reporting an unrecognized resident.
        /// </summary>
        public static string[] AllKeywords()
        {
            return Sections.SelectMany(s => s.Keywords).ToArray();
        }

        /// <summary>The section that renders a given item, or null if its type has none.</summary>
        public static CollectionSection For(Collectible item)
        {
            if (item == null) return null;
            return Sections.FirstOrDefault(s => s.ItemType == item.GetType());
        }
    }
}
