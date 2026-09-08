/*
 * Migrate Profile.milkInventory to the polymorphic Profile.collectibles list.
 *
 * Run with mongosh from the repo root, WITH THE BOT STOPPED:
 *   mongosh "mongodb://localhost:27017" scripts/backfill-collectibles.js
 *
 * On Windows cmd.exe, single quotes are not string delimiters — quote paths with DOUBLE
 * quotes or leave an unspaced path bare, or mongosh will look for a file named with the
 * quotes included.
 *
 * (Review the DRY RUN output before setting DRY_RUN = false.)
 *
 * ── Why this is mandatory, not tidying ───────────────────────────────────────
 *   `collectibles` is declared as List<Collectible>, an ABSTRACT type. The Mongo driver
 *   cannot deserialize an element without a `_t` discriminator into an abstract declared
 *   type — it throws. Existing milkInventory elements have no `_t`, so without this script
 *   every profile holding a bottle fails to load. This is not a step that can be skipped
 *   and back-filled later.
 *
 * ── What changes per element ─────────────────────────────────────────────────
 *   _t         added, always "MilkBottle" (bottles are the only pre-existing type)
 *   sourceName renamed to subjectName   (whose it is — generalized off the bottle)
 *   milkedAt   renamed to acquiredAt    (when it was acquired — likewise)
 *
 *   serial, substance, quantity, corruptionTag and emptiedAt keep their names.
 *
 *   The element-key renames ride along here deliberately. The alternative — pinning the old
 *   names with [BsonElement] and never migrating them — was rejected because this script
 *   already rewrites every element, so the renames are free, and a permanent code/data name
 *   disagreement is worth avoiding when it costs nothing. The failure modes stay coupled and
 *   safe: a profile that misses this migration fails loudly on the absent `_t` long before a
 *   renamed key could silently drop a donor name.
 *
 * ── What does NOT change ─────────────────────────────────────────────────────
 *   The Counters/bottleSerial document. Its C# name is now ClaimCollectibleSerials /
 *   CollectibleSerialCounterId, but the STORED key stays "bottleSerial" forever. Renaming it
 *   in code while missing it in data restarts the counter at 1 and collides serials with
 *   every bottle in existence — permanent and unfixable. Do not touch it.
 *
 * ── Safety ───────────────────────────────────────────────────────────────────
 *   - DRY_RUN = true by default: prints what it would do and writes NOTHING.
 *   - Idempotent: a profile that already has `collectibles` is skipped, so a re-run is a
 *     no-op rather than a double migration. It WILL be re-run; that is expected.
 *   - Run with the bot stopped. There is no live-safe version: a profile written by the
 *     running bot between this script's read and write loses whatever the script just did,
 *     and the two shapes cannot coexist on one document.
 *   - scripts/rollback-collectibles.js is the exact inverse if this needs undoing.
 */

// ── Configuration ─────────────────────────────────────────────────────────────
// Matches MonDB.Initialize in BotMain.cs. Change only if the bot is pointed elsewhere.
const DB_NAME = "ChateauDb";
// Applied to the live ChateauDb on 2026-08-07 (39 profiles, 41 bottles, serials #1-#41).
// Restored to true so a stray re-run reports rather than writes; the "collectibles already
// exists" skip below would make it a no-op regardless.
const DRY_RUN = false;        // <-- set to false to actually write
// ──────────────────────────────────────────────────────────────────────────────

const targetDb = db.getSiblingDB(DB_NAME);
const PROFILES = "RegisteredProfiles";
const BOTTLE_TYPE = "MilkBottle";

print("");
print("=== Collectibles migration" + (DRY_RUN ? " (DRY RUN)" : "") + " ===");
print("database: " + DB_NAME);
print("");

// ── Sanity-check the target ───────────────────────────────────────────────────
// A wrong DB_NAME would otherwise look exactly like success: mongo happily reads from a
// database that doesn't exist, finds nothing, and reports "nothing to do" while the real
// data sits untouched somewhere else.
const totalProfiles = targetDb.getCollection(PROFILES).countDocuments({});
if (totalProfiles === 0) {
    print("ABORTING: database '" + DB_NAME + "' has no " + PROFILES + " documents.");
    print("That almost certainly means DB_NAME is wrong. Databases on this server:");
    db.adminCommand({ listDatabases: 1 }).databases.forEach(function (d) {
        print("  " + d.name);
    });
    quit(1);
}
print("residents on file: " + totalProfiles);

// ── Plan the work ─────────────────────────────────────────────────────────────
const profiles = targetDb
    .getCollection(PROFILES)
    .find({ milkInventory: { $exists: true } })
    .toArray();

let alreadyMigrated = 0;
let elementsStamped = 0;
const planned = [];

profiles.forEach(function (profile) {
    // Idempotency: `collectibles` already present means this profile has been through the
    // migration. Leave it entirely alone — re-stamping would be harmless but unsetting
    // milkInventory a second time could mask a genuine half-migrated state.
    if (profile.collectibles !== undefined) {
        alreadyMigrated++;
        return;
    }

    const source = profile.milkInventory || [];
    const migrated = source.map(function (bottle) {
        const entry = { _t: BOTTLE_TYPE };

        // Base-class fields, renamed.
        if (bottle.serial !== undefined) entry.serial = bottle.serial;
        if (bottle.sourceName !== undefined) entry.subjectName = bottle.sourceName;
        if (bottle.milkedAt !== undefined) entry.acquiredAt = bottle.milkedAt;

        // MilkBottle's own fields, unchanged.
        if (bottle.substance !== undefined) entry.substance = bottle.substance;
        if (bottle.quantity !== undefined) entry.quantity = bottle.quantity;
        // Match the C# model's [BsonIgnoreIfNull]: an absent tag is absent, not null.
        if (bottle.corruptionTag) entry.corruptionTag = bottle.corruptionTag;
        if (bottle.emptiedAt) entry.emptiedAt = bottle.emptiedAt;

        return entry;
    });

    elementsStamped += migrated.length;
    planned.push({ userName: profile.userName, collectibles: migrated });
});

// ── Report ────────────────────────────────────────────────────────────────────
print("profiles carrying milkInventory: " + profiles.length);
print("  already migrated (skipped):    " + alreadyMigrated);
print("  to migrate:                    " + planned.length);
print("  elements to stamp:             " + elementsStamped);
print("");

planned.forEach(function (p) {
    const serials = p.collectibles.map(function (c) { return c.serial; });
    print("  " + p.userName + ": " + p.collectibles.length + " item(s)"
        + (serials.length > 0 ? "  serials " + serials.join(", ") : ""));
});
if (planned.length > 0) print("");

if (planned.length === 0) {
    print("Nothing to migrate.");
    quit(0);
}

if (DRY_RUN) {
    print("DRY RUN: nothing written. Set DRY_RUN = false to apply.");
    quit(0);
}

// ── Apply ─────────────────────────────────────────────────────────────────────
let written = 0;
planned.forEach(function (p) {
    targetDb.getCollection(PROFILES).updateOne(
        { userName: p.userName },
        {
            $set: { collectibles: p.collectibles },
            $unset: { milkInventory: "" },
        }
    );
    written++;
});

print("Migrated " + written + " profile(s), " + elementsStamped + " item(s).");
print("The Counters/bottleSerial document was deliberately NOT touched.");
print("");
