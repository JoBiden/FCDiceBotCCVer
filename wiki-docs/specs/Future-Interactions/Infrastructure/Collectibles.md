# Collectibles — user-keyed items beyond bottles

**Status:** Design. Not implemented. Awaiting owner sign-off.

Origin: resident feedback `6a6f3d94` (2026-08-02), approved for spec on 2026-08-02. Dossier:
`scripts/feedback-triage/queue/2026-08-02-6a6f3d94-generalize-the-collection-concept.md`.

> expand the 'collection' concept. Consider panties, signatures, portraits, statuettes, make sure
> it's expandable and tied to users (as opposed to currencies which are generic)

Infrastructure, not a feature. This promotes the bottle collection into a general model for
individually-identified items that remember whose they are, and ships **no new item type** — see
[Phasing](#phasing) for why that is the point rather than a shortfall.

**Depends on:** [Bottle-Consumption-And-Transfer](../../Bottle-Consumption-And-Transfer.md),
[Currency-and-Milk-Inventory](../../Infrastructure/Currency-and-Milk-Inventory.md).

---

## The split this formalizes

The contrast the submission draws is already the real architectural split on `Profile`:

| | Store | Shape |
|---|---|---|
| Generic / fungible | `Profile.currencies` | `Dictionary<string, int>` — a name and a count, no identity |
| Individual / user-keyed | `Profile.milkInventory` | one document per item, each with its own serial and donor |

`MilkBottle` already has every field a general collectible needs, and the collectible framing is
already explicit in `Bottle-Consumption-And-Transfer.md` §31 ("a low serial is demonstrably old")
and §35 ("permanent proof that you once held a bottle of Alice's corrupt milk, and drank it").
**The bottle collection is this system, built once for one item type.** The work is promotion, not
invention.

---

## Owner decisions this spec is built on

Answered during the 2026-08-02 feedback review.

1. **Absorb bottles, with bottles as the first collectible type.** One system, one verb set. Costs
   a migration of live data.
2. **A single subject name, not subject-plus-creator.** Not the recommended option, and the
   consequence is stated rather than hidden: a *portrait of Alice by Bob* cannot be represented —
   two of the four examples in the submission (portraits, statuettes) are that shape. See
   [Where a second name would go](#where-a-second-name-would-go); the slot is marked so the
   decision can be revisited before live data exists, which is the cheap moment.
3. **Code, one class per type** — not data-driven via an `Identifier` category. Every new
   collectible is a PR and a redeploy, and types do not get `Identifier.displayText` / `eicon`
   rendering for free. In exchange the model is strongly typed, per-type fields need no extension
   slot, and `MilkBottle`'s `corruptionTag` / `emptiedAt` stay ordinary fields on a subclass
   instead of entries in a generic bag.
4. **Phase 1 ships no new item type** — assumed, not decided; the question was not covered by (3).
   Overridable at sign-off.

---

## Model

### The base class

```
Model/Collectible.cs          abstract class Collectible
Model/MilkBottle.cs           class MilkBottle : Collectible      (existing fields stay)
```

| Member | Type | Notes |
|---|---|---|
| `serial` | `int` | globally unique across **all** types, never reused |
| `subjectName` | `string` | whose it is. Frozen `userName` — see below |
| `acquiredAt` | `DateTime` | generalizes `milkedAt`; drives newest-first ordering |
| `TypeLabel` | `abstract string` | "bottle", "pair of panties" — for grouping and display |
| `IsSellable` | `virtual bool` | default true |
| `IsTransferable` | `virtual bool` | default true |
| `DescribeFor(IChateauDatabase)` | `abstract string` | the item's own one-line rendering |

`MilkBottle` keeps `substance`, `corruptionTag`, `emptiedAt`, `quantity` (legacy) as its own
fields, and overrides `IsSellable => !IsEmpty` — a rule that is currently enforced by callers and
becomes the type's own business, which is most of the value of this shape.

### Frozen names, resolved late

`subjectName` stores the `userName` at acquisition time and **does not follow renames**. It must be
resolved through `GetDisplayName` before reaching any player-facing string — exactly what
`MilkBottle.sourceName` already does and documents. This is the "tied to users" property the
submission asks for, and it is already solved including the rename trap; the base class inherits the
solution and the invariant.

### Where a second name would go

A `createdBy` field on `Collectible`, nullable, alongside `subjectName`: `subjectName` is who the
item is *of* or *from*, `createdBy` is who made it. `MilkBottle` would leave it null forever.
Adding it now costs one nullable field and one line in the display helper. Adding it after live
non-bottle data exists costs a second migration and a re-read of every rendering path. Flagged
here per the review; the rest of the spec assumes it is absent.

### One shared serial space

All types draw from the **same** counter, so serial #1 is the oldest *thing* anyone holds rather
than the oldest bottle. Per-type counters would let two items both be "#1" and would break the
property the bottle spec is built on.

The persisted counter document keeps its historical `_id` of `bottleSerial`
(`ChateauDatabase.BottleSerialCounterId`). The C# surface is renamed
(`ClaimCollectibleSerials`, `CollectibleSerialCounterId`) with a comment saying why the stored key
disagrees. **Do not rename the counter document.** A rename that is applied to the code but missed
in the data restarts the counter at 1 and collides serials with every bottle in existence — a
permanent, unfixable data injury traded for a tidier string.

---

## Persistence and migration

`Profile.milkInventory : List<MilkBottle>` becomes `Profile.collectibles : List<Collectible>`.

Polymorphic storage means the Mongo driver writes a `_t` discriminator on each element
(`[BsonKnownTypes(typeof(MilkBottle))]` on the base). **Existing elements have no `_t`**, and an
abstract declared type cannot deserialize a document without one — so this is not an optional
tidying step, it is required for the profiles to load at all.

### Backfill script

`scripts/backfill-collectibles.js`, mongosh, following the existing one-off script convention.
Per `RegisteredProfiles` document:

1. Skip if `collectibles` already exists (idempotent — safe to re-run, and it will be re-run).
2. Copy `milkInventory` to `collectibles`, stamping `_t: "MilkBottle"` on every element.
3. Unset `milkInventory`.
4. Report counts: profiles touched, elements stamped, profiles skipped.

Run it **with the bot stopped**. There is no version of this that is safe to run live: a profile
written by the running bot between read and write loses whatever the script just did to it, and
the shapes are not compatible enough for the two to coexist.

Rollback is the inverse script, and is worth writing at the same time rather than at the moment it
is needed.

### Interface changes

| Now | Becomes |
|---|---|
| `IChateauDatabase.SetMilkInventory(userName, List<MilkBottle>)` | `SetCollectibles(userName, List<Collectible>)` |
| `ChateauDatabase.ClaimBottleSerials(count)` | `ClaimCollectibleSerials(count)` |
| `Profile.milkInventory` | `Profile.collectibles` |

`SetCollectibles` keeps the re-fetch-immediately-before-write behavior `SetMilkInventory` has —
it exists because a targeted write must not clobber a concurrent profile update, and that reason
does not change.

---

## Verbs

The bottle commands are the reference implementations of the four shapes a collection needs.

| Shape | Bottle version | Generalized |
|---|---|---|
| List | `!bottles` | `BottleInventory` → `CollectionInventory`, type-filtered |
| Consume | `!drink` | stays bottle-specific — consumption is per-type by nature |
| Sell | `!sell` | reads `IsSellable`; pricing stays per-type |
| Transfer | `!pay` | reads `IsTransferable`; `BottlePayment` generalizes |

`BottleInventory`'s selection, grouping, serial-formatting (`FormatSerials`, display cap) and
filter helpers are already type-agnostic apart from their signatures. Generalizing them is a
mechanical change: `MilkBottle` → `Collectible`, plus a type filter alongside the existing
substance and source filters.

**Phase 1 adds no command.** `!bottles` becomes a bottle-filtered view over `collectibles` and
behaves identically. A generic `!collection` list command arrives with the second type, when there
is something for it to show that `!bottles` cannot — adding it now would ship two commands with
identical output.

### Privacy

`Bottle-Consumption-And-Transfer.md` §113: another resident's collection is visible only in
summary — counts public, donor names not. This becomes a **base-class rule inherited by every
type**, not a per-type decision, so a new collectible cannot accidentally leak who it came from.
`!dossier`'s collection block enforces it in one place.

---

## Acquisition is the real work

Bottles exist because `!milk` makes them. Every new collectible needs its own way of coming into
being — an interaction, an event reward, a purchase — and **that, not the storage, is where the
effort is for each type**. This spec deliberately provides none of it: it provides the shelf, not
the things on it.

A rough shape for what a future type costs, so the phasing decision is informed: one `Collectible`
subclass, one acquisition path (usually a processor + command + registry entry + tests), one
display line, and a redeploy. The storage, serial, listing, selling, transfer, privacy, and
rename-safety are free by then. That ratio is the whole argument for doing this.

---

## Phasing

**Phase 1 — the framework, invisible to residents.** Base class, polymorphic storage, the
migration, generalized inventory helpers, `MilkBottle` reparented. Every existing bottle test
passes **unmodified** — that is the acceptance criterion, and it is a strong one: it means the
promotion changed no behavior.

Shipping with zero new item types is legitimate and probably correct. It is not user-visible on
its own, and that is exactly what makes it safe: nothing residents can see changes, so nothing
residents can see can break. Trying to land the framework *and* panties *and* portraits in one
pass is how this becomes a large risky change instead of a foundation.

**Phase 2 and beyond — one type at a time**, each with its own spec covering its acquisition path.
Item types are the owner's creative territory and can be added indefinitely afterward.

---

## Files

### New

| File | What |
|---|---|
| `Model/Collectible.cs` | the abstract base |
| `CollectionInventory.cs` | generalized `BottleInventory` (rename in place) |
| `scripts/backfill-collectibles.js` | the migration |
| `scripts/rollback-collectibles.js` | the inverse |
| `FChatDicebot.Tests/Unit/Collectiblemodeltests.cs` | see Tests |

### Modified

| File | Change |
|---|---|
| `Model/MilkBottle.cs` | reparent to `Collectible`; `sourceName` → `subjectName`, `milkedAt` → `acquiredAt` |
| `Model/ChateauDB.cs` | `milkInventory` → `collectibles`, retyped |
| `Database/Ichateaudatabase.cs`, `Chateaudatabase.cs` | `SetCollectibles`, `ClaimCollectibleSerials` |
| `BottlePayment.cs` | reads `IsTransferable` |
| `BotCommands/ChateauBottles.cs`, `ChateauDrink.cs`, `ChateauSell.cs`, `ChateauBank.cs`, `ChateauDossier.cs` | field rename; `!sell` reads `IsSellable` |
| `InteractionProcessors/Involved/MilkProcessor.cs` | writes `collectibles` |
| `wiki-docs/Database-and-Persistence.md`, `Bottle-Consumption-And-Transfer.md` | as-shipped notes |

`FChatDicebot.csproj` globs `**\*.cs`, so new files need no csproj edit.

---

## Tests

- **The existing bottle suite passes unmodified.** Non-negotiable, and the main assertion this
  phase makes.
- Polymorphic round-trip: a `MilkBottle` in a `List<Collectible>` saves and loads with every field
  intact and the right runtime type.
- A pre-migration profile document (no `_t`, field named `milkInventory`) fails to load — pinning
  *why* the migration is mandatory, so nobody later "simplifies" it away.
- Migration: stamps `_t`, renames the field, preserves serials and order; **re-running it changes
  nothing**; a profile with an empty or absent inventory survives.
- Serial uniqueness across types: two different subclasses claiming serials never collide, and
  claims stay contiguous per call.
- `IsSellable` / `IsTransferable` are honored by `!sell` and `!pay` — an empty bottle is still
  unsellable, via the override rather than the caller's check.
- `subjectName` is resolved through `GetDisplayName` at display time, so a renamed donor shows
  their current name.
- Privacy: another resident's collection renders counts without subject names.

---

## User-facing text

**None in Phase 1.** No string changes, by design — residents should not be able to tell this
shipped.

One thing to watch at sign-off: if the framework ever leads to `!bottles`, `!drink`, `!sell` and
`!bank` being reworded from "the collection" to "a collectible you hold", that is a wording pass
over a system whose text is already settled, and every one of those strings comes back for approval
first. Phase 1 does not do it and should not.

---

## Assumptions

1. **Phase 1 ships no new item type.** Not covered by the owner's answers; the lower-risk read.
2. **One shared serial space**, keeping the historical `bottleSerial` counter `_id`.
3. **`acquiredAt` replaces `milkedAt`** on the base rather than each type carrying its own
   timestamp — newest-first ordering is a collection-wide concern.
4. **`quantity` stays on `MilkBottle`** as the legacy field it already is; it does not go on the
   base class, since no future type should have one.
5. **No `createdBy`**, per the owner's answer. Marked above.
6. **The migration runs with the bot stopped**, once, with a rollback script written alongside it.
7. **`!collection` waits for the second type.**
