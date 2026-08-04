# Drink From Source (`!drinkfrom` / `!forcedrink`)

**Status:** Design. Not implemented. Awaiting owner sign-off.

Origin: resident feedback `6a6efae4` (2026-08-02), approved for spec on 2026-08-02. Dossier:
`scripts/feedback-triage/queue/2026-08-02-6a6efae4-drink-from-the-source.md`.

> expand the drink and feed system to allow residents to consume directly from the source, gaining
> any corruption effects and fueling any addictions but skipping the !milk step. Consider the
> reverse interaction as well. Something like !drinkfrom and !forcedrink

Today, consuming another resident's fluid is a two-step loop: `!milk` them into a numbered bottle,
then `!drink` that bottle. This adds a one-step path that keeps the mechanical payload and
produces no bottle — and its mirror, where the donor initiates.

**Depends on:** [Bottle-Consumption-And-Transfer](../Bottle-Consumption-And-Transfer.md) (the
`!drink` effect helpers), [Milk](../Milk.md) (the per-direction lock),
[Dose-and-Detox](../Dose-and-Detox.md) (vice intensification, craving satisfaction),
[Status-Effect-Hook](../Infrastructure/Status-Effect-Hook.md).

**Adds no infrastructure.** Every effect already exists as a reusable piece; this is a delivery
path.

---

## Owner decisions this spec is built on

Answered during the 2026-08-02 feedback review. These are settled — the rest of the spec follows
from them.

1. **`!forcedrink` is a new command. `!feed` is untouched.** `!feed` keeps being generic-substance
   flavor for food and energy, with no mechanical effects; the from-the-body path is separate and
   carries effects. No migration of `feedgive` / `feedtake`.
2. **`!drinkfrom` shares `!milk`'s per-direction cooldown lock.** A resident can be drawn from at
   one rate whether or not it ends up in a bottle.
