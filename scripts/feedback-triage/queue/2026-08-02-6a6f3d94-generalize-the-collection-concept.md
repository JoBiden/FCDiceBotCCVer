# 6a6f3d94 — Generalize "collections": user-keyed collectibles beyond bottles

| | |
|---|---|
| **Feedback id** | `6a6f3d94ed5471a2a4bc7654` |
| **Submitted** | `2026-08-02T12:52:36Z` (today) by **Queen Contract** (`Queen Contract`) |
| **Source** | private message |
| **Tagged** | `idea` |
| **Verdict** | `feature` |
| **Recommendation** | `spec` |
| **Confidence** | high on what exists; the design is open |
| **Effort** | large — it is an infrastructure change, not one feature |

## What they said

> expand the 'collection' concept. Consider panties, signatures, portraits, statuettes, make sure it's expandable and tied to users (as opposed to currencies which are generic)

## What it means

Build a general collectibles system: individually-identified items that remember **whose** they
are, in contrast with the existing currency stores, which are interchangeable numbers. The named
examples — panties, signatures, portraits, statuettes — all carry a person: something taken
*from* a resident, or made *of* one.

## Assessment

**The bottle collection is already exactly this system, built once for one item type.** The
request is effectively "promote `MilkBottle` to a framework." That is a good sign for
feasibility and the main reason to spec it carefully rather than grow a second copy.

The contrast the submission draws is already the real architectural split on `Profile`:

| | Store | Shape |
|---|---|---|
| Generic / fungible | `Profile.currencies` ([ChateauDB.cs:27](../../../FChatDicebot/Model/ChateauDB.cs:27)) | `Dictionary<string, int>` — a name and a count, no identity |
| Individual / user-keyed | `Profile.bottles` ([ChateauDB.cs:47](../../../FChatDicebot/Model/ChateauDB.cs:47)) | one document per item, each with its own serial and donor |

`MilkBottle` ([Model/MilkBottle.cs](../../../FChatDicebot/Model/MilkBottle.cs)) already has every
field a general collectible needs:

- `serial` — globally unique, never reused, issued from an atomic `$inc` on the `Counters`
  collection ([Chateaudatabase.cs:344](../../../FChatDicebot/Database/Chateaudatabase.cs:344),
  counter id `bottleSerial` at `:359`). **The counter mechanism is already generic** — a second
  counter id costs one constant.
- `sourceName` — the donor's `userName`, frozen against renames and resolved through
  `GetDisplayName` before display ([MilkBottle.cs:38](../../../FChatDicebot/Model/MilkBottle.cs:38)).
  This is the "tied to users" property the submission asks for, already solved including the
  rename trap.
- `substance` — an entry in the `Identifier` catalog.
- `corruptionTag`, `milkedAt`, `emptiedAt` — per-type state and provenance.

And the collectible framing is already explicit in the spec, not accidental:
`Bottle-Consumption-And-Transfer.md` §31 — global serials "give the collection a collectible
dimension for free: a low serial is demonstrably old" — and §35, where the numbered empty is
"permanent proof that you once held a bottle of Alice's corrupt milk, and drank it."

### What the generalization would ride on

- **The `Identifier` catalog** ([ChateauDB.cs:244](../../../FChatDicebot/Model/ChateauDB.cs:244))
  has a `categories` array, an optional `displayText`, and an optional `eicon`. A new category
  (`keepsake`, say) would let new collectible *kinds* be added as data — rendering and icons
  included — with no code change, which is the same trick `Utils.*ToText` already relies on.
- **Existing verbs to mirror**: `!bottles` (list with filters and display caps), `!drink`
  (consume), `!sell` (convert to currency), `!pay` (transfer). A general system needs the same
  four shapes, and the bottle versions are the reference implementations.
- **`!dossier`'s privacy line**: another resident's collection is visible only in summary — counts
  public, donor names not (`Bottle-Consumption-And-Transfer.md` §113). Any new collectible
  inherits that question immediately.

### Where it gets hard

- **The examples are not one thing.** Panties come *from* a resident (like milk). A portrait or
  statuette is *of* a resident but made by someone else — so `sourceName` splits into at least
  "subject" and "provenance." A signature is *given*, which implies a consent flow the bottle
  system never needed.
