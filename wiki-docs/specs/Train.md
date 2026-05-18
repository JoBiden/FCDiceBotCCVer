# `!train`

Train another resident in a special technique or skill. Both parties practice together; either or both may advance one level in the chosen training. A higher-level partner can tutor a lower-level partner — the higher partner stays put while the lower partner catches up.

**Status:** Implemented.
**Investment level:** Commitment.
**Reversal:** none.
**Depends on:** —

## Command syntax

```
!train [user]Bob[/user] {training}
```

## Validation

- Recipient must be registered (base validation).
- Self-target is rejected with a private "find someone to train with" message — both at the command layer (`ChateauTrain.Run`) and as a defensive guard in `TrainProcessor.ValidateInteraction`.
- `training` must resolve via `MonDB.getIdentifier(training)` with category `"training"`; missing/unknown/wrong-category identifiers are rejected with the standard `ChateauInteractionHandler.typeNotFoundText("training")` / `notFoundText` helpers.
- **Daily pair lock**: the (initiator, recipient) pair may `!train` only once per day, **regardless of training type**. The single timer `train_pair_{otherUserName}` is set symmetrically on both profiles by `ProcessInteraction`. Command-layer pre-check produces a private message including the time remaining (`Utils.GetTimeSpanPrint`); failure text reads `"You and {Recipient} have already trained together today. You'll be able to train together again in {time}."` The processor does not re-check the lock — only `ChateauTrain` gates on it.
- The base `ValidateInteraction` calls into the status-effect hook for the recipient, so any future blocker contributor will gate `!train` without changes here.

## Processor logic (`TrainProcessor`)

Constants on the class: `LevelCap = 100`, `TutorThreshold = 10`, `MaxLearnableGap = 10`.

1. `AddInteraction` to history.
2. Read current levels: `initiator.trainings[trainingId]` and `recipient.trainings[trainingId]` (default `0` if unset).
3. `ApplyProgression(initiatorLevel, recipientLevel, out initiatorAfter, out recipientAfter)` — pure static function, see below.
4. Persist the new levels on both profiles' `trainings` dicts.
5. Set `train_pair_{otherUserName}` on both profiles to a `CoolDown` ending at `DateTime.UtcNow.Date.AddDays(1)` (next day boundary).
6. `GrantTrainingTitles` for each participant — awards `·{TitleCase(training)} Apprentice/Adept/Master/Grandmaster·` titles for any threshold crossed this session (see Titles).
7. `Database.SetProfile` for both.
8. `Database.IncrementCount(initiator, "traingive")` and `IncrementCount(recipient, "traintake")` — count-based titles in `ChateauSystemTitles` fire automatically when `ChateauConsent` runs `CheckAndGrantTitles` afterward.
9. Snapshot the pre/post levels on instance fields (`_lastInitiator/RecipientLevelBefore/After`, `_lastSnapshotPopulated`) so the immediately-following `GetCompletionMessage` call can render the right asymmetric / both-advanced / neither phrasing without re-deriving the pre-state from the post-state (which would be ambiguous around the `TutorThreshold`). The snapshot is drained on read.
10. `Database.DeletePendingCommand`.

### Level-progression rule (`ApplyProgression`)

Pure static; `out` parameters return the new levels.

- **Both below `TutorThreshold` (10)**: both advance by 1.
- **One below 10, the other ≥ 10**: only the below-10 partner advances.
- **Both ≥ 10, equal**: both advance by 1.
- **Both ≥ 10, not equal**: only the lower partner advances. (The gap-too-wide case `|Hi − Lo| > MaxLearnableGap` collapses to the same outcome under the current rule — lower still advances; higher stays.)
- Cap is 100; the `Bump` helper clamps any advance.

## Persistence

- Profile: `trainings` (`Dictionary<string, int>`, training-id → level 0–100).
- Profile timers: `train_pair_{otherUserName}` — single 24-hour timer per pair, set symmetrically on both profiles. Cross-training: training Bob in magic also blocks a same-day flight session with Bob.
- Counts: `traingive` on initiator, `traintake` on recipient.
- Titles: per-training tier titles on `Profile.titles` (system-granted; see below).

## Status-effect contributions

None. (`!train` is an action, not a passive status.)

## Status-effect consumption

Standard recipient-side blocker gating via the base class's status-effect hook in `ValidateInteraction`. No custom blocker code in `TrainProcessor`.

## Titles

### Per-training tier titles (granted by processor)

`TrainProcessor.GrantTrainingTitles` awards a system title for each tier threshold *crossed* in the current session — old-level below threshold, new-level at or above:

| Threshold | Title |
|-----------|-------|
| Level 10  | `·{Training} Apprentice·` |
| Level 25  | `·{Training} Adept·` |
| Level 50  | `·{Training} Master·` |
| Level 100 | `·{Training} Grandmaster·` |

`{Training}` is the training identifier title-cased (`magic` → `Magic`). Duplicate-award guard prevents repeats. Titles accumulate — a level-50 trainer in magic carries `Magic Apprentice`, `Magic Adept`, and `Magic Master`.

### Count-based give/take titles (`ChateauSystemTitles`)

