# `!corrupt` + `!purify`

Move someone's corruption value down (corrupt) or up (purify). One signed integer per character, stored as a string in `profile.characteristics["corruption"]`.

**Status:** Implemented.
**Investment level:** Commitment.
**Reversal:** Same processor handles both directions — one `CorruptionProcessor` instance registered under both `"corrupt"` and `"purify"` interaction-type keys.
**Depends on:** [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md).

## Command syntax

```
!corrupt [user]Bob[/user] {amount}      → positive amount: shifts Bob's corruption value downward (more corrupt)
!corrupt [user]Bob[/user] -{amount}     → equivalent to !purify {amount}
!purify  [user]Bob[/user] {amount}      → shifts Bob's corruption value upward (more pure)
!purify  [user]Bob[/user] -{amount}     → equivalent to !corrupt {amount}
```

If `amount` is omitted, defaults to `1`. Self-target is allowed.

## Daily quota

A single initiator may move any single recipient's corruption value by **at most 10 total magnitude per UTC day**, summed across both directions. `!corrupt 6` then `!purify 5` on the same recipient → second call partially clamps to 4.

- Tracked on the **initiator's** `Profile.dailyMagnitudes` (new field, `Dictionary<string, int>`).
- Key shape: `corruption_{recipientUserName}_{yyyy-MM-dd}` (UTC date).
- Stale-date entries are pruned on next write to keep the map small. `[BsonIgnoreIfNull]` so legacy profiles don't grow an empty field.

