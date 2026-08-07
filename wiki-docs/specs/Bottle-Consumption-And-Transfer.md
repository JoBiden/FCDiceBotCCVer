# Bottle Consumption & Transfer — `!bottles`, `!drink`, `!pay … bottles`

Closes the loop on the milk-bottle economy. Today `!milk` fills `Profile.milkInventory` and `!sell` is the only thing that ever reads it — residents cannot see what they own and cannot do anything with it but cash it out. This spec adds serial numbers, persistent empties, a view surface, a consumption path with real mechanical effects, and player-to-player transfer.

**Status:** Implemented (2026-07-29).

> **As shipped since [Collectibles](Infrastructure/Collectibles.md):** bottles live in
> `Profile.collectibles` alongside other collectible types and draw from one shared serial space, so
> a serial identifies an *item* rather than a bottle. `BottleInventory` keeps the substance/donor
> filtering and grouping; the type-agnostic selection moved to `CollectionInventory`. `!sell` now
> reads `MilkBottle.IsSellable` and `!pay` reads `IsTransferable` rather than each caller
> re-deriving the empty-bottle rule, and the privacy rule below is inherited by every type.
**Investment level:** `!bottles` is a private self-command. `!drink` is a channel self-command (no consent flow). Bottle transfer rides the existing Involved-tier `!pay` consent flow.
**Depends on:** [Currency-and-Milk-Inventory](Infrastructure/Currency-and-Milk-Inventory.md) (`MilkBottle`, `milkInventory`, `ChateauCurrency`), [Dose-and-Detox](Dose-and-Detox.md) (`ViceInstance`, `DoseStatusContributor`, `DoseProcessor.IntensifyExistingVices`), [Corrupt-and-Purify](Corrupt-and-Purify.md) (`CorruptionProcessor.ReadCorruption`, daily-quota pattern), [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md).

> **This is a deliberate extension, not a correction.** [Currency-and-Milk-Inventory.md](Infrastructure/Currency-and-Milk-Inventory.md) originally stated that bottles cannot be transferred between residents and that the `bottle` side-currency is a pure tally. Both were accurate as-shipped decisions; this feature supersedes them by owner direction (2026-07-29), and that doc now carries a pointer here.

> **Deploy note:** [`scripts/backfill-bottle-serials.js`](../../scripts/backfill-bottle-serials.js) must run before or with the deploy. Bottles left un-numbered render as `#?` and cannot be named by `!drink` or `!pay`.

## Motivating gaps

1. **No view surface.** `!bank` reads `profile.currencies` only ([`ChateauBank.cs:123`](../../FChatDicebot/BotCommands/ChateauBank.cs)); `!dossier` has no bottle section. A resident who forgets what they milked has no way to find out short of guessing filters at `!sell`.
2. **`!sell` points at commands that don't show bottles.** Its filter-miss message reads *"Use !bank or your !dossier to review what you've got bottled up"* ([`ChateauSell.cs:90`](../../FChatDicebot/BotCommands/ChateauSell.cs)). Neither does. A shipped-text bug regardless of the rest of this spec.
3. **No consumption path.** `!sell` is the only exit. `!feed` takes a substance identifier but has no inventory precondition, so bottles are never actually required for any downstream fiction.
4. **`corruptionTag` only moves price.** A bottle milked from a deeply corrupt resident is worth 1.5×, and that is its entire consequence.
5. **Bottles have no handle**, which is what makes precise transfer and precise consumption impossible to express.

---

## 1. Bottle serial numbers

`!milk`'s completion already says *"Bottled, sealed, and tagged."* This makes the tag real: **every physical bottle gets a globally unique serial number**, issued at milking time and carried for the life of the bottle — including after it's been emptied.

### Why global, not a per-cellar index

A per-cellar index (bottles numbered 1..N in display order, computed at read time) needs no storage and no backfill, and was the cheaper option on the table. It loses on one point that turns out to be decisive: **`!pay` has a consent gap.** Between the payer typing the command and the recipient answering, the payer can `!drink` or `!sell`, and every index after the affected bottle shifts. The handle the payer typed would then name a different bottle at consent time. A deferred-consent command needs a stable handle, and per-cellar indices are not stable. They also renumber on transfer, which destroys the provenance story that makes transfer interesting in the first place.

Global serials are stable forever, in anyone's collection, and give the collection a collectible dimension for free: a low serial is demonstrably old.

### Empties keep their serial

Drinking does not destroy a bottle. It **empties** it, and the empty stays in the resident's collection carrying the same serial, donor, substance, and corruption tag it always had. Bottle #142 is permanent proof that you once held a bottle of Alice's corrupt milk, and drank it.

