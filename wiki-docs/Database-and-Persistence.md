# Database and Persistence

FCDiceBot uses a dual-storage system combining MongoDB for Chateau data and JSON files for the legacy dicebot state.

Schemas below are illustrative; the model classes in `FChatDicebot/Model/ChateauDB.cs` are authoritative. When you add or change a field, update the matching section here.

## Storage Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    FCDiceBot Data                        │
└───────────────┬─────────────────────┬───────────────────┘
                │                     │
                ▼                     ▼
    ┌─────────────────────┐  ┌──────────────────────┐
    │  MongoDB (Chateau)   │  │  JSON Files (Legacy) │
    │  ChateauDb           │  │  C:\BotData\DiceBot\ │
    └─────────────────────┘  └──────────────────────┘
                │                     │
                ▼                     ▼
      Profiles, interactions,   Account + channel settings,
      pendings, duties,         chip piles, roll tables,
      identifiers, pledges,     decks, slots, potions,
      feedback, events, ...     coupons, VC orders
```

## MongoDB Storage

### Connection

`BotMain.Run()` calls `MonDB.Initialize("mongodb://localhost:27017", "ChateauDb")` at startup, before anything touches the database. Tests point the same adapter at `ChateauDb_Test` instead (see `FChatDicebot.Tests/TestConfiguration.cs`).

**Implementation:** `FChatDicebot/Database/Chateaudatabase.cs` behind the `IChateauDatabase` interface (`Ichateaudatabase.cs`).

### Collections

The full set, as of mid-2026 (grep `GetCollection<` in `Chateaudatabase.cs` for the current list):

| Collection | Purpose |
|------------|---------|
| `RegisteredProfiles` | One document per resident: counts, characteristics, lists, timers, currencies, titles, pregnancies, milk inventory, trainings, job experience, employee earnings |
| `Interactions` | Historical record of every completed interaction |
| `PendingCommands` | Consent workflow — interactions awaiting consent (including group seats) |
| `Duties` | Authored `!work` / `!volunteer` scenarios |
| `PendingDuties` | A started work/volunteer session awaiting the player's `!w N` / `!v N` choice |
| `Identifiers` | The vocabulary interactions draw on (bodyparts, substances, monsters, scents, vices, …) |
| `Pledges` | `!pledge` / `!fulfill` promised-interaction records |
| `Feedback` | `!feedback` submissions (read via admin `!feedbacklist`; the triage pipeline in `scripts/feedback-triage` writes its ledger through `ft.ps1`, never directly) |
| `RandomEvents` | Authored ambient channel events for `!random` / the scheduler (authored via `scripts/random-event-builder`) |
| `ModMessages` | Moderator announcements shown by `!modmessage` |
| `MonsterStats` | Per-monster breeding statistics (pregnancy/offspring counts) |
| `Counters` | Atomic counters — currently the bottle-serial counter (`ClaimBottleSerials`) |
| `Commands` | Command metadata documents (aliases/params); legacy, not the source of `!help` (that's the command classes) |
| `SlotsJackpots` | Persistent slots jackpot state |

#### RegisteredProfiles

The `Profile` class is large and grows with features — read `Model/ChateauDB.cs` for the current shape. Core structure:

```json
{
  "userName": "Alice",              // F-Chat handle (may be "[s]old[/s] new" after rename)
  "displayName": "Alice the Great", // what every user-facing string shows
  "counts": { "kiss": 42, "milkgive": 3, "milktake": 1 },
  "characteristics": { "species": "human", "job": "maid", "employer": "Carol" },
  "lists": { "bonds": ["..."], "scents": ["..."] },
  "timers": { "ratelimit_kiss": { "timerStart": "...", "timerEnd": "..." } },
  "currencies": { "copper": 500 },
  "escrow": { },
  "jobExperience": { "maid": 150 },
  "titles": [ { "titleText": "·First Kiss·", "givenBy": "system", "grantedTime": "..." } ],
  "displayedTitleSlots": [0, 1, -1, -1, -1, -1, -1, -1, -1],
  "pregnancies": [ ],
  "dailyMagnitudes": { },
  "milkInventory": [ ],
  "trainings": { },
  "dailyClimaxCounts": { },
  "employeeEarnings": { }
}
```

Conventions:
- Simple values go in `characteristics["key"]`, lists in `lists["key"]`, cooldowns in `timers["key"]` (a `CoolDown` with UTC `timerStart`/`timerEnd`).
- Rate-limit timers are keyed `ratelimit_{countLabel}`; per-direction cooldowns use keys like `milk_give_<recipient>`.
- New optional fields use `[BsonIgnoreIfNull]` / defaults so legacy documents need no migration.

#### Interactions

One document per completed interaction:

```json
{
  "initiator": "Alice",
  "recipient": "Bob",
  "type": "kiss",
  "identifier": "",
  "investmentLevel": "casual",
  "extraParameters": [],
  "interactionTime": "2026-06-22T12:34:56Z"
}
```

Queried by initiator / recipient / type for dossiers, statistics, and title checks.

#### PendingCommands

The consent workflow. Key fields (see `PendingCommand` in `Model/ChateauDB.cs` — the group-seat comments there are worth reading):

```json
{
  "pendingInteraction": { "initiator": "Alice", "recipient": "Bob", "type": "kiss", ... },
  "awaitingConsentFrom": "Bob",
  "startTime": "2026-06-22T12:30:00Z",
  "groupId": null,          // shared id across every seat of one group invocation
  "sourceChannel": null,    // group seats only: where to post the resolved moment
  "consentState": "Pending",
  "consentedOrder": 0       // 1,2,3... once consented; drives lapsit stack order
}
```

**Expiration:** 10 minutes (`GroupInteractionResolver.PendingMinutesKeep`). Expired pendings are swept when the recipient next interacts with the consent flow, and a periodic sweep in `BotMain` resolves timed-out *group* moments so they post without anyone typing another command.

#### Duties

**Purpose:** Job system - the authored duty scenarios `!work` and `!volunteer` roll from

Each document is one scenario for one job. When a player works, one of their job's
duties is picked at random; if a job has no duties, the `"default"` job's duties are
used as a fallback. Model classes: `Duty`, `DutyResult`, `Reward`, `Conditional` in
`FChatDicebot/Model/ChateauDB.cs`.

**Document Structure:**

```json
{
  "_id": ObjectId("..."),
  "label": "PolishTheSilverware",
  "categories": ["mercantile"],
  "job": "maid",
  "startText": "The head maid hands you a list of chores...",
  "dutyResults": {
    "Dust the shelves": {
      "choiceName": "Dust the shelves",
      "resultText": "You dust until you can see your reflection.",
      "conditional": { "type": "none", "value": 0 },
      "rewardList": {
        "copper": { "currency": "copper", "weight": 9, "min": 5, "max": 20 },
        "silver": { "currency": "silver", "weight": 1, "min": 1, "max": 3 }
      }
    },
    "Fly to the chandelier": {
      "choiceName": "Fly to the chandelier",
      "resultText": "Only the winged can dust up here.",
      "conditional": { "type": "monflight", "value": 0 },
      "rewardList": { }
    }
  }
}
```

`dutyResults` is a keyed subdocument (`Dictionary<string, DutyResult>` in code); its
field order is the order choices are numbered in (`!w 1`, `!w 2`, ...). On completion
ONE reward is rolled from `rewardList`, weighted by `weight`, for an amount between
`min` and `max` inclusive; an empty `rewardList` is pure flavor.

**Conditionals:** `conditional.type` is a three-letter kind prefix plus a lookup key,
parsed by `BotCommands/Support/DutyConditionalSupport.cs`:

| Type | Choice is shown when |
| --- | --- |
| `none` | Always |
| `job` + job, e.g. `jobadventurer` | That job's experience >= `value` |
| `trn` + training, e.g. `trndeepthroat` | Player has the training |
| `cur` + currency, e.g. `curgold` | Currency balance >= `value` |
| `mon` + category, e.g. `monflight` | Player's species identifier carries that category |

Every duty must keep at least one `none` choice - a player who fails every
conditional would otherwise get a duty with zero choices and lose the work day.

**Queried by:** `label` (exact), `job` (exact, lowercase), `categories` (contains).
No indexes are created; the collection is small.

**Authoring:** duties are seeded externally, not by the bot. Use the Work Duty
Builder (`scripts/work-duty-builder`, `run.ps1`, http://localhost:8788) - it edits
this collection live with validation and a preview of the resulting PMs.

#### PendingDuties

**Purpose:** A started work/volunteer session awaiting the player's choice

Created when `!work` or `!volunteer` presents a duty's choices; deleted when the
player answers with `!w N` / `!v N` (rewards granted, job experience +1, daily
timer set), or cleaned up on their next invocation if it's from a previous
Chateau day. Model class: `PendingDuty` in `FChatDicebot/Model/ChateauDB.cs`.

**Document Structure:**

```json
{
  "_id": ObjectId("..."),
  "dutyResults": [
    { "choiceName": "...", "resultText": "...", "conditional": {...}, "rewardList": {...} }
  ],
  "dutyLabel": "PolishTheSilverware",
  "job": "maid",
  "awaitingInputFrom": "Alice",
  "startTime": ISODate("2025-11-22T08:00:00Z")
}
```

Unlike `Duties`, `dutyResults` here is an ARRAY: only the choices the player
qualified for, in the order they were numbered - the player's `!w N` indexes
straight into it.

**Queried by:** `awaitingInputFrom` (exact). No indexes are created.

#### Identifiers

**Purpose:** The vocabulary interactions draw on — one document per identifier, each tagged with the categories it belongs to.

**Document Structure:**

```json
{
  "_id": ObjectId("..."),
  "type": "lowerback",
  "description": "The small of the back, just above the hips.",
  "categories": ["bodypart", "break"],
  "displayText": "lower back",
  "eicon": "[eicon]stockback[/eicon]"
}
```

| Field | Notes |
|---|---|
| `type` | The token residents type. Lowercase; unique. |
| `description` | Prose shown by `!whatis`. |
| `categories` | Every category this identifier belongs to. One identifier can be in several. |
| `displayText` | Optional flavor-text override consumed by the `Utils.*ToText` helpers. Unset = render the raw `type`. |
| `eicon` | Optional bot-wide decorative icon, full `[eicon]…[/eicon]` bbcode. Shown by `!whatis`; set with the admin-only `!setidentifiereicon`. |
| `gestationDays`, `broodSizeMin`, `broodSizeMax` | Monster-only `!breed` overrides. Zero/absent = fall back to the category defaults. |

`displayText`, `eicon`, and the brood fields are omitted from documents that don't set them, so adding either needs no migration.

**Categories** include `bodypart`, `break`, `substance`, `attire`, `species`/`monster`, `object`, `plant`, `scent`, `vice`, `parasite`, `curse`, `training`, `location`, `job`, `bond` — `!category {name}` lists a category live, which is more reliable than any list written here.

Identifiers are curated directly in Mongo — `SetIdentifierEicon` is the one in-chat write path, for the cosmetic `eicon` field only.

#### Other collections

`Pledges` (`Pledge`), `Feedback` (`FeedbackEntry`), `RandomEvents` (`RandomEvent`), `ModMessages`, `MonsterStats`, and `SlotsJackpots` are small and single-purpose; their model classes in `Model/ChateauDB.cs` are the schema documentation. `Counters` holds atomic counters claimed with `FindOneAndUpdate` + `$inc` (currently just bottle serials).

## File-Based Storage

### Location

```
C:\BotData\DiceBot\
```

JSON files loaded by `BotMain` at startup for the legacy dicebot: `account_settings.txt` (see [Installation-and-Setup](Installation-and-Setup.md) for the fields), `channel_settings.txt` (per-channel prefix + feature toggles), plus saved chip piles, roll tables, decks, slots settings, potions, coupons, and VelvetCuff chip-order state. The classes in `FChatDicebot/SavedData/` are the authoritative shapes.

### Backup

On startup the bot copies the data files to `C:\BotData\DiceBot\ImmediateBackup` (`BotMain.BackupFolder`) before modifying anything. Only one generation is kept — anything more robust (versioned backups, `mongodump` scheduling) is an operator concern outside the bot.

## Data Access Layer

### MonDB Static Adapter

**Location:** `FChatDicebot/MonDB.cs`

`MonDB.Initialize(connectionString, databaseName)` constructs the singleton `ChateauDatabase`; `MonDB.GetDatabase()` returns it — and **throws** if `Initialize` hasn't run yet. That guard is deliberate: it used to silently default to the production database, which is exactly the accident it now prevents. Never work around it; initialize explicitly (tests do this through `TestDatabaseFixture`).

The rest of `MonDB` is thin lowercase delegation (`getProfile`, `setProfile(userName, profile)`, `incrementCount`, `changeCurrency`, `addPendingCommand`, `getPending`, `removePendingInteraction`, `getIdentifier`, …) kept for the many legacy call sites. `tryGetIdentifier` is the one special case: a non-throwing lookup so the `Utils.*ToText` display helpers can consult `Identifier.displayText` without requiring a live database.

New code — processors especially — should take an `IChateauDatabase` (constructor injection) and use it directly; that's what makes it testable against `ChateauDb_Test`.

### IChateauDatabase

**Location:** `FChatDicebot/Database/Ichateaudatabase.cs`

The full interface: profile CRUD, counts/currencies with rate-limited variants, interactions, pending commands (including group queries), duties, identifiers, pledges, feedback, random events, mod messages, monster stats, bottle serials. Read the interface for the current surface — it grows with every feature and any list here would rot.

## Practical Notes

- `RegisteredProfiles` is looked up by `userName` constantly; interaction-history queries scan by initiator/recipient/type. Collections are small enough that explicit indexes haven't been needed.
- Fetch a profile once, mutate, then `setProfile` once — don't round-trip per field.
- `changeCurrency` is the atomic currency path (added for wager settlement); prefer it over read-modify-write for money.
- Schema changes should be additive with `[BsonIgnoreIfNull]`/defaults so old documents keep deserializing. For historic backfills, one-off `mongosh` scripts live in `scripts/` (e.g. the employer-earnings backfill).

## See Also

- [Installation and Setup](Installation-and-Setup) - MongoDB setup
- [Architecture](Architecture) - Data access layer details
- [Interaction System](Interaction-System) - How interactions use the database
- [Development Guide](Development-Guide) - Working with the database
