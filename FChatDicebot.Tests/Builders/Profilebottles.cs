using FChatDicebot.Model;
using System.Collections.Generic;

namespace FChatDicebot.Tests.Builders
{
    /// <summary>
    /// Test-side shorthand for "the bottles in this profile's collection".
    ///
    /// <see cref="Profile.collectibles"/> is a mixed <see cref="Collectible"/> list, so reading a
    /// bottle-only field off it needs a type filter. Rather than spell that out at every
    /// assertion, these tests say <c>profile.Bottles()[0].substance</c>. Routed through the
    /// production <see cref="CollectionInventory.OfType{T}"/> so the tests exercise the real
    /// selector rather than a parallel one.
    ///
    /// The returned list is new, but the elements are the same instances the profile holds —
    /// mutating <c>Bottles()[0].emptiedAt</c> mutates the stored bottle, which several tests rely on.
    /// </summary>
    internal static class ProfileBottles
    {
        internal static List<MilkBottle> Bottles(this Profile profile)
        {
            return CollectionInventory.OfType<MilkBottle>(profile);
        }
    }
}