This changes the meaning of `!sell` versus `!drink` into a real decision rather than a formality:

| | Copper | `bottle` currency | Serial |
|---|---|---|---|
| `!sell` | substance value | +1 (the Chateau takes the empty back) | leaves your collection with the bottle |
| `!drink` | none | none | **stays, as an empty** |

**This depreciates the `bottle` side-currency**, deliberately. It is no longer the only record that a bottle passed through your hands; the empty itself is, and it is far more specific. `!sell` and the self-milk shortcut keep crediting it so existing balances stay meaningful, but `!drink` does not, and nothing new is built on it.

### Shape

One collection with a nullable emptied-timestamp, rather than two parallel lists. A bottle's *state* changes; its identity does not, so it should not move between containers. Filters, transfer, and display all work off one list with a predicate.

```csharp
public class MilkBottle
{
    public int serial;              // NEW. Globally unique, never reused.
    public string substance;
    public string sourceName;
    public DateTime milkedAt;
    public int quantity;            // now always 1 for newly created bottles

    [BsonIgnoreIfNull]
    public string corruptionTag;

    /// NEW. Null while full. Set when drunk; the bottle stays in the
    /// collection as a numbered empty from that point on.
    [BsonIgnoreIfNull]
    public DateTime? emptiedAt;

    public bool IsEmpty => emptiedAt.HasValue;
}
```

`emptiedAt` satisfies the full/empty boolean and carries *when* for free, following the `[BsonIgnoreIfNull]` pattern `corruptionTag` already uses.

**One entry per physical bottle.** Today a milking that rolls 3 stores a single entry with `quantity = 3`. A serial identifies one bottle, so aggregated entries must be split: a milking that rolls 3 writes three entries with three consecutive serials. `quantity` is retained on the model so legacy documents still deserialize (see `Profiledeserializationtests.cs`), but nothing new ever writes a value above 1.

This is a net simplification downstream. `ChateauSell`'s partial-entry-consumption logic exists only to service aggregated entries; with one entry per bottle, selling is a straight remove.

**On collection growth:** empties accumulate permanently. A `MilkBottle` document is on the order of 150 bytes against Mongo's 16MB document ceiling, so this is not a practical limit concern, but the display cap in §2 exists so a long-serving resident's `!bottles` output stays readable.

### Issuing serials

Serials come from an atomic counter so two simultaneous milkings can never collide. A single-document `Counters` collection keyed `bottleSerial`, incremented with a `findAndModify` `$inc` that returns the new value. A milking that produces N bottles does **one** `$inc` of N and claims the returned range, rather than N separate round-trips.

Serials are **never reused.** Selling bottle #142 to the Chateau retires #142 permanently; drinking it keeps it in your hands as an empty.

### Backfill

A one-time `mongosh` script, following the precedent of the employer-earnings historic backfill:

1. Read every profile's `milkInventory`.
2. Flatten to individual bottles, splitting any entry with `quantity > 1` into that many entries (same `substance`, `sourceName`, `milkedAt`, `corruptionTag`).
3. Sort **all** bottles across **all** profiles by `milkedAt` ascending and assign serials from 1. Chronological assignment across the whole Chateau means low serials really are the oldest milk in the building, which is the property that makes them worth showing.
4. Leave `emptiedAt` null on everything. Bottles drunk or sold before this ships are simply not recoverable, and inventing empties for them would be fiction.
5. Set the `bottleSerial` counter to the highest assigned value.
6. Write each profile's rebuilt `milkInventory`.

**The backfill must run before or with the deploy.** Un-numbered bottles would otherwise render with no serial and be unreferenceable by `!drink` / `!pay`. Ties on `milkedAt` break by profile name then substance, purely for determinism if the script is re-run.

> This remains the single largest cost in the spec: a schema change to live player data plus a migration script. Worth confirming appetite before implementation starts. The fallback if not: per-cellar indices, accepting the consent-gap hazard by re-resolving the index at consent time and aborting on mismatch.

---

## 2. `!bottles` — view

Private self-command, modeled on [`ChateauBank`](../../FChatDicebot/BotCommands/ChateauBank.cs). Argument shape mirrors `!sell` so the two read as a pair.

```
!bottles                              — everything you have
!bottles {substance}                  — filtered by substance
!bottles {substance} [user]X[/user]   — filtered by substance and original donor
```

