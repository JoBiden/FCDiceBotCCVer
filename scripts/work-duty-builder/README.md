# Work Duty Builder

A local web UI for authoring the `!work` / `!volunteer` duties that live in the bot's
`ChateauDb.Duties` Mongo collection. List, create, edit, duplicate and delete duties
with a live preview of exactly what the bot will PM — no hand-written BSON, no more
spreadsheet round-trips.

The bot queries the duty list fresh every time someone works, so anything you save
here is live immediately. **No bot restart needed.**

## Run it

```powershell
cd scripts\work-duty-builder
.\run.ps1
```

That compiles the tiny helper server (first run only), starts it on
`http://localhost:8788/`, and opens your browser. The server binds localhost only.
(The Random Event Builder uses 8787, so both tools can run at the same time.)

The page can also be opened as a plain file (or in an embedded preview panel) — it
falls back to calling the API at `http://localhost:8788`, so it works anywhere as
long as the helper server is running. If you see "Helper server not reachable",
start `run.ps1` and reload.

Prerequisite: the FChatDicebot solution must have been built (Debug) at least once —
the helper borrows the bot's MongoDB driver DLLs and binding redirects.

Options: `-Port 9000`, `-Db OtherDb`, `-Conn mongodb://...`, `-NoBrowser`,
`-DllSource <path to FChatDicebot\bin\Debug>`.

## Desktop shortcut / standalone deploy

The built tool is fully standalone — `bin\` (exe + DLLs + config) plus `ui.html`
next to it is everything. To run it outside the repo (e.g. from a desktop or
taskbar shortcut that shouldn't break when branches/worktrees change):

```powershell
$deploy = "C:\BotData\DiceBot\tools\work-duty-builder"
New-Item -ItemType Directory -Force "$deploy\bin" | Out-Null
Copy-Item bin\* "$deploy\bin" -Force
Copy-Item ui.html, README.md $deploy -Force
```

Then point a shortcut at `$deploy\bin\WorkDutyBuilder.exe` with arguments
`--open` (starts the server and opens the browser; closing the console window
stops it). Because the shortcut targets an exe, it can be pinned to the taskbar
as its own app. Re-run the copy above after changing the tool in the repo.

## What the fields mean

| Field | Meaning |
| --- | --- |
| label | Unique id for the duty (used by this tool to address it) |
| job | Which job rolls this duty. A job with several duties picks one at random per work day. `default` is the fallback set used when a job has no duties of its own |
| categories | Stored on the document (queryable via GetDutiesByCategory) but not used for selection |
| startText | PMed when the duty starts, followed by the numbered choice list. BBCode ok |
| choices | Shown in order as `!w 1`, `!w 2`, ... (`!v N` for volunteers). Choices a player doesn't qualify for are hidden and later ones renumber |
| result text | PMed when the player picks that choice, followed by the rolled reward |
| rewards | ONE is rolled per completion, weighted by `weight`; the amount is rolled between `min` and `max` inclusive. No rewards = pure flavor |

Completing any choice also grants +1 job experience for the duty's job and uses up
the player's daily work (or volunteer) slot.

## Choice conditionals

Each choice is either always available or gated. In the stored document the gate is
`conditional.type` = a three-letter kind + the key, with `value` as the threshold:

| Kind | Stored as | Available when |
| --- | --- | --- |
| always | `none` | Everyone sees it |
| job experience | `job` + job, e.g. `jobadventurer` | That job's experience ≥ value |
| training | `trn` + training, e.g. `trndeepthroat` | Player has the training |
| currency | `cur` + currency, e.g. `curgold` | Currency balance ≥ value |
| species | `mon` + category, e.g. `monflight` | Player's species identifier carries that category |

Every duty must keep at least one always-available choice (the server refuses to
save otherwise) — a player who fails every conditional would otherwise get a duty
with zero choices and lose the work day.

Note: before 2026-07-12 the bot matched `job`/`cur` conditionals against the full
prefixed string (so they never unlocked) and treated the kind prefix
case-sensitively (so `None` hid the choice entirely). Both are fixed on the branch
that added this tool; job/cur gates only start working once that bot build is
deployed.

## Safety notes

- **Save writes to the live database.** The Export buttons (JSON / mongosh insert)
  are there if you'd rather stage a duty elsewhere first.
- Deleting asks for confirmation, but there is no undo.
- Job, currency, training and species dropdowns are read live from the
  `Identifiers` collection; unknown keys show a warning but are kept, since
  identifiers may be seeded later.
