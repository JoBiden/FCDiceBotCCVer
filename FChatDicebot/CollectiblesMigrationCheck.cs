using FChatDicebot.Database;
using System;

namespace FChatDicebot
{
    /// <summary>
    /// Startup guard: refuses to run the bot against a database that hasn't been through
    /// <c>scripts/backfill-collectibles.js</c>.
    ///
    /// <para>
    /// <b>Why this has to exist.</b> The collectibles migration renamed the stored list from
    /// <c>milkInventory</c> to <c>collectibles</c>. The Mongo driver ignores fields it doesn't
    /// recognize, so an unmigrated profile does <i>not</i> fail to load — it loads cleanly with
    /// an empty collection, showing the resident no bottles at all. The first write back to that
    /// profile then persists the empty list and the collection is gone for real.
    /// </para>
    ///
    /// <para>
    /// That is the one failure mode a migration must never have: silent, and destructive on the
    /// next write rather than at the moment of the mistake. The absent <c>_t</c> discriminator
    /// throws loudly, but only for profiles that made it as far as the new field name — it does
    /// nothing for a profile the script never touched. This check covers the gap, converting
    /// "quietly lose every bottle in the Chateau" into "won't start, here's the script to run".
    /// </para>
    ///
    /// <para>
    /// Safe on a fresh database: a server with no profiles, or no pre-migration profiles, counts
    /// zero and passes.
    /// </para>
    /// </summary>
    public static class CollectiblesMigrationCheck
    {
        /// <summary>The pre-migration field name. Its presence anywhere is the failure signal.</summary>
        public const string LegacyInventoryField = "milkInventory";

        /// <summary>
        /// Throws when any profile still carries the pre-migration field. Called once at startup,
        /// immediately after <c>MonDB.Initialize</c> and before anything can write a profile.
        /// </summary>
        public static void AssertMigrated(IChateauDatabase database)
        {
            if (database == null) return;

            int unmigrated = database.CountLegacyMilkInventoryProfiles();
            if (unmigrated == 0) return;

            throw new InvalidOperationException(
                "Refusing to start: " + unmigrated + " profile(s) still carry the pre-collectibles '"
                + LegacyInventoryField + "' field.\n"
                + "Those residents' bottles are invisible to this build, and the first write to "
                + "one of those profiles would delete them permanently.\n"
                + "Stop the bot, then run:  mongosh \"mongodb://localhost:27017\" scripts/backfill-collectibles.js\n"
                + "(it starts in DRY_RUN mode — review the output, then set DRY_RUN = false and re-run).");
        }
    }
}
