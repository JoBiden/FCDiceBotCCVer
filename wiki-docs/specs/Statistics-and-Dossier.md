# Statistics + Dossier Expansion

A reporting sweep that adds chateau-wide statistics readouts and extends `!dossier` to cover newer interactions. **Not an interaction** — no processor, no consent flow, no cooldowns. Read-only commands plus dossier rendering changes.

**Status:** Implemented (2026-05-27).
**Investment level:** N/A (information commands).
**Reversal:** N/A.
**Depends on:** existing interaction log (`MonDB.getInteractionsByType`), profile state (`Profile.characteristics`, `Profile.lists`, `Profile.counts`, `Profile.titles`, currency totals), `MonsterStats` aggregate. Adds `IChateauDatabase.GetAllProfiles()` and `GetAllMonsterStats()`.

## Design tenets

- **Celebrate group efforts, not individuals.** Lifetime totals and snapshots that belong to "the Chateau", not user-named leaderboards. "Goblins are the most-bred monster" — never "Alice bred the most goblins."
- **Personal stats on `!dossier` are fine, but no cross-user superlatives.** "Parent to 11 goblins" — never "has parented more goblins than any other resident."
- **Lifetime totals never decrement.** Reversal commands (`!purge`, `!cleanse`, `!rest`, `!detox`, `!wash`, `!birth`) get their own *gross* counters so the richer story shows. Snapshots reflect current state.
- **Ties show all tied entries.** "Most bred monster: goblin, ogre (47 each)".
- **Historical interactions count.** Backfilled — `MonDB` is the source of truth.
- **All-time only.** No daily/weekly windows in v1.

## `!statistics`

Top-level overview, single message. Zero-count rows are suppressed (only the section headers stay), so a quiet Chateau emits a short readout rather than a wall of zeroes.

```
A record of life within the Chateau, in broad strokes~
[b]Population[/b]
  Converted to Monsterkind: 8 (most common: goblin)
  Monsters birthed: 1,204 (most bred: goblin)
  People planted: 89 (most planted: oak)
  Statues petrified: 91 (most decorated: entrance hall)
  Infested Individuals: 12 (most widespread: tentacles)
  Parasites spread: 247   Parasites purged: 198
[b]Influence[/b]
  Climaxes recorded: 387
  Corruption Cultivated: 1,204    Purity Promoted: 387
  Corruption Conquers {Purity Prevails} by 817
[b]Workforce[/b]
  Total employees: 22
  Most employed: 3 maids, 2 cooks, 1 blacksmith
  Duties completed: 155
  Most earned currencies - Gold: 14,837    Silvercoin: 2,203    Lustessence: 442
[sub]Further information can be found through !statues, !populations,
!flora, !birthrates, !parasites, !payroll, !economics.[/sub]
```

### Rendering rules

| Line | Source | Notes |
|------|--------|-------|
| Converted to Monsterkind | Count profiles where `characteristics["monster"]` is set. "Most common" = top species. | Snapshot. |
| Monsters birthed | Count of `birth` interactions (or whatever interaction type records each offspring). "Most bred" = top monster type on those interactions. | Lifetime. Note that `!consume` / NPC death do not decrement. |
| People planted | Count of `plant` interactions. "Most planted" = top plant identifier. | Lifetime. |
| Statues petrified | Count of `petrify` interactions. "Most decorated" = top `interaction.identifier` (location). | Lifetime — distinct from `!statues` which is the snapshot of currently-petrified. |
| Infested Individuals | Count distinct profiles with non-empty `lists["parasites"]`. "Most widespread" = parasite present on the most distinct hosts. | Snapshot. |
| Parasites spread | Count of `infest` interactions + count of `SpreadFromContact == true` parasite instances ever recorded. | Lifetime. |
| Parasites purged | Count of `purge` events (see assumption A1 — purge currently has no interaction log). | Lifetime. |
| Climaxes recorded | Count of `climax` + `climaxfor` interactions (verify exact types). | Lifetime. |
| Corruption Cultivated / Purity Promoted | Gross vice-delta volumes from `corrupt` / `purify` interactions, not interaction counts. | Lifetime. |
| Corruption Conquers / Purity Prevails | Difference of the two volumes; phrasing switches based on which is larger. Ties: `"Corruption and Purity hold even at {N}"`. | Computed. |
| Total employees | Count of profiles with `characteristics["job"]` set. | Snapshot. |
| Most employed | Group employed profiles by job, render top 3 as `"N {job}s"`. | Snapshot. Pluralize via `Utils.JobToText` + simple `s` suffix (revisit if any job has irregular plural). |
| Duties completed | Count of `work` interactions (Chateau-wide). | Lifetime. See assumption A2. |
| Most earned currencies | Sum each currency across all profiles' wallets; render top 3. | All-time totals (currencies don't currently un-earn). |

