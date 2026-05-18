# Milk Inventory + Bottle Currency

Infrastructure for [`!milk`](../Milk.md) and `!sell`: a per-profile milk inventory, a side bottle-currency, and a tier-based sell-pricing table.

**Status:** Implemented (milk inventory + bottle currency + `!sell` shipped). Denomination conversion between copper/silver/gold was explicitly deferred — see [Deferred](#deferred) below.

> **Note:** This document is named `Currency-and-Milk-Inventory.md` for historical reasons (it was originally a broader currency-overhaul spec). The actual scope landed is the milk-inventory and bottle-currency infrastructure described here. The denomination-conversion ambitions of the original spec are intentionally unshipped.

## Profile additions

```csharp
public List<MilkBottle> milkInventory { get; set; } = new List<MilkBottle>();
```

`MilkBottle` (one entry per milking session — not aggregated):

```csharp
public class MilkBottle
{
    public string substance;        // identifier from substance/vice catalog
    public string sourceName;       // recipient's userName at milking time
    public DateTime milkedAt;       // for sell-order tiebreakers
    public int quantity;            // bottles produced in one milking (>=1)
    public string corruptionTag;    // "corrupt", "purified", or null
}
```

Different milking sessions of the same `(substance, sourceName)` stay as separate entries so each retains its own `milkedAt` and `corruptionTag`. Bottles cannot be transferred between users — they are either kept as a trophy or sold.

There is **no `emptyBottles` field** on `Profile`. The original spec proposed one as a precondition for `!milk`; that gate was dropped before merge. The Chateau always provides the empty bottles needed to milk.

## Currency model

The existing `currencies` dict on `Profile` (a `Dictionary<string, int>`) stays exactly as it was — separate per-denomination buckets for copper, silver, gold, tokens, etc. The original spec called for unified copper-only storage with display-time denomination decomposition; that path was abandoned during implementation. Copper, silver, and gold remain independent currencies that **do not auto-convert**.

Two new constants in [`ChateauCurrency.cs`](../../../FChatDicebot/ChateauCurrency.cs):

| Constant | Value | Used for |
|----------|-------|----------|
| `SellPayoutCurrency` | `"copper"` | The currency `!sell` deposits substance-value proceeds into. |
| `BottleCurrency` | `"bottle"` | A side-currency credited one-per-bottle when the Chateau takes the empty back at `!sell` time, or in the self-milk shortcut. |

Players accumulate `currencies["bottle"]` over time. The bottle currency is purely a tally today — no command spends it. Future features (a milking achievement, an item the bottle currency unlocks, etc.) can hook into it without changing this layer.

## Sell pricing

All pricing is in copper, hardcoded in [`ChateauCurrency.cs`](../../../FChatDicebot/ChateauCurrency.cs):

| Substance tier | Members | Base price per bottle |
|----------------|---------|-----------------------|
| common | `milk`, `water` | `CommonBottlePrice = 5` |
| standard | every substance not in either set (e.g. `cum`, `sweat`, `saliva`) | `StandardBottlePrice = 25` |
| rare | `lustessence` (flag-controlled set) | `RareBottlePrice = 100` |

`GetBasePrice(substance)` resolves to the correct tier; unknown substances fall through to standard so a missing entry doesn't zero the payout.

Corruption-tag multipliers on top of base price:

- `"corrupt"` → ×`CorruptPriceMultiplier` (1.5)
- `"purified"` → ×`PurifiedPriceMultiplier` (2.0)
- `null` or anything else → ×1

`GetSellPricePerBottle(substance, tag)` returns the final per-bottle copper price (`Math.Floor` to stay in integer currency).

Corruption-tag thresholds (also in `ChateauCurrency.cs`):

```csharp
public const int CorruptionTagThreshold = 10;
// corruption <= -10 → "corrupt"; >= +10 → "purified"; else null
```

`GetCorruptionTagForValue(int corruption)` returns the right tag string (or null) and is used by both the milk processor (at bottle-creation time) and the sell command (at sell time).

## `!sell` command

```
!sell                              — sell the single most recent bottle
!sell {amount}                     — sell N newest bottles (LIFO)
!sell {amount} {substance}         — N newest bottles of substance
!sell {amount} {substance} [user]X[/user]
                                   — N newest of substance, sourced from X
```

Sell order is **LIFO** (newest first). A single `MilkBottle` entry can be partially consumed: selling 1 from a `quantity=3` entry leaves `quantity=2` under the same `milkedAt`. Emptied entries are pruned.

**Payout per call:**

- `currencies["copper"] += sum(GetSellPricePerBottle(substance, tag) per bottle sold)`
- `currencies["bottle"] += BottlesSold` (one unit per sold bottle, as the Chateau hands the empty back)

The core logic lives in `ChateauSell.SellBottles(Profile, substanceFilter, sourceFilter, requestedAmount)` — a pure inventory-mutation helper that returns a `SellResult` and leaves persistence to the caller. The command's `Run` saves the profile and credits the two currencies separately via `Database.ChangeCurrency` so concurrent edits to other currency keys aren't clobbered.

**Failures** (private to initiator):

- Not registered → existing `notRegisteredText()`.
- Empty inventory → `"You don't have any bottles in your inventory to sell."`
- Non-positive amount → `"The amount of bottles to sell must be a positive whole number."`
- Filter matched nothing → `"No bottles in your inventory matched that filter. Use !bank or your !dossier to review what you've got bottled up."`

**Result message** (channel):

> `{Seller} sold {N} bottle(s) of {substanceText} (sourced from {SourceName}) for [b]{P} copper[/b]. Empty bottles returned: {N}.`

When the sale spans multiple `(substance, source)` combinations, line items are inlined:

> `{Seller} sold {N} bottles ({a} of {sub1} from {src1}, {b} of {sub2} from {src2}) for [b]{P} copper[/b]. Empty bottles returned: {N}.`

## Tests

- [`Chateauselltests.cs`](../../../FChatDicebot.Tests/Unit/Chateauselltests.cs): LIFO ordering, partial entry consumption, substance and source filters, pricing tiers + corruption multipliers, bottle-currency 1:1 refund, edge cases (empty inventory, zero/negative amount, filter miss).
- [`Chateaucurrencytests.cs`](../../../FChatDicebot.Tests/Unit/Chateaucurrencytests.cs): base price tier resolution (including `null` / empty / unknown substance → standard fallback), corruption-multiplier math (floor rounding), threshold bands for tag assignment.

## Deferred

The original spec proposed several adjacent features that were explicitly deferred during implementation. They remain candidates for separate future work:

- **Unified copper-only storage.** Internal storage was to be a single `int64` per profile with silver/gold as display denominations only. Per user direction (2026-05-18), currencies do not translate in the back end — copper/silver/gold remain independent.
- **Denomination shorthand parsing in `!pay`.** Originally `!pay [user]X[/user] 1g 50s` would parse to a single copper amount. `!pay` retains its existing single-currency-name syntax.
- **Currency display decomposition.** Showing `12345 copper` as `"1g 23s 45c"`. Players see currencies as separate dict entries via `!bank` exactly as before.
- **A `!convert`-style command.** Manual exchange between denominations was acknowledged as a future, separate command.
- **In-game empty-bottle purchase fallback.** The original spec mentioned a "5 silver per empty bottle" fallback at the Chateau supplies. Moot now that empties aren't a precondition.

## Files

- [`FChatDicebot/Model/MilkBottle.cs`](../../../FChatDicebot/Model/MilkBottle.cs) *(new)*
- [`FChatDicebot/Model/ChateauDB.cs`](../../../FChatDicebot/Model/ChateauDB.cs) *(added `milkInventory` to `Profile`)*
- [`FChatDicebot/ChateauCurrency.cs`](../../../FChatDicebot/ChateauCurrency.cs) *(new — tiers, multipliers, thresholds, constants)*
- [`FChatDicebot/BotCommands/ChateauSell.cs`](../../../FChatDicebot/BotCommands/ChateauSell.cs) *(new)*
- [`FChatDicebot.Tests/Unit/Chateauselltests.cs`](../../../FChatDicebot.Tests/Unit/Chateauselltests.cs) *(new)*
- [`FChatDicebot.Tests/Unit/Chateaucurrencytests.cs`](../../../FChatDicebot.Tests/Unit/Chateaucurrencytests.cs) *(new)*
