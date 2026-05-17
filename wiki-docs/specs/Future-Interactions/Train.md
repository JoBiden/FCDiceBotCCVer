# `!train`

Train the recipient (and yourself) in a special technique or skill. Both parties advance, with the higher-level partner constraining the lower-level partner's growth above 10.

**Investment level:** Commitment
**Reversal:** none
**Depends on:** —

## Command syntax

```
!train [user]Bob[/user] {training}
```

## Validation

- Recipient must be registered.
- `training` must resolve via `MonDB.getIdentifier(training)` with category `"training"`.
- Self-target **not** allowed (the user said training is a practice session that benefits both parties; solo training isn't supported).
- **Daily handshake-pair lock**: the (initiator, recipient) pair may train **once per UTC day across all training types**. Symmetric — the lock binds both directions and all trainings. Reject with "You and {recipient} have already trained together today."
- Above-10 training requires a partner. If both parties' levels in this training are < 10, both can advance freely. If either is ≥ 10, see Processor logic.

## Processor logic (`TrainProcessor`)

1. Look up current levels in `initiator.trainings[trainingId]` and `recipient.trainings[trainingId]` (default 0 if unset).
2. Apply the level-progression rule:
   - Let `Hi`, `Lo` be the higher and lower of the two levels.
   - **Both parties below 10**: both advance by 1.
   - **`Lo < 10 ≤ Hi`**: only `Lo` advances by 1; `Hi` stays.
   - **Both `≥ 10`**:
     - If `|Hi - Lo| > 10`: only `Lo` advances; `Hi` stays. (Gap too wide to learn from.)
     - If `|Hi - Lo| <= 10`:
       - If `Hi == Lo`: both advance.
       - Else: only `Lo` advances.
3. Cap level at 100 — never advance past.
4. Set `initiator.timers["train_pair_" + recipient.userName]` and the recipient's symmetric mirror to a `CoolDown` ending at `DateTime.UtcNow.Date.AddDays(1)`.
5. Increment training counts: `traingive` / `traintake` (and per-training `train_{trainingId}` if granular stats are wanted).
6. Apply title checks (see Titles).
7. Save the interaction record.

## Persistence

On `Profile`:

```csharp
public Dictionary<string, int> trainings; // key = training identifier, value = level 0-100
```

Cooldown: `train_pair_{otherUserName}` (24-hour, symmetric).

## Status-effect contributions

None.

## Status-effect consumption

`!train` consent warning includes both parties' current levels in the chosen training so the recipient can understand the projected outcome:

> "Alice (level 50) wants to train you, Bob (level 30), in magic. Both of you will advance — Alice stays at 50, you advance to 31. Do you !consent?"

The processor pre-computes the same projection so the warning isn't lying.

## Titles

System titles per training, with thresholds:

- `·Apprentice {Training}·` — level 10
- `·Adept {Training}·` — level 25
- `·Master {Training}·` — level 50
- `·Grandmaster {Training}·` — level 100

`{Training}` is the training identifier in title-case. Award on level-up crossing the threshold (in `TrainProcessor`, not at level-display time).

## `!work` integration

When `!work` resolves a duty, it reads the worker's `trainings` map and may select task descriptions or compensation modifiers based on relevant training levels (specific mapping is the work system's concern; this spec only guarantees the data is available).

## Completion message

Template:

> "Alice and Bob spar/practice/study {training} together. Result: Alice {N → M}, Bob {N → M}."

If only one advances, phrasing reflects the asymmetry: "...the tutoring pays off — Alice's expertise stays steady at 50 while Bob picks up to 31."

## Tests

- `TrainProcessorTests.cs`:
  - Both-low: both +1.
  - Mixed (one ≥10, one <10): only lower +1.
  - Equal high (50/50): both +1.
  - Adjacent high (50/30, gap 20): only lower +1 (gap too wide).
  - Adjacent high (50/45, gap 5): only lower +1.
  - Cap at 100.
  - Symmetric daily pair lock: A trains B in magic at 9am, A tries to train B in flight at 10am: rejected. B tries to train A in anything: rejected.
  - Title award on threshold crossing.
- `TrainCatalogTests.cs`:
  - Training identifier must exist in the `Identifiers` catalog with category `"training"`.

## Assumptions

- Cap is 100. **Override** with higher cap if requested.
- "Gap too wide" is exactly 10. Above that, lower still advances. **Override** if the rule should be stricter (e.g. lower also doesn't advance above gap-10).
- Daily lock is UTC-day. **Override:** swap to channel-local time.
- Training levels are stored as ints. Decimal/fractional levels not supported.
- The work-system integration is **read-only on the work side** — this spec doesn't dictate work's mapping; it only guarantees the data shape.

## Files to create/modify

- `FChatDicebot/InteractionProcessors/Commitment/TrainProcessor.cs` *(new)*
- `FChatDicebot/BotCommands/Train.cs` *(new)*
- `FChatDicebot/Model/Profile.cs` *(modify — `trainings` dict)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` *(modify — register)*
- `FChatDicebot/ChateauSystemTitles.cs` *(modify — apprentice/adept/master/grandmaster)*
- `FChatDicebot.Tests/Unit/TrainProcessorTests.cs` *(new)*
