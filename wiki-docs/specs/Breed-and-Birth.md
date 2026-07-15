# `!breed` + `!birth`

Breed the recipient with new monster life, then have them birth at a time of their choosing. Multiple concurrent pregnancies are supported and indexed.

**Status:** Implemented.
**Investment level:** Commitment (for `!breed`); `!birth` is a self-action with a time gate, not a consent interaction.
**Reversal:** `!birth` releases an individual pregnancy — but "reversal" in the cost-bearing sense doesn't apply; abortion / cancellation is **not** in scope. (Add a separate `!miscarry` spec later if requested.)
**Depends on:** [NPC-System](Future-Interactions/Infrastructure/NPC-System.md) (degrades gracefully without it — see Assumptions)

## `!breed` command syntax

```
!breed [user]Bob[/user] {monster}
!breed [user]Bob[/user] random
!breed [user]Bob[/user] mixed
```

### Mystery keywords (added 2026-07, feedback 6a51d2fa)

`random` and `mixed` are reserved keywords, not catalog monsters:

- **`random`** — one species is rolled (uniform over every catalog Identifier with category `"monster"`) for the whole brood. Gestation and brood size resolve from the rolled species exactly as a normal `!breed` of it would. `Pregnancy.MonsterType` holds the real species, `Pregnancy.MysteryKind = "random"`.
- **`mixed`** — a **host** species is rolled for the womb math (gestation + brood size), then **each child** rolls its own species. `Pregnancy.MonsterType = "mixed"`, `MysteryKind = "mixed"`, and `Pregnancy.Children` holds one `{Species, Categories}` snapshot per child; the host contributes nothing beyond the numbers.

The roll happens at breed time, but every pre-birth surface (gestation status DM, the `!birth` ready list) masks the species as `???` / `??? (mixed brood)` — the birth announcement is the reveal ("the mystery is finally revealed — …"; mixed broods enumerate the litter, e.g. "2 slimes, a wasp, and an imp"). Global `MonsterStats`: `random` counts pregnancy/offspring under the rolled species as usual; `mixed` counts +1 pregnancy per **distinct** species/category in the litter at breed time and +1 offspring per child at birth. Neither keyword ever gets a `monster:` counter of its own.

## `!breed` validation

- Recipient must be registered.
- `monster` must resolve via the existing monster catalog (`MonDB.getIdentifier(monster)` with category `"monster"`), **or** be one of the mystery keywords above (valid only while at least one monster is registered to roll from).
- The recipient's pregnancy slot count is **uncapped** (insect broods, etc.). Multiple pregnancies of the same monster type are allowed.
- Initiator-side broken-state check (`break:dick` or `break:ball`) blocks `!breed`. The block is on the initiator, not the recipient — see [Break-and-Rest](Future-Interactions/Break-and-Rest.md) and `IStatusEffectContributor` validation surface.
- Recipient-side `break:pussy` or `break:ass` may block depending on monster's gestation route — but for v1, treat `break:body` as the only recipient-side blocker. **Override** if a per-monster route map is desired.

## `!breed` processor logic (`BreedProcessor`)

1. Determine gestation duration for the monster type. The duration is read from a new field on the existing monster `Identifier`:
   ```csharp
   public int gestationDays = 1; // default 1, max 7, configurable in catalog per monster
   ```
2. Determine brood size: another field on the monster `Identifier`:
   ```csharp
   public int broodSizeMin = 1;
   public int broodSizeMax = 1;
   ```
   Roll uniform `[broodSizeMin, broodSizeMax]`.
3. Append a `Pregnancy` to recipient's `pregnancies` list (see Persistence).
4. Save the interaction record.
5. Delete pending command.

Crucially: **no NPC is created at !breed time.** NPCs come into existence at `!birth`.

## `!birth` command syntax

```
!birth                          → birth the oldest ready pregnancy
!birth {index}                  → birth pregnancy #{index} (1-based, only ready ones counted)
!birth {index} name="Name"      → birth + name the headliner NPC
!birth {index} name="Name" description="..."
```

If no NPC system is installed (graceful degradation), `name=` and `description=` are silently ignored.

## `!birth` validation

- The caller is the *recipient* of the original `!breed` (their own pregnancies).
- The pregnancy must be **ready**: `DateTime.UtcNow >= readyAt`. Otherwise: "That pregnancy isn't ready yet — {time remaining} to go."
- The pregnancy index must exist in the caller's `pregnancies` list.

## `!birth` processor logic

1. Pop the chosen pregnancy from `recipient.pregnancies`.
2. If [NPC-System](Future-Interactions/Infrastructure/NPC-System.md) is installed:
   - Create a single named `NPC` record. Name = explicit `name=` or generator fallback (per-species pool, then `"{Species} Spawn #{shortId}"`).
   - If brood size > 1, set `Attributes["brood_size"] = broodSize.ToString()` on the NPC.
   - `ParentNames = [initiator, recipient]` from the pregnancy.
