# Random Event Builder

A local web UI for authoring the B12 ambient random events that live in the bot's
`ChateauDb.RandomEvents` Mongo collection. List, create, edit, duplicate and delete
events with a live preview of exactly what the bot will post — no hand-written BSON,
no mongosh required.

The bot loads the event list lazily each time an event is about to fire, so anything
you save here is live immediately. **No bot restart needed.**

## Run it

```powershell
cd scripts\random-event-builder
.\run.ps1
```

That compiles the tiny helper server (first run only), starts it on
`http://localhost:8787/`, and opens your browser. The server binds localhost only.

The page can also be opened as a plain file (or in an embedded preview panel) — it
falls back to calling the API at `http://localhost:8787`, so it works anywhere as
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
$deploy = "C:\BotData\DiceBot\tools\random-event-builder"
New-Item -ItemType Directory -Force "$deploy\bin" | Out-Null
Copy-Item bin\* "$deploy\bin" -Force
Copy-Item ui.html, README.md $deploy -Force
```

Then point a shortcut at `$deploy\bin\RandomEventBuilder.exe` with arguments
`--open` (starts the server and opens the browser; closing the console window
stops it). Because the shortcut targets an exe, it can be pinned to the taskbar
as its own app. Re-run the copy above after changing the tool in the repo.

## What the fields mean

| Field | Meaning |
| --- | --- |
| label | Unique id for the event (used by this tool to address it) |
| weight | Selection weight relative to **all** events (categories are stored but not used for selection yet) |
| announceText | Posted to the channel when the event fires. BBCode ok. Placeholders: `{keyword}`, `{challenge}`, `{window}` |
| responseType | `none` (any `!random` counts), `keyword` (must repeat a random word from the engine's pool), `challenge` (must solve a generated sum) |
| responseWindowSeconds | How long `!random` responses are accepted (0 = engine default, 60s) |
| winnerRule | `firstValid`, `allInWindow`, `nth` (with `winnerN`), `random` |
| outcomes | One is rolled by weight when the event resolves. `resultText` may use `{winners}`; rewards apply per winner |

Reward types: `currency`, `title`, `training`, `corruption`, `purity`, `curse`
(must be one of the engine's cataloged curses), `none` (pure flavor). Amounts are
rolled between `min` and `max` per winner.

The curse dropdown mirrors `CurseProcessor.CatalogMap` in code (the engine silently
grants nothing for unknown curse keys) — if new curses are added there, update the
`CurseKeys` list at the top of `server.cs`. Currency/training dropdowns are read live
from the `Identifiers` collection.

## Safety notes

- **Save writes to the live database.** The Export buttons (JSON / mongosh insert)
  are there if you'd rather stage an event elsewhere first.
- Deleting asks for confirmation, but there is no undo.
- The engine only fires events in channels with `AllowRandomEvents` enabled
  (`!updatesetting allowrandomevents on`).
