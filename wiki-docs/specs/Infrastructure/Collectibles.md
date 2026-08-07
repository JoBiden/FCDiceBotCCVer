# Collectibles — user-keyed items beyond bottles

**Status:** Implemented.

Origin: resident feedback `6a6f3d94` (2026-08-02), approved for spec on 2026-08-02. Dossier:
`scripts/feedback-triage/queue/2026-08-02-6a6f3d94-generalize-the-collection-concept.md`.

> expand the 'collection' concept. Consider panties, signatures, portraits, statuettes, make sure
> it's expandable and tied to users (as opposed to currencies which are generic)

Infrastructure plus its first tenant. This promotes the bottle collection into a general model for
individually-identified items that remember whose they are, and ships **panties** as the first
non-bottle type to prove the abstraction against something that isn't milk.

**Depends on:** [Bottle-Consumption-And-Transfer](../Bottle-Consumption-And-Transfer.md),
[Currency-and-Milk-Inventory](Currency-and-Milk-Inventory.md), and — for the panties verb pair —
[Invertible-Role-Interactions](Invertible-Role-Interactions.md), which shipped first and
separately.

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

Answered during the 2026-08-02 feedback review, completed at sign-off on 2026-08-07.

1. **Absorb bottles, with bottles as the first collectible type.** One system, one verb set. Costs
   a migration of live data.
