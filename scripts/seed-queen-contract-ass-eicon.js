/*
 * Re-seat the Queen Contract "qcass" spank easter egg on the bodypart-eicon system.
 *
 * SpankProcessor used to hardcode "[eicon]qcass[/eicon]" whenever Queen Contract was the
 * one being spanked. Personal bodypart eicons (!seteicon ass) express exactly that, so
 * both hardcodes were removed — but the icon only keeps showing once her profile carries
 * the eicon itself. Running "!seteicon ass [eicon]qcass[/eicon]" as Queen Contract does
 * the same thing; this script is for when logging in as her isn't convenient.
 *
 * Run with mongosh, e.g.:
 *   mongosh "mongodb://USER:PASS@HOST:PORT" scripts/seed-queen-contract-ass-eicon.js
 * (edit DB_NAME below first.)
 *
 * Idempotent: re-running it just re-sets the same value.
 */

const DB_NAME = "ChateauDb";
const USER_NAME = "Queen Contract";
const EICON = "[eicon]qcass[/eicon]";

const profiles = db.getSiblingDB(DB_NAME).getCollection("RegisteredProfiles");

const result = profiles.updateOne(
    { userName: USER_NAME },
    { $set: { "characteristics.eicon_part_ass": EICON } }
);

if (result.matchedCount === 0) {
    print(`No profile found for "${USER_NAME}" in ${DB_NAME}.RegisteredProfiles — nothing changed.`);
} else {
    print(`Set characteristics.eicon_part_ass = "${EICON}" on ${USER_NAME}.`);
}