### Tie behavior

When computing "most X" superlatives and the top is tied, list all tied entries: `"most common: goblin, ogre"`. The count next to the line stays as the single grand total.

## Drill-down commands

Each is a separate read-only command, modeled on `!statues`. All output via private DM.

### `!populations`

Current monsterized residents, grouped by species.

```
Currently dwelling in the Chateau as monsters:
  Goblin: 3 — Alice, Bob, Charlie
  Ogre: 2 — Diana, Eve
  Succubus: 1 — Faye
```

Source: scan all profiles, group by `characteristics["monster"]`.

### `!flora`

Lifetime plant cultivations, grouped by plant type.

```
Plants ever cultivated in the Chateau gardens:
  Oak: 23
  Rose: 18
  Mandrake: 12
  ...
```

Source: count `plant` interactions, group by plant identifier.

Per-planter detail (`!flora alice`) is **out of scope** — per-user plant history lives on the dossier instead.

### `!birthrates`

Lifetime births, per monster species, split by sired vs birthed.

```
Monsters born to the Chateau:
  Goblin: 47 (23 sired, 24 birthed)
  Ogre: 31 (16 sired, 15 birthed)
  ...
```

Source: count `birth` interactions, group by offspring monster type, split based on whether the role was sire or mother. Per-sire / per-mother lives on the dossier, not in this drill-down.

### `!parasites`

Per-parasite snapshot + lifetime spread + lifetime purged.

```
Parasites recorded in the Chateau:
  Tentacles: 5 currently infested, 73 ever spread, 68 purged
  Paraslime: 4 currently infested, 51 ever spread, 47 purged
  Lust leeches: 2 currently infested, 33 ever spread, 31 purged
  ...
```

Source: scan profiles' `lists["parasites"]` for snapshot; count `infest` interactions for "ever spread" (split if needed: direct + spread); count purge events (assumption A1).

Use `ParasiteText.ParasiteName` for rendering each parasite name (per existing memory on rendering helpers).

### `!payroll`

Per-job current workforce + per-job duties completed.

```
The Chateau's current workforce:
  Maid: 3 — currently employed
  Cook: 2 — currently employed
  Blacksmith: 1 — currently employed

Duties completed by job:
  Maid: 84
  Cook: 47
  Blacksmith: 24
```

Source: scan profile `characteristics["job"]` for current; count `work` interactions grouped by the worker's job at time of interaction (assumption A2). Per-employer breakdown is **not** included (would tilt toward leaderboard).

### `!economics`

Full per-currency totals across all residents.

```
The Chateau's wealth, by currency:
  Gold: 14,837
  Silvercoin: 2,203
  Lustessence: 442
  ...
```

Source: sum each currency across all profile wallets. List all 69+ currencies, sorted by total descending. No filter — economics is the drill-down so it shows everything.

## `!dossier` additions

The existing dossier structure (header → job → casual counts → marks → bonds → job experience → last reported → last seen) is preserved. New blocks slot in at the noted positions.

### New active-state blocks (insert near top, after the job line)

Each block is its own line, similar to the existing **Marks** block. **Do not touch the existing marks display or wording.**

- **Active Curses** — render `Profile.lists["curses"]` (or wherever curse instances live; verify during implementation). One line per active curse with curse name and source.
- **Active Parasites** — render `Profile.lists["parasites"]` via `ParasiteText.ParasiteName`. One line per parasite with infester name and (if spread) grace status.
- **Active Breaks** — render the broken bodyparts list, with days remaining if available.
- **Active Odorizes** — render `Profile.lists["scents"]` (or equivalent scent-layer list). Use `ScentText` per existing convention.

Empty blocks render nothing (no "Active Curses: none" lines).

### Casual / Involved counts — split give vs take

Current dossier already maintains separate `give` and `take` keys for many interactions in `CasualCountSpecialistText` / `InvolvedSpecialistText` / etc. The **count display row** (currently just "Casual interactions:" with kisses/cuddles/etc.) should be extended to show give/take split for every tracked interaction with a known display name, using the existing `CountDisplayNames` mapping where it exists and extending it where it doesn't.

User-confirmed mappings to extend:
- `climaxtake` → **"Orgasms"** (new)
- All other give/take labels reuse existing `CountDisplayNames` entries; where a paired interaction has no `give`/`take` entry in `CountDisplayNames`, propose wording during implementation and surface for user review.

Section header: keep as **"Casual interactions:"** if all entries are casual; if non-casual counts are included too, retitle to **"Counts"** or split into "Casual interactions:" + "Involvements:" rows. Implementer's call, with wording reviewed.