2. **A single subject name, not subject-plus-creator.** Re-asked at sign-off — Phase 1 is the last
   cheap moment — and **held**. The consequence is stated rather than hidden: a *portrait of Alice
   by Bob* cannot be represented, so portraits and statuettes (two of the four examples in the
   submission) are out of reach until this is revisited. See
   [Where a second name would go](#where-a-second-name-would-go).
3. **Code, one class per type** — not data-driven via an `Identifier` category. Every new
   collectible is a PR and a redeploy, and types do not get `Identifier.displayText` / `eicon`
   rendering for free. In exchange the model is strongly typed, per-type fields need no extension
   slot, and `MilkBottle`'s `corruptionTag` / `emptiedAt` stay ordinary fields on a subclass
   instead of entries in a generic bag.
4. **Phase 1 ships one new type: panties.** Overrides the original spec's assumption of
   framework-only. Validates the abstraction against a non-bottle item and gives residents
   something visible.
5. **Panties are transferable but not sellable.** The Chateau won't buy them — a keepsake, not a
   commodity. This is also what makes `IsSellable` do real work on day one instead of being a hook
   with only bottles behind it.
6. **Panties are acquired through a two-phase consent interaction that works in both directions**:
   asked for or handed over, with the recipient consenting either way. Verbs are `!panties` and
   `!givepanties`.
7. **The invertible-role plumbing is extracted first, as its own PR.** See
   [Sequencing](#sequencing).

---

## Model

### The base class

```
Model/Collectible.cs          abstract class Collectible
Model/MilkBottle.cs           class MilkBottle : Collectible      (existing fields stay)
Model/Panties.cs              class Panties : Collectible
```

| Member | Type | Notes |
|---|---|---|
| `serial` | `int` | globally unique across **all** types, never reused |
| `subjectName` | `string` | whose it is. Frozen `userName` — see below |
| `acquiredAt` | `DateTime` | generalizes `milkedAt`; drives newest-first ordering |
| `TypeLabel` | `abstract string` | "bottle", "pair of panties" — for grouping and the type filter |
| `IsSellable` | `virtual bool` | default true |
| `IsTransferable` | `virtual bool` | default true |

`MilkBottle` keeps `substance`, `corruptionTag`, `emptiedAt`, `quantity` (legacy) as its own
fields, and overrides `IsSellable => !IsEmpty` — a rule that is currently enforced by callers and
becomes the type's own business, which is most of the value of this shape. `Panties` overrides
`IsSellable => false`.

**No `DescribeFor` on the base.** The original draft had an abstract `DescribeFor(IChateauDatabase)`,
but nothing in this phase calls it: `!bottles` renders through the group collapser, not per-item
one-liners, and the panties listing does the same. An abstract method every subclass must implement
and nothing invokes is dead weight. It arrives when a cross-type `!collection` view needs it.

### Frozen names, resolved late

`subjectName` stores the `userName` at acquisition time and **does not follow renames**. It must be
resolved through `GetDisplayName` before reaching any player-facing string — exactly what
`MilkBottle.sourceName` already does and documents. This is the "tied to users" property the
submission asks for, and it is already solved including the rename trap; the base class inherits the
solution and the invariant.

### Where a second name would go

A `createdBy` field on `Collectible`, nullable, alongside `subjectName`: `subjectName` is who the
item is *of* or *from*, `createdBy` is who made it. Both shipped types would leave it null forever.

Re-asked at sign-off and declined. Recording the cost so a later reversal is an informed one:
adding it now would have been one nullable field and one line in the display helper. Adding it
after live non-bottle data exists costs a second migration and a re-read of every rendering path.
Portraits and statuettes are the shapes this rules out.

### One shared serial space

All types draw from the **same** counter, so serial #1 is the oldest *thing* anyone holds rather
than the oldest bottle. Per-type counters would let two items both be "#1" and would break the
property the bottle spec is built on.

The persisted counter document keeps its historical `_id` of `bottleSerial`
(`ChateauDatabase.CollectibleSerialCounterId`). The C# surface is renamed
(`ClaimCollectibleSerials`, `CollectibleSerialCounterId`) with a comment saying why the stored key
disagrees. **Do not rename the counter document.** A rename that is applied to the code but missed
in the data restarts the counter at 1 and collides serials with every bottle in existence — a
permanent, unfixable data injury traded for a tidier string.

This is deliberately *not* the same call made for the per-element field names below, and the
difference is that the counter has no migration touching it while the elements do. See
[Field renames](#field-renames).

---

## Persistence and migration

`Profile.milkInventory : List<MilkBottle>` becomes `Profile.collectibles : List<Collectible>`.

Polymorphic storage means the Mongo driver writes a `_t` discriminator on each element
(`[BsonKnownTypes(typeof(MilkBottle), typeof(Panties))]` on the base). **Existing elements have no
`_t`**, and an abstract declared type cannot deserialize a document without one — so this is not an
optional tidying step, it is required for the profiles to load at all.

A discriminator convention that defaulted absent-`_t` to `MilkBottle` would make the migration
optional and is rejected on purpose: it turns "which subclass is this" into a silent guess that
stays wrong forever once there are three types, and it trades a loud startup failure for quiet
mistyping. The migration stays mandatory.

### Field renames

The rename of `sourceName` → `subjectName` and `milkedAt` → `acquiredAt` is **not** a code-only
change. Those are Mongo element names. Renaming the C# properties without either pinning the
stored keys or migrating them makes every existing bottle load with a null donor and a zeroed
timestamp — the exact injury the counter-document warning above is about, applied to every bottle
in the world instead of one document.

**Decision: migrate the element keys**, in the same pass that stamps `_t`. The migration already
rewrites every element, so the renames cost nothing extra, and code and data agree permanently
instead of carrying a `[BsonElement]` mismatch in two places forever.

The consequence is that **the rollback script has to undo the key renames correctly too**, or a
rollback becomes the data injury the migration avoided. Both directions are written together.

### Correction: a skipped migration is silent, not loud

An earlier draft of this section argued the failure modes were "coupled and safe" — that a missed
migration would fail loudly on the absent `_t` before a renamed key could lose a donor name. **That
is wrong**, and the tests written for this phase demonstrate it. There are two distinct failure
modes, not one:

| Situation | What happens |
|---|---|
| Document reached the new field name, `_t` missing | Throws on load. Loud, non-destructive. |
| Document never migrated at all (still `milkInventory`) | **Loads fine, with an empty collection.** |

The second is the dangerous one and the likelier one. The Mongo driver ignores fields the model
doesn't declare, so an unmigrated profile doesn't fail — it comes back looking like a resident who
owns nothing, and the next write persists that emptiness. The bottles are then gone for real, with
no error at any point.

**So the migration needs a guard, not just a script.** `CollectiblesMigrationCheck.AssertMigrated`
runs immediately after `MonDB.Initialize` in `BotMain`, counts profiles still carrying
`milkInventory`, and refuses to start with a message naming the script to run. That converts the
silent mode into the loud one, which is what the original reasoning assumed was already true. It
passes on a fresh database, so a new install is never told to run a migration it doesn't need.

### Backfill script

`scripts/backfill-collectibles.js`, mongosh, following the existing one-off script convention
(`scripts/backfill-bottle-serials.js` is the closest model). Per `RegisteredProfiles` document:

1. Skip if `collectibles` already exists (idempotent — safe to re-run, and it will be re-run).
2. Copy `milkInventory` to `collectibles`, and per element: stamp `_t: "MilkBottle"`, rename
   `sourceName` → `subjectName`, rename `milkedAt` → `acquiredAt`.
3. Unset `milkInventory`.
4. Report counts: profiles touched, elements stamped, profiles skipped.

`scripts/rollback-collectibles.js` is the exact inverse, including both key renames, and is
written at the same time rather than at the moment it is needed.

Run it **with the bot stopped**. There is no version of this that is safe to run live: a profile
written by the running bot between read and write loses whatever the script just did to it, and
the shapes are not compatible enough for the two to coexist.

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
| List | `!bottles` | stays bottle-specific; `!panties` listing is its own view |
| Consume | `!drink` | stays bottle-specific — consumption is per-type by nature |
| Sell | `!sell` | reads `IsSellable`; pricing stays per-type |
| Transfer | `!pay` | reads `IsTransferable`; `BottlePayment` generalizes |

### CollectionInventory is a split, not a rename

The original draft claimed `BottleInventory`'s helpers were "already type-agnostic apart from their
signatures" and could be renamed in place. That is not accurate, and building on it would have
produced a class that pretends to be general while reading bottle fields:

- `Group()` keys on substance + `sourceName` + `corruptionTag`
- `CountsBySubstance()` reads `substance`
- `MatchesFilters()` filters on substance and source

None of those exist on `Collectible`. Only `FindBySerial`, `IsCollectionEmpty` and `FormatSerials`
genuinely generalize.

So: a new `CollectionInventory` holds the type-agnostic parts — `FindBySerial`,
`IsCollectionEmpty`, `FormatSerials`, and an `OfType<T>` selector — and `BottleInventory` stays as
the bottle-specific layer over it, keeping `SelectFull`, `SelectEmpty`, `Group`,
`CountsBySubstance` and `MatchesFilters`. Smaller change, honest boundary, and the panties layer
sits beside the bottle layer rather than inside a fake-general one.

**`!bottles` is unchanged in behavior.** It becomes a bottle-filtered view over `collectibles` and
renders identically. A cross-type `!collection` command is still deferred — with two types whose
display shapes differ, it needs its own design pass, and `!bottles` plus the panties view cover
what residents can actually do today.

### Privacy

`Bottle-Consumption-And-Transfer.md` §113: another resident's collection is visible only in
summary — counts public, donor names not. This becomes a **base-class rule inherited by every
type**, not a per-type decision, so a new collectible cannot accidentally leak who it came from.
`!dossier`'s collection block enforces it in one place. Panties inherit it without deciding
anything, which is the rule working as intended.

---

## Panties — the first non-bottle type

Bottles exist because `!milk` makes them. Every collectible needs its own way of coming into being,
and **that, not the storage, is where the effort is for each type**. For panties that is an
invertible two-phase consent interaction.

### The verb pair

| Verb | Who ends up holding | Whose panties | Who consents |
|---|---|---|---|
| `!panties [user]` | the initiator | the recipient | the recipient |
| `!givepanties [user]` | the recipient | the initiator | the recipient |

One processor, two verbs, the typed verb deciding which side is the subject — structurally the
same shape as `!climaxfor`/`!climax` and `!drinkfrom`/`!forcedrink`. That shape currently exists as
a hand-copied convention rather than shared code, which is why it gets extracted first (see
[Sequencing](#sequencing)); panties then declares onto it instead of becoming the third copy.

Which of the two roles the interaction's flavor and eicon attach to is settled in the panties PR,
not here — the role plumbing gives the vocabulary, and the answer is a wording question.

### Behavior

- Minted into the holder's `collectibles` with `subjectName` = the donor's frozen `userName` and a
  serial from the shared counter.
- `IsSellable => false`, `IsTransferable => true` — giftable with `!pay`, never bought by the
  Chateau. `!sell` needs a clear refusal rather than a silent skip.
- Cooldown-gated per direction, following the `!milk` precedent. No currency cost and no consumable:
  residents are assumed to have more panties.
- Give/take counts from the subject's perspective, matching the project-wide convention
  (`climaxgive`/`climaxtake`, `milkgive`/`milktake`).

Everything else — storage, serial, listing, transfer, privacy, rename-safety — is inherited. That
ratio is the whole argument for doing this.

---

## Sequencing

Three PRs, in order. Each does one job.

**PR 1 — [Invertible-Role-Interactions](Invertible-Role-Interactions.md).**
Name the two-verb/one-processor pattern once and retrofit `ClimaxforProcessor` and
`SourceDrinkProcessor` onto it. No behavior change; their existing test suites are the proof. This
lands first so panties never becomes a third hand-copy, and so a consent-system refactor never
shares a diff with a live data migration.

**PR 2 — the framework and the migration.** Base class, polymorphic storage, the backfill and
rollback scripts, the `CollectionInventory`/`BottleInventory` split, `MilkBottle` reparented.
Invisible to residents: no user-facing string changes at all, which is what makes it safe to land
on its own.

**PR 3 — panties.** The `Panties` type, the processor, `!panties` and `!givepanties`, the registry
entry, tests, and the user-facing text.

Beyond that: **one type at a time**, each with its own spec covering its acquisition path. Item
types are the owner's creative territory and can be added indefinitely afterward.

---

## Files

### New

| File | What |
|---|---|
| `Model/Collectible.cs` | the abstract base |
| `CollectiblesMigrationCheck.cs` | startup guard against an unmigrated database |
| `Model/Panties.cs` | the first non-bottle type |
| `CollectionInventory.cs` | the type-agnostic half split out of `BottleInventory` |
| `InteractionProcessors/Involved/PantiesProcessor.cs` | acquisition |
| `InteractionProcessors/Involved/PantiesCommandSupport.cs` | shared command wiring for both verbs |
| `BotCommands/ChateauPanties.cs`, `ChateauGivepanties.cs` | the two verbs |
| `scripts/backfill-collectibles.js` | the migration |
| `scripts/rollback-collectibles.js` | the inverse |
| `FChatDicebot.Tests/Unit/Collectiblemodeltests.cs` | see Tests |
| `FChatDicebot.Tests/Unit/Pantiesprocessortests.cs` | see Tests |

### Modified

| File | Change |
|---|---|
| `Model/MilkBottle.cs` | reparent to `Collectible`; `sourceName` → `subjectName`, `milkedAt` → `acquiredAt` |
| `Model/ChateauDB.cs` | `milkInventory` → `collectibles`, retyped |
| `Database/Ichateaudatabase.cs`, `Chateaudatabase.cs` | `SetCollectibles`, `ClaimCollectibleSerials` |
| `BottleInventory.cs` | bottle-specific helpers only; shared ones move to `CollectionInventory` |
| `BottlePayment.cs` | reads `IsTransferable` |
| `BotCommands/ChateauBottles.cs`, `ChateauDrink.cs`, `ChateauSell.cs`, `ChateauBank.cs`, `ChateauDossier.cs` | field rename; `!sell` reads `IsSellable` |
| `InteractionProcessors/Involved/MilkProcessor.cs` | writes `collectibles` |
| `InteractionProcessors/InteractionProcessorRegistry.cs` | register `PantiesProcessor` |
| `wiki-docs/Database-and-Persistence.md`, `Command-Reference.md`, `Bottle-Consumption-And-Transfer.md` | as-shipped notes |

`FChatDicebot.csproj` globs `**\*.cs`, so new files need no csproj edit.

---

## Tests

### The bottle suite is the regression proof

The original draft made "every existing bottle test passes **unmodified**" the acceptance
criterion. That cannot hold alongside a file table renaming five identifiers used roughly sixty
times across eleven test files — the suite would not compile, let alone pass.

The criterion that actually means what that one was reaching for: **no assertion in the existing
bottle suite changes. Identifier renames only.** A reviewer can diff the test files and confirm
every hunk is a rename, which is a stronger and checkable claim.

### New coverage

- Polymorphic round-trip: a `MilkBottle` and a `Panties` in one `List<Collectible>` save and load
  with every field intact and the right runtime types.
- A pre-migration profile document (no `_t`, field named `milkInventory`) fails to load — pinning
  *why* the migration is mandatory, so nobody later "simplifies" it away.
- The stored shape the migration must produce: `_t` is the bare class name, and the element keys
  are `subjectName` / `acquiredAt` with no `sourceName` / `milkedAt` left behind. This is what
  stops the script and the model drifting apart.
- **An unmigrated profile loads with an empty collection** — the silent failure mode, pinned as a
  test so it stays visible rather than being rediscovered.
- The startup guard throws on an unmigrated database with an actionable message, passes on a
  migrated one, and passes on a fresh one.

The mongosh scripts themselves are verified by their `DRY_RUN` output against real data, following
the existing convention (`scripts/backfill-bottle-serials.js` has no unit tests either). The C#
tests pin the contract the scripts have to satisfy; running them is still an operator step.
- Serial uniqueness across types: a bottle and a pair of panties never collide on a number, and
  claims stay contiguous per call.
- `IsSellable` is honored by `!sell` — an empty bottle is still unsellable via the override rather
  than the caller's check, and panties are refused with a message rather than silently skipped.
- `IsTransferable` is honored by `!pay`, with panties transferring and the flag actually consulted.
- `subjectName` is resolved through `GetDisplayName` at display time, so a renamed donor shows
  their current name — for both types.
- Privacy: another resident's collection renders counts without subject names, panties included.
- Panties acquisition in both directions: the right side holds, the right side is the subject, the
  right side consents, and the cooldown blocks a repeat.

---

## User-facing text

**PR 2 changes no strings, by design** — residents should not be able to tell the framework
shipped.

**PR 3 introduces new strings**: the consent prompts for both verbs, the completion messages, the
`!sell` refusal for a non-sellable item, the panties listing rows, and the help fields for
`!panties` and `!givepanties`. Every one of them is drafted against `wiki-docs/Style-Guide.md` and
surfaced before → after for approval before that PR is called done.

Not in scope, in any of the three: rewording `!bottles`, `!drink`, `!sell` or `!bank` from "the
collection" to "a collectible you hold". That is a wording pass over a system whose text is already
settled, and it needs its own approval round if it ever happens.

---

## Intended follow-on: collection achievements

Recorded 2026-08-07, not specced. A collection is an obvious source of achievement titles and the
existing title system has no shape that fits it — breadth across distinct subjects, depth from one
resident, holding one of every type, and the provenance shapes a shared age-ordered serial makes
possible. Written up as **Axis 8** in
[Achievement-Titles](../Future-Social/Achievement-Titles.md#axis-8--collection--checkcollectiontitlesprofile--zero-new-data),
alongside the seven axes already designed there.

Everything except a "this item has changed hands" shape is zero new data. That one needs a
previous-holder or hop-count field on `Collectible`, and it is worth deciding **before** more item
types exist — the same timing argument that applied to `createdBy`, and the same one that will keep
applying every time this model grows.

---

## Differences from the original spec

Five things changed between the signed-off design and what shipped. Each is described where it
belongs above; collected here so the divergence is findable.

1. **The migration needed a startup guard, not just a script.** The spec argued a skipped
   migration would fail loudly. It doesn't — an unmigrated profile loads silently with an empty
   collection. `CollectiblesMigrationCheck` was added to close that. See
   [Correction: a skipped migration is silent, not loud](#correction-a-skipped-migration-is-silent-not-loud).

2. **`CollectionInventory` is a split, not a rename.** The spec claimed `BottleInventory`'s helpers
   were type-agnostic apart from their signatures. Three of them read `substance` /
   `corruptionTag`, which don't exist on the base. Only `FindBySerial`, `IsCollectionEmpty` (as
   `HasNone<T>`) and `FormatSerials` moved. See
   [CollectionInventory is a split, not a rename](#collectioninventory-is-a-split-not-a-rename).

3. **`IsCollectionEmpty` stayed per-type.** Making it "holds nothing at all" would have broken
   `!bottles` the moment panties existed: a resident holding only panties would no longer get the
   "you have no bottles" branch. It is `CollectionInventory.HasNone<T>` now, with
   `BottleInventory.IsCollectionEmpty` as the bottle-shaped caller.

4. **Panties declare no `CooldownSpec`.** The design assumed one. `CooldownSpec.Binds` cannot
   express a *directional* lock — its nearest value renders "per pair", which would claim Alice
   taking Bob's pair also blocks Bob taking Alice's, and it doesn't. The two commands hand-write
   their help strings exactly as `!milk` and `!drinkfrom` do.

5. **The panties consent prompt carries no seriousness block.** An early draft used
   `ConsentWarningText.Block`. That is the commitment/consequence shape — no involved processor
   uses it, and the Style Guide reserves it for those tiers. The prompt states neither the block
   nor the frequency, matching `!milk` and `!drinkfrom`; the daily lock speaks for itself when it
   fires.

---

## Assumptions

1. **One shared serial space**, keeping the historical `bottleSerial` counter `_id` while renaming
   the C# surface.
2. **`acquiredAt` replaces `milkedAt`** on the base rather than each type carrying its own
   timestamp — newest-first ordering is a collection-wide concern.
3. **`quantity` stays on `MilkBottle`** as the legacy field it already is; it does not go on the
   base class, since no future type should have one.
4. **The migration runs with the bot stopped**, once, with a tested rollback script alongside it.
5. **A cross-type `!collection` command stays deferred** — two types is not yet enough to design it
   against.
6. **Panties have no acquisition cost** beyond the cooldown, and no consumable backing them.
