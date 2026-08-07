/*
 * Inverse of scripts/backfill-collectibles.js: Profile.collectibles back to
 * Profile.milkInventory, undoing the _t stamp and both element-key renames.
 *
 * Run with mongosh from the repo root, WITH THE BOT STOPPED:
 *   mongosh "mongodb://localhost:27017" scripts/rollback-collectibles.js
 *
 * (Review the DRY RUN output before setting DRY_RUN = false.)
 *
 * ── Why this exists ──────────────────────────────────────────────────────────
 *   The forward migration renames element keys, so an unwritten rollback would leave the
 *   only way back as hand-editing live documents under pressure. It is written alongside
 *   the migration rather than at the moment it is needed.
 *
 * ── THE GUARD THAT MATTERS ───────────────────────────────────────────────────
 *   milkInventory is a List<MilkBottle>. It cannot hold anything else. So the moment a
 *   resident owns a collectible that is NOT a bottle, rolling back would silently delete it.
 *
 *   This script therefore ABORTS if it finds any element whose _t is not "MilkBottle",
 *   and reports who holds what. Rolling back past the first non-bottle type is not a
 *   supported operation — it is data loss wearing a recovery script's clothes. If you truly
 *   need to, remove those items deliberately and separately first, so the deletion is a
 *   decision someone made rather than a side effect.
 *
 * ── Also not touched ─────────────────────────────────────────────────────────
 *   The Counters/bottleSerial document, same as the forward migration. Serials already
 *   issued stay issued; rolling the counter back would re-issue numbers residents have
 *   already seen.
 */

// ── Configuration ─────────────────────────────────────────────────────────────
const DB_NAME = "ChateauDb";
const DRY_RUN = true;        // <-- set to false to actually write
// ──────────────────────────────────────────────────────────────────────────────

const targetDb = db.getSiblingDB(DB_NAME);
const PROFILES = "RegisteredProfiles";
const BOTTLE_TYPE = "MilkBottle";

print("");
print("=== Collectibles ROLLBACK" + (DRY_RUN ? " (DRY RUN)" : "") + " ===");
print("database: " + DB_NAME);
print("");

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

const profiles = targetDb
    .getCollection(PROFILES)
    .find({ collectibles: { $exists: true } })
    .toArray();

// ── Refuse to destroy non-bottle collectibles ─────────────────────────────────
const blockers = [];
profiles.forEach(function (profile) {
    (profile.collectibles || []).forEach(function (item) {
        if (item._t !== BOTTLE_TYPE) {
            blockers.push({
                userName: profile.userName,
                type: item._t || "(no _t)",
                serial: item.serial,
            });
        }
    });
});

if (blockers.length > 0) {
    print("");
    print("ABORTING: " + blockers.length + " collectible(s) are not bottles.");
    print("milkInventory cannot hold them, so rolling back would delete them outright:");
    print("");
    blockers.forEach(function (b) {
        print("  " + b.userName + ": " + b.type + " #" + b.serial);
    });
    print("");
    print("Remove or migrate these deliberately first if a rollback is genuinely intended.");
    quit(1);
}

let alreadyRolledBack = 0;
let elementsRestored = 0;
const planned = [];

profiles.forEach(function (profile) {
    // Mirror of the forward script's idempotency check.
    if (profile.milkInventory !== undefined) {
        alreadyRolledBack++;
        return;
    }

    const restored = (profile.collectibles || []).map(function (item) {
        const bottle = {};

        if (item.serial !== undefined) bottle.serial = item.serial;
        if (item.substance !== undefined) bottle.substance = item.substance;
        if (item.subjectName !== undefined) bottle.sourceName = item.subjectName;
        if (item.acquiredAt !== undefined) bottle.milkedAt = item.acquiredAt;
        if (item.quantity !== undefined) bottle.quantity = item.quantity;
        if (item.corruptionTag) bottle.corruptionTag = item.corruptionTag;
        if (item.emptiedAt) bottle.emptiedAt = item.emptiedAt;

        return bottle;
    });

    elementsRestored += restored.length;
    planned.push({ userName: profile.userName, milkInventory: restored });
});

print("profiles carrying collectibles: " + profiles.length);
print("  already rolled back (skipped): " + alreadyRolledBack);
print("  to roll back:                  " + planned.length);
print("  elements to restore:           " + elementsRestored);
print("");

planned.forEach(function (p) {
    const serials = p.milkInventory.map(function (b) { return b.serial; });
    print("  " + p.userName + ": " + p.milkInventory.length + " bottle(s)"
        + (serials.length > 0 ? "  serials " + serials.join(", ") : ""));
});
if (planned.length > 0) print("");

if (planned.length === 0) {
    print("Nothing to roll back.");
    quit(0);
}

if (DRY_RUN) {
    print("DRY RUN: nothing written. Set DRY_RUN = false to apply.");
    quit(0);
}

let written = 0;
planned.forEach(function (p) {
    targetDb.getCollection(PROFILES).updateOne(
        { userName: p.userName },
        {
            $set: { milkInventory: p.milkInventory },
            $unset: { collectibles: "" },
        }
    );
    written++;
});

print("Rolled back " + written + " profile(s), " + elementsRestored + " bottle(s).");
print("The Counters/bottleSerial document was deliberately NOT touched.");
print("");
