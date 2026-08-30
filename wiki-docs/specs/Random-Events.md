# Random Events — ambient channel events + `!random`

**Status:** ✅ SHIPPED 2026-06-29, wording pass + design tweak 2026-07-01 (owner review), **winner conditions + `invert` reward 2026-08-30** (see *Winner conditions* below). Per-event `announceText` / `resultText` and reward magnitudes are authored data in the seeded `RandomEvents` collection; a single starter event ("Cutie says {word}") has been authored — see *Seed data* below.
**Feature-Requests source:** "Random events, to encourage spontaneous chat activity. Responding to a random event would always start !random but might also require an additional argument, to slow down campers/snipers" (B12).

> **Note on paths:** this spec was written before the latest Dice Bot integration flattened `FChatDicebot/FChatDicebot/…` to `FChatDicebot/…`. The links below predate that move; the as-built files are: model in [Model/ChateauDB.cs](../../FChatDicebot/Model/ChateauDB.cs), engine in [BotCommands/Support/RandomEventEngine.cs](../../FChatDicebot/BotCommands/Support/RandomEventEngine.cs), command in [BotCommands/ChateauRandom.cs](../../FChatDicebot/BotCommands/ChateauRandom.cs), scheduler in [BotMain.cs](../../FChatDicebot/BotMain.cs) (`HandleRandomEventsTick`), DB in [Database/Chateaudatabase.cs](../../FChatDicebot/Database/Chateaudatabase.cs), tests in `FChatDicebot.Tests/Unit/Randomeventenginetests.cs`. Discord is intentionally excluded — the tick is wired into `RunLoopFList` only (the deployment is F-Chat / Chateau-only).

> This is the largest of the Social/events specs. It has three buildable chunks that can land incrementally: **(1)** the event data model + seeded collection, **(2)** the `!random` participation command + active-event lifecycle, **(3)** the tick-driven scheduler. The bulk of the work is wiring the **six reward types** to existing grant mechanisms (below).

---

## Overview

Once every several hours, the bot fires a **random event** into an opted-in channel — a short in-character prompt posted to chat. Residents join with the new **`!random`** command (optionally with an extra argument the event demands, to thwart snipers). After the event's response window resolves, the bot announces the outcome and grants the reward(s): currency, a title, training, corruption/purity, a curse, or just flavor.

Key properties:
- Events are **data, not code** — authored in a seeded `RandomEvents` Mongo collection (the [Duties](../../FChatDicebot/Database/Chateaudatabase.cs)/`ModMessages` pattern: read-only in-bot, authored externally), so new events ship without a deploy.
- Firing is **channel broadcast** (TOS-safe — the bot already posts freely to channels it's in; the TOS constraint is only about PMing users who didn't invoke). `!random` is user-invoked, so all replies to participants are TOS-safe.
- Existing infrastructure does most of the heavy lifting: the bot already has a heartbeat (`RunLoop` ticking every `TickTimeMiliseconds`) and a delayed-message mechanism (`SendFutureMessage` / [`HandleFutureMessagesTick`](../../FChatDicebot/BotMain.cs)). The scheduler is a sibling tick handler.

---

## Event data model

Mirrors the existing `Duty` / `DutyResult` / `Reward` shapes ([ChateauDB.cs:187-221](../../FChatDicebot/Model/ChateauDB.cs)) so authoring feels familiar.

```csharp
public class RandomEvent
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    public string label { get; set; }
    public string[] categories { get; set; }
    public int weight { get; set; }              // selection weight among eligible events
    public string announceText { get; set; }      // posted to the channel when the event fires
    public string responseType { get; set; }      // "none" | "keyword" | "challenge"
    public int responseWindowSeconds { get; set; } // how long !random is accepted (e.g. 60)
    public string winnerRule { get; set; }         // "firstValid" | "allInWindow" | "nth" | "random"
    public int winnerN { get; set; }               // for "nth": which responder wins (e.g. 4)
    public List<EventOutcome> outcomes { get; set; } = new List<EventOutcome>(); // weighted roll
}

public class EventOutcome
{
    public int weight { get; set; }       // weighted pick among the event's outcomes
    public string resultText { get; set; } // announced when this outcome is granted; may use {winners}
    public List<EventCondition> conditions { get; set; }                      // gates this outcome per winner
    public List<EventReward> rewards { get; set; } = new List<EventReward>(); // applied to winner(s)
}

public class EventReward
{
    public string type { get; set; } // "currency" | "title" | "training" | "corruption" | "purity" | "invert" | "curse" | "none"
    public string key { get; set; }  // currency name / title id / training skill / curse id (unused for corruption/purity/none)
    public int min { get; set; }     // magnitude/amount low  (currency, training, corruption, purity)
    public int max { get; set; }     // magnitude/amount high
}
```

