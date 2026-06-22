/*
 * Backfill Profile.employeeEarnings (B6 "employer earnings") with an ESTIMATE of the
 * historic MANOR kickbacks employers would have earned from their employees' !work,
 * for duties completed BEFORE the kickback hook shipped.
 *
 * Run with mongosh, e.g.:
 *   mongosh "mongodb://USER:PASS@HOST:PORT" scripts/backfill-employee-earnings.js
 * (edit DB_NAME / DRY_RUN below first; review the DRY RUN output before applying.)
 *
 * ── Owner-provided assumptions ────────────────────────────────────────────────
 *   1. No one has changed jobs.        -> a resident only ever worked their CURRENT
 *                                         characteristics.job.
 *   2. All job experience in the        -> workCount = jobExperience[currentJob].
 *      current job was while employed.    (Volunteering can't target your current job,
 *                                         so this counter is pure !work completions.)
 *   3. All currency rolls were ~average -> per roll, the amount is the midpoint of the
 *                                         reward's [min, max] range.
 *
 * ── One extra modelling choice (no data exists to do better) ──────────────────
 *   Within a duty, the player's result choice is unknown and result gating
 *   (job-experience / training / monster / currency conditionals) can't be replayed.
 *   RESULT_STRATEGY decides how to collapse a duty's result choices into an estimate:
 *     "average"  (default) every result equally likely; gating ignored.
 *     "baseline"           only the no-condition result(s) — the safe/low estimate.
 *     "best"               the single highest-expected-reward result — the high estimate.
 *
 * The employer cut mirrors ChateauWork.EmployerCut exactly:
 *   25% of the rolled amount, floored, minimum 1 when the amount is positive.
 *
 * ── Safety ────────────────────────────────────────────────────────────────────
 *   - DRY_RUN = true by default: prints a per-employer summary and writes NOTHING.
 *   - Idempotent: it recomputes from the stable jobExperience counter and $set's the
 *     whole employeeEarnings map, so re-running produces the same result. Run this
 *     ONCE at deploy time, BEFORE the live kickback hook starts accruing real
 *     earnings — a run after go-live would overwrite real accruals with the estimate.
 */

// ── Configuration ─────────────────────────────────────────────────────────────
const DB_NAME = "chateau";          // <-- set to the live database name
const DRY_RUN = true;               // <-- set to false to actually write
const RESULT_STRATEGY = "average";  // "average" | "baseline" | "best"
// ──────────────────────────────────────────────────────────────────────────────

const targetDb = db.getSiblingDB(DB_NAME);
const PROFILES = "RegisteredProfiles";
const DUTIES = "Duties";

// Mirror of ChateauWork.EmployerCut.
function employerCut(amount) {
    return amount > 0 ? Math.max(1, Math.floor(amount * 0.25)) : 0;
}

// Expected reward amount of a single result, weighting each currency by its weight
// and taking the midpoint of each [min, max] range.
function resultExpectedAmount(result) {
    const rl = result.rewardList || {};
    let totalWeight = 0;
    Object.keys(rl).forEach(function (k) { totalWeight += (rl[k].weight || 0); });
    if (totalWeight <= 0) return 0;
    let ev = 0;
    Object.keys(rl).forEach(function (k) {
        const rw = rl[k];
        ev += (rw.weight / totalWeight) * (((rw.min || 0) + (rw.max || 0)) / 2);
    });
    return ev;
}

// The set of results a worker is assumed to "choose" from, per RESULT_STRATEGY.
function chosenResults(duty) {
    const dr = duty.dutyResults || {};
    const all = Object.keys(dr).map(function (k) { return dr[k]; });
    if (!all.length) return [];

    if (RESULT_STRATEGY === "baseline") {
        const base = all.filter(function (r) {
            return r.conditional && String(r.conditional.type).substr(0, 3) === "non";
        });
        return base.length ? base : all;
    }
    if (RESULT_STRATEGY === "best") {
        let best = null, bestVal = -1;
        all.forEach(function (r) {
            const ev = resultExpectedAmount(r);
            if (ev > bestVal) { bestVal = ev; best = r; }
        });
        return best ? [best] : [];
    }
    return all; // "average"
}

// 1) Load duties grouped by job.
const dutiesByJob = {};
targetDb.getCollection(DUTIES).find({}).forEach(function (d) {
    if (!dutiesByJob[d.job]) dutiesByJob[d.job] = [];
    dutiesByJob[d.job].push(d);
});

