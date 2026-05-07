# `!climaxfor`

Cum with or for someone (or yourself). Mostly flavor, but interacts with [Dose-and-Detox](Dose-and-Detox.md) (vice cravings) and unlocks titles for daily streaks/multiples.

**Investment level:** Involved
**Reversal:** none
**Depends on:** [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md)

## Command syntax

```
!climaxfor                        → self-targeted (default)
!climaxfor [user]Bob[/user]       → climax for/with Bob
```

If args are present but no proper `[user]…[/user]` recipient is found:

```
!climaxfor someone               → REJECT, malformed
```

→ "Couldn't find a recipient in that command. Use `[user]Name[/user]` to climax for someone, or `!climaxfor` alone to climax for yourself."

## Validation

- Self-targeting allowed.
- If `terms.length > 0` and no recipient resolves, error and stop. Do **not** silently fall back to self.
- Recipient must be registered (base validation).

## Processor logic (`ClimaxforProcessor`)

1. `IncrementBothCountsWithRateLimit(initiator, recipient, "climaxfor", RateLimit)` (30 min).
2. If self-target, only increment for initiator (don't double-count).
3. Track **today's count** for the initiator on a `dailyClimax` field — see Persistence.
4. Apply title checks (see Titles).
5. Save the interaction record. Delete pending command.

## Persistence

On `Profile`:

```csharp
public Dictionary<string, int> dailyClimaxCounts; // key = "yyyy-MM-dd", value = climax count that day
```

Increment the entry for `DateTime.UtcNow.Date.ToString("yyyy-MM-dd")` on each !climaxfor by initiator. Old entries can be aggressively pruned (keep last 30 days).

## Status-effect contributions

None. (`!climaxfor` does not produce status effects on the recipient.)

## Status-effect consumption (cross-interaction reads)

The completion message includes flavor for active vice cravings if the **initiator** is dosed. Implementation: in `GetCompletionMessage`, call `GetActiveStatusEffects(initiatorProfile, Completion)` with `interactionType = "climaxfor"`. Dose contributors return craving text like:

> "...the climax briefly takes the edge off Alice's craving for alcohol."

If the initiator is corrupted (negative corruption number), corruption contributor adds flavor:

> "...with a wickedly satisfied moan, befitting their corrupted state."

## Titles

- `·First Climax·` — first time using `!climaxfor` (self or otherwise).
- `·Climax Connoisseur·` — 100 total uses.
- `·Endurance·` — 3 climaxes within the same UTC day.
- `·Tireless·` — 5 climaxes within the same UTC day.
- `·Inhuman·` — 10 climaxes within the same UTC day.

The N-per-day titles are awarded the moment the count crosses the threshold; subsequent climaxes that day surface flavor like "amazement at the continued endurance" but do not re-award the title.

## Completion message

Self-target template: `"Alice climaxes alone — {flavor descriptor}."`
Other-target template: `"Alice climaxes for Bob — {flavor descriptor}."`

Flavor descriptor pool size ~12, weighted by the count that day:

- Counts 1–2: standard descriptors ("intense", "satisfying", etc.).
- Counts 3–4: stamina-flavored descriptors ("the third today, and they're not slowing down").
- Counts 5+: awe-flavored descriptors ("at this point everyone's just impressed", "the staff's clipboard is running out of room").

**Volume language must remain consistent across counts** — the user explicitly said: do not describe diminishing volume on repeats; describe endurance and amazement.

## Tests

- `ClimaxforProcessorTests.cs`:
  - Self-target with no args defaults correctly; counts incremented once for initiator.
  - Malformed args (text but no recipient tag) rejects, no counts incremented.
  - Other-target increments both counts.
  - Daily count persists across calls; title awarded at threshold transitions.
  - Volume language unaffected by count (regression test against accidental "diminishing" descriptors).
  - Dose-craving text appended when initiator has active dose (with mock contributor).

## Assumptions

- Daily reset is UTC. **Override:** swap to local time per channel if requested.
- Title thresholds (3/5/10) are configurable constants. **Override:** move to MongoDB if tunable in production.
- Self-target counts toward `climaxgive` only, not `climaxtake`. **Override:** add a separate `climaxsolo` count if telemetry demands it.

## Files to create/modify

- `FChatDicebot/FChatDicebot/InteractionProcessors/Involved/ClimaxforProcessor.cs` *(new)*
- `FChatDicebot/FChatDicebot/BotCommands/Climaxfor.cs` *(new)*
- `FChatDicebot/FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` *(modify — register)*
- `FChatDicebot/FChatDicebot/Model/Profile.cs` *(modify — `dailyClimaxCounts`)*
- `FChatDicebot/FChatDicebot/ChateauSystemTitles.cs` *(modify — climax titles)*
- `FChatDicebot.Tests/Unit/ClimaxforProcessorTests.cs` *(new)*
