# Random Events — ambient channel events + `!random`

**Status:** Proposed (design-complete, owner-reviewed 2026-06-21).
**Feature-Requests source:** "Random events, to encourage spontaneous chat activity. Responding to a random event would always start !random but might also require an additional argument, to slow down campers/snipers" (B12).

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
    public string responseType { get; set; }      // "none" | "keyword" | "challenge" | "delay"
    public int responseWindowSeconds { get; set; } // how long !random is accepted (e.g. 60)
    public string winnerRule { get; set; }         // "firstValid" | "allInWindow" | "nth" | "random"
    public int winnerN { get; set; }               // for "nth": which responder wins (e.g. 4)
    public List<EventOutcome> outcomes { get; set; } = new List<EventOutcome>(); // weighted roll
}

public class EventOutcome
{
    public int weight { get; set; }       // weighted pick among the event's outcomes
    public string resultText { get; set; } // announced when this outcome is granted
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
- `challenge` — a tiny generated problem (e.g. a small sum or a die to "roll"); the user submits the answer as the arg. Generated per-fire; expected answer held in active-event state.
- `delay` — `!random` with no arg, but responses are only accepted after a randomized minimum delay from the fire (held in active-event state); earlier `!random`s are ignored (silently or with a "too eager" nudge), slowing campers.

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
1. No active event in this channel → short reply, e.g. *(owner review)* `"There's nothing happening right now. Stick around — something might."`
2. Active event → validate per `responseType` (token/answer match; delay elapsed); on invalid, a brief nudge (wrong/early) without consuming their one shot, or consume it — owner's call (see open items).
3. Record the responder once (ignore duplicate `!random` from the same user in the same event).
4. Resolve per `winnerRule`: `firstValid` resolves immediately; the others accumulate and resolve when the window closes (handled by the scheduler tick).
5. On resolution, post the chosen outcome's `resultText` + a generated reward summary to the channel, and grant rewards to the winner(s).

---

## Scheduling (B12.2)

Per-channel opt-in + a tick handler parallel to `HandleFutureMessagesTick`:

- **Opt-in:** new `ChannelSettings.AllowRandomEvents` flag (default `false`), matching the existing per-channel toggles (`AllowWork`, `AllowGames`, …) in [ChannelSettings.cs](../../../FChatDicebot/SavedData/ChannelSettings.cs). Only opted-in joined channels are eligible.
- **Cadence:** about **one event per 6–8 hours per channel**, with jitter. Track a per-channel `nextFireUtc`; when reached, attempt a fire, then schedule the next at `now + random(6h, 8h)`.
- **Activity gate:** to "encourage spontaneous chat activity" rather than spam dead rooms, only fire if the channel saw a human message within the last ~15 minutes. Record a per-channel `lastActivityUtc` in `HandleMessage` (where channel messages are already processed). If a fire is due but the channel is quiet, **skip and reschedule** (don't fire into an empty room).
- **Fire:** pick a `RandomEvent` by `weight` (optionally filtered by category), materialize per-fire state (token/challenge/delay), post `announceText` to the channel, and open the active event with `windowEnd = now + responseWindowSeconds`.
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
- **Response validation:** `keyword` accepts only the matching token (case-insensitive) and rejects others; `challenge` accepts only the correct answer; `delay` rejects responses before the min delay and accepts after; `none` accepts a bare `!random`.
- **Winner rules:** `firstValid` resolves on the first valid response; `nth` selects exactly the N-th; `allInWindow` grants every valid responder; `random` picks among valid responders (seeded); duplicate responses from one user ignored.
- **Reward application:** each `type` mutates the right store by the rolled amount (currency add; training clamp 0–100; title added once; corruption up / purity down; curse added; `none` no write); profile saved once.
- **Scheduler:** a due fire into an active channel opens an event and reschedules; a due fire into a quiet channel skips + reschedules without posting; only one active event per channel; window elapse resolves non-`firstValid` events.

---

## Decisions resolved (owner, 2026-06-21)

- **B12.1** `!random` is a **new command**; payoffs span **currency, title, training, corruption/purity, curse, or flavor**. The bot occasionally fires an event; users join with `!random {optional event-requested arg}`; the bot announces the outcome.
- **B12.2** Per-channel timer into opted-in channels, **activity-gated**, ~**once per 6–8 hours** with jitter, open response window.
- **B12.3** Anti-snipe arg is **per-event** — `keyword`, `challenge`, or `delay`.
- **B12.4** Winner selection is **per-event** — first-come, all-in-window, exact Nth responder, or random.
- **B12.5** Events authored in a **seeded collection**.
- **B12.6** **One event at a time** per channel.

## Open items

- All **framework** user-facing strings (no-event reply, too-early/wrong-answer nudge, window-closed/no-winner, generic win confirmation, reward-summary phrasing) pending owner wording approval. Per-event `announceText` / `resultText` are authored data, not code.
- Exact tuning numbers: jitter bounds within 6–8h, activity-gate window (~15 min?), default `responseWindowSeconds`, per-reward magnitude ranges — owner to set when authoring events.
- Whether an invalid/early `!random` **consumes** the user's one attempt or not.
- Category-based event selection (seasonal/themed pools) — schema supports it; wire if wanted.
- Whether to persist `nextFireUtc` across restarts (spec keeps it in-memory for simplicity).

## Assumptions

- Channel broadcasts are TOS-safe (already how the bot posts); only PMs to non-invoking users are restricted, and events don't do that.
- Reuse of `currencies` / `titles` / `trainings` / corruption / curse mechanisms matches their current public entry points; system-granted corruption/curse intentionally bypass the consent flow (player opted in by responding).
- The heartbeat (`RunLoop` + tick handlers) and `ChannelsJoined` / `ChannelSettings` are the right hooks for scheduling.
- F-Chat output constraints match existing outputs (4096-char cap, BBCode, `[spoiler]` fallback if long).
