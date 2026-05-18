# `!odorize` + `!wash`

Saturate the recipient with a scent that gets noticed in subsequent interactions and fades by mention rather than by time. `!wash` removes one layer of one scent per day.

**Status:** Implemented.
**Investment level:** Consequence
**Reversal:** `!wash` (one layer/day, no other cost — the cost *is* the slow pace).
**Depends on:** [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md)

## Catalog

Reuses the existing scent catalog (`MonDB.getIdentifier(scent)` with category `"scent"`).

## `!odorize` command syntax

```
!odorize [user]Bob[/user] {scent}
```

## `!odorize` validation

- Recipient registered.
- `scent` resolves to a scent identifier.
- Scents stack: same scent can be applied multiple times. No reject for duplicates — instead, increment the layer count.

## `!odorize` processor logic (`OdorizeProcessor`)

1. Find existing `ScentLayer` with matching `scent` on the recipient. If present, increment `Layers` by 1 (capped at, say, 5 — see Assumptions). Otherwise, append a new `ScentLayer { scent, Layers = 1, RemainingMentions = LayerToMentions(1) }`.
2. **Recompute** `RemainingMentions = LayerToMentions(Layers)` — this is total mentions left, derived from layer count.

   `LayerToMentions(layers) = layers * 3` — each layer is "worth" 3 mentions before it fades.

3. Set 7-day cooldown on the (initiator → recipient → scent) tuple — `initiator.timers["odorize_" + scent + "_" + recipient.userName]`.
4. Save interaction. Delete pending command.

## ScentLayer shape

```csharp
public class ScentLayer
{
    public string Scent;              // identifier
    public int Layers;                // 1..N
    public int RemainingMentions;     // counts down each time mentioned in another interaction
    public string AppliedBy;          // most recent applier
    public DateTime LastAppliedAt;
}
```

Stored on `recipient.lists["scents"]`.

## Status-effect contributions

`OdorizeStatusContributor` runs on every consent-driven interaction. For each `ScentLayer` on the recipient (or initiator — see below):

1. If `RemainingMentions <= 0`, **remove the layer entirely** before contributing.
2. Otherwise, emit a fragment whose intensity depends on the current `Layers` value:

   | Layers | Descriptor template |
   |--------|---------------------|
   | 5+ | "the room is *thick* with {scent}" |
   | 4 | "{recipient} reeks of {scent}" |
   | 3 | "{recipient} smells strongly of {scent}" |
   | 2 | "the scent of {scent} clings to {recipient}" |
   | 1 | "a faint hint of {scent} lingers on {recipient}" |

3. Decrement `RemainingMentions` by 1.
4. If `RemainingMentions == 0`, also drop one `Layers` (so a 5-layer scent fades through descriptors as it goes: 15 mentions total → reads "thick", then "reeks", then "strongly", then "clings", then "faint", then gone).

   Concretely: `RemainingMentions` ticks within a layer's allotted 3 mentions. When the allotment hits 0, decrement `Layers` by 1 and recompute `RemainingMentions = 3` for the new layer level. When `Layers == 0`, remove the entry.

5. **Both initiator and recipient** of the parent interaction can be sources of scent fragments — call the contributor for both profiles. Decrement is independent per profile.

Multiple distinct scents on the same profile **all** contribute and **all** decrement. Walking-bath effect: a heavily-odorized character in many interactions sheds scents fast.

## `!wash` command syntax

```
!wash {scent}            → reduce one layer of {scent}
!wash                    → reduce one layer of the most-saturated scent (highest Layers)
```

## `!wash` validation

- Self-targeted only — washes the caller's own profile.
- Caller has the scent (if specified) or any scent (default).
- Caller has not used `!wash` today: `caller.timers["wash_used"]` not set or expired.

## `!wash` processor logic (`WashCommand`, system command)

1. Find target `ScentLayer`.
2. Decrement `Layers` by 1; reset `RemainingMentions` to `Layers * 3` (the new layer count's allotment).
3. If `Layers == 0`, remove entry.
4. Set `caller.timers["wash_used"] = CoolDown { timerEnd = next UTC day midnight }`.
5. Output: "Bob washes off a layer of musk. {n_remaining} layers remain."

## Persistence

- Profile: `lists["scents"]` (list of JSON-encoded `ScentLayer` or typed list).
- Profile timers: `wash_used` (24-hour, on the wash caller).

## Tests

- `OdorizeProcessorTests.cs`:
  - First !odorize creates layer with 3 mentions remaining.
  - Second !odorize same scent stacks to 2 layers / 6 mentions.
  - Same-scent stack capped at 5.
  - Cooldown set per-(initiator, recipient, scent).
- `OdorizeStatusContributorTests.cs`:
  - Descriptor matches Layers value.
  - `RemainingMentions` decrements per call.
  - Hitting 0 mentions ticks `Layers` down with reset.
  - Layer 0 removes the entry.
  - Multiple scents all contribute and decrement.
- `WashCommandTests.cs`:
  - Decrements layer by 1.
  - Once-per-day enforced.
  - Default selects highest-saturation scent.

## Assumptions

- Layers cap at 5. **Override** if higher saturation is desired.
- Mentions per layer = 3. **Override** in `OdorizeProcessor.MentionsPerLayer`.
- Both initiator and recipient profiles' scents contribute. **Override:** restrict to recipient if the spec proves overwhelming.
- `!wash` is self-only. **Override:** add an other-targeted variant later if requested (would require consent flow).
- Stacking same scent multiple times in quick succession bypasses the cooldown only when *different initiators* apply it (the cooldown is per-initiator). One initiator cannot re-odorize the same recipient with the same scent for 7 days.

## Files to create/modify

- `FChatDicebot/Model/ScentLayer.cs` *(new)*
- `FChatDicebot/InteractionProcessors/Consequence/OdorizeProcessor.cs` *(new)*
- `FChatDicebot/BotCommands/Odorize.cs` *(new)*
- `FChatDicebot/BotCommands/Wash.cs` *(new)*
- `FChatDicebot/InteractionProcessors/StatusEffectContributors/OdorizeStatusContributor.cs` *(new)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` *(modify — register)*
- `FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs` *(modify — register)*
- `FChatDicebot.Tests/Unit/OdorizeProcessorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/OdorizeStatusContributorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/WashCommandTests.cs` *(new)*
