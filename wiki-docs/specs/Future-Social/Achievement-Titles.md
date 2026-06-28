# Achievement Titles Beyond Magnitude

**Status:** Planned (specced 2026-06-24). Pre-implementation design. Not from a Feature-Requests B-number — a fresh expansion of the system-title system requested by the owner. All title texts below are **DRAFT wording pending owner approval** (same house rule as the rest of `wiki-docs/specs/` — user-facing strings get a wording pass before they ship; "Chateau day", never "UTC day").

This spec adds achievement *shapes* that are **not** "one number crosses one threshold." It does not touch the existing grind ladders — it layers new check methods alongside them.

---

## Background: the title system today

System titles are granted by [`ChateauSystemTitles`](../../../FChatDicebot/BotCommands/ChateauSystemTitles.cs). Every existing title is one of four single-metric thresholds:

| Shape | Source | Method |
|-------|--------|--------|
| Lifetime count of one interaction give/take | `profile.counts["{verb}give"/"{verb}take"]` | `CheckInteractionCountTitles` |
| Climaxes in one Chateau day | `profile.dailyClimaxCounts[today]` | `CheckDailyClimaxTitles` |
| Single resolved group's size | event-time, by group size | `GetGroupSizeTitles` |
| Lap-stack position in one group | event-time, by stack depth | `GetLapsitPositionTitles` |

The grant aggregator is [`CheckAndGrantCountTitles(userName)`](../../../FChatDicebot/BotCommands/ChateauSystemTitles.cs) — it loads the profile, runs each check (which appends `Title` objects and returns the newly-earned text), saves once, and returns a `GroupTitleGrant` that the 1:1 and group paths both fold into the shared **"═══ Title Time! ═══"** banner (`FormatTitleNotification` / `FormatGroupTitleNotification`). The file already carries stubbed extension points:

```csharp
// newTitles.AddRange(CheckBondTitles(profile));
// newTitles.AddRange(CheckEmploymentTitles(profile));
// newTitles.AddRange(CheckTransformationTitles(profile));
```

This is exactly the seam every axis below plugs into.

### Invariants every new axis inherits for free

- **Once-ever / idempotent.** Dedup is by text: `profile.titles.Any(t => t.IsSystemTitle && t.titleText.Equals(x, OrdinalIgnoreCase))`. A check re-runs on every pass and grants only what's newly true, so combo/state checks can be re-evaluated every time with no double-award.
- **One save, one banner.** New methods append to the same `newTitles` list and ride the existing save + banner. No new notification plumbing.
- **No `.csproj` churn if we extend the existing file.** New check methods added inside `ChateauSystemTitles.cs` compile already. Any *new* `.cs` file must be added to `FChatDicebot.csproj`'s explicit `<Compile Include>` list (the main project does not glob).

### One cross-cutting integration requirement: check the *recipient*

The count ladders mostly fire for the actor who ran the command. Several new axes trigger on the **recipient's** changed state (you become "Hot Mess" because someone *else* cursed you; a take-side combo completes when you're acted upon). The title-check call sites must run `CheckAndGrantCountTitles` for **every party whose profile changed**, recipients included, and fold their grants into the same banner — the group path already does this per participant; the 1:1 directional paths must be audited to confirm the take-side party is checked. **This audit is a Phase-1 prerequisite, not an afterthought.**

---

## The seven axes

Grouped by infrastructure cost, which drives the phasing at the end.

### Axis 1 — Combo / set-completion · `CheckComboTitles(profile)` · zero new data

Reads `profile.counts`, `profile.titles`, `profile.characteristics`, `profile.pregnancies`. The highest-leverage axis: it turns every existing grind title into an *ingredient*.

**1a. Switch titles** — hold give ≥ T **and** take ≥ T of the *same* verb. Table of `(giveKey, takeKey, threshold, title)`:

