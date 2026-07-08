# Random Events — ambient channel events + `!random`

**Status:** ✅ SHIPPED 2026-06-29, wording pass + design tweak 2026-07-01 (owner review). Per-event `announceText` / `resultText` and reward magnitudes are authored data in the seeded `RandomEvents` collection; a single starter event ("Cutie says {word}") has been authored — see *Seed data* below.
**Feature-Requests source:** "Random events, to encourage spontaneous chat activity. Responding to a random event would always start !random but might also require an additional argument, to slow down campers/snipers" (B12).

> **Note on paths:** this spec was written before the latest Dice Bot integration flattened `FChatDicebot/FChatDicebot/…` to `FChatDicebot/…`. The links below predate that move; the as-built files are: model in [Model/ChateauDB.cs](../../../FChatDicebot/Model/ChateauDB.cs), engine in [BotCommands/Support/RandomEventEngine.cs](../../../FChatDicebot/BotCommands/Support/RandomEventEngine.cs), command in [BotCommands/ChateauRandom.cs](../../../FChatDicebot/BotCommands/ChateauRandom.cs), scheduler in [BotMain.cs](../../../FChatDicebot/BotMain.cs) (`HandleRandomEventsTick`), DB in [Database/Chateaudatabase.cs](../../../FChatDicebot/Database/Chateaudatabase.cs), tests in `FChatDicebot.Tests/Unit/Randomeventenginetests.cs`. Discord is intentionally excluded — the tick is wired into `RunLoopFList` only (the deployment is F-Chat / Chateau-only).

> This is the largest of the Social/events specs. It has three buildable chunks that can land incrementally: **(1)** the event data model + seeded collection, **(2)** the `!random` participation command + active-event lifecycle, **(3)** the tick-driven scheduler. The bulk of the work is wiring the **six reward types** to existing grant mechanisms (below).

---

## Overview

Once every several hours, the bot fires a **random event** into an opted-in channel — a short in-character prompt posted to chat. Residents join with the new **`!random`** command (optionally with an extra argument the event demands, to thwart snipers). After the event's response window resolves, the bot announces the outcome and grants the reward(s): currency, a title, training, corruption/purity, a curse, or just flavor.

Key properties:
- Events are **data, not code** — authored in a seeded `RandomEvents` Mongo collection (the [Duties](../../../FChatDicebot/Database/Chateaudatabase.cs)/`ModMessages` pattern: read-only in-bot, authored externally), so new events ship without a deploy.
- Firing is **channel broadcast** (TOS-safe — the bot already posts freely to channels it's in; the TOS constraint is only about PMing users who didn't invoke). `!random` is user-invoked, so all replies to participants are TOS-safe.
- Existing infrastructure does most of the heavy lifting: the bot already has a heartbeat (`RunLoop` ticking every `TickTimeMiliseconds`) and a delayed-message mechanism (`SendFutureMessage` / [`HandleFutureMessagesTick`](../../../FChatDicebot/BotMain.cs)). The scheduler is a sibling tick handler.

---

## Event data model

Mirrors the existing `Duty` / `DutyResult` / `Reward` shapes ([ChateauDB.cs:187-221](../../../FChatDicebot/Model/ChateauDB.cs)) so authoring feels familiar.

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
    public List<EventReward> rewards { get; set; } = new List<EventReward>(); // applied to winner(s)
}

