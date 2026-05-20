# `!climaxfor` + `!climax`

Two commands backed by a single processor. `!climaxfor` reads as "I climax (for/with you)"; `!climax` is the inverted reskin that reads as "I make you climax". The verbs route the same logic to opposite role assignments.

**Status:** Implemented.
**Investment level:** Involved.
**Reversal:** none.
**Depends on:** [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md). Interacts with future [Dose-and-Detox](Future-Interactions/Dose-and-Detox.md) (vice cravings) once that lands.

## Command syntax

```
!climaxfor                          → self-targeted (initiator climaxes alone)
!climaxfor [user]Bob[/user]         → initiator climaxes for/with Bob
!climax                             → self-targeted (alias for !climaxfor with no args)
!climax    [user]Bob[/user]         → initiator makes Bob climax (Bob is the climaxer)
```

If args are present but no `[user]…[/user]` recipient is found, both commands fall back to the standard `ChateauInteractionHandler.notFoundText` private rejection — same wording every other Chateau command uses ("The user … could not be found … make sure you have a [user] tag!").

## Who's the climaxer?

| Command | Recipient | Climaxer | Partner |
|---------|-----------|----------|---------|
| `!climaxfor` (bare or self-target) | initiator | initiator | (none) |
| `!climaxfor [user]Bob[/user]` | Bob | initiator | Bob |
| `!climax` (bare or self-target) | initiator | initiator | (none) |
| `!climax [user]Bob[/user]` | Bob | **Bob** | initiator |

`ClimaxforProcessor.ResolveClimaxer(type, initiator, recipient)` is the single source of truth; everything downstream (count routing, daily-counter bump, completion-message wording, status-effect attribution) reads from it.

## Validation

- Both parties must be registered (base validation).
- Self-target is allowed at the **command** layer but explicitly **rejected** by `ClimaxforProcessor.ValidateInteraction` as a defensive guard — self-targets bypass the consent flow via `PerformSelfTarget` and must never enter the consent-driven `ProcessInteraction` path.
- Status-effect hook gates both verbs automatically: any recipient-side blocker from a future contributor will gate them without code changes here.

## Self-target shortcut

`ChateauClimaxfor` and `ChateauClimax` both detect self-target (no recipient parsed, or `[user]<self>[/user]`) and call `ClimaxforProcessor.PerformSelfTarget(initiator, typedVerb)` directly — no `PendingCommand`, no consent prompt. The processor:

1. Bumps the initiator's `dailyClimaxCounts[today]` by one (pruning entries older than 30 days).
2. Saves an `Interaction` with `type` = the typed verb (so analytics can tell which alias the user typed).
3. `SetProfile` to persist the daily counter.
4. `IncrementCountWithRateLimit(initiator, "climaxtake", 30 min)` — only the climaxer's `climaxtake` increments; no `climaxgive` since there's no partner.
5. Returns the channel completion message; the command layer appends any newly-earned title text from `ChateauSystemTitles.CheckAndGrantTitles` before emitting.

## Other-target (consent flow)

`ChateauClimaxfor` and `ChateauClimax` build a `PendingCommand` with `type = typedVerb` and a placeholder identifier `"{typedVerb}|0"`. `ChateauConsent` calls `ProcessInteraction`, which:

1. Reads the climaxer from the typed verb (`ResolveClimaxer`).
2. Bumps the climaxer's `dailyClimaxCounts[today]` (with the same 30-day prune).
3. Overwrites the identifier with `"{typedVerb}|{newDailyCount}"` so `GetCompletionMessage` can render the right wording AND read the post-bump count for flavor selection.
4. `AddInteraction`; `SetProfile(climaxer)` *before* the count-helper writes (read-modify-write ordering trap shared with `MilkProcessor`).
5. `IncrementDifferentCountsWithRateLimit(initiator, recipient, …, …, 30 min)` with the climaxer's label set to `climaxtake` and the partner's to `climaxgive` regardless of which side they happen to be on.
6. Deletes the pending command.

`ChateauConsent.GetCountKeys` knows both type keys so the post-process rate-limit message text picks the right (give/take) pair.

## Counts

| Role | Count key | Existing title milestones |
|------|-----------|---------------------------|
| Partner (helped the climaxer) | `climaxgive` | Generous Lover (5), Cum Collector (10), CliMAX (25) |
| Climaxer (had the orgasm) | `climaxtake` | Drank The Juice (1), Cum Again? (5), Came Buckets (10), Keeps On Cumming (25) |

(The original spec proposed `First Climax` at 1 and `Climax Connoisseur` at 100, but the user opted to preserve the existing climaxtake titles unchanged.)

The give/take convention is from the **climaxer's** perspective, not the project-wide initiator/recipient convention — the user picked this so the `climaxtake` titles read naturally for the person actually orgasming (the existing "Came Buckets" / "Keeps On Cumming" titles imply the title-holder is the climaxer).

## Daily-streak titles

Tracked off `Profile.dailyClimaxCounts` (a sparse `Dictionary<string,int>` keyed `"yyyy-MM-dd"`). `ChateauSystemTitles.CheckDailyClimaxTitles` awards:

- `·Rule of Three·` — 3 climaxes in one Chateau day.
- `·Can Go All Night·` — 5 in one Chateau day.
- `·Inhuman Stamina·` — 10 in one Chateau day.

Each title is awarded once-ever; reaching the threshold again on a future day surfaces flavor in the completion message but does not re-award.

Daily entries older than `ClimaxforProcessor.DailyCountRetentionDays` (30) are pruned on the next write so the map stays bounded.

## Completion message

Three opening templates, picked by `(typeKey, isSelf)`:

