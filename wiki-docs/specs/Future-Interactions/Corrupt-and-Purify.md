# `!corrupt` + `!purify`

Move someone's corruption value down (corrupt) or up (purify). One signed integer per character; no caps except `int.MinValue` / `int.MaxValue`.

**Investment level:** Commitment
**Reversal:** Same processor handles both directions (single processor, two command aliases).
**Depends on:** [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md)

## Command syntax

```
!corrupt [user]Bob[/user] {amount}      → amount is positive: subtracts from Bob's corruption (more corrupt)
!corrupt [user]Bob[/user] -{amount}     → equivalent to !purify {amount}
!purify  [user]Bob[/user] {amount}      → adds to Bob's corruption (more pure)
!purify  [user]Bob[/user] -{amount}     → equivalent to !corrupt {amount}
```

If `amount` omitted, default to `1`.

## Validation

- Recipient must be registered.
- `amount` parses as a positive integer once sign-flipping is resolved.
- **Daily per-initiator quota**: a single initiator may move *any single recipient's* corruption value by **at most 10 total per UTC day**, summed across both directions. Initiating `!corrupt 6` then `!purify 5` = 11 total magnitude → second call partially clamps to 4. Reject the *excess* with a warning, not the whole command.
- Self-target allowed (philosophy: you can corrupt or purify yourself).

## Processor logic (`CorruptionProcessor`)

Single processor, registered under both interaction types `"corrupt"` and `"purify"`. Determines effective sign from:

1. The verb (`corrupt` ⇒ negative, `purify` ⇒ positive).
2. The amount sign (negative flips the verb's sign).

Composite: effective delta = `verb_sign * sign(amount) * abs(amount)`.

Then:

1. Read `recipient.characteristics["corruption"]` as int (default 0).
2. Look up `initiator.timers["corruption_quota_" + recipient.userName + "_" + UtcDay]` for today's already-used quota magnitude.
3. Compute clamped magnitude = `min(abs(delta), 10 - already_used)`.
4. Apply: `recipient.characteristics["corruption"] = old + sign(delta) * clamped`.
5. Update quota tracker.
6. Set 24-hour cooldown timer (standard commitment).
7. Increment counts: `corruptgive` / `corrupttake` if delta is negative; `purifygive` / `purifytake` if positive.
8. Save interaction. Delete pending command.

## Persistence

On `Profile`:

```csharp
// in characteristics
characteristics["corruption"] = "<signed int>"; // negative = corrupt, positive = pure
```

On `Profile.timers`:

```csharp
timers["corruption_quota_{recipientName}_{yyyy-MM-dd}"] = CoolDown {
    timerEnd = next UTC day midnight,
    // re-uses the existing CoolDown shape — `quota_used_magnitude` stored as a side field if extended,
    // OR use a new typed `Dictionary<string, int>` on profile for daily quotas (cleaner).
}
```

**Recommended:** add a new `dailyMagnitudes` dictionary instead of overloading timers:

```csharp
public Dictionary<string, int> dailyMagnitudes; // key = "corruption_{recipient}_{yyyy-MM-dd}", value = magnitude consumed
```

Pruned by date at write time (drop entries where the date prefix is not today).

## Status-effect contributions

`CorruptionStatusContributor` reads `recipient.characteristics["corruption"]` and emits flavor on completion messages of other interactions:

| Range | Flavor descriptor |
|-------|-------------------|
| ≤ -100 | "utterly debased" |
| -99 to -50 | "deeply corrupted" |
| -49 to -10 | "tinged with corruption" |
| -9 to 9 | (no fragment) |
| 10 to 49 | "noticeably purified" |
| 50 to 99 | "radiant with purity" |
| ≥ 100 | "transcendently pure" |

Examples in other specs:

- [Milk](Milk.md): "corrupt milk" / "purified milk" tags on bottles, derived from these thresholds (use **±10** as the threshold for the bottle tag — same as the `tinged with corruption` / `noticeably purified` band).
- [Climaxfor](Climaxfor.md): adds a "wickedly satisfied" / "blessedly serene" flavor.

## Easter egg

Roughly 5% of completion messages should render the verb as `un-purify` (when corrupting) or `un-corrupt` (when purifying):

> "Alice un-purifies Bob by a magnitude of 3. Bob's corruption is now -8."

The dice roll happens in `GetCompletionMessage`, not deterministic.

## Consent warning

> "Alice wants to {corrupt|purify} you by {amount} magnitude. Your corruption is currently {value}; if you !consent it will become {projected_value} (or less, if Alice has already used some of their daily quota with you). Do you !consent?"

## Tests

- `CorruptionProcessorTests.cs`:
  - Sign flipping: `!corrupt -5` ↔ `!purify 5`.
  - Daily quota: 6 + 5 → second clamps to 4 with partial-success message.
  - Self-target works.
  - Counts go to the right give/take labels based on effective sign.
- `CorruptionStatusContributorTests.cs`:
  - Threshold bands return correct fragments.
  - 0 returns no fragment.
  - Easter-egg occurrence is non-zero in 200 trials and approximately 5%.

## Assumptions

- 5% easter-egg rate is hardcoded. **Override** in a constant.
- Daily quota magnitude limit is 10. **Override** if play-tests show it's too restrictive.
- Storage as a stringified int in `characteristics`. **Override:** add a typed int slot on `Profile` if desired.
- `dailyMagnitudes` over timer-overload is a recommendation; either works.

## Files to create/modify

- `FChatDicebot/InteractionProcessors/Commitment/CorruptionProcessor.cs` *(new — registered for both `"corrupt"` and `"purify"`)*
- `FChatDicebot/BotCommands/Corrupt.cs` *(new)*
- `FChatDicebot/BotCommands/Purify.cs` *(new — thin wrapper that flips verb_sign)*
- `FChatDicebot/Model/Profile.cs` *(modify — `dailyMagnitudes`)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` *(modify — register both interaction types pointing to one processor instance)*
- `FChatDicebot/InteractionProcessors/StatusEffectContributors/CorruptionStatusContributor.cs` *(new)*
- `FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs` *(modify — register)*
- `FChatDicebot.Tests/Unit/CorruptionProcessorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/CorruptionStatusContributorTests.cs` *(new)*