3. If no NPC system: store an aggregate offspring record on the recipient profile in `lists["offspring"]` with one line per birth (`"{date}: {monster} brood of {n} (parent: {initiator})"`). Migrate later when NPC system arrives.
4. Increment `birthgive` (none — solo) and `birth` counts.
5. Output completion message.

`!birth` is **not** a consent-flow interaction — it's a self-action. No `PendingCommand` involved.

### DM status readout

`!birth` no longer hard-requires a channel. Messaged privately, it doesn't birth anything — it lists every one of the caller's pregnancies with a status line ("Ready now!" or "ready in {time remaining}"), then reminds them that actually giving birth requires running `!birth` in a channel (the completion message is posted publicly). See `ChateauBirth.BuildGestationStatusMessage`.

## `!whatis` gestation display

`!identifier` / `!whatis` appends a `Gestation: {N} days. Brood size: {min}-{max}.` line for any identifier in the `monster` category, resolved via `BreedProcessor.ResolveGestationAndBroodRange` (the same category-default-then-override precedence as the real roll, minus the brood-size RNG — pure and side-effect-free so looking it up doesn't consume anything).

## Persistence

On `Profile`:

```csharp
public List<Pregnancy> pregnancies;

public class Pregnancy
{
    public string Id;            // GUID, also the index reference
    public string Initiator;     // userName who bred them
    public string MonsterType;
    public DateTime ConceivedAt;
    public DateTime ReadyAt;     // ConceivedAt + gestationDays
    public int BroodSize;
}
```

When listing pregnancies (e.g. `!pregnancies` informational command — recommended sub-spec), display 1-based indices ordered by `ConceivedAt`.

## Status-effect contributions

`!breed` itself contributes nothing passive. The active pregnancies on the recipient should surface via a `PregnancyStatusContributor`:

- During the gestation window, the recipient's profile in completion messages of *other* interactions includes flavor like "—their pregnancy showing visibly" (varying by `(now - conceivedAt) / (readyAt - conceivedAt)` ratio).
- Multiple pregnancies stack: "carrying three different monstrous broods at once".

## Status-effect consumption

`!breed` consent warning includes the recipient's existing pregnancies count (informational), and the recipient's corruption flavor.

## Cooldown

24 hours from initiator → recipient pair (standard commitment cooldown). Set `initiator.timers["breed_pair_" + recipient]` and the symmetric mirror.

## Tests

- `BreedProcessorTests.cs`:
  - Pregnancy added with correct fields, brood size in range.
  - 24-hour pair cooldown enforced.
  - Initiator broken-state blockers trigger.
  - Pregnancy persists across loads.
- `BirthCommandTests.cs`:
  - Birth before ready: rejection with time remaining.
  - Birth oldest by default.
  - Birth by index: out-of-range error message.
  - With NPC system: NPC created, parented correctly, name applied.
  - Without NPC system: offspring list entry added.
  - Multiple concurrent pregnancies: birthing one leaves others intact, indices renumber correctly.

## Assumptions

- Gestation duration is monster-catalog-driven, capped at 7 days. **Override:** if a global cap of 7 is too narrow for some monsters, raise the cap and document the new max.
- Brood size is monster-catalog-driven. The catalog needs new fields `gestationDays`, `broodSizeMin`, `broodSizeMax`. **Confirm before adding to monster identifiers** — these may need a migration script.
- One named NPC per breeding regardless of brood size (per user direction). **Override** with a multi-name syntax if disputes happen.
- `!birth` is self-only (only the recipient/carrier can birth). **Override** with initiator-can-induce if needed (would require consent flow).
- No miscarriage / abortion command in v1.

## Files to create/modify

- `FChatDicebot/InteractionProcessors/Commitment/BreedProcessor.cs` *(new)*
- `FChatDicebot/BotCommands/Breed.cs` *(new)*
- `FChatDicebot/BotCommands/Birth.cs` *(new — system command, not interaction)*
- `FChatDicebot/Model/Pregnancy.cs` *(new)*
- `FChatDicebot/Model/Profile.cs` *(modify — add `pregnancies`)*
- `FChatDicebot/Model/Identifier.cs` *(modify — add `gestationDays`, `broodSizeMin/Max`)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` *(modify — register)*
- `FChatDicebot/InteractionProcessors/StatusEffectContributors/PregnancyStatusContributor.cs` *(new)*
- `FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs` *(modify — register)*
- `FChatDicebot.Tests/Unit/BreedProcessorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/BirthCommandTests.cs` *(new)*
- Recommended: `FChatDicebot/BotCommands/Pregnancies.cs` *(new — informational list)*