public class EventReward
{
    public string type { get; set; } // "currency" | "title" | "training" | "corruption" | "purity" | "curse" | "none"
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
| `currency` | `profile.currencies[key] += roll(min,max)` — the [ChateauWork](../../../FChatDicebot/BotCommands/ChateauWork.cs) credit pattern (denominations independent, per `ChateauCurrency`). |
| `title` | award `Title` `key` into `profile.titles` — reuse the title-grant path used by the interaction processors / `!entitle`. |
| `training` | `profile.trainings[key]` += `roll(min,max)`, clamped 0–100 ([TrainProcessor](../../../FChatDicebot/InteractionProcessors)). |
| `corruption` | apply magnitude `roll(min,max)` via the existing [CorruptionProcessor / CorruptionCommandSupport](../../../FChatDicebot/InteractionProcessors/Commitment/CorruptionCommandSupport.cs) path. |
| `purity` | the purify/cleanse side of the corruption axis (reduce corruption) — reuse the same support. |
| `curse` | add curse `key` via [CurseInstance / CurseProcessor](../../../FChatDicebot/Model/CurseInstance.cs). |
| `none` | flavor-only; no profile write. |

> Wiring these six paths cleanly (and writing the winner's profile once via `SetProfile` after all rewards apply) is the main implementation effort. A small `ApplyEventReward(Profile, EventReward)` dispatcher keeps it testable. Self-consistency note: corruption/curse here are **system-granted**, so they bypass the consent flow real interactions require — that's intended for an event the player opted into by responding, but call it out to the owner.

---

## The `!random` command

New `ChatBotCommand` [ChateauRandom.cs](../../../FChatDicebot/BotCommands/ChateauRandom.cs). `Name="random"` — collides with nothing in the dispatch table.

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

- **Opt-in:** new `ChannelSettings.AllowRandomEvents` flag (default `false`), matching the existing per-channel toggles (`AllowWork`, `AllowGames`, …) in [ChannelSettings.cs](../../../FChatDicebot/SavedData/ChannelSettings.cs). Only opted-in joined channels are eligible.
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

## Files to create / modify

**Create:**
- `FChatDicebot/Model/ChateauDB.cs` additions — `RandomEvent`, `EventOutcome`, `EventReward` (in that file alongside `Duty`).
- `FChatDicebot/BotCommands/ChateauRandom.cs` — `!random`.
- A small event-engine support class (e.g. `BotCommands/Support/RandomEventEngine.cs`) holding: in-memory channel state, fire/select/resolve logic, `ApplyEventReward`, and the per-fire token/challenge generation — so it's unit-testable away from `BotMain`.
- `FChatDicebot.Tests/Unit/Randomeventenginetests.cs`.

**Modify:**
- `FChatDicebot/SavedData/ChannelSettings.cs` — `AllowRandomEvents` (+ the settings update/display command that lists channel flags).
- `FChatDicebot/BotMain.cs` — record `lastActivityUtc` in `HandleMessage`; add a `HandleRandomEventsTick(tickMs)` call beside `HandleFutureMessagesTick` in `RunLoop`.
- `FChatDicebot/Database/Ichateaudatabase.cs` + `Chateaudatabase.cs` + `MonDB.cs` — `GetRandomEvents`.
- `FChatDicebot.Tests/Fixtures/Testdatabasefixture.cs` — in-memory RandomEvents.
- `FChatDicebot/BotCommands/ChateauHelp.cs` — register `!random`.
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
- **Multi-winner resolution.** One outcome is rolled per event; its `resultText` is the header, with an optional `{winners}` placeholder substituted with every winner's `[user]` tag (joined "A, B and C") — this is how an author gives a multi-winner outcome its own combined flavor (e.g. `"{winners} are now glowing purple."`) instead of the engine inventing a generic per-person line. Each reward is then re-rolled and applied per winner (so `allInWindow` winners can receive varying amounts); winners who ended up with byte-identical reward fragments are grouped onto one line each (`"[user]Alice[/user] and [user]Bob[/user] receive [b]3 rosequartz[/b]!"`, singular "receives" for one winner); a winner granted nothing (a `none` reward, an already-maxed stat, a duplicate title, etc.) gets no reward line — the `{winners}` header is expected to already cover them.
- **Category-based selection: NOT wired into the scheduler** — it weights across *all* events. `GetRandomEventsByCategory` exists (mirrors `GetDutiesByCategory`) for future seasonal/themed pools.
- **`nextFireUtc` stays in-memory** — resets on restart; each eligible channel re-seeds its first fire at `now + random(6–8h)` on the first tick, so events don't all fire at once after a restart.

## Seed data

One starter event, **"Cutie says {word}"**, has been authored (owner inserted the document directly into the `RandomEvents` collection; it isn't tracked in this repo since events are pure Mongo data, not code). It's a `keyword`/`allInWindow` event: every resident who repeats Cutie's randomly-chosen word back within the window is granted the `Cutie` title, using the `{winners}` placeholder to greet everyone who caught it in one line. Authoring more events is tracked as a to-do (see `wiki-docs/Feature-Requests.md`) — write additional `RandomEvent` documents in the same shape; the scheduler picks among all of them by `weight` automatically, no code change needed.

## Assumptions

- Channel broadcasts are TOS-safe (already how the bot posts); only PMs to non-invoking users are restricted, and events don't do that.
- Reuse of `currencies` / `titles` / `trainings` / corruption / curse mechanisms matches their current public entry points; system-granted corruption/curse intentionally bypass the consent flow (player opted in by responding).
- The heartbeat (`RunLoop` + tick handlers) and `ChannelsJoined` / `ChannelSettings` are the right hooks for scheduling.
- F-Chat output constraints match existing outputs (4096-char cap, BBCode, `[spoiler]` fallback if long).