- `IdentifierCategory = "substance"`, `RequireChannel = false`, no cooldown, private reply.
- Self-only in v1. Another resident's collection is visible in summary form via their `!dossier` (§5), which is the deliberate privacy line: counts are public, donor names are not.

### Display

Full bottles first, grouped by `(substance, sourceName, corruptionTag)`, ordered **newest-first** so the top line is what an unfiltered `!sell` or `!drink` acts on next. Each group lists its serials. Empties follow in their own section, grouped the same way — they have no sell value, so they carry no price column.

Serial lists cap at `BottleSerialDisplayCap` per group with an `and N more` tail, so a large collection stays in one readable message. The empties section as a whole caps too; a resident with 200 empties wants the count and a sample, not a wall.

Substance names render through `Utils.SubstanceToText`. **Donor names render through `MonDB.getDisplayName(sourceName)`** — `sourceName` is a stored `userName` and must never reach a player-facing string raw. (Note: [`ChateauSell.BuildResultMessage`](../../FChatDicebot/BotCommands/ChateauSell.cs) currently interpolates `line.SourceName` directly, which is this same bug already shipped. Fix it while in the area.)

---

## 3. `!drink` — consume

**Channel self-command** (`RequireChannel = true`), **no consent flow** (you own the bottle). Structured like [`ChateauWash`](../../FChatDicebot/BotCommands/ChateauWash.cs) and `ChateauDetox`: a `ChatBotCommand` plus a pure `DrinkCommand.Execute(...)` helper so effect logic is unit-testable without a database.

```
!drink                                — your newest full bottle
!drink {substance}                    — newest full bottle of that substance
!drink #{serial}                      — that exact bottle
```

The `#` prefix is required on serials so the argument can't be confused with a count. Term parsing strips it before the int parse.

**Naming:** `!consume` is already taken by the devour-a-resident Commitment interaction ([`ChateauConsume.cs:19`](../../FChatDicebot/BotCommands/ChateauConsume.cs)), so `!drink` is the primary. `!imbibe` and `!swig` were suggested as aliases but **rejected by the owner (2026-08-02) — don't re-add them**. Note `drink` is itself a vice/substance identifier, so `!drink drink` is legal and slightly silly. Harmless, but worth knowing before someone reports it.

### Why it must be a channel command