Clamping happens at **both** command time (so the recipient never sees a consent prompt for a magnitude that can't fully land) and process time (TOCTOU safety net for rapid command queueing).

## Validation

- Recipient must be registered (falls through to `ChateauInteractionHandler.notFoundText` on failure, same pattern as `!breed`).
- `amount` parses as an integer (`ChateauPay`-style wording on failure).
- `amount == 0` rejected with a "no needle movement" private message.
- Quota fully exhausted → command refuses *before* sending a consent prompt; the initiator gets a private heads-up.
- Quota partially available → command pre-clamps the pending magnitude and sends an "enthusiasm appreciated" private heads-up before forwarding the (clamped) consent prompt.

## Processor logic (`CorruptionProcessor`)

Single processor registered twice: once under its own `InteractionType` (`"corrupt"`) and a second time under `"purify"` via a new `RegisterProcessor(string aliasType, IInteractionProcessor)` overload on `InteractionProcessorRegistry`.

### Effective verb resolution

The command computes the **effective verb** before creating the pending interaction. Effective verb = `verbSign(typedVerb) * sign(amount)`:

| Typed | Amount | Effective verb |
|-------|-------:|----------------|
| `corrupt` | `+N` | `corrupt` |
| `corrupt` | `-N` | `purify` |
| `purify` | `+N` | `purify` |
| `purify` | `-N` | `corrupt` |

Stored on `Interaction.type`. The processor never re-derives direction.

### Identifier payload trick

The `Interaction.identifier` field is repurposed as a compact `"{verb}|{magnitude}"` payload:

- Set by the command with the **requested** (pre-clamped) magnitude.
- Overwritten inside `ProcessInteraction` with the **actually applied** magnitude (after process-time clamp).
- Read by `GetCompletionMessage` immediately after `ProcessInteraction` returns — they share the in-memory `PendingCommand`, so the rewrite is visible.

This is the only way to get the post-clamp magnitude into `GetCompletionMessage`, whose signature is `(Profile, Profile, string identifier)` — there is no per-call state slot on the interface.

### Process flow

1. Read `effectiveVerb` from `Interaction.type` and `requestedMagnitude` from the identifier payload.
2. Look up the initiator's current quota usage for today against this recipient.
3. `appliedMagnitude = Math.Min(requestedMagnitude, RemainingQuota)`.
4. **If applied > 0:** update `recipient.characteristics["corruption"]`, update `initiator.dailyMagnitudes`, and (after `SetProfile`) call `IncrementDifferentCounts` with direction-keyed labels (`corruptgive`/`corrupttake` or `purifygive`/`purifytake`).
   **If applied == 0** (rare TOCTOU from rapid command queueing): suppress channel output (set identifier magnitude to 0 — `GetCompletionMessage` returns empty) and stash a private heads-up for the initiator in `_lastInitiatorPrivateMessage`; ChateauConsent drains it via `GetAndClearInitiatorPrivateMessage`.
5. Rewrite identifier with `appliedMagnitude`, then `AddInteraction`.
6. `SetProfile` for both participants **before** the count increments — the count increments go directly to the DB; doing them after `SetProfile` avoids the in-memory profile clobbering them back to zero.
7. `DeletePendingCommand`.

**Self-target:** initiator and recipient resolve to the same `Profile` reference so reads/writes against the underlying record stay coherent; only one `SetProfile` call is made.

### Direction-of-effect wording (`LevelChange`)

The user-facing "increasing corruption" / "decreasing purity" wording is chosen from the **end-state side**, not the verb's direction. Going from `2` to `7` via `!purify 5` reads as *"increasing their purity by 5"*; going from `-7` to `-2` via the same `!purify 5` reads as *"decreasing their corruption by 5"*. Implemented by the `CorruptionProcessor.LevelChange.From(oldValue, newValue)` struct, called by both consent and completion paths (consent uses the projected new value; completion uses the actual stored value, recovered as `newCorruption - sign * appliedMagnitude`).

| Old | New | Side | Action |
|-----|-----|------|--------|
| 2 | 7 | purity | increasing |
| 7 | 2 | purity | decreasing |
| -7 | -2 | corruption | decreasing |
| -2 | -7 | corruption | increasing |
| -2 | 3 | purity | increasing (cross-zero) |
| 5 | 0 | purity | decreasing (inherits side from prior) |

The zero end-state gets a special trailing clause — *"…leaving them once again neutral in the eternal tug of war of purity and corruption."* — instead of the standard *"leaving them with X degrees of (corruption|purity)"*.

### Easter egg

`1 / EasterEggChanceDenominator` (= 1/20 = 5%) of completion messages substitute the verb for its inverse — `corrupt` → `un-purify`, `purify` → `un-corrupt`. The `Rng` field is `internal static` so tests can swap in a seeded `Random` or an `AlwaysZeroRandom` to force the egg.

## Persistence

```csharp
// On the recipient's Profile
characteristics["corruption"] = "<signed int>";  // negative = corrupt, positive = pure

// On the initiator's Profile (new field)
[BsonIgnoreIfNull]
public Dictionary<string, int> dailyMagnitudes;  // key: corruption_{recipient}_{yyyy-MM-dd}
```

## Counts incremented (per call with appliedMagnitude > 0)

- Effective `corrupt`: `corruptgive` on initiator, `corrupttake` on recipient.
- Effective `purify`: `purifygive` on initiator, `purifytake` on recipient.

Direction-keyed (not verb-typed) so `!corrupt -5` correctly bumps the purify counters.

## Status-effect contributions

`CorruptionStatusContributor` (registered in `StatusEffectRegistry.Initialize()`) reads `profile.characteristics["corruption"]` and emits a fragment on the **completion** call site of other interactions. Consent and validation paths are intentionally silent. Self-referencing call sites (`"corrupt"` / `"purify"` as the parent `interactionType`) are skipped so the corrupt/purify completion message doesn't get a redundant fragment on top.

| Range | Fragment |
|-------|----------|
| ≤ -100 | An absolute aura of corruption radiates from {name}. |
| -99 to -50 | A strong aura of corruption emanates from {name}. |
| -49 to -10 | A faint aura of corruption emanates from {name}. |
| -9 to 9 | *(no fragment)* |
| 10 to 49 | A faint aura of purity emanates from {name}. |
| 50 to 99 | A strong aura of purity emanates from {name}. |
| ≥ 100 | An absolute aura of purity radiates from {name}. |

The strongest tier uses *"radiates"*; the milder tiers use *"emanates"*.

## Cross-spec consumers

- [Milk](Milk.md): the `corruptionTag` on milk bottles is computed from the ≥ ±10 thresholds (this spec's first non-neutral band).
- [Climaxfor](Climaxfor.md): may add its own initiator-side flavor based on the same characteristic.

## TOCTOU private-message wiring

Added to support the rare case where a queued pending lands but its quota has been eaten between command and consent:

- `IInteractionProcessor.GetAndClearInitiatorPrivateMessage()` — drained by `ChateauConsent` after every successful `ProcessInteraction`.
- `InteractionProcessorBase._lastInitiatorPrivateMessage` — backing field for any processor that wants to use this channel.
- `CorruptionProcessor.QuotaExhaustedPrivateMessage(verb, displayName)` — shared between the command-time refusal and the TOCTOU fallback so the two paths can't drift.

Any future processor that wants out-of-band private notifications to the initiator after a consent fires can set `_lastInitiatorPrivateMessage` and the existing drain will deliver it.

## Tests

- `CorruptionProcessorTests.cs` (~50 tests):
  - Sign-flipping for `EffectiveVerb` across all four verb × sign combinations.
  - Identifier-payload round-trip + parse defaults on garbage input.
  - Quota math (`RemainingQuota`, `RecordUsedQuota` pruning by date and not crossing recipients).
  - Quota partial clamp (the spec's 6 + 5 → 4 case).
  - Different recipients / different initiators don't share quotas.
  - Self-target.
  - Directional counts on success.
  - Pending command deleted on success.
  - Identifier rewritten with applied magnitude.
  - Completion wording for the corrupt-side, purify-side, cross-zero, and lands-exactly-at-zero cases.
  - TOCTOU exhaustion: empty completion + private note that matches the command-time refusal wording.
  - `GetAndClearInitiatorPrivateMessage` clears after drain.
  - Consent wording for end-state-on-corruption / end-state-on-purity / cross-zero.
  - `LevelChange.From` direction resolution across 10 representative combos.
  - `DescribeCurrentLevel` neutrality + non-zero degrees rendering.
  - Easter-egg substitution (forced via `AlwaysZeroRandom`) and statistical sanity over 200 trials.
- `CorruptionStatusContributorTests.cs` (~15 tests): null profile, consent silence, neutral-band silence, exact band thresholds for all 6 tiers, self-reference skip, no leading whitespace, unparseable / missing field defaults.

## Assumptions and overrides

- **`DailyMagnitudeLimit = 10`** — overridable as a constant on `CorruptionProcessor`.
- **`EasterEggChanceDenominator = 20` (5%)** — overridable as a constant.
- **Storage as stringified int in `characteristics`** — matches every other simple scalar in the Profile model. Override would be a typed `int corruption` slot if the value gets accessed in hot paths.
- **`dailyMagnitudes` over timer-overload** — the spec originally suggested either approach; the typed dictionary won for clarity.
- **`Profile.dailyMagnitudes`** is initialized in C# but defaults to `null` on legacy profiles loaded from MongoDB (the initializer doesn't run on deserialization). All callers null-check before use.

## Files created / modified

- `FChatDicebot/Model/ChateauDB.cs` *(modified — added `dailyMagnitudes` field on `Profile`)*
- `FChatDicebot/InteractionProcessors/Commitment/CorruptionProcessor.cs` *(new — registered for both `"corrupt"` and `"purify"`)*
- `FChatDicebot/InteractionProcessors/Commitment/CorruptionCommandSupport.cs` *(new — shared command-side parse/wire/pre-clamp logic; placed outside `FChatDicebot.BotCommands` so the reflection loader skips it)*
- `FChatDicebot/BotCommands/ChateauCorrupt.cs` *(new — thin shell delegating to `CorruptionCommandSupport`)*
- `FChatDicebot/BotCommands/ChateauPurify.cs` *(new — thin shell delegating to `CorruptionCommandSupport`)*
- `FChatDicebot/InteractionProcessors/StatusEffectContributors/CorruptionStatusContributor.cs` *(new)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` *(modified — alias-registration overload + register both type keys)*
- `FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs` *(modified — register contributor)*
- `FChatDicebot/InteractionProcessors/IInteractionProcessor.cs` *(modified — `GetAndClearInitiatorPrivateMessage` on the interface)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorBase.cs` *(modified — `_lastInitiatorPrivateMessage` field + drain accessor)*
- `FChatDicebot/BotCommands/ChateauConsent.cs` *(modified — drain `GetAndClearInitiatorPrivateMessage` after each `ProcessInteraction`)*
- `FChatDicebot/FChatDicebot.csproj` *(modified — `<Compile Include>` entries for new files)*
- `FChatDicebot.Tests/Unit/Corruptionprocessortests.cs` *(new)*
- `FChatDicebot.Tests/Unit/Corruptionstatuscontributortests.cs` *(new)*
