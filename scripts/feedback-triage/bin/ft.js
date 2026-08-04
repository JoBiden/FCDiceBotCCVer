/**
 * Feedback triage ledger — mongosh backend.
 *
 * Not meant to be called directly; go through ../ft.ps1, which sets the env vars
 * and handles payload encoding. Read-only against `Feedback`; the only collection
 * this writes is `FeedbackTriage`.
 *
 *   FT_MODE     new | status | set | seed | info   (required)
 *   FT_PAYLOAD  base64-encoded JSON argument for the mode (optional)
 *
 * Usage:
 *   mongosh "mongodb://localhost:27017/ChateauDb" scripts/feedback-triage/bin/ft.js
 *
 * `Feedback` docs are never modified: FeedbackEntry in Model/ChateauDB.cs lacks
 * [BsonIgnoreExtraElements], so an extra field there would break !feedbacklist
 * deserialization until the bot model changes and redeploys.
 */

const TRIAGE = 'FeedbackTriage';
const FEEDBACK = 'Feedback';

// Statuses. Anything in PRE_DECISION is waiting on the owner; CLOSED is done with.
const PRE_DECISION = ['assessed', 'needs_info'];
const POST_DECISION = ['approved', 'in_progress', 'pr_open', 'deferred'];
const CLOSED = ['shipped', 'declined', 'duplicate', 'obsolete'];
const ALL_STATUSES = PRE_DECISION.concat(POST_DECISION, CLOSED);

function fail(message) {
    print(JSON.stringify({ ok: false, error: message }));
    quit(1);
}

function payload() {
    const raw = process.env.FT_PAYLOAD;
    if (!raw || raw.length === 0) {
        return {};
    }
    try {
        return JSON.parse(Buffer.from(raw, 'base64').toString('utf8'));
    } catch (e) {
        fail('could not decode FT_PAYLOAD: ' + e.message);
    }
}

function emit(value) {
    print(EJSON.stringify(value, null, 2, { relaxed: true }));
}

function hex(doc) {
    return doc._id.toString();
}

/** Every triage row keyed by the feedback _id hex it covers. */
function triageMap() {
    const map = {};
    db.getCollection(TRIAGE).find({}).toArray().forEach(function (row) {
        map[row._id] = row;
    });
    return map;
}

function feedbackMap() {
    const map = {};
    db.getCollection(FEEDBACK).find({}).toArray().forEach(function (doc) {
        map[hex(doc)] = doc;
    });
    return map;
}

function summarize(doc) {
    return {
        feedbackId: hex(doc),
        shortId: hex(doc).substring(0, 8),
        submitterUserName: doc.submitterUserName,
        submitterDisplayName: doc.submitterDisplayName,
        category: doc.category,
        text: doc.text,
        sourceChannel: doc.sourceChannel === undefined ? null : doc.sourceChannel,
        submittedAt: doc.submittedAt
    };
}

/**
 * Feedback entries with no triage row yet — the daily assessment work list,
 * oldest first so a backlog is worked in submission order.
 */
function modeNew() {
    const seen = triageMap();
    const pending = db.getCollection(FEEDBACK)
        .find({})
        .sort({ submittedAt: 1 })
        .toArray()
        .filter(function (doc) { return seen[hex(doc)] === undefined; })
        .map(summarize);

    emit({ ok: true, mode: 'new', count: pending.length, entries: pending });
}

/**
 * Triage rows, newest submission first. `missing: true` means the underlying
 * Feedback doc is gone (the owner deletes resolved submissions by hand), which
 * the playbooks treat as a signal to close out a pre-decision row.
 */
function modeStatus() {
    const args = payload();
    const feedback = feedbackMap();
    let rows = db.getCollection(TRIAGE).find({}).toArray();

    if (args.status) {
        const wanted = Array.isArray(args.status) ? args.status : [args.status];
        rows = rows.filter(function (row) { return wanted.indexOf(row.status) !== -1; });
    }
    if (args.awaitingOwner === true) {
        rows = rows.filter(function (row) { return PRE_DECISION.indexOf(row.status) !== -1; });
    }
    if (args.open === true) {
        rows = rows.filter(function (row) { return CLOSED.indexOf(row.status) === -1; });
    }

    rows.forEach(function (row) {
        row.missing = feedback[row._id] === undefined;
    });
    rows.sort(function (a, b) {
        const at = a.submittedAt ? new Date(a.submittedAt).getTime() : 0;
        const bt = b.submittedAt ? new Date(b.submittedAt).getTime() : 0;
        return bt - at;
    });

    emit({ ok: true, mode: 'status', count: rows.length, rows: rows });
}

/**
 * Upsert one triage row. Payload:
 *   { feedbackId, fields: {...}, event: "assessed", detail: "..." }
 * `fields` is merged over the row; every call appends a history entry so the
 * trail of what the automation did survives even if a dossier file is lost.
 */