Switch keys off raw counts (give ≥ T **and** take ≥ T). Wording locked 2026-06-24. `employ` joins this table (its former "Middle Management" paradox folds in here — boss *and* worker).

| Verb | Threshold (each side) | Title |
|------|----------------------|-------|
| spank | 10 | Clapback |
| bully | 10 | Always a Bigger Fish |
| lick | 10 | Saliva Swap |
| mark | 1 | Marked People Mark People |
| breed | 1 | Honorary Seahorse |
| climax | 5 | Cum Together |
| milk | 1 | Who Milks the Milkers? |
| corrupt | 1 | Corruption Spreads |
| dressup | 5 | Fashion Show |
| employ | 1 | Middle Management |

> **Paradox axis (former 1b) — cut.** None of the candidate paradoxes read as genuinely paradoxical; the one keeper ("Middle Management") is really a switch and moved into the table above.

**1b. Category clears** — one title per interaction category for holding ≥ 1 system title in it, plus a capstone for all four. Needs a static `countKey → category` map (Casual / Involved / Commitment / Consequence — the comment headers in `CheckInteractionCountTitles` already group them; lift those into data). Wording locked 2026-06-24.

| Category with ≥1 title | Title |
|------------------------|-------|
| Casual | Casual Connection |
| Involved | Involved and Invested |
| Commitment | Community Commitment |
| Consequence | Consequence Chaser |
| **all four** | **Casually Involved, Consequently Committed** |

### Axis 2 — Meta · `CheckMetaTitles(profile)` · zero new data

The titles themselves are the metric. Reads count of system titles held. Wording locked 2026-06-24.

| System titles held | Title |
|--------------------|-------|
| 10 | And This Makes Eleven |
| 25 | Tons of Titles |
| 50 | Title Titan |
| 100 | Title Tzar |
| 200 | Has Done Just About Everything |
| **all others** | **Completionist** |

**Completionist** is dynamic: granted when the profile holds *every other grantable system title in the catalog* (everything except Completionist itself). This requires a single **enumerable title catalog** — today the title texts are scattered across `CheckInteractionCountTitles`'s inline list, `CheckDailyClimaxTitles`, `GroupSizeTitleTables`, `LapsitBelow/AboveTitles`, and (after this spec) the new axis tables. Completionist needs all of them reachable from one place to diff against `profile.titles`. **Recommendation: introduce a `SystemTitleCatalog` that every check method registers into (or reads its tier table from), so "all known titles" is computable.** This catalog also lets `!titles` show "N of M earned" if ever wanted.

Notes:
- Completionist is evaluated against the live catalog **at check time**; once granted it is permanent. If new titles are added in a later release, a holder keeps Completionist (they completed the catalog of their era) — it does not get revoked.
- Some titles gate on mutually-awkward or very-long-tail conditions, so Completionist is deliberately aspirational. That's intended.

> **Ordering hazard:** granting a meta title raises the count and could bootstrap the next tier off its own grant within one pass. Mitigation: `CheckMetaTitles` **runs last** in the aggregator and snapshots the system-title count (and the earned-set, for Completionist) **before** adding any of its own. Tier spacing makes a same-pass cascade impossible in practice anyway.

### Axis 3 — State / condition · `CheckStateTitles(profile)` · zero new data

Reads *current* transient state, not history.

**3a. "Hot Mess" — total concurrent afflictions (owner-scoped).** The affliction set is the **Consequence** category *except monsterize and rename* (those are identity changes, not "messy" afflictions). The counter sums **each unique instance**, not each type:

- each unique active **parasite** (`ParasiteInstance.LoadAll`)
- each unique active **odor / scent layer**
- each unique active **curse**
- each unique active **break / injury**
- each unique active **addiction** (dose substance)

So someone with 2 parasites, 1 curse, and 1 addiction sits at 4. Wording locked 2026-06-24.

