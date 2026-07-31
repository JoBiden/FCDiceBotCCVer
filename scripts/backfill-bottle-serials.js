/*
 * Backfill bottle serial numbers onto every existing MilkBottle, and split the pre-serial
 * aggregated entries into one entry per physical bottle.
 *
 * Run with mongosh from the repo root:
 *   mongosh "mongodb://localhost:27017" scripts/backfill-bottle-serials.js
 *
 * On Windows cmd.exe, single quotes are not string delimiters — quote paths with DOUBLE
 * quotes or leave an unspaced path bare, or mongosh will look for a file named with the
 * quotes included.
 *
 * (Review the DRY RUN output before setting DRY_RUN = false.)
 *
 * ── Why this is needed ────────────────────────────────────────────────────────
 *   A serial identifies ONE bottle, so an entry with quantity > 1 has nothing to name.
 *   Before serials, a milking that rolled 3 wrote a single entry with quantity: 3. This
 *   splits those into three entries and numbers each one.
 *
 *   Bottles left un-numbered would render as "#?" and be unreachable by "!drink #142"
 *   and "!pay ... bottle #142", so this MUST run before or with the deploy that ships
 *   those commands.
 *
 * ── Assignment order ─────────────────────────────────────────────────────────
 *   All bottles across ALL profiles are sorted by milkedAt ascending and numbered from 1.
 *   Chronological across the whole Chateau is deliberate: it makes a low serial genuinely
 *   mean "one of the oldest bottles in the building", which is the property that makes
 *   the number worth showing to players at all.
 *
 *   Ties on milkedAt break by userName then substance, purely so a re-run is deterministic.
 *
 * ── emptiedAt ────────────────────────────────────────────────────────────────
 *   Left unset on everything. Bottles drunk or sold before this ships weren't recorded,
 *   and inventing empties for them would be fiction.
 *
 * ── Safety ───────────────────────────────────────────────────────────────────
 *   - DRY_RUN = true by default: prints what it would do and writes NOTHING.
 *   - Refuses to run if any bottle already carries a serial, because a second run would
 *     renumber bottles residents have already seen and referred to. Delete the counter
 *     and clear the serials first if you genuinely need to redo it.
 *   - Sets the Counters/bottleSerial document to the highest number assigned, so live
 *     milkings continue from there rather than colliding with backfilled numbers.
 */

// ── Configuration ─────────────────────────────────────────────────────────────
// Matches MonDB.Initialize in BotMain.cs. Change only if the bot is pointed elsewhere.
const DB_NAME = "ChateauDb";
// Applied to the live ChateauDb on 2026-07-29 (35 bottles, serials #1-#35). Left at true so
// a stray re-run reports rather than writes; the already-numbered guard below would stop it
// regardless.
const DRY_RUN = true;        // <-- set to false to actually write
// ──────────────────────────────────────────────────────────────────────────────

const targetDb = db.getSiblingDB(DB_NAME);
const PROFILES = "RegisteredProfiles";
const COUNTERS = "Counters";
const COUNTER_ID = "bottleSerial";

print("");
print("=== Bottle serial backfill" + (DRY_RUN ? " (DRY RUN)" : "") + " ===");
print("database: " + DB_NAME);
print("");

// ── Sanity-check the target ───────────────────────────────────────────────────
// A wrong DB_NAME would otherwise look exactly like success: mongo happily reads from a
// database that doesn't exist, finds no bottles, and reports "nothing to do" while the
// real data sits untouched somewhere else.
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

// ── Collect every bottle in the Chateau ───────────────────────────────────────
const profiles = targetDb
    .getCollection(PROFILES)
    .find({ milkInventory: { $exists: true, $ne: [] } })
    .toArray();

let alreadyNumbered = 0;
const flattened = [];

profiles.forEach(function (profile) {
    (profile.milkInventory || []).forEach(function (bottle, index) {
        if (bottle.serial && bottle.serial > 0) {
            alreadyNumbered++;
        }
        // An entry of quantity N becomes N bottles. Anything missing or below 1 counts
        // as a single bottle rather than vanishing.
        const count = bottle.quantity && bottle.quantity > 1 ? bottle.quantity : 1;
        for (let i = 0; i < count; i++) {
            flattened.push({
                userName: profile.userName,
                entryIndex: index,
                milkedAt: bottle.milkedAt,
                substance: bottle.substance,
                sourceName: bottle.sourceName,
                corruptionTag: bottle.corruptionTag,
            });
        }
    });
});

if (alreadyNumbered > 0) {
    print("ABORTING: " + alreadyNumbered + " bottle(s) already carry a serial.");
    print("Re-running would renumber bottles residents have already seen and referred to.");
    print("If you really mean to redo this, clear the serials and the Counters/" + COUNTER_ID + " document first.");
    quit(1);
}

if (flattened.length === 0) {
    print("No bottles found. Nothing to do.");
    print("Seeding the counter at 0 so live milkings start from #1.");
    if (!DRY_RUN) {
        targetDb.getCollection(COUNTERS).updateOne(
            { _id: COUNTER_ID },
            { $set: { value: 0 } },
            { upsert: true }
        );
    }
    quit(0);
}

// ── Number them oldest first, Chateau-wide ────────────────────────────────────
flattened.sort(function (a, b) {
    const aTime = a.milkedAt ? a.milkedAt.getTime() : 0;
    const bTime = b.milkedAt ? b.milkedAt.getTime() : 0;
    if (aTime !== bTime) return aTime - bTime;
    if (a.userName !== b.userName) return a.userName < b.userName ? -1 : 1;
    if (a.substance !== b.substance) return (a.substance || "") < (b.substance || "") ? -1 : 1;
    return a.entryIndex - b.entryIndex;
});

const rebuilt = {};
flattened.forEach(function (bottle, i) {
    if (!rebuilt[bottle.userName]) rebuilt[bottle.userName] = [];
    const entry = {
        serial: i + 1,
        substance: bottle.substance,
        sourceName: bottle.sourceName,
        milkedAt: bottle.milkedAt,
        quantity: 1,
    };
    // Match the C# model's [BsonIgnoreIfNull]: an absent tag is absent, not null.
    if (bottle.corruptionTag) entry.corruptionTag = bottle.corruptionTag;
    rebuilt[bottle.userName].push(entry);
});

const highestSerial = flattened.length;

// ── Report ────────────────────────────────────────────────────────────────────
const owners = Object.keys(rebuilt).sort();
print("residents with bottles: " + owners.length);
print("bottles after splitting: " + highestSerial);
print("");
owners.forEach(function (userName) {
    const bottles = rebuilt[userName];
    const first = bottles[0].serial;
    const last = bottles[bottles.length - 1].serial;
    print("  " + userName + ": " + bottles.length + " bottle(s), serials #" + first + " ... #" + last);
});
print("");

if (DRY_RUN) {
    print("DRY RUN: nothing written. Set DRY_RUN = false to apply.");
    quit(0);
}

// ── Apply ─────────────────────────────────────────────────────────────────────
let written = 0;
owners.forEach(function (userName) {
    targetDb.getCollection(PROFILES).updateOne(
        { userName: userName },
        { $set: { milkInventory: rebuilt[userName] } }
    );
    written++;
});

targetDb.getCollection(COUNTERS).updateOne(
    { _id: COUNTER_ID },
    { $set: { value: highestSerial } },
    { upsert: true }
);

print("Wrote " + written + " profile(s).");
print("Counter " + COUNTER_ID + " set to " + highestSerial + "; the next bottle milked will be #" + (highestSerial + 1) + ".");
print("");