### New per-resident blocks (insert before "Last reported")

- **Sired** — mirrors the **Bonds** layout. Per-monster line.
  ```
  Sired:
  Goblins: 3
  Ogres: 2
  ```
- **Birthed** — same layout.
  ```
  Birthed:
  Goblins: 5
  ```
- **Has personally planted** — per-plant line.
  ```
  Has personally planted:
  Oaks: 3
  Roses: 1
  ```
- **Currently employs** — per-job line, only renders if the user is an employer.
  ```
  Currently employs:
  Maids: 2
  Artists: 1
  ```
- **Titles earned: N** — total = `Profile.titles.Count` summed; counts both user-bestowed (via `!entitle`) AND system-conferred (where `givenBy == "Chateau"`). One line.
- **Most abundant currency: N {currency}** — find the currency the resident has the most of by raw count. One line. Wording: `"Most abundant currency: 14,837 gold"`.

### Specialist-header bug fix (in `BuildNameTitleSpecialties`)

When a profile has `characteristics["monster"]` set but no specialist titles, the header renders as `"Name the Goblin; \n"` — a dangling `"; "` separator. Fix: only emit the `"; "` separator between the monster type and the specialist titles when there actually *is* at least one specialist title to follow it. Same vigilance for any other stray-separator cases (e.g., empty specialist + non-empty displayed titles).

## Data sources & helpers

- **Interactions:** `MonDB.getInteractionsByType("plant")`, etc. Existing pattern.
- **Profiles for snapshots:** need a `MonDB.getAllProfiles()` (or equivalent) to scan for current monster / job / parasite / currency state. Verify whether one exists; if not, add it as part of this sweep.
- **Currency totals:** verify how wallets are stored (per-profile dictionary?) and whether there's an aggregation helper or if the scan is per-profile.
- **Vice deltas for corruption/purity volume:** count must reflect *volume* (delta amount), not interaction count. Verify how `corrupt` / `purify` interactions log their deltas.
- **Rendering helpers:** `ParasiteText`, `ViceText`, `ScentText`, `Utils.JobToText`, `Utils.LocationToText` already exist and should be used wherever the corresponding entity appears in output.

## Tests

For each new command:
- Empty state renders gracefully (no plants, no statues, no employees — `!flora`, `!statues`, `!payroll` should still produce a coherent message).
- Tie behavior: two species tied for "most bred" both surface.
- Snapshot vs lifetime separation: planting+consuming the offspring still shows the offspring in lifetime counts; purging a parasite removes from snapshot but lifetime-spread count is unchanged.
- Net-tilt phrasing flips correctly between "Corruption Conquers" / "Purity Prevails" / tied phrasing.

For dossier:
- Active-state blocks render only when the corresponding lists are non-empty.
- Specialist-header bug: profile with monster + no specialties no longer emits trailing `"; "`.
- Give/take split appears for every tracked interaction with a known display name, and unknown ones don't crash.
- `Currently employs:` only renders for actual employers (not empty for everyone).

## As-shipped notes

- **A1 resolved — purges now log an interaction.** `ChateauPurge.PurgeType` (`"purge"`) is stamped on a self-initiator/self-recipient `Interaction` at purge time. No historical backfill (purges before this change are unseen by the lifetime tally) — the user confirmed that's acceptable since no purges had occurred yet.
- **A2 resolved — duties come from `Profile.jobExperience`, not `!work` interactions.** `!work` doesn't log an Interaction; each completed duty increments `profile.jobExperience[job]` by 1 in [ChateauWork.cs](../../FChatDicebot/BotCommands/ChateauWork.cs). `ChateauStatisticsSupport.SumDutiesByJob` aggregates across all profiles.
- **A3 confirmed — raw currency counts only.** Lustessence at 5 beats gold at 4. Intended behaviour.
- **A4 resolved — explicit `Utils.JobToPlural` mapping.** Irregular plurals are codified in [Utils.cs:608](../../FChatDicebot/Utils.cs:608); jobs not listed fall back to "{singular}s".
- **A5 confirmed — `Currently employs` is a dossier-time profile scan** via `IChateauDatabase.GetAllProfiles()`.

## Differences from the original spec