| Total concurrent afflictions | Title |
|------------------------------|-------|
| 3 | Walk It Off |
| 5 | A Lot Going On |
| 7 | In The Mud |
| **every unique status** | **How did we get here?** |

**How did we get here?** is dynamic, the affliction-side mirror of Completionist: granted when the profile concurrently holds *one of literally every status it is possible to hold* (every parasite type, every odor, every curse, every break, every addiction — at the same time). Needs the same kind of enumerable catalog as Completionist, but for **statuses** rather than titles, so "do you have all of them right now?" is computable. Aspirational by design.

**3b. Same-type concurrent.** Separate from the grand total, each affliction *type* gets a "carrying several of THIS at once" title, **threshold 3 concurrent** for all of them (distinct from the lifetime take-count ladders, which never required simultaneity). Wording locked 2026-06-24.

| Affliction type | Concurrent | Title |
|-----------------|-----------|-------|
| Parasites | 3 | Generous Host |
| Curses | 3 | Rule of Three |
| Odors | 3 | Melange |
| Breaks | 3 | I Need Healing |
| Addictions | 3 | A Vice For Every Occasion |

> **Title-text collisions to resolve for "Rule of Three":**
> 1. The existing `entitlegive` threshold-3 title is currently **"Rule Of Three"** (nobody holds it yet) — **rename it to "A, B, C"** to free the text for curses here.
> 2. `CheckDailyClimaxTitles` *also* grants a **"Rule of Three"** (3 climaxes in one Chateau day). Since titles dedup by text, two criteria sharing one text means whoever triggers first wins and the other silently no-ops — confusing. **Decision needed:** rename the daily-climax one, pick a different curse title, or accept the shared text. (Recommend renaming one — they're unrelated achievements.)

**3c. Extremes** — corruption at max → **Past Saving**; any training at 100 → **Mastery: {skill}** (also reachable via Axis 4 collection; grant once, dedup handles overlap).

> State titles fire whenever the condition is true at check time. Because afflictions usually arrive on the *take* side, this axis is the main reason the recipient-check audit (above) is a hard Phase-1 prerequisite — otherwise "Hot Mess" can only ever fire on a pass the victim happens to trigger themselves.

### Axis 4 — Variety / breadth · `CheckVarietyTitles(profile)` · mostly zero new data

Distinct-count, not repetition.

**4a. Distinct verbs performed** — count distinct base verbs with any give-side activity (`counts["{verb}give"] > 0`). Wording locked 2026-06-24.

| Distinct verbs ever performed | Title |
|-------------------------------|-------|
| 5 | Varied |
| 15 | Spice of Life |
| all (~30) | Will Try Anything Once |

**4b. Collection sets** (complete-the-set). Wording locked 2026-06-24.
- Every skill trained ≥ 1 (touched every training at least once) → **Many Talents** (reads `profile.trainings`). Zero new data. *(Renamed from a "Jack of All Trades" candidate, which stays on the existing `employtake`-10 title.)*
- Bred at least one of every monster *category* → **Ditto**. Derivable from `lists["offspring"]` via `ChateauStatisticsSupport.BirthedByMonsterType` / `SiredByMonsterType` cross-referenced against the known category set. Light aggregation, no new data. *(Give vs. take side — sired vs. carried — pinned at build.)*
- Monsterized one of every monster *category* → **For All Monster Kind**. *Needs new data* — monster state is mutable (un-monsterized on restore) and not currently kept as a lifetime set. Add a `lists["monsterizedCategoriesEver"]` appended on monsterize. **Phase 3.** *(Give vs. take pinned at build; mirrors Ditto.)*
- Carried every parasite type **at some point** → **Walking Ecosystem**. *Needs new data* — current parasites are mutable and cleared on `!purge`. Add `lists["parasitesEverCarried"]` appended on infest/spread. **Phase 3.**

### Axis 5 — Relationship depth · `CheckRelationshipTitles(profile, …)` · mixed cost

About a single dyad, not totals.

**5a. Multi-bond to one person** — hold ≥ 3 *different* bond types to the same counterpart. **Zero new data**: bonds are stored as `lists["bond{type}initiated"]` / `["bond{type}received"]` (see [BondTreeSupport](../../../FChatDicebot/BotCommands/Support/BondTreeSupport.cs)); intersect the per-type lists for a shared name. → **Close Ties** (wording locked 2026-06-24). *This one ships in Phase 1.*

**5b. Variety with one partner** — did ≥ N *distinct* interaction types with the same person. Source: `GetInteractionsByInitiator(user)` grouped by recipient, distinct `type` count. → 5 types → **Inseparable**; 10 → **Joined At The Hip**. *Needs an interaction-log scan per check.* **Phase 3.**

**5c. Reciprocity / nemesis** — V done both ways with the same person → **Tit For Tat**; same person bullied/spanked ≥ N times → **Personal Vendetta**. *Log-scan.* **Phase 3.**

### Axis 6 — Streak / cadence · `CheckStreakTitles(profile)` · light new data

**6a. Consecutive active days** — add two small fields: `characteristics["lastActiveDay"]` (`yyyy-MM-dd`) and `counts["activeStreak"]`. On any qualifying activity: same day → no change; yesterday → `activeStreak++`; older/unset → `activeStreak = 1`; then stamp today. Purely activity-driven (a broken streak is simply detected and reset on the next activity — fine, since titles are once-earned).

Wording locked 2026-06-24.

| Consecutive active days | Title |
|-------------------------|-------|
| 3 | Streaking |
| 7 | Busy Week |
| 30 | Doesn't Miss a Day |

**6b. Comeback** — first activity after a ≥ 30-day gap (from `lastActiveDay`) → **Prodigal Return**. Free once 6a's field exists.

**6c. Burst** — N interactions within one rolling hour → **Busy Bee** (5) / **Tireless** (10). *Needs a log-scan or rolling-timestamp list.* **Phase 3.**

### Axis 7 — Narrative arc · `CheckArcTitles(profile)` · light new data (breadcrumbs)

A *sequence* of states. The "before" state may no longer be true when the "after" fires, so current state alone can't prove the journey — stamp a **breadcrumb** when the first half happens, award when the second half completes. Breadcrumbs are **kept** after completion (arcs are permanent, un-farmable — owner decision).

> **Wording deferred to implementation** (owner, 2026-06-24): the candidate titles didn't land at a glance, so the arc *mechanics* are specced but the title text is left open and will be settled when this phase is built.

Arcs to support (mechanics fixed, text TBD):
1. **Redemption** — corruption first reaches a high threshold (set `characteristics["wasDeeplyCorrupted"] = "1"`), then later returns to 0.
2. **Round-trip monster** — monsterized, then restored to baseline.
3. **Pledge turnaround** — pledged → abandoned → later *fulfilled* the same interaction type. (May be readable straight from the `Pledge` collection's status history without a breadcrumb — investigate at build.)
4. **Rags to riches** — currency hit 0 after being positive, then later wealthy.

---

## Phasing

| Phase | Axes | Infra cost |
|-------|------|-----------|
| **1** | 1 (combo: switch + category-clear), 2 (meta + Completionist), 3 (state + "How did we get here?"), 4a (distinct verbs), 4b Polymath+Menagerie, **5a It's Complicated** | Zero new persisted fields. New check methods reading current profile + the recipient-check audit + the `SystemTitleCatalog` / status catalog (needed by Completionist and "How did we get here?"). |
| **2** | 6a/6b (day streak + comeback), 7 (arcs via breadcrumbs) | A few small fields written at event time (`lastActiveDay`, `activeStreak`, breadcrumb characteristics). |
| **3** | 4b Walking Ecosystem, 5b/5c (per-partner depth), 6c (burst) | Interaction-log scans per check and/or new lifetime-set lists. Do these once the cheaper tiers prove popular. |

Phase 1 is the bulk of the value and reuses everything already stored — recommended starting point.

## File scaffolding (Phase 1)

- **Extend** [`ChateauSystemTitles.cs`](../../../FChatDicebot/BotCommands/ChateauSystemTitles.cs): add `CheckComboTitles`, `CheckMetaTitles`, `CheckStateTitles`, `CheckVarietyTitles`, and the bond-only `CheckRelationshipTitles`; wire them into `CheckAndGrantCountTitles` (meta last). Keeping them in this already-compiled file avoids a `.csproj` edit. If split into a new file, add it to `FChatDicebot.csproj`.
- Static data tables (switch pairs, `countKey → category` map + category-clear titles, state-affliction set, 3b per-type tier, streak/meta tiers) as `private static readonly` fields next to the existing `GroupSizeTitleTables`, mirroring its shape so they're the single source of truth.
- **`SystemTitleCatalog`** — a single place that enumerates every grantable system-title text (the existing count/daily/group/lapsit tables + all new axis tables). Required by **Completionist** (diff catalog against `profile.titles`) and reused by the status-catalog needed for **"How did we get here?"**. This is the one genuinely new piece of infra in Phase 1; everything else is check methods over existing data.
- **Title renames to free up colliding text:**
  - `entitlegive` threshold-3: "Rule Of Three" → **"A, B, C"** (frees the text for the 3b curse title).
  - `CheckDailyClimaxTitles` threshold-3: "Rule of Three" → **"C U M"** (the 3b curse title keeps "Rule of Three").
  - `employtake` threshold-10 "Jack Of All Trades": **unchanged** — the 4b every-skill title was named **"Many Talents"** instead, so no rename and no impact on anyone already holding it.
- **Audit + fix** the 1:1 directional title-check call sites so the take-side party is checked (the Axis-3 prerequisite).
- Tests in `FChatDicebot.Tests/Unit/` mirroring `Groupachievementtitletests.cs`: one fixture per axis — a profile crafted to sit just under and just over each boundary, asserting grant/no-grant and once-ever dedup. Meta-ordering test: a profile at 9 system titles earning one more grants "And This Makes Eleven" exactly once. Completionist test: a profile holding the whole catalog-minus-one earns nothing, then earns Completionist when the last one lands.

## Design decisions (owner, 2026-06-24)

- **Switch** keys off raw counts (give ≥ T & take ≥ T); wording locked; `employ → Middle Management` folded in — *resolved*.
- **Paradox axis cut** — none read as paradoxical — *resolved*.
- **Category clears**: one title per category + an all-four capstone; wording locked — *resolved*.
- **Meta**: tiers + dynamic **Completionist** (hold every other title); wording locked — *resolved*.
- **Hot Mess (3a)** affliction set = Consequence minus monsterize/rename, summing each unique parasite/odor/curse/break/addiction; dynamic **"How did we get here?"** (hold one of every status); wording locked — *resolved*.
- **3b same-type concurrent**: threshold **3** for every type; wording locked — *resolved*.
- **Arc breadcrumbs kept** after completion (permanent, un-farmable) — *resolved*.
- **New infra:** Completionist and "How did we get here?" require a `SystemTitleCatalog` + a status catalog (enumerate-all so the "hold everything" diff is computable).

### Still open

1. **Arc (axis 7) wording** — deferred to implementation by owner; mechanics fixed, text TBD.
2. **Give-vs-take side** for the monster-category collection titles (Ditto / For All Monster Kind) — pin at build.

> **Resolved:** Rule-of-Three collision (daily-climax → "C U M"; entitlegive-3 → "A, B, C"; curse-concurrent keeps "Rule of Three"). Jack-of-All-Trades collision (4b skill-collection named "Many Talents"; existing `employtake`-10 unchanged). Optional 3d "Unrecognizable" — dropped. **All Phase-1 wording is now locked.**