The effects below produce **public** output: craving satisfaction and corruption drift are things the channel is supposed to witness (`!dose`'s consent text already promises *"Everyone will know when you're satisfying an addictive craving"*). Running `!drink` in a private message would either silently swallow that output or leak a public-by-design fragment into a PM. Channel-only also naturally rate-limits the command socially, which pairs with the 3-per-day corruption budget to keep it from being spammed.

### Consumption mechanics

1. Select **one full bottle**: by serial if given, otherwise newest-first among full bottles honoring the substance filter. Empties are never selected implicitly. This is the same selection `ChateauSell` performs; factor it into a shared helper with a full/empty predicate rather than copying it, so `!sell`, `!drink`, and transfer can never drift.
2. Set `emptiedAt = UtcNow`. The entry stays in `milkInventory`.
3. **No `bottle` currency is credited** — you keep the physical empty instead. (See §1.)
4. Persist via the targeted `SetMilkInventory` write, not a whole-profile `SetProfile`, matching how `!sell` avoids clobbering concurrent currency writes.

### Effects

**(a) Craving satisfaction.** Today the only ways to satisfy a `!dose` craving are `!feed`, `!odorize`, or another `!dose` — *all of which require a second consenting resident*. An addict alone in the channel has no relief. Drinking a bottle of the substance you are hooked on becomes the first solo route.

`DoseStatusContributor` already matches on `parentInteractionType` + `parentIdentifier`, so this is one case added to its satisfaction list: `parentInteractionType == "drink"` && `parentIdentifier == substance`. It reuses the contributor's existing approved satisfaction string, so no new copy is needed for this branch.

> **Infra note.** Per the folder conventions, status effects fire on interactions you `!consent` to; self-commands don't invoke the hook. `!drink` is a deliberate exception — the drinker is consenting with themselves. `GetActiveStatusEffects` is currently an instance method on `InteractionProcessorBase`, so this needs a small **public** shared entry point a `ChatBotCommand` can call: invoke the registered contributors against a single subject at `StatusEffectCallSite.Completion` with `isInitiator: true`. `!detox` and `!wash` would benefit from the same hook later. This is the only genuinely new infrastructure in the spec.

**(b) Corruption transfer.** The bottle's `corruptionTag` moves the *drinker*, closing the loop with Corrupt-and-Purify and giving the tag consequence beyond price:

| Bottle tag | Corruption shift on the drinker |
|---|---|
| `"corrupt"` | −1 |
| `"purified"` | +1 |
| `null` | 0 |

(Sign follows `ChateauCurrency.GetCorruptionTagForValue`: negative is corrupt, positive is pure. Player-facing text never shows the sign — it says "gains 1 corruption" or "gains 1 purity".)

Bounded by a **daily shift budget of 3** per Chateau day, stored in `dailyMagnitudes` under a `drinkshift_{yyyy-MM-dd}` key with the same stale-key pruning `CorruptionProcessor.RecordUsedQuota` performs. Deliberately a **separate key** from the corruption quota, so drinking never draws from or eats a resident's 10/day `!corrupt` budget.

Past the budget, further drinks still work and still taste of what they are. Only the mechanical half of the message is replaced (see the two-part fragment structure in the text section).

**(c) Addiction risk.** A `DrinkAddictionChance` (10%) roll to intensify an **existing** `ViceInstance` for that substance, via `DoseProcessor.IntensifyExistingVices`. **Intensify-only, never creates** — the rule `!climaxfor`'s auto-dose already follows, so drinking your first bottle of something can never addict a clean resident.

> **Corrected during implementation.** The design originally said this roll was skipped when the same drink satisfied a craving, on the grounds that relief-plus-escalation read as double-dipping. That rule makes the effect unreachable: only an existing vice can intensify, and an existing vice is exactly what satisfaction requires, so "skip when satisfied" and "intensify only what exists" have no overlap. The roll therefore fires regardless. This is the better mechanic anyway — the bottle quiets the want *and* tightens the hook, which is the dynamic the vice system exists to model.

### Why this doesn't become a corruption farm

Supply is throttled at the source: one milking per direction per Chateau day, 1 to 3 bottles, and every bottle in existence came from a consenting `!milk`. On top of that, the 3/day shift budget caps the stat impact regardless of how many bottles a resident holds — which is what keeps the numbers sane once transfer (§4) lets someone pool another resident's supply. The budget is doing the real balance work here, not scarcity.

---

## 4. Bottle transfer via `!pay`

Full bottles **and empties** become payable between residents through the existing [`!pay`](../../FChatDicebot/BotCommands/ChateauPay.cs) flow. Empties are transferable on purpose: once a serial is a collectible, gifting or trading one is a meaningful act, and a numbered empty from a notable donor is exactly the sort of thing residents will want to hand around.

### Syntax

```
!pay [user]Bob[/user] {amount} bottles              — N newest full bottles
!pay [user]Bob[/user] {amount} bottles {substance}  — N newest full of a substance
!pay [user]Bob[/user] bottle #142                   — that exact bottle, full or empty
!pay [user]Bob[/user] bottles #142 #143 #187        — those exact bottles
!pay [user]Bob[/user] -{amount} bottles             — bill Bob for bottles
```

Amount-based forms select **full bottles only** — someone asking for "3 bottles" means three with something in them. Empties move by explicit serial, which is the right friction for a collectible.

The literal keyword `bottles` (accept `bottle` too) occupies `!pay`'s currency slot. This is available: `bottle` is **not** a currency-category identifier in the snapshot, so `!pay X 3 bottle` fails today with "type not found", meaning the empty-bottle side-currency is itself unpayable and there is no ambiguity to resolve. Worth confirming against live Mongo before implementing, since the snapshot can lag.

`ChateauPay.Run` special-cases the keyword *before* the currency-identifier lookup, then reads either `#`-prefixed serials or an amount plus an optional `substance` identifier.

**Serials resolve the donor-filter problem.** `!pay`'s `[user]` tag is already spoken for by the payee, so unlike `!sell` there is no room for a source filter. Naming bottles by serial sidesteps this entirely: you don't filter for Alice's milk, you hand over #142.

### Bottles travel intact

A transferred `MilkBottle` keeps its `serial`, `substance`, `sourceName`, `milkedAt`, `corruptionTag`, and `emptiedAt`. Provenance survives the handoff. A bottle milked from a corrupt donor still corrupts whoever eventually drinks it, three owners later, and it still answers to the same number.

### Consent and TOCTOU

Rides the existing `paymentGive` / `paymentReceive` processors and their consent flow. Two changes:

- `PaymentProcessorBase.ProcessInteraction` branches: currency payments keep the atomic `TryDebitCurrency` path; bottle payments move `MilkBottle` entries between the two `milkInventory` lists.
- `BuildConsentWarning` on both processors gains a bottle branch naming what is actually changing hands, including tags and whether any are empties. A recipient deserves to know they're being handed corrupt goods, or empties, before consenting.

**Do not escrow at command time.** Between `!pay` and `!consent` the payer may `!sell` or `!drink` the bottles. Re-resolve at consent time and abort with a private message if the payer no longer holds them — the TOCTOU pattern [`MilkProcessor.ProcessInteraction`](../../FChatDicebot/InteractionProcessors/Involved/MilkProcessor.cs) already uses for its direction lock. Serials make this check exact.

**A bottle drunk between command and consent is a mismatch, not a substitution.** It's still in the payer's collection under the same serial, but it's empty now, and the recipient consented to a full one. Abort rather than deliver an empty.

Amount-based transfers resolve to concrete serials at command time and store those on the pending command, so the re-check is always the same exact-serial check. This stops a payer promising "3 bottles of milk", drinking the corrupt ones, and delivering three plain ones.

Self-pay is already rejected by `ChateauPay` and stays rejected.

---

## 5. Surfacing changes to existing commands

**`!bank`** — a collection line in *both* branches, counting full and empty separately. The no-currency branch matters: a resident with zero currency but a full collection is currently told they own nothing. Omit the line entirely when the collection is empty.

**`!dossier`** — one cell in the At-a-Glance section ([`ChateauDossier.cs:662`](../../FChatDicebot/BotCommands/ChateauDossier.cs)), beside the most-abundant-currency cell. **Substance counts only, no donor names and no serials.** The dossier is public, and `sourceName` is effectively "who has this resident milked", which is the donor's business, not the reader's.

**`!sell`** — fix the filter-miss text to point at `!bottles`. Show serials in its result message. Sells full bottles only in v1; empties stay put. Add `bottles` and `drink` to its `RelatedCommands`, and to `!milk`'s.

> **Future hook, not v1:** the `!drink` closing pool jokes that someone out there pays well for an empty bottle. If empties should ever become sellable, that line is the setup already planted.

---

## Persistence

**No new `Profile` fields.** Everything reuses `milkInventory`.

| Key | Location | Meaning |
|---|---|---|
| `serial` | `MilkBottle` | Globally unique bottle number. Never reused. |
| `emptiedAt` | `MilkBottle` | Null while full; set when drunk. The bottle stays in the collection either way. |
| `bottleSerial` | `Counters` collection *(new)* | Atomic issuing counter for serials. |
| `drinkshift_{yyyy-MM-dd}` | `Profile.dailyMagnitudes` | Corruption shift already spent by drinking today. Capped at `DrinkCorruptionDailyLimit`. Stale-dated keys pruned on write. |

New constants on `ChateauCurrency`, alongside the existing milk and pricing constants:

```csharp
public const int DrinkCorruptionShiftPerBottle = 1;
public const int DrinkCorruptionDailyLimit = 3;
public const double DrinkAddictionChance = 0.10;
public const int BottleSerialDisplayCap = 8;
```

---

## User-facing text (as shipped)

Drafted against [Style-Guide.md](../Style-Guide.md). No em-dashes, no "UTC", donor names via `getDisplayName`, `(or !no)` never hand-written (applied by `GetConsentWarning` from `ConsentWarningText.DeclineHint`).

### `!bottles`

**Populated (private):**
> Our records show you're holding [b]7 bottles[/b]:
> [b]milk[/b] from Alice, [b]corrupt[/b] | #142, #143, #187 | 7 copper each
> [b]cum[/b] from Bob | #201, #202 | 25 copper each
> [b]saliva[/b] from Bob, [b]pure[/b] | #55, #91 | 50 copper each
> Empties: #12, #40, #41, #98
> That's [b]171 copper[/b] if you choose to !sell the lot to the Chateau on the cheap, but someone might be willing to let you !pay them with a few bottles... or you could always have a !drink.

Groups exceeding the serial display cap tail with `and 4 more`. The empties line is omitted when there are none.

**Empty (private):**
> No bottles to speak of yet. Go !milk a willing resident to start your collection!

**Only empties left (private).** Added during implementation: persistent empties create a state where the collection is worth showing but "you're holding 0 bottles" would be a strange way to open it.
> Nothing left to drink, but our records show you're keeping [b]4 empties[/b]:
> #12, #40, #41, #98
> Go !milk a willing resident if you'd like something to drink.

**Filter matched nothing (private):**
> We don't see anything like that in your collection. Use !bottles on its own to see everything you're holding.

### `!drink`

**Completion (channel).** Setup plus one randomly chosen closing flourish. Phrased "the tasty {substance} from {Donor}" rather than a possessive, since a `displayName` ending in *s* makes `'s` read badly and the substance is often not milk:
> {Drinker} uncorks bottle [b]#142[/b] and drinks down the tasty {substance} from {Donor}.

Closing pool:
> Unbottled, unsealed, and thoroughly enjoyed.
> Not a drop wasted!
> Keep the empty for your collection.
> Rumors say green clad adventurers will pay a lot for an empty bottle...
> Got {substance}?

**Corruption fragments** (appended). Two composable halves — a taste half that always fires, and an effect half that the daily budget can replace:

| Half | Corrupt | Pure |
|---|---|---|
| Taste | `How delightfully dark and rich~` | `So light and invigorating!` |
| Effect | `{Drinker} gains 1 corruption.` | `{Drinker} gains 1 purity.` |

Assembled: `How delightfully dark and rich~ {Drinker} gains 1 corruption.`

**Budget spent** — the taste half still fires, only the effect half is replaced:
> No effect though... maybe tomorrow they'll be able to metabolize a bit more.

Assembled: `How delightfully dark and rich~ No effect though... maybe tomorrow they'll be able to metabolize a bit more.`

**Addiction intensified** (appended):
> ...and {Drinker} finds themself wanting another already. [sub](addiction 4/10)[/sub]

**Craving satisfied:** no new string. `DoseStatusContributor` emits its existing approved satisfaction fragment.

**Empty collection (private):**
> No bottles to speak of yet. Go !milk a willing resident to start your collection!

**Filter matched nothing (private):**
> We don't see any of that in your collection. Use !bottles to check what you're actually holding.

**Everything you own is already empty (private).** Added during implementation, for the same reason as the `!bottles` case above:
> Every bottle you're holding is already empty. Go !milk a willing resident if you'd like something to drink.

**Unknown serial (private):**
> Bottle [b]#142[/b] isn't in your collection. Perhaps you sold it off... use !bottles to check your numbers.

**Bottle already empty (private).** New state created by persistent empties, and distinct from the case above:
> Bottle [b]#142[/b] is already empty! Use !bottles to see which of yours still have something in them.

### `!pay` bottle transfer

**Consent prompt, give (channel).** The `(or !no)` reminder is appended by the framework:
> {Initiator} is going to pass {Recipient} [b]3 bottles[/b]: two of the milk from {Donor} ([b]corrupt[/b]), and one of the cum from {Donor2}! Do you !consent to receiving them?

**Consent prompt, bill (channel):**
> {Initiator} is asking {Recipient} for [b]2 bottles[/b] of the milk from {Donor}! Do you !consent to handing them over?

**Empty-bottle flag** (appended to the parcel description in either prompt). Added during implementation: whether the contents are still in there is the single most important thing a recipient needs before agreeing, so it goes on the whole parcel rather than per line, and a mixed parcel says exactly how many rather than implying either.
> [sub](already emptied)[/sub]
> [sub](1 of them already emptied)[/sub]

**Completion, give (channel):**
> {Initiator} hands [b]3 bottles[/b] over to {Recipient}. Is that a vintage?

**Completion, bill (channel):**
> {Initiator} accepts [b]3 bottles[/b] from {Recipient}. Is that a vintage?

**Not enough bottles, command time (private):**
> You don't have that many bottles to hand over. Use !bottles to see what you're holding.

**Bottles gone by consent time (private, to initiator):**
> {Payer} doesn't have those bottles anymore. It seems they were sold or enjoyed while we waited on an answer... nothing changed hands.

### `!bank` addition

Appended to both the has-currency and no-currency branches, omitted when the collection is empty:
> You're also holding [b]7 bottles[/b] and [b]4 empties[/b] of your own. Use !bottles to look them over.

### `!dossier` cell

> [b]Bottled:[/b] 7 bottles (3 milk, 2 cum, 2 saliva) and 4 empties

### `!sell` revised filter-miss

Replaces the current false pointer:
> No bottles in your collection matched that filter. Use !bottles to review what you've got bottled up.

### Help fields

**`!bottles`**
- `ShortDescription`: `Look over the bottles you've collected.`
- `LongDescription`: `Review the bottled fluid in your personal collection. Each bottle carries its own number, the resident it came from, whether it's corrupt or pure, and what the Chateau would pay for it. Empties keep their number too, so your collection remembers everything you've ever drunk. Filter by substance and by donor to narrow the list.`
- `Usage`: `!bottles\nor\n!bottles {substance}\nor\n!bottles {substance} [noparse][user]NameInUserTag[/user][/noparse]`
- `RelatedCommands`: `milk`, `sell`, `drink`, `pay`, `bank`

**`!drink`**
- `ShortDescription`: `Drink down one of the bottles you're holding.`
- `LongDescription`: `Uncork and drink a bottle from your collection, keeping the empty. Whatever was inside affects you: a corrupt or pure bottle shifts you the same way, up to 3 bottles per day, and a substance you're addicted to will quiet the craving. There's always a small chance you find yourself wanting more. Name a bottle by its number, or leave it off to drink your newest.`
- `CooldownDuration`: `null` (the 3-per-day limit is a corruption budget, not a cooldown, and is described in the long text)
- `Usage`: `!drink\nor\n!drink {substance}\nor\n!drink #{number}`
- `RelatedCommands`: `bottles`, `milk`, `sell`, `dose`, `detox`

**`!pay`** `LongDescription` gains a trailing sentence:
> You can also pass along bottles from your collection, full or empty, either by number or by naming a substance and an amount.

---

## Tests

**`Bottleinventorytests.cs`** *(new)* — the shared selection contract: newest-first ordering, empties excluded from `SelectFull`, substance and donor filters, serial lookup finding full and empty alike, serial 0 never matching, grouping collapsing same-`(substance, source, tag)` bottles while separating on tag, the display cap's `and N more` tail, and unnumbered bottles rendering as `#?` rather than a misleading `#0`.

**`Chateaubottlestests.cs`** *(new)* — the view: count, serials and per-bottle price; donor names rendered as `displayName`; total value counting full bottles only; the empties line present when there are empties and omitted when there aren't; corrupt/pure labelling; filter-miss distinguished from an empty collection; separate milkings collapsing to one line; and the `!bank` collection line counting full and empty separately and vanishing at zero.

**`Chateaudrinktests.cs`** *(new)*
- Empties the bottle but keeps it and its serial in the collection.
- Credits **no** `bottle` currency.
- Implicit selection takes the newest full bottle and never an empty, even when the empty is newest.
- Serial targeting selects that exact bottle; an unknown serial changes nothing; an already-empty serial gets its own distinct message.
- Corrupt shifts −1, purified +1, untagged 0.
- Past the daily budget the bottle is still drunk and still narrated, but shifts 0 and prints the metabolize line; stale-dated `drinkshift_` keys are pruned; the drink budget never writes a `corruption_` key.
- The addiction roll intensifies an existing vice and never creates one, and still fires on a drink that satisfied a craving.
- Serial parsing requires the `#` prefix, so a bare number is never mistaken for one.

**`Bottleserialtests.cs`** *(new)* — the counter starts at 1 on a fresh database, returns the first number of a claimed block, never repeats across 50 mixed-size claims, no-ops on a non-positive count; a sold serial leaves the collection while a drunk one stays; serials survive transfer unchanged.

**`Bottlepaymenttests.cs`** *(new)*
- The `bottles` keyword is recognised case-insensitively; serial parsing keeps order and drops duplicates.
- Amount forms take newest-first full bottles only; a short collection fails; a substance filter narrows them.
- A named serial can move an empty; a serial the payer doesn't hold fails by name.
- Transfer moves serial, donor and tag intact.
- Consent gap: a bottle sold in the gap aborts; a bottle *drunk* in the gap aborts rather than delivering the empty; a partial mismatch moves nothing at all.
- `EncodePromise` carries the empty-state alongside the serial.
- `Describe` names count, substance, donor and tag, and is distinguishable from a currency amount.

**`Milkprocessortests.cs`** *(modified)* — a milking now writes one entry per bottle rather than one entry with a count, each numbered and full, and claims its serials as one contiguous block.

**`Chateauselltests.cs`** *(unchanged, passing)* — the existing suite is the regression guard on the extracted selection helper. Its bottles are all full and un-numbered, which incidentally covers the legacy `quantity > 1` path still working.

Full suite: 1766 passing.

---

## Assumptions / overrides

- **Empties persist forever and are never garbage-collected.** That permanence is the feature. Override with a retention cap only if collection sizes become a real problem, which the arithmetic in §1 suggests they won't.
- **`!drink` credits no `bottle` currency.** This is the inference from depreciating that currency: you keep the physical empty instead of a tally mark. `!sell` and the self-milk shortcut keep crediting it so existing balances aren't stranded. Override by crediting both, though then the tally and the empty say the same thing twice.
- **Global serials over per-cellar indices**, at the cost of a data migration. Reasoning in §1.
- **One entry per physical bottle.** `quantity` retained for legacy deserialization only.
- **Chronological backfill across all profiles**, so low serials are genuinely the oldest bottles.
- **Empties transfer by explicit serial only**, never by amount. Amount forms mean full bottles.
- **Empties are not sellable in v1**, despite the joke in the closing pool.
- **`!drink` is self-only.** Drinking *at* someone is `!feed`'s territory, and `!feed` stays free-form with no inventory precondition. Gating a shipped consent interaction on inventory would break every current use.
- **One bottle per `!drink` call.** No `!drink 3`.
- **Corruption shift is ±1 per bottle, 3 per day.** The daily cap is the load-bearing balance lever once transfer exists. Do not remove it without revisiting §3.
- **Addiction risk is intensify-only at 10%, and fires even on a drink that satisfied a craving.** Matches the climax auto-dose precedent on the first half; the second half is a correction made during implementation (see §3c) without which the effect is unreachable.
- **`bottle` is not a currency identifier.** Verified against `IdentifiersSnapshot.json`. **Worth confirming against live Mongo:** if someone has since added a `bottle` currency identifier, `!pay X 3 bottle` would now route to the bottle-transfer path instead of paying that currency.

## Files

**New:**
- `FChatDicebot/BottleInventory.cs` — shared selection, grouping and serial formatting.
- `FChatDicebot/BottlePayment.cs` — transfer parsing, selection, the promise encoding, and transfer wording.
- `FChatDicebot/BotCommands/ChateauBottles.cs`
- `FChatDicebot/BotCommands/ChateauDrink.cs` (thin command over a pure `Execute`)
- `FChatDicebot/InteractionProcessors/SelfCommandStatusEffects.cs` — the public hook a consentless command uses to run the contributors.
- `scripts/backfill-bottle-serials.js` (mongosh migration)
- `FChatDicebot.Tests/Unit/Bottleinventorytests.cs`
- `FChatDicebot.Tests/Unit/Bottlepaymenttests.cs`
- `FChatDicebot.Tests/Unit/Bottleserialtests.cs`
- `FChatDicebot.Tests/Unit/Chateaubottlestests.cs`
- `FChatDicebot.Tests/Unit/Chateaudrinktests.cs`

**Modified:**
- `FChatDicebot/Model/MilkBottle.cs` — `serial`, `emptiedAt`, `IsEmpty`.
- `FChatDicebot/InteractionProcessors/Involved/MilkProcessor.cs` — claims a serial block; writes one entry per bottle.
- `FChatDicebot/BotCommands/ChateauSell.cs` — uses the shared selection; excludes empties; reports sold serials; fixes the filter-miss pointer; renders donor names via `getDisplayName`.
- `FChatDicebot/BotCommands/ChateauPay.cs` — `bottles` keyword branch ahead of the currency lookup, serial and amount forms.
- `FChatDicebot/InteractionProcessors/Involved/PaymentProcessorBase.cs` — bottle-transfer branch in `ProcessInteraction`.
- `FChatDicebot/InteractionProcessors/Involved/PaymentGiveProcessor.cs` / `PaymentReceiveProcessor.cs` — bottle branch in `BuildConsentWarning` / `GetCompletionMessage`.
- `FChatDicebot/InteractionProcessors/StatusEffectContributors/DoseStatusContributor.cs` — `"drink"` satisfaction case and the `DrinkType` key.
- `FChatDicebot/ChateauCurrency.cs` — drink constants and the display cap.
- `FChatDicebot/Database/Ichateaudatabase.cs` / `Chateaudatabase.cs` — `ClaimBottleSerials`.
- `FChatDicebot/BotCommands/ChateauBank.cs` — collection line in both branches.
- `FChatDicebot/BotCommands/ChateauDossier.cs` — At-a-Glance bottled cell.
- `FChatDicebot/BotCommands/ChateauMilk.cs` — `RelatedCommands`.
- `FChatDicebot.Tests/Fixtures/Testdatabasefixture.cs` — clears the `Counters` collection on reset.
- `FChatDicebot.Tests/Unit/Milkprocessortests.cs` — one-entry-per-bottle and contiguous-serial coverage.
- `wiki-docs/specs/Infrastructure/Currency-and-Milk-Inventory.md` — superseded-transfer and depreciated-currency notes.
- `wiki-docs/Command-Reference.md` — the Bottle Collection section, the `!pay` bottle syntax, and the new aliases.

Commands are discovered by reflection over `ChatBotCommand` subclasses (`BotCommandController.LoadChatBotCommands`), and `.csproj` globs `**\*.cs`, so neither new command needed a registration or project edit.
