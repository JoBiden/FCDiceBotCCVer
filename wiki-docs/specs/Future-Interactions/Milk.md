# `!milk`

Milk a substance (or other bodily fluid) from the recipient. Produces tagged bottles in the initiator's milk inventory which can be kept as trophies or sold.

**Investment level:** Involved
**Reversal:** none
**Depends on:** [Currency-and-Milk-Inventory](Infrastructure/Currency-and-Milk-Inventory.md), [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md)

## Command syntax

```
!milk [user]Bob[/user] {substance}
```

## Validation

- Recipient must be registered (base validation).
- `substance` must resolve via the existing substance/vice catalog (`MonDB.getIdentifier(substance)` returns an Identifier with a "substance" or "vice" category). Reject otherwise with `ChateauInteractionHandler.typeNotFoundText("substance")`.
- Initiator must have **at least one empty bottle** (`profile.emptyBottles >= 1`). Otherwise reject with the no-bottles message in the inventory infra spec.
- **Daily uniqueness check:** the (initiator, recipient) pair may only `!milk` once per UTC day. Reject if the pair has milked already today (regardless of substance — milking once exhausts the pair for the day).
- Self-target **not** allowed. Reject with "You cannot milk yourself."
- Recipient broken-state check via status-effect blockers (`break:breast` blocks `!milk` if substance is breast-milked; see [Break-and-Rest](Break-and-Rest.md) blocking matrix).

## Processor logic (`MilkProcessor`)

1. Roll bottle quantity: `Random.Next(1, 4)` → 1–3 bottles.
2. Determine corruption tag from recipient's corruption value (see [Corrupt-and-Purify](Corrupt-and-Purify.md)):
   - `corruption <= -10` → `"corrupt"`
   - `corruption >= 10` → `"purified"`
   - else → `null`
3. Decrement initiator `emptyBottles` by `quantity` (clamped — if initiator has fewer empty bottles than the rolled quantity, cap quantity at empties available, but always produce at least 1).
4. Append a `MilkBottle` to initiator's `milkInventory`:
   ```
   substance = identifier
   sourceName = recipient.userName
   milkedAt = DateTime.UtcNow
   quantity = rolled quantity
   corruptionTag = computed tag
   ```
5. Set the daily-pair cooldown: `initiator.timers["milk_pair_" + recipient.userName] = CoolDown { timerEnd = DateTime.UtcNow.Date.AddDays(1) }`. Symmetric: also set the recipient's mirrored timer so the pair is locked from either direction.
6. Increment counts: `milkgive` for initiator, `milktake` for recipient.
7. Save interaction. Delete pending command.

## Persistence

- Profile: `milkInventory`, `emptyBottles` (from infra spec).
- Profile timers: `milk_pair_{otherUserName}` (24-hour daily lock per pair, set on both profiles).

## Status-effect contributions

None. (Milking is an action, not a passive status.)

## Status-effect consumption

Completion message reads recipient's corruption / odorize / dose state for flavor:

- Corrupt recipient: "the substance has a faint dark sheen to it…"
- Purified recipient: "the substance practically glows…"
- Dosed recipient (matching substance): "—and it's *positively saturated* with traces of {vice}."

## Completion message

Template:

> "{Initiator} milks {recipient} for {quantity} bottle(s) of {substanceText}. Bottled, sealed, and tagged. {flavorAppendix}"

`substanceText` uses `Utils.SubstanceToText(identifier)`.

## Consent warning

> "{Initiator} wants to milk {quantity-unknown-yet} bottles of {substanceText} from you, {recipient}. The bottles will go into their personal trophy collection and cannot be transferred. Do you !consent?"

If the recipient is corrupted/purified, the warning includes the modifier flavor so consent is informed.

## Tests

- `MilkProcessorTests.cs`:
  - Daily pair lock: second milk same day rejects.
  - Substance must be in catalog.
  - Self-target rejected.
  - Empty-bottles depletion / floor at 1.
  - Corruption tag thresholds applied correctly.
  - `break:breast` blocker rejects breast-substance milkings.
- `MilkBottleIntegrationTests.cs`:
  - Bottle ends up in initiator's inventory with correct fields.
  - Counts increment.

## Assumptions

- Bottle quantity range: 1–3, uniform random. **Override** with rarity-weighted distribution if requested.
- Corruption thresholds: ±10. **Override** in spec for [Corrupt-and-Purify](Corrupt-and-Purify.md).
- Pair lock applies regardless of substance choice. The user said one milking per pair per day; substance-specific locks would weaken that. **Override** if substance-keyed locks preferred.
- The `break` matrix entry (`break:breast` blocks breast-derived substances only) is inferred — see [Break-and-Rest](Break-and-Rest.md) for the proposed mapping. Confirm before implementation.

## Files to create/modify

- `FChatDicebot/InteractionProcessors/Involved/MilkProcessor.cs` *(new)*
- `FChatDicebot/BotCommands/Milk.cs` *(new)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` *(modify — register)*
- `FChatDicebot.Tests/Unit/MilkProcessorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/MilkBottleIntegrationTests.cs` *(new)*