**Response types (B12.3 — "depends on the event, could be any of the three"):**
- `none` — `!random` with no arg participates.
- `keyword` — `announceText` instructs the user to include a token; only `!random <token>` (matched case-insensitively) counts. The token is chosen per-fire (so it can be randomized) and held in the active-event state. Defeats pre-typed snipes.
- `challenge` — a tiny generated problem (e.g. a small sum); the user submits the answer as the arg. Generated per-fire; expected answer held in active-event state.

> **As-built design change (owner, 2026-07-01): the third response type ("delay" — reject responses before a randomized minimum time after the fire) was dropped.** The anti-snipe protection is entirely the keyword/challenge itself — you can't pre-type an answer you don't yet know. A pure time-gate would instead penalize residents who genuinely react fast and correctly, which isn't the goal. So `responseType` is now exactly `none` | `keyword` | `challenge`, and an invalid response is always just nudged (never time-gated) with no attempt consumed.

> **As-built authoring note — `announceText` placeholders.** Because the keyword/challenge are randomized per-fire, the engine substitutes them into `announceText` at fire time: `{keyword}` → the chosen token, `{challenge}` → the generated problem (e.g. `3 + 7`), `{window}`/`{seconds}` → the response-window length. So a `keyword` event authors `"…shout the word {keyword} to be counted!"`. If a `keyword`/`challenge` event's `announceText` omits the placeholder, the engine appends a generated `[sub]Quick, !random {token}…[/sub]` instruction line so it's never unanswerable. `none` events ignore these placeholders.

**Winner rules (B12.4 — "depends on the event"):**
- `firstValid` — first valid responder wins; resolve immediately on that response.
- `allInWindow` — everyone who validly responds before the window closes wins (each granted an outcome roll).
- `nth` — exactly the `winnerN`-th valid responder wins.
- `random` — a random valid responder among those in the window wins.

Outcomes are picked by `weight` (the existing weighted-roll helper used by `!work` rewards); each `EventReward` in the chosen outcome is applied to the winner(s).

---

## Reward application (the bulk of the work)

Each `EventReward.type` maps to an **existing** grant mechanism — reuse, don't reinvent:

| type | applied via |
|------|-------------|
| `currency` | `profile.currencies[key] += roll(min,max)` — the [ChateauWork](../../FChatDicebot/BotCommands/ChateauWork.cs) credit pattern (denominations independent, per `ChateauCurrency`). |
| `title` | award `Title` `key` into `profile.titles` — reuse the title-grant path used by the interaction processors / `!entitle`. |
| `training` | `profile.trainings[key]` += `roll(min,max)`, clamped 0–100 ([TrainProcessor](../../FChatDicebot/InteractionProcessors)). |
| `corruption` | apply magnitude `roll(min,max)` via the existing [CorruptionProcessor / CorruptionCommandSupport](../../FChatDicebot/InteractionProcessors/Commitment/CorruptionCommandSupport.cs) path. |
| `purity` | the purify/cleanse side of the corruption axis (reduce corruption) — reuse the same support. |
| `invert` | mirror the whole signed corruption axis (`value → -value`). Added 2026-08-30; see *Winner conditions* below. |
| `curse` | add curse `key` via [CurseInstance / CurseProcessor](../../FChatDicebot/Model/CurseInstance.cs). |
| `none` | flavor-only; no profile write. |

