using System;
using System.Linq;

namespace FChatDicebot.Model
{
    /// <summary>
    /// Renders a vice identifier as a user-facing noun phrase for !dose / !detox text and
    /// for craving / satisfaction fragments emitted by DoseStatusContributor.
    ///
    /// Routing:
    /// <list type="bullet">
    /// <item><description><c>lustessence</c> → <c>[color=purple]lust essence[/color]</c></description></item>
    /// <item><description><c>golden</c> → <c>[color=yellow]golden fluid[/color]</c></description></item>
    /// <item><description>Scent-category vices (musk, liquor, perfume, …) → <see cref="ScentText.ScentPhrase"/>
    /// so the vice display mirrors !odorize: "Alice's musk", "a scent of liquor",
    /// "a salty scent", …</description></item>
    /// <item><description>Anything else → the raw vice name as stored.</description></item>
    /// </list>
    /// </summary>
    public static class ViceText
    {
        public const string ScentCategory = "scent";

        public static string ViceName(Identifier viceIdentifier, string viceName, string dosedBy)
        {
            if (string.IsNullOrEmpty(viceName)) return string.Empty;
            string normalized = viceName.ToLowerInvariant();

            if (normalized == "lustessence") return "[color=purple]lust essence[/color]";
            if (normalized == "golden") return "[color=yellow]golden fluid[/color]";

            if (viceIdentifier?.categories != null
                && viceIdentifier.categories.Any(c => string.Equals(c, ScentCategory, StringComparison.OrdinalIgnoreCase)))
            {
                return ScentText.ScentPhrase(viceIdentifier, viceName, dosedBy);
            }

            return viceName;
        }
    }
}