function modeSet() {
    const args = payload();
    if (!args.feedbackId) {
        fail('set requires payload.feedbackId');
    }
    const fields = args.fields || {};
    if (fields.status && ALL_STATUSES.indexOf(fields.status) === -1) {
        fail('unknown status "' + fields.status + '" (expected one of: ' + ALL_STATUSES.join(', ') + ')');
    }

    const now = new Date();
    const collection = db.getCollection(TRIAGE);
    const existing = collection.findOne({ _id: args.feedbackId });

    // Snapshot the submission on first write so the row stays readable after the
    // owner deletes the Feedback doc.
    const seed = {};
    if (!existing) {
        const doc = db.getCollection(FEEDBACK).find({}).toArray().filter(function (d) {
            return hex(d) === args.feedbackId;
        })[0];
        if (doc) {
            const summary = summarize(doc);
            seed.submitterUserName = summary.submitterUserName;
            seed.submitterDisplayName = summary.submitterDisplayName;
            seed.category = summary.category;
            seed.textSnapshot = summary.text;
            seed.sourceChannel = summary.sourceChannel;
            seed.submittedAt = summary.submittedAt;
        }
        seed.firstSeenAt = now;
    }

    const update = Object.assign({}, seed, fields, { updatedAt: now });
    const historyEntry = {
        at: now,
        event: args.event || (fields.status ? 'status:' + fields.status : 'update'),
        detail: args.detail === undefined ? null : args.detail
    };

    collection.updateOne(
        { _id: args.feedbackId },
        { $set: update, $push: { history: historyEntry } },
        { upsert: true }
    );

    emit({ ok: true, mode: 'set', row: collection.findOne({ _id: args.feedbackId }) });
}

/**
 * Insert triage rows only where none exists — for pre-marking history the
 * automation should not re-litigate. Never clobbers an existing row.
 * Payload: { rows: [ { feedbackId, fields: {...}, event, detail } ] }
 */
function modeSeed() {
    const args = payload();
    // A single row may arrive as a bare object: Windows PowerShell's ConvertTo-Json
    // unwraps one-element arrays.
    let rows = args.rows === undefined ? [] : args.rows;
    if (!Array.isArray(rows)) {
        rows = [rows];
    }
    if (rows.length === 0) {
        fail('seed requires payload.rows[]');
    }

    const collection = db.getCollection(TRIAGE);
    const inserted = [];
    const skipped = [];

    rows.forEach(function (spec) {
        if (!spec.feedbackId) {
            fail('every seed row needs a feedbackId');
        }
        if (collection.findOne({ _id: spec.feedbackId })) {
            skipped.push(spec.feedbackId);
            return;
        }
        const now = new Date();
        const doc = db.getCollection(FEEDBACK).find({}).toArray().filter(function (d) {
            return hex(d) === spec.feedbackId;
        })[0];
        const row = { _id: spec.feedbackId, firstSeenAt: now, updatedAt: now };
        if (doc) {
            const summary = summarize(doc);
            row.submitterUserName = summary.submitterUserName;
            row.submitterDisplayName = summary.submitterDisplayName;
            row.category = summary.category;
            row.textSnapshot = summary.text;
            row.sourceChannel = summary.sourceChannel;
            row.submittedAt = summary.submittedAt;
        }
        Object.assign(row, spec.fields || {});
        row.history = [{
            at: now,
            event: spec.event || 'seeded',
            detail: spec.detail === undefined ? null : spec.detail
        }];
        collection.insertOne(row);
        inserted.push(spec.feedbackId);
    });

    emit({ ok: true, mode: 'seed', inserted: inserted, skipped: skipped });
}

/** Counts by status, plus how many submissions have never been assessed. */
function modeInfo() {
    const rows = db.getCollection(TRIAGE).find({}).toArray();
    const byStatus = {};
    rows.forEach(function (row) {
        const key = row.status || '(none)';
        byStatus[key] = (byStatus[key] || 0) + 1;
    });
    const seen = triageMap();
    const untriaged = db.getCollection(FEEDBACK).find({}).toArray()
        .filter(function (doc) { return seen[hex(doc)] === undefined; }).length;

    emit({
        ok: true,
        mode: 'info',
        database: db.getName(),
        feedbackTotal: db.getCollection(FEEDBACK).countDocuments({}),
        triageRows: rows.length,
        untriaged: untriaged,
        awaitingOwner: rows.filter(function (r) { return PRE_DECISION.indexOf(r.status) !== -1; }).length,
        byStatus: byStatus
    });
}

const mode = process.env.FT_MODE;
switch (mode) {
    case 'new': modeNew(); break;
    case 'status': modeStatus(); break;
    case 'set': modeSet(); break;
    case 'seed': modeSeed(); break;
    case 'info': modeInfo(); break;
    default: fail('set FT_MODE to one of: new, status, set, seed, info (got "' + mode + '")');
}