> Wiring these six paths cleanly (and writing the winner's profile once via `SetProfile` after all rewards apply) is the main implementation effort. A small `ApplyEventReward(Profile, EventReward)` dispatcher keeps it testable. Self-consistency note: corruption/curse here are **system-granted**, so they bypass the consent flow real interactions require — that's intended for an event the player opted into by responding, but call it out to the owner.

---

## The `!random` command

New `ChatBotCommand` [ChateauRandom.cs](../../FChatDicebot/BotCommands/ChateauRandom.cs). `Name="random"` — collides with nothing in the dispatch table.

| Field | Value |
|-------|-------|
| `Name` | `random` |
| `Category` | `General` |
| `ShortDescription` | "Join the current random event" |
| `Usage` | `!random`  •  `!random <answer/keyword>` |
| `RequireChannel` | `true` (events live in a channel) |
| `CooldownDuration` | `null` (participation is gated by the event/window, not a timer) |

**Behavior:**
1. No active event in this channel → short reply (PM'd): `"There's no random event for you to respond to right now. When there is, everyone will be explicitly prompted."`
2. Active event → validate per `responseType` (token/answer match); on invalid, a brief nudge, PM'd, **without consuming their one shot** — the resident can just try again.
3. Record the responder once (ignore duplicate `!random` from the same user in the same event; PM'd "can't participate twice" nudge).
4. Resolve per `winnerRule`: `firstValid` resolves immediately; the others accumulate (each accepted attempt gets a PM'd "you're in, results once everyone's had a chance to join") and resolve when the window closes (handled by the scheduler tick).
5. On resolution, post the chosen outcome's `resultText` to the channel — substituting `{winners}` with every winner's `[user]` tag if present, so multi-winner outcomes can carry their own combined flavor (e.g. `"{winners} are now glowing purple."`) instead of a generic engine-authored line — followed by one combined reward line per distinct reward grant (winners who received identical rewards are grouped onto a single "X and Y receive Z!" line rather than repeated per person), and grants the rewards to the winner(s).

---

## Scheduling (B12.2)

Per-channel opt-in + a tick handler parallel to `HandleFutureMessagesTick`:

- **Opt-in:** new `ChannelSettings.AllowRandomEvents` flag (default `false`), matching the existing per-channel toggles (`AllowWork`, `AllowGames`, …) in [ChannelSettings.cs](../../FChatDicebot/SavedData/ChannelSettings.cs). Only opted-in joined channels are eligible.
- **Cadence:** about **one event per 6–8 hours per channel**, with jitter. Track a per-channel `nextFireUtc`; when reached, attempt a fire, then schedule the next at `now + random(6h, 8h)`.
- **Activity gate (arm-and-wait):** to "encourage spontaneous chat activity" rather than spam dead rooms, only fire into a channel that saw a human message within the last ~15 minutes. Record a per-channel `lastActivityUtc` in `HandleMessage` (where channel messages are already processed). If a fire comes due while the channel is quiet, **arm the channel and hold the due slot** — do *not* discard it. The event fires a small random delay (0–`WakeDelayMaxMinutes`, kept below the 15-minute activity window) after the next human message, so it lands in a freshly-awake room without landing on the exact first message. Only once a fire actually lands does the 6–8h cadence restart. *(The original design discarded the slot and rescheduled +6–8h; that starved low-traffic channels — a room silent for many hours at a time could go a very long time without ever firing, because each ~thrice-daily attempt had to coincide with live chatter to the minute.)*
- **Fire:** pick a `RandomEvent` by `weight` (optionally filtered by category), materialize per-fire state (token/challenge), post `announceText` to the channel, and open the active event with `windowEnd = now + responseWindowSeconds`.
- **Resolve:** each tick, close any active events whose window has elapsed (for non-`firstValid` rules) and emit the outcome.
- **One at a time (B12.6):** at most one active event per channel; if one is active, a due fire is skipped (events are far enough apart that this is rare anyway).

**State location.** Active events, per-channel `nextFireUtc` / `lastActivityUtc`, and collected responders live **in memory** (like `FutureMessages`) — they reset on restart, which is fine for ambient events; after a restart, seed each eligible channel's first `nextFireUtc` at `now + random(6h,8h)` (or a small random offset) so events don't all fire at once. Only **event definitions** (the `RandomEvents` collection) and **granted rewards** (player profiles) are persisted.

---

## Persistence / DB layer

- **`RandomEvents`** collection — read-only in-bot, seeded externally (Duties pattern). Add `GetRandomEvents()` / optionally `GetRandomEventsByCategory(string)` to `Ichateaudatabase.cs` + `Chateaudatabase.cs` + `MonDB.cs`, mirroring `GetDutiesByJob`.
- No new persisted player field — rewards write through existing `currencies` / `titles` / `trainings` / corruption / curse storage via `SetProfile`.
- Test fixture gains an in-memory `RandomEvents` list.

---

## Files

**Created:**
- `FChatDicebot/Model/ChateauDB.cs` additions — `RandomEvent`, `EventOutcome`, `EventReward` (in that file alongside `Duty`).
- `FChatDicebot/BotCommands/ChateauRandom.cs` — `!random`.
- A small event-engine support class (e.g. `BotCommands/Support/RandomEventEngine.cs`) holding: in-memory channel state, fire/select/resolve logic, `ApplyEventReward`, and the per-fire token/challenge generation — so it's unit-testable away from `BotMain`.
- `FChatDicebot.Tests/Unit/Randomeventenginetests.cs`.

**Modified:**
- `FChatDicebot/SavedData/ChannelSettings.cs` — `AllowRandomEvents` (+ the settings update/display command that lists channel flags).
- `FChatDicebot/BotMain.cs` — record `lastActivityUtc` in `HandleMessage`; add a `HandleRandomEventsTick(tickMs)` call beside `HandleFutureMessagesTick` in `RunLoop`.
- `FChatDicebot/Database/Ichateaudatabase.cs` + `Chateaudatabase.cs` + `MonDB.cs` — `GetRandomEvents`.
- `FChatDicebot.Tests/Fixtures/Testdatabasefixture.cs` — in-memory RandomEvents.
- `FChatDicebot/BotCommands/ChateauHelp.cs` — added `!random` to the listing. *No longer a step: the listing is derived from each command's `Category`, so `!random` would list itself today. See [Development-Guide](../Development-Guide.md#adding-a-new-command).*
- `wiki-docs/Feature-Requests.md` — B12 bullet retires on ship.

---

## Tests

- **Selection:** weighted event pick is deterministic under a seeded RNG; category filter respected.
- **Response validation:** `keyword` accepts only the matching token (case-insensitive) and rejects others; `challenge` accepts only the correct answer; `none` accepts a bare `!random`.
- **Winner rules:** `firstValid` resolves on the first valid response; `nth` selects exactly the N-th; `allInWindow` grants every valid responder; `random` picks among valid responders (seeded); duplicate responses from one user ignored.
- **Reward application:** each `type` mutates the right store by the rolled amount (currency add; training clamp 0–100; title added once; corruption up / purity down; curse added; `none` no write); profile saved once.
- **Multi-winner resolution:** an outcome's `{winners}` placeholder substitutes every winner's tag with no reward line appended when nothing was granted; winners who receive identical reward fragments are combined onto one grouped line with the correct singular/plural verb.
- **Scheduler:** a due fire into an active channel opens an event and reschedules; a due fire into a quiet channel **arms** the channel (holds the slot, posts nothing) and, on the next message, fires after a random post-wake delay (< the activity window); the slot survives arbitrarily long silences and restarts of arming; only one active event per channel; window elapse resolves non-`firstValid` events. Operator-facing log lines are emitted on quiet-arming (once per arm) and on a due fire that finds no authored events.

---

## Decisions resolved (owner, 2026-06-21)

- **B12.1** `!random` is a **new command**; payoffs span **currency, title, training, corruption/purity, curse, or flavor**. The bot occasionally fires an event; users join with `!random {optional event-requested arg}`; the bot announces the outcome.
- **B12.2** Per-channel timer into opted-in channels, **activity-gated**, ~**once per 6–8 hours** with jitter, open response window.
- **B12.3** Anti-snipe arg is **per-event** — `keyword` or `challenge` (a bare-`!random` "none" is also available for events that don't need one). A third `delay` type — gating on elapsed time rather than a code — was considered and dropped 2026-07-01 (owner): it would penalize a genuinely fast, correct response, and the keyword/challenge already supplies all the anti-snipe value.
- **B12.4** Winner selection is **per-event** — first-come, all-in-window, exact Nth responder, or random.
- **B12.5** Events authored in a **seeded collection**.
- **B12.6** **One event at a time** per channel.

## As-built defaults (owner-reviewed 2026-07-01)

All framework strings live as `const`s on `RandomEventEngine` (search `// Framework user-facing strings`); tuning numbers are `const`s near the top of the same class.

- **Framework user-facing strings** — final wording:
  - *No active event* (`NoEventMessage`): "There's no random event for you to respond to right now. When there is, everyone will be explicitly prompted."
  - *Wrong keyword* (`WrongArgMessage`, keyword type): "That's not the word this one's looking for. Try again!"
  - *Wrong answer* (`WrongArgMessage`, challenge type): "That's not quite the answer this one's looking for. Give it another read and try again!"
  - *Already responded* (`AlreadyRespondedMessage`): "You can't participate twice! Wait for next event."
  - *Accepted, waiting* (`AcceptedWaitMessage`, PM'd, non-firstValid): "You're in! We'll announce the results once everyone has had some time to join."
  - *No winner / window closed* (`NoWinnerMessage`): "Time's up for now. Until next time~"
  - *Auto-appended anti-snipe hint* (when `announceText` omits the `{keyword}`/`{challenge}` placeholder): `[sub]Quick, [b]!random rose[/b] to take part![/sub]`
  - *Win line*: `[user]Name[/user] receives [b]5 rosequartz[/b] and the title ·Lucky·!` for a single winner. Beyond this, all other resolution text is event-authored — see *Multi-winner resolution* below.
- **Tuning numbers** (all `const` on `RandomEventEngine`): jitter `IntervalMinHours=6`/`IntervalMaxHours=8`; `ActivityWindowMinutes=15`; `WakeDelayMaxMinutes=10` (post-wake hold for an armed channel — **must stay below `ActivityWindowMinutes`** so a lone wake message still counts as active when the delay elapses); `DefaultResponseWindowSeconds=60` (used only when an event leaves `responseWindowSeconds` at 0). Per-reward magnitude ranges are authored per-event (engine just rolls `min..max` inclusive). Scheduler scan throttle `BotMain.RandomEventScanIntervalSeconds=10`.
- **Invalid `!random` does NOT consume the attempt** — a wrong keyword/answer gets a PM nudge and the resident can retry immediately. There is no time-based rejection (see the dropped `delay` type above): the anti-snipe value is entirely in needing the right token/answer, so a genuinely quick, correct response should never be filtered out.
- **Pending accepts are PM'd, not announced** — only the fire announcement and the resolution are public; accept/nudge/no-event replies go to the responder privately, keeping the channel clean (still TOS-safe since `!random` is user-invoked).
- **Multi-winner resolution.** One outcome is rolled per event; its `resultText` is the header, with an optional `{winners}` placeholder substituted with every winner's `[user]` tag (joined "A, B and C") — this is how an author gives a multi-winner outcome its own combined flavor (e.g. `"{winners} {is|are} now glowing purple."`) instead of the engine inventing a generic per-person line.
- **Count agreement (`{singular|plural}`).** `allInWindow` is the only winner rule whose count varies — the other three resolve to exactly one winner by construction — so authored plural grammar used to strand at a single winner ("Fia **are all** officially cuties"). `RandomEventEngine.ResolveCountAgreement` resolves `{singular|plural}` alternations against the winner count (one winner → left branch, any other count → right) and runs **before** `{winners}` substitution, so names never interact with it. A `{…}` with no `|` is passed through untouched, which is what keeps `{winners}` / `{keyword}` / `{challenge}` / `{window}` and every pre-existing authored string safe. No nesting, no escaping, `resultText` only — `announceText` fires before anyone has responded. See [Winner-Count-Agreement](Winner-Count-Agreement.md). Each reward is then re-rolled and applied per winner (so `allInWindow` winners can receive varying amounts); winners who ended up with byte-identical reward fragments are grouped onto one line each (`"[user]Alice[/user] and [user]Bob[/user] receive [b]3 rosequartz[/b]!"`, singular "receives" for one winner); a winner granted nothing (a `none` reward, an already-maxed stat, a duplicate title, etc.) gets no reward line — the `{winners}` header is expected to already cover them.
- **Category-based selection: NOT wired into the scheduler** — it weights across *all* events. `GetRandomEventsByCategory` exists (mirrors `GetDutiesByCategory`) for future seasonal/themed pools.
- **`nextFireUtc` stays in-memory** — resets on restart; each eligible channel re-seeds its first fire at `now + random(6–8h)` on the first tick, so events don't all fire at once after a restart.

## Seed data

One starter event, **"Cutie says {word}"**, has been authored (owner inserted the document directly into the `RandomEvents` collection; it isn't tracked in this repo since events are pure Mongo data, not code). It's a `keyword`/`allInWindow` event: every resident who repeats Cutie's randomly-chosen word back within the window is granted the `Cutie` title, using the `{winners}` placeholder to greet everyone who caught it in one line. Authoring more events is tracked as a to-do (see `wiki-docs/Feature-Requests.md`) — write additional `RandomEvent` documents in the same shape; the scheduler picks among all of them by `weight` automatically, no code change needed.

## Winner conditions and the `invert` reward (shipped 2026-08-30)

Owner-specced 2026-08-30 in response to "a random event with a reward that inverts the corruption/purity of the winner — and while we're at it, make the system sensitive to variables of the winner."

### `invert`

A reward type that mirrors the winner's whole signed corruption value: `value → -value`. `key`, `min` and `max` are unused — it is **always the full flip** (owner's call), which is why it dwarfs the `DailyMagnitudeLimit` quota that gates the player-driven `!corrupt` / `!purify`. Like the other system-granted rewards it bypasses the consent flow; the player opted in by responding.

Degenerate cases fall out of the existing machinery rather than needing special handling: a winner sitting at exactly 0 has nothing to mirror, so the reward returns an empty fragment and that winner simply gets no line — the outcome's `{winners}` header still covers them. `int.MinValue` is refused rather than overflowing `Math.Abs`.

**`invert` is the first *self-verbed* reward** (`RandomEventEngine.IsSelfVerbedReward`). Every other reward is a noun phrase the engine hangs off its shared verb — "Alice receives 5 rosequartz and the title ·Lucky·!" — but an inversion is not *received*, it happens to what the winner already had. So its fragment is a whole predicate and it is rendered on its own line:

```
[user]Alice[/user] now has [b]27 purity[/b], inverted from [b]27 corruption[/b]!
```

The `{has|have}` alternation is resolved by the same `ResolveCountAgreement` that handles authored `resultText`, against that line's winner count — so winners who happened to hold the same magnitude group onto one line and read "now have". Winners with different magnitudes produce different predicates and therefore don't group, each getting their own truthful line. An outcome that mixes a received reward with an inversion emits both line shapes rather than joining them (which would put two verbs in one clause).

Reward-line grouping keys on the noun fragments **and** the predicates together, so the two shapes can never cross-merge.

### `EventCondition` — one idea, no operator vocabulary

`BotCommands/Support/EventConditionSupport.cs` is authoritative. **Every stat projects a winner's profile down to a single integer, and a condition is an inclusive range on that integer** (`min` / `max`, each independently nullable = unbounded). There is no `atLeast` / `atMost` / `has` / `lacks` vocabulary to learn.

> **Why not the duty `Conditional`?** That shape packs its kind into a three-letter prefix (`curgold`, `trndeepthroat`) and only ever compares "at least". Corruption is stored **signed** — negative is corrupt, positive is pure — so expressing "the corrupted" requires an upper bound on a negative number, which the duty conditional cannot say at all. The two systems are deliberately separate; converging them is a possible follow-up, not part of this change.

Corruption therefore reads naturally in all three directions:

| Intent | Condition |
|---|---|
| the corrupted | `{stat: "corruption", max: -10}` |
| the pure | `{stat: "corruption", min: 10}` |
| the untouched middle | `{stat: "corruption", min: -9, max: 9}` |

**Stat vocabulary** (`EventConditionSupport.Stats` is the source of truth): `corruption`, `currency`, `training`, `job`, `count`, `title`, `curse`, `parasite`, `vice`, `pregnancy`, `collectible`.

**Key semantics are per stat, and consistently shaped:**
- The **dictionary-backed** stats (`currency` / `training` / `job` / `count`) **require** a key — there is no meaningful total across all of them. A key that isn't held projects to 0, so "the broke" is authorable as `max: 0`.
- The **list-backed** stats (`title` / `curse` / `parasite` / `vice` / `pregnancy` / `collectible`) take an **optional** key: blank counts how many they hold at all, a named key narrows to that one. A "titles held" count therefore needs no separate stat — it is `{stat: "title"}` with no key.
- `corruption` ignores the key entirely.
- `vice` is the one asymmetry: a **named** vice projects its `AddictionLevel` (1–10, 0 when absent) rather than a plain 0/1, because that is the number an author actually wants to gate on. `min: 1` still reads as "has this vice".
- `pregnancy` counts **active** pregnancies only — `!birth` removes them from the list.

**Failure direction is deliberate.** An unknown stat, or a blank key where one is required, makes the condition **fail** rather than pass. A typo kills the branch it was written on and the winner falls through to a catch-all; the alternative would silently hand out the wrong branch.

### What conditions do to resolution

Authoring **any** condition on **any** outcome switches that event from one shared outcome roll to a **per-winner** roll. `RandomEventEngine.ResolveLocked` is authoritative:

- **No conditions anywhere** — one outcome is rolled and shared by every winner, exactly as before this change. Every event authored before conditions existed keeps its original behavior; a stored document with no `conditions` key deserializes to `null`, which is unconditional.
- **Any condition present** — each winner's outcome is rolled among the outcomes *that winner* qualifies for. Winners are then grouped by the outcome they landed on (by reference, so two structurally identical outcomes stay distinct blocks), and each group gets its **own header block** — `{singular|plural}` count agreement resolves against **that block's** winner count, not the event total, and `{winners}` substitutes only that block's names. Reward-line grouping is scoped inside the block, so winners under different headers can never be merged onto one sentence.

Since `firstValid` / `nth` / `random` resolve to exactly one winner by construction, only `allInWindow` can actually produce a multi-block announcement.

**Conditions filter the table; they do not replace the weighted roll.** A winner who qualifies for both a gated outcome and an unconditional catch-all can land on either — which is what lets an author mix "a rare thing that can happen to anyone" with state-specific branches. **To branch deterministically, put a condition on every outcome so they partition the range** (`≤ -10` / `-9…9` / `≥ 10`). The builder surfaces this as a warning.

Conditions are evaluated against the winner's state **before** this event grants anything, which is precisely what lets the invert outcome gate on the very stat it is about to flip.

**A winner who qualifies for nothing is omitted from the announcement.** If *no* winner lands on any outcome, the event closes with the existing `NoWinnerMessage` and logs why (operator-facing only) — posting nothing after residents responded reads as the bot having broken. Authors are expected to keep a catch-all or a total partition; the builder warns when neither is present.

### Authoring

The builder (`scripts/random-event-builder`) gained a conditions editor per outcome, `invert` in the reward dropdown, gate-aware resolve captions, and warnings for the failure modes above. Its `ConditionStats` / `ConditionStatsNeedingKey` / `RewardTypes` arrays in `server.cs` mirror the engine and must be re-synced if the vocabulary grows.

No migration: `conditions` is `[BsonIgnoreIfNull]`, the builder omits it entirely for an unconditional outcome, and an omitted bound stays omitted rather than being stored as 0.

### Files

- `FChatDicebot/BotCommands/Support/EventConditionSupport.cs` (new) — stat projection + range test.
- `FChatDicebot/Model/ChateauDB.cs` — `EventCondition`; `EventOutcome.conditions`.
- `FChatDicebot/BotCommands/Support/RandomEventEngine.cs` — `invert` branch in `ApplyEventReward`, `SelectOutcomeFor`, per-winner grouping in `ResolveLocked`.
- `FChatDicebot.Tests/Unit/Eventconditionsupporttests.cs` (new), additions to `Randomeventenginetests.cs`.
- `scripts/random-event-builder/server.cs` + `ui.html` + `README.md`.

## Assumptions

- Channel broadcasts are TOS-safe (already how the bot posts); only PMs to non-invoking users are restricted, and events don't do that.
- Reuse of `currencies` / `titles` / `trainings` / corruption / curse mechanisms matches their current public entry points; system-granted corruption/curse intentionally bypass the consent flow (player opted in by responding).
- The heartbeat (`RunLoop` + tick handlers) and `ChannelsJoined` / `ChannelSettings` are the right hooks for scheduling.
- F-Chat output constraints match existing outputs (4096-char cap, BBCode, `[spoiler]` fallback if long).