3. **A source drink leaves nothing behind — no bottle, no serial — and is stronger than a
   bottled drink.** The "fresher is stronger" half overrode the recommendation to reuse the bottled
   numbers unchanged, so the potency numbers and their interaction with the daily corruption budget
   are worked out in [Balance](#balance) below and are the part most worth arguing with.

### One reading to confirm

"Fresher is stronger" is specced as **a bigger magnitude, not a wider net**: the corrupt/purified
*tag* still comes from the donor's live corruption at the same ±10 thresholds
(`ChateauCurrency.GetCorruptionTagForValue`), and what changes is how far one drink moves you.
A neutral-corruption donor still gives a drink that shifts nothing. The alternative reading —
lower the threshold so a donor at ±5 already counts as potent — would make more residents
corrupting without making any single drink stronger. Say so if that was the intent.

---

## The two commands

Both are **Involved** interactions using the standard two-phase consent flow. They are the same
act with the roles swapped, so they share one processor and one set of counts; the difference is
who types the command and therefore who is asked to consent.

| | `!drinkfrom` | `!forcedrink` |
|---|---|---|
| Initiator | the drinker | the source |
| Recipient (consents) | the source | the drinker |
| Reads as | "may I drink from you?" | "drink from me" |
| Role → `drinker` | initiator | recipient |
| Role → `source` | recipient | initiator |

`!forcedrink` is consent-gated like everything else in the Chateau; "force" is flavor in the same
way `!breed` and `!corrupt` are. Worth keeping in mind when the consent prompt is worded — the
prompt must not imply the recipient has no say, because they do.

### Syntax

```
!drinkfrom [user]Name[/user] {substance}
!forcedrink [user]Name[/user] {substance}
```

The substance is required, the same as `!milk` and `!feed`. Bare-name targeting works for free —
both usages carry `[user]`, so `ChatBotCommand.AcceptsRecipient` derives.

`RelatedCommands` for both: `drink`, `milk`, `bottles`, `dose`, `detox`. Add `drinkfrom` and
`forcedrink` to `ChateauDrink`'s and `ChateauMilk`'s `RelatedCommands` in the same PR.

### Aliases

None initially. `!suckle` and `!nurse` are obvious full-word candidates and cost one array entry
each if the owner wants them — an alias of an interaction also needs an
`InteractionEiconSupport.TokenToVerbKeys` entry.

---

## Validation

Runs in `SourceDrinkProcessor.ValidateInteraction`, in this order:

1. `base.ValidateInteraction` — profiles exist, plus the status-effect blockers, so a future
   contributor gates this with no code here.
2. **Substance provided and real.** Reject an empty identifier with
   `ChateauInteractionHandler.typeNotFoundText("substance")`. Look the identifier up and require
   a `substance` or `vice` category — copy `MilkProcessor.ValidateInteraction`'s check verbatim so
   the two paths can't drift on what counts as drinkable.
3. **Self-target rejected.** Unlike `!milk`, there is no self-shortcut worth having: `!drink`
   already covers "consume something you hold", and drinking from yourself with no bottle produced
   is a no-op with flavor. Reject at the command layer with a message pointing at `!drink`.
4. **Per-direction lock** (see below) — reject if the drinker has already drawn from the source
   today.
5. **Broken-part gate.** `MilkProcessor.CheckBreakBlockForSubstance` maps a substance to the
   bodyparts that produce it and blocks when the source's relevant part is broken. A source drink
   must respect the same gate — you cannot drink milk from a broken breast. Promote
   `CheckBreakBlockForSubstance` and `BodypartsForSubstance` out of `MilkProcessor` into a shared
   helper (`SubstanceBodyparts`, alongside the other interaction support classes) and call it from
   both. Leaving a copy in each is how the two silently diverge the next time a substance is added.

---

## The per-direction lock

`!milk` stamps `milk_give_<sourceUserName>` on the **milker** until the next day-boundary
(`MilkProcessor.DirectionTimerKey`, `HasActiveDirectionLock`). The key is per (taker, source) pair
and directional — being milked by Alice does not stop you milking Alice back.

**All three commands share that exact key.** A source drink stamps
`MilkProcessor.DirectionTimerKey(source)` on the drinker's profile, and checks it the same way.
Consequences, stated plainly because they are the balance decision:

- Alice can draw from Bob **once per Chateau day**, by any of `!milk`, `!drinkfrom`, or Bob's
  `!forcedrink` — whichever happens first consumes the day.
- Bob drawing from Alice is unaffected, in either direction.
- Carol drawing from Bob is unaffected.

Reuse `MilkProcessor.HasActiveDirectionLock` unchanged. The refusal message cannot be
`DirectionLockMessage` as written — it says "you've already milked" — so it needs a variant that
names what actually happened. Both belong on the shared helper rather than in either processor.

**TOCTOU recheck.** `MilkProcessor.ProcessInteraction` re-checks the lock at consent time because
a second pending command against the same source can land in between, and suppresses its channel
message when it fires. The source-drink processor needs the same recheck and the same suppression.

---

## Effects

All three already exist and are already factored. `ChateauDrink.Execute` is a DB-injected static
whose only bottle-specific dependency is that it takes a `MilkBottle`; the effect helpers
underneath it are what get reused.

| Effect | Reused from | Source-drink behavior |
|---|---|---|
| Corruption / purity shift | `ChateauDrink.ApplyCorruption`, `ShiftForTag` | tag computed live from the source's corruption; **doubled magnitude** — see Balance |
| Craving satisfaction | `SelfCommandStatusEffects.GetCompletionFragments` with `DoseStatusContributor.DrinkType` | **identical** — a craving is satisfied or it isn't; there is no potency axis here |
| Addiction deepening | `DoseProcessor.IntensifyExistingVices` | **doubled chance**, still intensify-only |

Order matters and should match `ChateauDrink.Execute`: corruption, then status fragments, then the
addiction roll — so a satisfied craving reads ahead of the "wanting another already" line that
follows from it.

### Refactor these out of `ChateauDrink`

`ApplyCorruption` and `ApplyAddictionRoll` are private and take a `MilkBottle`. Lift both into a
shared `SourceOfDrink` shape — a `substance` plus a `corruptionTag` — that a `MilkBottle` and a
live donor can both produce. `ChateauDrink` keeps its public surface and behavior unchanged; this
is purely so there is one implementation of "what a drink does to you". Doing it any other way
means the bottled and unbottled paths drift the first time either is tuned.

### No keepsake

No `MilkBottle` is created, **no serial is claimed** (`ClaimBottleSerials` is not called), nothing
lands in `!bottles`, and there is nothing to `!sell`. This is the deliberate trade-off and should
be said out loud in the help text: the bottle path gives you a permanent numbered empty, the source
path gives you a stronger hit and no proof it happened.

---

## Balance

The bottled path's numbers (`ChateauCurrency`): `DrinkCorruptionShiftPerBottle = 1`,
`DrinkCorruptionDailyLimit = 3`, `DrinkAddictionChance = 0.10`.

New constants, in the same `ChateauCurrency` `!drink` block:

```
SourceDrinkCorruptionShift  = 2      // vs 1 per bottle
SourceDrinkAddictionChance  = 0.20   // vs 0.10 per bottle
```

**The daily corruption budget is shared, not duplicated.** Source drinks spend from the same
`drinkshift_<date>` key on the drinker's profile (`ChateauDrink.DrinkShiftQuotaKey`), against the
same limit of 3. That is what makes "stronger" a real trade-off instead of a strict upgrade: a
resident's day holds *three points of drift*, and they choose how to spend it.

| A day's drinking | Points spent | Drinks used |
|---|---|---|
| 3 bottles | 3 | 3 |
| 1 source drink + 1 bottle | 3 | 2 |
| 1 source drink, budget to spare | 2 | 1 |

A separate budget was considered and rejected: it would let a resident take 3 bottles *and* 3
source drinks, doubling the achievable daily drift, which is the exact thing
`DrinkCorruptionDailyLimit` exists to bound (`ChateauCurrency`'s own comment calls it "the
load-bearing balance lever", since bottles pool freely through `!pay`).

### Partial spend

The bottled path is binary: `if (used >= limit)` → no effect at all. With a shift of 2 that leaves
a gap — a drinker with 1 point left would get nothing, having been told they can "metabolize a bit
more". **A source drink clamps to the remaining budget** instead: `applied = min(shift, limit -
used)`. So 2 points normally, 1 point when only 1 is left, 0 (and the existing over-budget message)
when the budget is gone.

This is a third message state the bottled path does not have (full / partial / none) and is the
one place this spec adds surface rather than reusing it. The alternative — refuse outright unless
the full 2 points are free — is simpler and consistent with `!drink`, at the cost of a refusal that
reads arbitrary. Owner's call; clamping is what the rest of this spec assumes.

### Addiction stays intensify-only

Doubling the chance to 20% does **not** mean a source drink can hook a clean resident.
`DoseProcessor.IntensifyExistingVices` only deepens a vice already present, and `!dose` remains the
only way to create one. Making the source path able to create addictions would route around the
whole Dose-and-Detox consent framing, so it is deliberately out of scope.

---

## Counts and titles

Two new counts, **assigned by role, not by who typed the command** — the trap this whole feature
sets. This is the `!climax` / `!climaxfor` precedent: `climaxgive` and `climaxtake` follow the
partner and climaxer roles regardless of which command was used.

| Count | Goes to | `!drinkfrom` | `!forcedrink` |
|---|---|---|---|
| `drinkfromgive` | the drinker | initiator | recipient |
| `drinkfromtake` | the source | recipient | initiator |

The `give` / `take` convention here follows `milkgive` (the milker, who performs the act) and
`milktake` (the one milked) — *not* the direction the fluid travels. Naming it the other way round
would be defensible in isolation and wrong next to every existing pair.

Use `InteractionProcessorBase.IncrementDifferentCounts` with the role-mapped names, and note that
`IncrementDifferentCountsWithRateLimit` is **not** wanted: the per-direction daily lock is already
a much tighter gate than the 30-minute involved rate limit.

Title ladders go in `ChateauSystemTitles`' milestone table at the usual 1 / 5 / 10 / 25 thresholds
for an Involved interaction. Title text is the owner's, pending wording review.

Counts are incremented on the consent path, so `ChateauConsent`'s sweep grants the titles — no
`CheckAndGrantTitles` call of its own is needed (see `Pledges.md` for what happens when a command
off that path forgets one).

---

## Eicons

- `BodypartEiconRule.Part("mouth", …)` on **the drinker**, which is `Recipient` for `!forcedrink`
  and `Initiator` for `!drinkfrom` — so the rule cannot be a fixed constant the way `FeedProcessor`'s
  is. Either the rule becomes role-aware, or the two commands get separate processor instances that
  differ only in this. Flagging as the one place the shared-processor decision costs something.
- `InteractionEiconSupport.TokenToVerbKeys` needs entries for both command names so a custom
  `!seteicon drinkfrom` resolves.
- `EiconAppliesToBothParties`: **no**. One party drinks, the other is drunk from; they are not
  symmetric.

---

## Persistence

Nothing new. No collection, no new `Profile` field.

| What | Where | Note |
|---|---|---|
| Per-direction lock | `Profile.timers["milk_give_<source>"]` on the drinker | shared with `!milk`, unchanged |
| Daily corruption budget | `Profile.dailyMagnitudes["drinkshift_<yyyy-MM-dd>"]` on the drinker | shared with `!drink`, unchanged |
| Corruption value | `Profile.characteristics[CorruptionProcessor.CorruptionCharacteristicKey]` | unchanged |
| Vices | via `DoseProcessor.IntensifyExistingVices` | unchanged |
| Counts | `Profile.counts["drinkfromgive" / "drinkfromtake"]` | new keys, no migration |
| Interaction history | `Interaction` document, type `drinkfrom` | both commands log the same type |

Both commands writing one interaction type means `!dossier` and the statistics commands see one
activity, which is right — it is one act.

---

## Reversal

**None, and none is needed.** A source drink is consumption, not a state change: it moves
corruption (already reversible through `!purify` / `!corrupt`) and may deepen an existing vice
(already reversible through `!detox`, at a `PurgeCostType` cost). Nothing here creates a state with
no exit, which is the test the reversal convention exists to satisfy.

## Status-effect contribution

**None.** This interaction *consumes* status effects (a satisfied craving) and *is gated by* them
(the base blocker check, plus the broken-part gate), but contributes no `IStatusEffectContributor`
of its own. No `IPostInteractionEffect` either.

---

## Files

### New

| File | What |
|---|---|
| `InteractionProcessors/Involved/SourceDrinkProcessor.cs` | the processor, role-mapped |
| `BotCommands/ChateauDrinkfrom.cs` | `!drinkfrom` |
| `BotCommands/ChateauForcedrink.cs` | `!forcedrink` |
| `InteractionProcessors/Support/SubstanceBodyparts.cs` | `BodypartsForSubstance` + the break gate, lifted out of `MilkProcessor` |
| `FChatDicebot.Tests/Unit/Sourcedrinkprocessortests.cs` | see Tests |

`FChatDicebot.csproj` globs `**\*.cs`, so none of these need a csproj edit.

### Modified

| File | Change |
|---|---|
| `InteractionProcessorRegistry.Initialize()` | one `RegisterProcessor` line |
| `ChateauCurrency.cs` | two new constants in the `!drink` block |
| `BotCommands/ChateauDrink.cs` | lift `ApplyCorruption` / `ApplyAddictionRoll` to take a source-agnostic shape; add the new commands to `RelatedCommands` |
| `InteractionProcessors/Involved/MilkProcessor.cs` | delegate the break gate to the shared helper; `DirectionTimerKey` / `HasActiveDirectionLock` stay put and get reused |
| `BotCommands/ChateauSystemTitles.cs` | two title ladders |
| `InteractionEiconSupport.cs` | `TokenToVerbKeys` entries |
| `wiki-docs/Command-Reference.md` | two entries |
| `wiki-docs/specs/README.md`, `Future-Interactions/README.md` | index moves on ship |

---

## Tests

DB-touching tests use `TestDatabaseFixture` and join the `Database` collection.

**Effects**
- a corrupt source shifts the drinker 2, not 1
- a purified source shifts +2
- a neutral-corruption source shifts nothing, and says so
- the budget is shared with `!drink`: one bottle then one source drink spends exactly 3 and the
  next drink of either kind moves nothing
- partial spend: 2 bottles then a source drink applies 1, not 2 and not 0
- craving satisfaction fires identically to the bottled path
- addiction intensifies only for a vice already carried; a clean drinker is never hooked
  (seeded `Random`, both above and below the 20% threshold)

**Lock**
- `!milk` then `!drinkfrom` against the same source, same day → refused
- `!drinkfrom` then `!milk` → refused, and the refusal names drinking, not milking
- reversed direction same day → allowed
- a third resident drawing from the same source → allowed
- TOCTOU: lock lands between command and consent → no effects, no channel message

**Roles**
- `!drinkfrom` gives the initiator `drinkfromgive`; `!forcedrink` gives the initiator
  `drinkfromtake`
- both write one `Interaction` of type `drinkfrom`

**No keepsake**
- neither command creates a `MilkBottle` or advances the bottle serial counter — assert the
  `Counters` document is untouched, which is the assertion that would actually catch a stray
  `ClaimBottleSerials`

**Validation**
- missing substance, non-substance identifier, self-target, broken source part

---

## User-facing text

**Every string below is pending owner wording review.** Nothing here is drafted yet; the first
drafts come from `wiki-docs/Style-Guide.md` and come back for approval before the PR closes.

| Where | Needs |
|---|---|
| `!drinkfrom` consent prompt | asked of the source. Must end with the standing "(or !no)" reminder — override `BuildConsentWarning`, never `GetConsentWarning`. Should carry the corrupt/pure projection the way `MilkProcessor.BuildConsentWarning` already does, since the source's corruption is what the drinker is signing up for |
| `!forcedrink` consent prompt | asked of the drinker. Same reminder. Must not read as though the recipient has no choice |
| completion message | one message, role-aware. Carries the corruption fragment, the status fragments, and the addiction line, assembled the way `ChateauDrink.BuildChannelMessage` does |
| partial-budget fragment | new state — "some of it lands" — with no existing string to reuse |
| lock refusal | a drinking-flavored variant of `MilkProcessor.DirectionLockMessage` |
| self-target refusal | points at `!drink` |
| help fields on both commands | `ShortDescription`, `LongDescription`, `Usage`. The `LongDescription` should say plainly that this leaves no bottle — that is the whole trade-off and residents will otherwise read it as a bug |
| two title ladders | 4 titles each |

`Utils.SubstanceToText` reads `Identifier.displayText`, so a new substance renders with no code
change. Never say "UTC day" — "Chateau day", or just "day".

---

## Assumptions

Defaults taken where the owner's answers did not reach. Any of these can be overridden before
implementation without changing the shape of the feature.

1. **One processor, two commands**, role-mapped like `!climax` / `!climaxfor`. The alternative is
   two processors sharing a base, which costs duplication but makes the eicon rule simpler.
2. **Involved tier**, matching `!milk` and `!feed`.
3. **Self-target rejected** rather than shortcut, since `!drink` covers the useful case.
4. **Counts are new** (`drinkfromgive` / `drinkfromtake`) rather than folded into
   `milkgive` / `milktake`. Folding them in would let existing milk titles progress from drinking,
   which reads as a bug on `!dossier`.
5. **Shift of 2 and a 20% addiction chance** — double the bottled numbers, chosen because doubling
   is legible to residents and keeps the shared budget arithmetic clean (a source drink is exactly
   two bottles' worth). Any other multiplier works mechanically.
6. **Clamping to remaining budget** rather than refusing outright.
7. **No aliases** initially.
8. **No `!drinkfrom` for NPCs or self** — residents only, like every other interaction.