The pre-existing `traingive` / `traintake` milestones in [`ChateauSystemTitles.cs`](../../FChatDicebot/BotCommands/ChateauSystemTitles.cs) (Tutor / Teacher / Mentor / Fountain Of Knowledge and the take-side equivalents) fire via the standard `CheckAndGrantTitles` pass that `ChateauConsent` runs after every interaction. `TrainProcessor` just increments the counts; the title award flows from there.

## User-facing text

**Command channel announcement** (`ChateauTrain.Run`, also returned by `TrainProcessor.GetConsentWarning` so the not-yet-shipped conversational-flow path stays in sync):

> `{Initiator} wants to do some {training} training with {Recipient}! Do you !consent to improving your skills together?`

**Completion message** (after consent) — three variants based on who advanced:

- **Both advance**: `{Initiator} and {Recipient} do some {training} training! They both feel a little more practiced now.`
- **Asymmetric** (only one advances): `{Initiator} and {Recipient} do some {training} training! {Tutor} was a lot more knowledgeable, so only {Student} benefitted from the time spent.`
- **Neither advances** (both at cap 100): `{Initiator} and {Recipient} do some {training} training! They're both just showing off at this point, there's not much more for them to learn.`

**Self-target rejection** (private):

> `You can't train with yourself. Find someone to train with!`

**Pair-lock rejection** (private, command-layer):

> `You and {RecipientDisplay} have already trained together today. You'll be able to train together again in {time}.`

`{time}` comes from `Utils.GetTimeSpanPrint` and never exposes the UTC implementation detail.

## `!work` integration

`Profile.trainings` is read-only data for the `!work` system. The shape (`Dictionary<string, int>`, level 0–100, per training-id) is guaranteed by this spec; how `!work` maps a training to a task or compensation modifier is the work system's concern and is not specified here.

## Tests

[`Trainprocessortests.cs`](../../FChatDicebot.Tests/Unit/Trainprocessortests.cs) — 36 tests covering:

- **`ApplyProgression`** pure-function cases: both-below, both-at-zero, low/threshold, mixed-tutor (both directions), equal-at-threshold, equal-high, adjacent-high small gap, wide gap, at-cap, higher-at-cap-lower-advances.
- **Validation**: empty / unknown / wrong-category identifier rejected; known training accepted; self-target rejected.
- **`ProcessInteraction`**: both-from-zero advances both; mixed-tutor only advances the lower partner; equal-high advances both; cap holds at 100; symmetric `train_pair_*` timer set on both profiles; pair-cooldown is cross-training (single key per pair, not per training); `traingive` / `traintake` increments; interaction is recorded; pending command is deleted.
- **Title awards**: crossing apprentice threshold awards `{Training} Apprentice`; not crossing awards nothing; crossing grandmaster awards `{Training} Grandmaster` for both participants; titles are per-training (training magic doesn't award flight titles); no double-award when re-running an already-granted title.
- **Text shape**: consent-warning string is exactly the approved template; both-advance / asymmetric (tutor phrasing from recipient's perspective) / neither-advance completion strings are exact-match.

## Differences from the original spec

The original `Future-Interactions/Train.md` design was followed closely. The shipped behavior differs in a few wording / scope choices, all approved during implementation:

- **Completion message no longer renders level numbers.** Original spec rendered `Alice {N → M}, Bob {N → M}`; since increments are always exactly 1, the as-shipped phrasing is flavor-only ("they both feel a little more practiced now" / tutor framing / "showing off"). The pre/post-level snapshot is still captured at process-time and used to choose which of the three flavor variants to emit.
- **Title format is `{Training} {Tier}` not `{Tier} {Training}`.** Spec said `Apprentice Magic`; shipped as `Magic Apprentice`. Reads better when titles are listed in a dossier.
- **Gap-too-wide (`|Hi − Lo| > 10`) collapses to the standard asymmetric branch.** Both rules produce the same outcome (lower advances, higher stays). The constant `MaxLearnableGap` is still defined for documentation, but no branch currently consumes it — kept as a hook in case the gate ever tightens (lower also denied above gap-10).
- **Consent prompt simplified.** Original spec rendered both parties' current levels and the projected outcome inline (`"Alice (level 50) wants to train you, Bob (level 30)..."`). Shipped wording is the shorter `"{Initiator} wants to do some {training} training with {Recipient}! Do you !consent to improving your skills together?"` — no level numbers in the consent prompt either.
- **Pair-lock rejection now includes time remaining.** Spec just said "already trained together today"; shipped form matches the BreedProcessor / MilkProcessor style with a `{time}` suffix.

## Files

- [`FChatDicebot/Model/ChateauDB.cs`](../../FChatDicebot/Model/ChateauDB.cs) (added `trainings` dict to `Profile`)
- [`FChatDicebot/InteractionProcessors/Commitment/TrainProcessor.cs`](../../FChatDicebot/InteractionProcessors/Commitment/TrainProcessor.cs)
- [`FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs`](../../FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs) (registers `TrainProcessor`)
- [`FChatDicebot/BotCommands/ChateauTrain.cs`](../../FChatDicebot/BotCommands/ChateauTrain.cs)
- [`FChatDicebot.Tests/Builders/Profilebuilder.cs`](../../FChatDicebot.Tests/Builders/Profilebuilder.cs) (added `WithTrainingLevel` helper)
- [`FChatDicebot.Tests/Unit/Trainprocessortests.cs`](../../FChatDicebot.Tests/Unit/Trainprocessortests.cs)