// 2) Build, per job, a per-currency model: { currency -> { prob, cut } } where
//    prob = average per-work probability of rolling that currency, and
//    cut  = employer cut for that currency at its average rolled amount.
function buildJobModel(duties) {
    const probAccum = {}; // currency -> summed probability mass across duties
    const amtAccum = {};  // currency -> summed (prob * avgAmount) across duties
    let dutyCount = 0;

    duties.forEach(function (duty) {
        const results = chosenResults(duty);
        if (!results.length) return;
        dutyCount++;
        const perResult = 1 / results.length; // each chosen result equally likely
        results.forEach(function (r) {
            const rl = r.rewardList || {};
            let totalWeight = 0;
            Object.keys(rl).forEach(function (k) { totalWeight += (rl[k].weight || 0); });
            if (totalWeight <= 0) return;
            Object.keys(rl).forEach(function (k) {
                const rw = rl[k];
                const cur = rw.currency;
                const pCur = perResult * (rw.weight / totalWeight);
                const avgAmt = ((rw.min || 0) + (rw.max || 0)) / 2;
                probAccum[cur] = (probAccum[cur] || 0) + pCur;
                amtAccum[cur] = (amtAccum[cur] || 0) + pCur * avgAmt;
            });
        });
    });

    const model = {};
    if (dutyCount === 0) return model;
    Object.keys(probAccum).forEach(function (cur) {
        const prob = probAccum[cur] / dutyCount;       // proper per-work distribution
        const avgAmt = amtAccum[cur] / probAccum[cur]; // E[amount | this currency]
        model[cur] = { prob: prob, cut: employerCut(avgAmt) };
    });
    return model;
}

// Job model with the same "default" fallback ChateauWork uses for jobs with no duties.
const modelCache = {};
function getJobModel(job) {
    if (modelCache[job]) return modelCache[job];
    let duties = dutiesByJob[job];
    if (!duties || !duties.length) duties = dutiesByJob["default"] || [];
    const m = buildJobModel(duties);
    modelCache[job] = m;
    return m;
}

// 3) Walk every profile and accumulate each employer's estimated earnings.
//    earningsByEmployer: employerUserName -> employeeUserName -> currency -> amount
const earningsByEmployer = {};
const profiles = targetDb.getCollection(PROFILES);

profiles.find({}).forEach(function (p) {
    const ch = p.characteristics || {};
    const worker = p.userName;
    const employer = ch["employer"];
    const job = ch["job"];
    if (!employer || employer === worker || !job) return; // unemployed or self-employed

    const workCount = (p.jobExperience || {})[job] || 0;
    if (workCount <= 0) return;

    const model = getJobModel(job);
    Object.keys(model).forEach(function (cur) {
        const m = model[cur];
        if (m.cut <= 0) return;
        const total = Math.round(workCount * m.prob * m.cut);
        if (total <= 0) return;
        if (!earningsByEmployer[employer]) earningsByEmployer[employer] = {};
        if (!earningsByEmployer[employer][worker]) earningsByEmployer[employer][worker] = {};
        earningsByEmployer[employer][worker][cur] =
            (earningsByEmployer[employer][worker][cur] || 0) + total;
    });
});

// 4) Summarize and (optionally) write.
print("Employer-earnings backfill — strategy=" + RESULT_STRATEGY + (DRY_RUN ? " [DRY RUN]" : " [APPLYING]"));
print("");

let employerCount = 0, writeCount = 0, skipped = 0;
Object.keys(earningsByEmployer).sort().forEach(function (employer) {
    if (profiles.countDocuments({ userName: employer }) === 0) {
        print("  [skip] employer not registered: " + employer);
        skipped++;
        return;
    }
    employerCount++;
    const ledger = earningsByEmployer[employer];
    print("Employer " + employer + ":");
    Object.keys(ledger).sort().forEach(function (emp) {
        const chips = Object.keys(ledger[emp]).sort().map(function (c) {
            return ledger[emp][c] + " " + c;
        }).join(" | ");
        print("    " + emp + ": " + chips);
    });

    if (!DRY_RUN) {
        profiles.updateOne({ userName: employer }, { $set: { employeeEarnings: ledger } });
        writeCount++;
    }
});

print("");
print("Employers with estimated earnings: " + employerCount +
    (skipped ? (" (" + skipped + " skipped — employer profile missing)") : ""));
if (DRY_RUN) {
    print("DRY RUN — nothing was written. Set DRY_RUN = false to apply.");
} else {
    print("Wrote employeeEarnings to " + writeCount + " employer profile(s).");
}