- **Acquisition is per-type and is most of the work.** Bottles exist because `!milk` makes them.
  Each new collectible needs its own way of coming into being — an interaction, an event reward,
  a purchase — and that, not the storage, is where the effort is.
- **Migration.** Whether `bottles` folds into a general `collectibles` list or stays beside it
  decides how much existing, live, serial-bearing data has to move. Bottles are permanent by
  design and residents can hold hundreds.

- **Reproduces / confirmed:** n/a (feature)
- **Related code:** `Model/MilkBottle.cs`, `ChateauDB.cs:27,47,244`,
  `Database/Chateaudatabase.cs:344-359`, `BottleInventory`, `ChateauBottles.cs`
- **Related specs / docs:** `wiki-docs/specs/Bottle-Consumption-And-Transfer.md` — the de facto
  prototype spec for this
- **Duplicate of:** none — not in `wiki-docs/Feature-Requests.md`, no `Future-*` spec
- **Already fixed since submission:** no

## Recommended action

Spec it as **infrastructure**, in `wiki-docs/specs/Future-*/Infrastructure`, and deliberately
without a shopping list of item types. The valuable and reusable half is the model: a serialized,
user-keyed item document; a shared counter; catalog-driven types so new kinds are data; and the
list/consume/sell/transfer verb set. Item types are the owner's creative territory and can be
added indefinitely afterward.

Scoping note worth making explicitly in the spec: shipping the framework with **zero** new item
types is a legitimate and probably correct Phase 1 — it is not user-visible on its own, but it is
what makes each later type cheap. Trying to land the framework *and* panties *and* portraits in
one pass is how this becomes a large risky change instead of a foundation.

## Decisions needed from the owner

1. **Does the framework absorb bottles, or sit beside them?**
   - **Absorb, with bottles as the first collectible type** (recommended) — one system, one set
     of verbs, and the bottle code becomes the proof the abstraction works. Costs a migration of
     live `Profile.bottles` data, and `MilkBottle`'s bottle-specific fields
     (`corruptionTag`, `emptiedAt`) need a per-type extension slot.
   - **Sit beside** — no migration and no risk to a shipped system, but two parallel collection
     systems forever, and `!bottles` vs a new list command will confuse residents.
   - **Absorb later** — build the framework standalone, migrate bottles once it has proven
     itself on a new type. Defers the risk; runs two systems in the interim.

2. **How is "tied to users" modelled when the item isn't taken from a body?**
   - **One `subjectName` plus an optional `createdBy`** (recommended) — covers "panties from
     Alice" and "a portrait of Alice by Bob" in one shape; `sourceName` maps onto `subjectName`.
   - **Just `sourceName`, renamed** — simplest, matches bottles exactly, but cannot express a
     portrait's two people.
   - **A free-form provenance list** — most flexible, hardest to render consistently and to query.

3. **Are new collectible types data or code?**
   - **Data, via a new `Identifier` category** (recommended) — a new type needs no build and no
     redeploy, and gets `displayText`/`eicon` rendering for free. Limits types to what a common
     field set can express.
   - **Code, one class per type** — every type can have bespoke fields and behavior; every type
     is also a PR and a redeploy.
   - **Hybrid — data types with an optional per-type extension document** — the most capable, and
     the most to design.

4. **Does Phase 1 ship any new item type at all?**
   - **No — framework plus bottles migrated** (recommended) — nothing new for residents to see,
     but the next type is small.
   - **Yes, one simple type (signatures)** — gives residents something immediately and validates
     the abstraction against a non-bottle item; roughly doubles Phase 1.

## User-facing text that would change

None yet — nothing here is decided enough to draft strings. Under option 1a a later phase would
touch `!bottles`, `!drink`, `!sell` and `!bank` wording as bottles become "a collectible you
hold" rather than "the collection", and every one of those strings would come back for approval
in the spec. Flagging now because option 1a's true cost includes a wording pass over a system
whose text is already settled.

| Where | Now | Proposed |
|---|---|---|
| — | — | none this pass; deferred to the spec |

## Owner decision
