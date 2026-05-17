# Currency-and-Milk-Inventory

Adds copper/silver/gold conversion to the existing Chateau currency, a dedicated *milk* inventory on profiles, and a `!sell` command. **No general-purpose inventory** — this exists only for milked substance bottles.

**Status:** Infrastructure spec — implement before [Milk](../Milk.md).

## Currency

The Chateau already has a coin currency awarded by `!work` and `!volunteer`, with `!pay` for transfers. This spec adds denominations:

| Coin | Equivalent |
|------|------------|
| copper | base unit |
| silver | 100 copper |
| gold | 100 silver = 10 000 copper |

(Stated user equivalence: 100 copper / 10 silver / 1 gold. Lock that exact ratio in code as constants in `ChateauCurrency.cs`.)

Internal storage is **always copper** (an `int64` count on the profile). Display logic decomposes into the largest denomination that yields a non-zero whole number, e.g. `12,345 copper` displays as `1g 23s 45c`.

`!pay` keeps its current behavior; arguments accept any denomination shorthand: `!pay [user]X[/user] 1g 50s` parses to 15 000 copper.

## Milk inventory

Each `Profile` gains:

```csharp
public List<MilkBottle> milkInventory;

public class MilkBottle
{
    public string substance;     // identifier from existing substance catalog
    public string sourceName;    // recipient's userName at the time of milking
    public DateTime milkedAt;    // for tiebreakers / display ordering
    public int quantity;         // bottles produced in one milking session (>=1)
    public string corruptionTag; // optional flavor tag, e.g. "corrupt", "purified" — see Corrupt-and-Purify.md
}
```

**Why a list, not a dict:** different milking sessions of the same `(substance, source)` should remain visually distinct (different `milkedAt`, possibly different `corruptionTag`). They can be displayed grouped by `(substance, sourceName)` but stored separately.

**No item transfers** — bottles cannot be given to another user. They are either kept as a trophy or sold.

## Empty-bottle currency

A separate count on the profile:

```csharp
public int emptyBottles;
```

`!sell` returns one empty bottle per bottle sold. Empty bottles are required to *receive* milk (one per bottle's worth). New profiles start with **5 empty bottles** as a starter allowance.

If the initiator of `!milk` has zero empty bottles, the milk command fails validation with "You have no empty bottles to milk into. Sell some bottles first or pay 5 silver per empty bottle in the Chateau supplies."

(That fallback purchase isn't required for v1 — see Assumptions.)

## `!sell` command

```
!sell {amount?} {substance?} {sourceName?}
```

**Behavior:**

- `!sell` (no args) — sells the entire milk inventory.
- `!sell 3` — sells the 3 oldest bottles (FIFO).
- `!sell 3 cum` — sells the 3 oldest bottles of substance `cum`.
- `!sell 3 cum [user]Bob[/user]` — sells the 3 oldest bottles of `cum` sourced from Bob.

**Pricing** (in copper, configurable in `ChateauCurrency.cs`):

| Substance rarity tier | per bottle |
|-----------------------|-----------|
| common (milk, water)  | 5 copper |
| standard (cum, sweat, saliva) | 25 copper |
| rare (special-flagged in catalog) | 1 silver = 100 copper |

`corruptionTag` modifies price: `corrupt` = ×1.5, `purified` = ×2 (markup pays the freak market). Specifics in [Corrupt-and-Purify.md](../Corrupt-and-Purify.md).

**Output:**

```
Bob sold 3 bottles of cum (sourced from Alice) for 75 copper. Empty bottles refunded: 3.
```

**No consent flow.** `!sell` is a system command, not an interaction.

## Tests

- `MilkInventoryTests.cs`:
  - Bottle add/remove integrity.
  - Display grouping `(substance, sourceName)` aggregates correctly.
- `SellCommandTests.cs`:
  - Args resolve correctly (none/amount/substance/source).
  - Pricing tiers + corruption tags compute correctly.
  - Empty bottles refunded match sold count.
  - Selling more than owned errors gracefully.
- `CurrencyDisplayTests.cs`:
  - 12 345 copper → `"1g 23s 45c"`.
  - 50 → `"50c"`.
  - 100 → `"1s"`.

## Assumptions

- Empty bottle starting allowance: 5. **Override** by changing `ChateauCurrency.StartingEmptyBottles`.
- Buying empty bottles in-game (the "5 silver fallback" mentioned above) is **out of scope for v1**. Players acquire empties only by selling.
- Pricing tiers are user-configurable but the proposed default tier list is hardcoded; a separate spec can move it to MongoDB if desired.
- `corruptionTag` markup multipliers: corrupt ×1.5, purified ×2. **Override** in `ChateauCurrency.cs`.
- Sell aggregation order is FIFO by `milkedAt`. **Override** with `LIFO` if desired.
- Self-target sell does not apply — `!sell` operates on the initiator's own inventory only.

## Files to create/modify

- `FChatDicebot/Model/MilkBottle.cs` *(new)*
- `FChatDicebot/Model/Profile.cs` *(modify — add `milkInventory`, `emptyBottles`)*
- `FChatDicebot/ChateauCurrency.cs` *(new — denominations, pricing, parsing)*
- `FChatDicebot/BotCommands/Sell.cs` *(new)*
- `FChatDicebot/BotCommands/Pay.cs` *(modify — accept denomination shorthand)*
- `FChatDicebot.Tests/Unit/MilkInventoryTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/SellCommandTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/CurrencyDisplayTests.cs` *(new)*