| Shape | Template |
|-------|----------|
| Self-target (either verb) | `"{init} makes a mess of themselves. {flavor}"` |
| `!climaxfor` other-target | `"{init} shudders in ecstasy for {rec}. {flavor}"` |
| `!climax` other-target | `"{init} brings {rec} to a pleasure filled climax. {flavor}"` |

Flavor descriptor pool is keyed by the climaxer's today count. Counts 1, 2, 3, and 4 each get their own dedicated 3-item pool; 5–9 share a "we lost count" tier; 10+ ratchets to the "is this a record?" tier. Examples:

- Count 1: `The first is always such sweet release.` · `May there be many more to cum~` · `What a display!`
- Count 4: `The fourth is when you start to lose count.` · `Careful you don't lose yourself...` · `Don't [i]four[/i]ce it too hard!`
- Counts 5–9 (Tireless tier): `How many is this now? We lost count.` · `Clean up on aisle sex!` · `Cum for the Cum god!` · `It just keeps going and going and going...` (9 total)
- Counts 10+ (Inhuman tier): `Is that a record?` · `One must imagine Sisyphus happy.` · `Eat, Cum, Live.` · `Don't forget to hydrate.` · `Cumming cumming and cumming cumming and...` (10 total)

**Volume language stays constant across counts** — no "diminishing" / "dribble" / "running dry" descriptors. The regression test enforces this.

After the opening sentence, status-effect contributors (currently only `CorruptionStatusContributor`) decorate the message via `GetActiveStatusEffects` keyed on the **climaxer's** profile — their corruption / vice state (once `!dose` lands) drives the appendix. This is per spec: the flavor attaches to the person actually having the orgasm, not whoever typed the command.

## Consent warning

| Verb | Template |
|------|----------|
| `!climaxfor` | `"{init} is about to climax for {rec}. Do you !consent to being responsible for the mess?"` |
| `!climax` | `"{init} is ready to make {rec} climax. Do you !consent to being made a mess of?"` |

The command layer passes the typed verb into `processor.GetConsentWarning(…, typeKey)`. The base 3-arg `GetConsentWarning` override defaults to the `climaxfor` wording for any caller that doesn't pass the typed verb.

## Persistence

- `Profile.dailyClimaxCounts` — `Dictionary<string,int>`, key `"yyyy-MM-dd"` (UTC), value = climax count that day. `[BsonIgnoreIfNull]`. Pruned to last 30 days on each write.
- `counts["climaxgive"]` / `counts["climaxtake"]` — standard count fields, populated via the rate-limited helpers.
- `Interaction` records — `type` is the typed verb (`climaxfor` or `climax`); `identifier` is the composite `"{typeKey}|{dailyCount}"`.

## Tests

`Climaxforprocessortests.cs` covers:

- `ResolveClimaxer` / `ResolvePartner` for all four (type × self/other) shapes.
- Validation: self-target rejected; missing initiator/recipient rejected; both-exist succeeds.
- `PerformSelfTarget` bumps `climaxtake` only (no `climaxgive`); bumps today's daily count; persists the interaction with the typed verb; returns "climaxes alone" wording.
- `ProcessInteraction` for both verbs: correct climaxer gets `climaxtake`, correct partner gets `climaxgive`; daily count goes only to the climaxer; pending command deleted; identifier carries the type + count.
- `IncrementDailyClimaxCount`: prunes entries older than the retention window; keeps recent entries intact; accumulates across calls.
- Completion message templates for self / climaxfor / climax shapes.
- Volume-language regression: 50 samples in the high-count branch never produce "diminishing" / "dribble" / "smaller" / "less and less" / "running dry".
- Inhuman threshold appends the "[sub]…just keeps going[/sub]" amazement flavor; lower counts do not.
- Consent warning wording differs by verb.
- Identifier round-trip; sentinel defaults for empty / bare identifiers.

## Files

- `FChatDicebot/InteractionProcessors/Involved/ClimaxforProcessor.cs` *(new)*
- `FChatDicebot/InteractionProcessors/Involved/ClimaxCommandSupport.cs` *(new, shared command-side wiring)*
- `FChatDicebot/BotCommands/ChateauClimaxfor.cs` *(new)*
- `FChatDicebot/BotCommands/ChateauClimax.cs` *(new)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` — single instance registered under both type keys.
- `FChatDicebot/Model/ChateauDB.cs` — `Profile.dailyClimaxCounts`.
- `FChatDicebot/BotCommands/ChateauSystemTitles.cs` — climax total-count titles + new `CheckDailyClimaxTitles` for the daily-streak titles.
- `FChatDicebot/BotCommands/ChateauConsent.cs` — `GetCountKeys` mapping for both type keys.
- `FChatDicebot.Tests/Builders/Profilebuilder.cs` — `WithDailyClimaxCount` helper.
- `FChatDicebot.Tests/Unit/Climaxforprocessortests.cs` *(new)*

## Assumptions / overrides

- Daily reset is the Chateau-day boundary (UTC midnight). Override by swapping the key format / cutoff math in `IncrementDailyClimaxCount` and `CheckDailyClimaxTitles`.
- Daily-streak thresholds are `const int` on `ClimaxforProcessor` (Rule of Three 3 / Can Go All Night 5 / Inhuman Stamina 10) and duplicated on `ChateauSystemTitles`. If they're ever tuned in production, lift to MongoDB.
- 30-day retention on `dailyClimaxCounts` is `ClimaxforProcessor.DailyCountRetentionDays`. Larger windows just grow the per-profile map.
- Bare `!climax` (no args) aliases to `!climaxfor` self-target. The role inversion is meaningless with one party; the typed verb is still recorded on the interaction history for analytics.
- The original spec's "Self-target counts toward `climaxgive` only" assumption was **overridden** at the user's direction: self-target credits `climaxtake` only (the climaxer always gets `climaxtake`), and there is no `climaxgive` because there's no partner.
