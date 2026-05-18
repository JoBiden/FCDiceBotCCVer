# `!dose` + `!detox`

Dose the recipient with an addictive vice. Cravings appear in subsequent interactions; `!feed` of the same substance or `!odorize` of the same scent satisfies the craving without raising severity. Re-dosing increases severity. `!detox` removes a vice at a cost.

**Investment level:** Consequence
**Reversal:** `!detox` (with cost).
**Depends on:** [Status-Effect-Hook](../Infrastructure/Status-Effect-Hook.md), interacts with `!feed` (existing), `!odorize` ([Odorize-and-Wash](../Odorize-and-Wash.md))

## Vice catalog

Vices are the union of:
- All entries in the existing **substance/vice** catalog (already populated).
- All entries in the existing **scent** catalog.

Plus these named vice "umbrellas" (which can match multiple substances/scents):

```
musk, pollen, drug, drink, energy
```

User-confirmed: every existing scent is also a vice for `!dose` purposes.

## `!dose` command syntax

```
!dose [user]Bob[/user] {vice}
```

## `!dose` validation

- Recipient registered.
- `vice` resolves to an entry in the substance, scent, or umbrella vice catalogs.
- Multiple vices allowed simultaneously. Same vice already-active increases severity rather than rejects.

## `!dose` processor logic (`DoseProcessor`)

1. Find existing `ViceInstance` for `vice` on recipient. If present, increment `AddictionLevel` by 1 (cap at 10 — see Assumptions). Otherwise append:
   ```csharp
   public class ViceInstance
   {
       public string Vice;
       public int AddictionLevel;        // 1..10
       public string DosedBy;            // most recent doser
       public DateTime FirstDosedAt;
       public DateTime LastEscalatedAt;
   }
   ```
2. Set 7-day cooldown on (initiator, recipient, vice).
3. Save interaction. Delete pending command.

## Cravings (status effect)

`DoseStatusContributor` runs on every consent-driven interaction. For each `ViceInstance` on the recipient:

1. Roll a craving chance based on `AddictionLevel`:
   - Level 1: 10%
   - Level 2: 20%
   - …
   - Level 10: 100%
2. On hit: emit a completion fragment like `"...{recipient}'s hands tremble — the {vice} craving doesn't go quietly."`
3. Cravings appear on **both** the initiator's and recipient's profile reads if both have the vice — this is symmetric.

## Satisfaction

When the parent interaction *itself* would naturally provide the vice — i.e.:

- `!feed` where the substance equals an active vice on the fed party.
- `!odorize` where the scent equals an active vice on the odorized party.
- `!dose` of the same vice (the new dose).

…the craving roll is **suppressed** (no craving fragment), and instead a satisfaction fragment is appended:

> "...the craving for {vice} momentarily quieted."

**Important:** only `!dose` *escalates* `AddictionLevel`. `!feed` and `!odorize` satisfy without raising severity.

## `!detox` command syntax

```
!detox {vice}
!detox                    → detoxes the vice with the highest AddictionLevel
```

## `!detox` validation

- Self-targeted only.
- Caller has the named vice (if specified) or any vice (default).

## `!detox` processor logic (`DetoxCommand`, system command)

1. Find the vice instance.
2. Apply detox cost via `PurgeCostType` (see [Infest-and-Purge](Infest-and-Purge.md) for the full enum). Recommended default: `RandomBreak` (the body rebels at withdrawal).
3. Remove the vice instance entirely (no graduated detox in v1 — one detox clears it).
4. Output completion message describing the detox and the cost suffered.

## Status-effect contributions

`DoseStatusContributor` (the contributor described above):

- **Consent path:** if the *initiator* has any active vice, the consent warning may include flavor about their craving (informational; does not block).
- **Completion path:** craving roll + fragment per the rules above.
- **Suppression rule:** the contributor checks the parent `interactionType` and `identifier` to decide if it's a satisfaction case rather than a craving case.

## `!climaxfor` integration

`!climaxfor` consumes vice-craving suppression: a climax briefly takes the edge off **any** active vice on the climaxer (initiator) per [Climaxfor.md](Climaxfor.md). Implementation: in completion path, if `interactionType == "climaxfor"`, treat all vices on the *initiator* as satisfied for that one message — no escalation, no craving fragment, append a soft satisfaction fragment.

## Persistence

- Profile: `lists["vices"]` (list of `ViceInstance`).

## Tests

- `DoseProcessorTests.cs`:
  - First !dose creates instance at level 1.
  - Second !dose same vice → level 2.
  - Cap at 10.
- `DoseStatusContributorTests.cs`:
  - Craving chance scales by level (statistical test over many trials).
  - !feed of matching substance triggers satisfaction, not craving.
  - !odorize of matching scent triggers satisfaction.
  - !climaxfor satisfies all vices on the initiator.
- `DetoxCommandTests.cs`:
  - Cost applied; vice removed.
  - Default selects highest-level vice.

## Assumptions

- Addiction cap: 10. **Override** if higher levels are desired.
- Linear craving probability scaling (10% per level). **Override** with a nonlinear curve if needed.
- Default detox cost: `RandomBreak` (1–3 days, random part). **Override** in `DoseProcessor.DefaultDetoxCost`.
- v1 detox is full-clear in one command. Graduated detox (level decrements) is a future extension.
- `!climaxfor` satisfies all initiator vices; a future spec could narrow this to "vices that are sex-coded".

## Files to create/modify

- `FChatDicebot/Model/ViceInstance.cs` *(new)*
- `FChatDicebot/InteractionProcessors/Consequence/DoseProcessor.cs` *(new)*
- `FChatDicebot/BotCommands/Dose.cs` *(new)*
- `FChatDicebot/BotCommands/Detox.cs` *(new)*
- `FChatDicebot/InteractionProcessors/StatusEffectContributors/DoseStatusContributor.cs` *(new)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` *(modify — register)*
- `FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs` *(modify — register)*
- `FChatDicebot.Tests/Unit/DoseProcessorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/DoseStatusContributorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/DetoxCommandTests.cs` *(new)*