- **`!birthrates` shows offspring/pregnancies, not sired/birthed.** Every offspring is both sired (by an initiator) and birthed (by a carrier) — splitting chateau-wide between the two roles would just emit two identical numbers. `MonsterStats` already tracks the two figures that actually differ: per-monster offspring count (each individual offspring) and pregnancy count (each !breed→!birth cycle). The dossier still uses the spec's per-resident `Sired` / `Birthed` split since those *are* distinct at the user level.
- **`!parasites` says "spread", not "ever spread".** User wording pass.
- **`!economics` lead-in says "The accumulated wealth of Chateau residents, by currency:"** rather than "The Chateau's wealth, by currency:". User wording pass.
- **`!statistics` hides zero-count rows.** Section headers always render, but any line whose total is 0 (including Corruption/Purity and the Net Tilt) is suppressed — a quiet Chateau emits a short readout. Confirmed during user review.
- **`!statistics` capitalizes currency keys** (`Gold`, `Silvercoin`, `Lustessence`) instead of rendering them as stored. Confirmed during user review.
- **Dossier counts row is split into two:** `Casual interactions:` (existing, unchanged) and a new `Notable counts:` row that holds the give/take split for non-casual counters (`Orgasms`, `Bodyparts Exhausted`, `Curses Endured`, `Costume Changes`, `Golden Showers`, `Personal Payments`) plus summed entries (`Marks Shared`, `Meals Shared`). The original spec left section title as implementer's call; this is the as-shipped split.
- **`ChateauStatisticsSupport` is a shared helper** under [FChatDicebot/BotCommands/Support/](../../FChatDicebot/BotCommands/Support/), not collocated with any single command. The auto-registration loop scans `FChatDicebot.BotCommands` for `ChatBotCommand` subclasses and crashes on static classes in that namespace — `Support` is a sibling namespace to keep the static helper out of its path.

## Files (as-shipped)

**New:**
- [`FChatDicebot/BotCommands/ChateauStatistics.cs`](../../FChatDicebot/BotCommands/ChateauStatistics.cs) — `!statistics` / `!stats`
- [`FChatDicebot/BotCommands/ChateauPopulations.cs`](../../FChatDicebot/BotCommands/ChateauPopulations.cs)
- [`FChatDicebot/BotCommands/ChateauFlora.cs`](../../FChatDicebot/BotCommands/ChateauFlora.cs)
- [`FChatDicebot/BotCommands/ChateauBirthrates.cs`](../../FChatDicebot/BotCommands/ChateauBirthrates.cs)
- [`FChatDicebot/BotCommands/ChateauParasites.cs`](../../FChatDicebot/BotCommands/ChateauParasites.cs) — no collision with `!infest` infrastructure, the bare name is used
- [`FChatDicebot/BotCommands/ChateauPayroll.cs`](../../FChatDicebot/BotCommands/ChateauPayroll.cs)
- [`FChatDicebot/BotCommands/ChateauEconomics.cs`](../../FChatDicebot/BotCommands/ChateauEconomics.cs)
- [`FChatDicebot/BotCommands/Support/ChateauStatisticsSupport.cs`](../../FChatDicebot/BotCommands/Support/ChateauStatisticsSupport.cs) — shared aggregation helpers
- [`FChatDicebot.Tests/Unit/Chateaustatisticstests.cs`](../../FChatDicebot.Tests/Unit/Chateaustatisticstests.cs)

**Modify:**
- [`FChatDicebot/BotCommands/ChateauDossier.cs`](../../FChatDicebot/BotCommands/ChateauDossier.cs) — Active Curses/Parasites/Breaks/Scents blocks, Sired/Birthed/Has personally planted/Currently employs blocks, Titles earned, Most abundant currency, Notable counts row, specialist-header trailing-separator fix.
- [`FChatDicebot/BotCommands/ChateauPurge.cs`](../../FChatDicebot/BotCommands/ChateauPurge.cs) — logs a `purge` Interaction so `!parasites` and `!statistics` can count purges (per A1).
- [`FChatDicebot/BotCommands/ChateauHelp.cs`](../../FChatDicebot/BotCommands/ChateauHelp.cs) — adds the 7 new commands to the General list.
- [`FChatDicebot/Utils.cs`](../../FChatDicebot/Utils.cs) — adds `JobToPlural` (per A4).
- [`FChatDicebot/Database/Ichateaudatabase.cs`](../../FChatDicebot/Database/Ichateaudatabase.cs) and [`Chateaudatabase.cs`](../../FChatDicebot/Database/Chateaudatabase.cs) — adds `GetAllProfiles()` and `GetAllMonsterStats()`.
- [`FChatDicebot/MonDB.cs`](../../FChatDicebot/MonDB.cs) — static delegates for the two new helpers.
- [`FChatDicebot/FChatDicebot.csproj`](../../FChatDicebot/FChatDicebot.csproj) — explicit `<Compile>` entries for each new `.cs` (this project uses the old-style csproj that requires them).
- [`FChatDicebot.Tests/Unit/Chateaudossiertests.cs`](../../FChatDicebot.Tests/Unit/Chateaudossiertests.cs) — adds tests for each new section + the specialist-header regression.
