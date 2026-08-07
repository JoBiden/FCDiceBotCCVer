# Drink From Source (`!drinkfrom` / `!forcedrink`)

**Status:** Implemented.

Origin: resident feedback `6a6efae4` (2026-08-02), approved for spec on 2026-08-02, design
reviewed and shipped 2026-08-03. Dossier:
`scripts/feedback-triage/queue/2026-08-02-6a6efae4-drink-from-the-source.md`.

See [Differences from the original spec](#differences-from-the-original-spec) for what the
design review changed — four of the open questions this spec raised were decided against its own
recommendation, and two of its claims about the codebase were wrong.

> expand the drink and feed system to allow residents to consume directly from the source, gaining
> any corruption effects and fueling any addictions but skipping the !milk step. Consider the
> reverse interaction as well. Something like !drinkfrom and !forcedrink

Today, consuming another resident's fluid is a two-step loop: `!milk` them into a numbered bottle,
then `!drink` that bottle. This adds a one-step path that keeps the mechanical payload and
produces no bottle — and its mirror, where the donor initiates.

**Depends on:** [Bottle-Consumption-And-Transfer](Bottle-Consumption-And-Transfer.md) (the
`!drink` effect helpers), [Milk](Milk.md) (the per-direction lock),
[Dose-and-Detox](Dose-and-Detox.md) (vice intensification, craving satisfaction),
[Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md).

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

### The reading, confirmed at review

"Fresher is stronger" is **a bigger magnitude, not a wider net** — confirmed 2026-08-03. The
corrupt/purified *tag* still comes from the donor's live corruption at the same ±10 thresholds
(`ChateauCurrency.GetCorruptionTagForValue`); what changes is how far one drink moves you. A
neutral-corruption donor still gives a drink that shifts nothing. The alternative — lowering the
threshold so a donor at ±5 already counts as potent — was considered and rejected.

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

`!milk` stamps `milk_give_<sourceUserName>` on the **milker** until the next day-boundary. The key
is per (taker, source) pair and directional — being milked by Alice does not stop you milking
Alice back.

**All three commands share the lock, but not the key.** `SourceDrawLock` owns both prefixes
(`milk_give_`, `drinkfrom_give_`) and every caller resolves through `SourceDrawLock.ActiveLock`,
which checks both and reports *which verb* holds it. A source drink stamps its own key on the
drinker's profile.

Sharing one key would have been less code, and was rejected at review for one reason: it makes
`!milk`'s refusal lie. `MilkProcessor.ValidateInteraction` would report "You've already milked Bob
today" to someone who had only drunk from him, and `CoolDown` carries nothing but
`timerStart`/`timerEnd` to distinguish the two after the fact. Separate keys mean each refusal
names what actually happened.

Consequences, stated plainly because they are the balance decision:

- Alice can draw from Bob **once per Chateau day**, by any of `!milk`, `!drinkfrom`, or Bob's
  `!forcedrink` — whichever happens first consumes the day.
- Bob drawing from Alice is unaffected, in either direction.
- Carol drawing from Bob is unaffected.

`MilkProcessor.HasActiveDirectionLock` and `DirectionTimerKey` survive as thin delegates to
`SourceDrawLock`, so every existing caller and test keeps working; `HasActiveDirectionLock` now
answers "drawn from by any verb". `ChateauMilk`'s self-milk pre-check and
`MilkProcessor.ValidateInteraction` were both switched to resolve the verb first.

**TOCTOU recheck.** `ChateauConsent` re-validates before processing, but only through the
three-argument `ValidateInteraction`, which carries no verb and therefore cannot do the
role-dependent checks. So `SourceDrinkProcessor.ProcessInteraction` re-checks both the lock and
the broken-part gate itself, does nothing at all when either fires, suppresses the channel message
by returning an empty completion, and tells the initiator privately why — the same shape
`MilkProcessor` uses, plus the private note.

---

## Effects

All three already exist and are already factored. `ChateauDrink.Execute` is a DB-injected static
whose only bottle-specific dependency is that it takes a `MilkBottle`; the effect helpers
underneath it are what get reused.

| Effect | Reused from | Source-drink behavior |
|---|---|---|
| Corruption / purity shift | `ChateauDrink.ApplyCorruption`, `ShiftForTag` | tag computed live from the source's corruption; **doubled magnitude** — see Balance |
| Craving satisfaction | `DoseStatusContributor` via the normal interaction path | **identical** — a craving is satisfied or it isn't; there is no potency axis here |
| Addiction deepening | `DoseProcessor.IntensifyExistingVices` | **doubled chance**, still intensify-only |

**Satisfaction needed two code changes the spec didn't anticipate.** `DoseStatusContributor.IsSatisfiedBy`
matches on the *interaction type name*, so the source-drink verbs had to be added alongside `feed`,
`odorize`, `dose` and `drink`. Without that a hooked drinker would have read a contradictory
craving fragment on the very drink that satisfied them.

The second is the larger one. The contributor is `SymmetricInvocation`, so it attends to **both**
parties, and satisfaction was originally keyed only to the vice — meaning a source drink quieted
the craving of whichever party was hooked, including a source who swallowed nothing. Owner's call
on 2026-08-07: **only the consumer is satisfied**, for `!feed` as well as for these two, and the
non-consuming party still runs the ordinary craving roll. Watching someone else get what you crave
is exactly when a craving line should fire. See
[Dose-and-Detox](Dose-and-Detox.md#cravings-status-effect) for the full rule; `!odorize` and
`!dose` are unchanged.

That forced a small base-class seam. The consumer is the initiator on `!drinkfrom` and the
recipient on `!forcedrink`, and the contributor has only `isInitiator` to tell the two parties
apart — so one shared interaction type cannot express the rule.
`InteractionProcessorBase.ContributorInteractionType` (default: `InteractionType`, so no existing
processor changes) lets this processor report the verb that was actually typed. It is the one place
the two verbs are not collapsed into one type, and it remains this processor's own override after
[Invertible-Role-Interactions](Infrastructure/Invertible-Role-Interactions.md) — contributors are
handed no identifier, so the base class has nothing to resolve a direction from. It now reads the
typed verb through the shared `ResolveTypedVerb` hook, keeping the per-call stash to one reader.

One further consequence of routing through the interaction path rather than
`SelfCommandStatusEffects` (a self-command seam, which would have double-emitted here since the
base runs the contributors anyway): the base wrapper appends status fragments *after* the
processor's own message, so the order is corruption → addiction line → satisfaction, where
`!drink` reads corruption → satisfaction → addiction line. Getting `!drink`'s order back would
mean either duplicating fragments or smuggling the addiction line through the rate-limit channel;
neither is worth a word order.

### Refactor these out of `ChateauDrink`

`ApplyCorruption` and `ApplyAddictionRoll` were private and took a `MilkBottle`. They are now
public statics taking exactly what they read — `ApplyDrinkCorruption(profile, corruptionTag,
magnitude)` and `ApplyDrinkAddictionRoll(profile, substance, chance, rng)` — each returning a small
outcome struct. The spec proposed a shared `SourceOfDrink` type for this; it wasn't needed, because
`ApplyCorruption` only ever read `bottle.corruptionTag` and `ApplyAddictionRoll` only ever read
`bottle.substance`. Two parameters, not a class.

`ShiftForTag` also needed a magnitude overload — it was public and hardcoded
`DrinkCorruptionShiftPerBottle`, which the spec didn't notice. The one-argument form remains and
delegates.

`ChateauDrink`'s public surface and behavior are otherwise unchanged, which the untouched
`ChateauDrinkTests` suite pins.

### No keepsake

No `MilkBottle` is created, **no serial is claimed** (`ClaimBottleSerials` is not called), nothing
lands in `!bottles`, and there is nothing to `!sell`. The bottle path gives you a permanent
numbered empty; the source path gives you a stronger hit and no proof it happened.

The spec argued this had to be spelled out in the help text or residents would read the missing
bottle as a bug. At the wording review the owner cut it: the shipped `LongDescription`s describe
the strength and the shared daily draw, and leave the absence of a bottle to be inferred.

A completion flourish pointing at `!milk` as the way to keep some was drafted and then cut, because
it contradicted the draw lock — having just drunk from someone, you cannot milk them until the next
Chateau day. Worth remembering if the pool is extended: **the two verbs are alternatives for the
day, not a sequence**, and flavor that suggests otherwise is wrong rather than merely loose.

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

A separate budget was considered and rejected: it would let a resident take 3 bottles *and* 3
source drinks, doubling the achievable daily drift, which is the exact thing
`DrinkCorruptionDailyLimit` exists to bound (`ChateauCurrency`'s own comment calls it "the
load-bearing balance lever", since bottles pool freely through `!pay`).

### The last drink lands whole

The gate is **"any budget left", not "enough budget left"** — the same `if (used >= limit)` test
the bottled path uses, unchanged. A source drink that starts inside the limit applies its full 2
even when that carries the day's total past 3.

So the real daily ceiling is `limit - 1 + shift` = **4 points**, not 3. That is bounded, not
open-ended: the next drink of either kind finds `used >= limit` and moves nothing.

| A day's drinking | Points spent | Effect of the last drink |
|---|---|---|
| 3 bottles | 3 | full |
| 2 bottles, then a source drink | 4 | full — starts at 2, which is under the limit |
| 1 source drink, then 2 bottles | 4 | full |
| 2 source drinks | 4 | full |
| anything, then one more | — | nothing; the over-budget message |

Two alternatives were considered and rejected at review: **clamping** to the remaining budget
(applies 1 instead of 2) adds a third message state the bottled path doesn't have, and a
half-strength drink reads as a bug; **refusing** outright unless the full 2 is free is consistent
with `!drink` but produces a refusal that reads arbitrary to someone who can see they have budget
left. Overshooting keeps one message state and one gate, and pays for it with a ceiling one point
higher than `DrinkCorruptionDailyLimit` names.

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

Title ladders sit in `ChateauSystemTitles`' milestone table at the usual 1 / 5 / 10 / 25
thresholds for an Involved interaction — `ChateauSystemTitles` is authoritative for the text.

Counts are incremented on the consent path, so `ChateauConsent`'s sweep grants the titles — no
`CheckAndGrantTitles` call of its own is needed (see `Pledges.md` for what happens when a command
off that path forgets one).

---

## Eicons

- **No `BodypartEiconRule`.** The part in play belongs to the drinker, which flips between the two
  verbs, and `BodypartEiconRule` is a no-argument property that cannot see the typed verb. Making
  it role-aware (a base-class API change) or splitting into two processor instances were both
  considered; dropping the rule was chosen because residents lose nothing they would actually set —
  the custom `!seteicon drinkfrom` path still renders on the right person.
- `GetEiconSubject(interactionVerb, …)` resolves the custom eicon to the drinker. This hook already
  existed for `!climax`/`!climaxfor` and the spec missed it, which is why it reported the eicon
  problem as larger than it was. Since
  [Invertible-Role-Interactions](Infrastructure/Invertible-Role-Interactions.md) shipped, the base
  class resolves it from `SourceDrinkProcessor.DrinkRoles` and this processor no longer overrides
  the hook at all.
- `InteractionEiconSupport.TokenToVerbKeys` gets both command names, and — the part that is easy to
  miss — `NormalizeVerbKey` folds `forcedrink` onto `drinkfrom` as well. The completion suffix looks
  the eicon up by the raw verb on `Interaction.type`, so without the fold a `!forcedrink` would read
  an `eicon_forcedrink` slot that `!seteicon` never writes. Same arrangement as
  `climaxfor` → `climax`: one stored icon, set it once with either name.
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
| `InteractionProcessors/Involved/SourceDrinkCommandSupport.cs` | shared command-side wiring for both verbs (mirrors `ClimaxCommandSupport`) |
| `InteractionProcessors/SourceDrawLock.cs` | `DrawVerb`, the two key prefixes, `ActiveLock` / `IsLocked` / `Stamp` / `LockMessage` |
| `InteractionProcessors/SubstanceBodyparts.cs` | the substance→bodypart map + the break gate, lifted out of `MilkProcessor` |
| `BotCommands/ChateauDrinkfrom.cs` | `!drinkfrom` |
| `BotCommands/ChateauForcedrink.cs` | `!forcedrink` |
| `FChatDicebot.Tests/Unit/Sourcedrinkprocessortests.cs` | see Tests |

The two shared helpers sit directly in `InteractionProcessors/` alongside `IdentifierPayload.cs`
and `BodypartEiconRule.cs`, not in a `Support/` subfolder — the spec invented one that doesn't
exist. `FChatDicebot.csproj` globs `**\*.cs`, so none of these need a csproj edit.

### Modified

| File | Change |
|---|---|
| `InteractionProcessorRegistry.Initialize()` | one instance registered under both type keys |
| `InteractionProcessorBase.cs` | `ContributorInteractionType` virtual (default `InteractionType`) so a two-key processor can report the typed verb to contributors |
| `ChateauCurrency.cs` | `SourceDrinkCorruptionShift`, `SourceDrinkAddictionChance` |
| `BotCommands/ChateauDrink.cs` | `ApplyDrinkCorruption` / `ApplyDrinkAddictionRoll` / `ShiftForTag(tag, magnitude)` made public and source-agnostic; `RelatedCommands` |
| `InteractionProcessors/Involved/MilkProcessor.cs` | lock and break gate delegated to the shared helpers; refusal now names the verb that holds the lock |
| `BotCommands/ChateauMilk.cs` | self-milk pre-check resolves the holding verb; `RelatedCommands` |
| `StatusEffectContributors/DoseStatusContributor.cs` | `IsSatisfiedBy` takes `isInitiator` and satisfies only the consuming party; source-drink verbs added. Changes `!feed` as well — see [Dose-and-Detox](Dose-and-Detox.md) |
| `BotCommands/ChateauSystemTitles.cs` | two title ladders |
| `InteractionEiconSupport.cs` | `TokenToVerbKeys` entries, `NormalizeVerbKey` fold, `CanonicalTokensInOrder` |
| `wiki-docs/Command-Reference.md` | two rows, a bottle-collection note, the eicon-owner line |
| `wiki-docs/specs/README.md`, `Future-Interactions/README.md` | index moves |

---

## Tests

DB-touching tests use `TestDatabaseFixture` and join the `Database` collection.

**Effects**
- a corrupt source shifts the drinker 2, not 1
- a purified source shifts +2
- a neutral-corruption source shifts nothing, and says so
- the budget is shared with `!drink`: one bottle then one source drink spends 1 + 2
- the last drink lands whole: at `limit - 1` used, a source drink still applies its full 2 and the
  day's total ends at 4
- with the budget gone, nothing moves and the message says so
- addiction intensifies only for a vice already carried; a clean drinker is never hooked
  (seeded `Random`, both above and below the 20% threshold)

**Lock**
- `!milk` then `!drinkfrom` against the same source, same day → refused, naming milking
- `!drinkfrom` then `!milk` → refused, and the refusal names drinking, not milking
- `!drinkfrom` twice → refused
- reversed direction same day → allowed
- a third resident drawing from the same source → allowed
- `!forcedrink` checks the lock against the drinker, not whoever typed it
- the stamp lands on the drink verb's own key, on the drinker's profile, expiring at the next
  day-boundary — and still blocks a milking through the shared lookup
- TOCTOU: lock lands between command and consent → no effects, no counts, no channel message, and
  a private note to the initiator

**Roles**
- `!drinkfrom` gives the initiator `drinkfromgive`; `!forcedrink` gives the initiator
  `drinkfromtake`
- `!forcedrink` applies the corruption to the recipient, and reads the tag off the initiator
- both report one interaction type, `drinkfrom`, to history — the sole exception being the
  contributor call, which sees the typed verb
- with both parties hooked on the substance, only the drinker is satisfied, in each direction;
  the source still rolls a craving

**No keepsake**
- neither command creates a `MilkBottle` or advances the bottle serial counter — assert the
  `Counters` document is untouched, which is the assertion that would actually catch a stray
  `ClaimBottleSerials`

**Validation**
- missing substance, non-substance identifier, self-target, broken source part

---

## User-facing text

All strings below shipped in the implementation PR and went through the owner wording review
described in CLAUDE.md. `SourceDrinkProcessor` and `SourceDrawLock` own them; the two title
ladders live in `ChateauSystemTitles`.

| Where | Owner |
|---|---|
| `!drinkfrom` / `!forcedrink` consent prompts | `SourceDrinkProcessor.BuildConsentWarning(…, typeKey)`. Both carry the corrupt/pure projection the way `MilkProcessor` does, and both close with the standing "(or !no)" reminder via `ConsentWarningText.WithDeclineHint` |
| completion message | `SourceDrinkProcessor.BuildChannelMessage`, role-aware; corruption fragment then addiction line, with contributor fragments appended by the base wrapper |
| lock refusals (both verbs, both self and other) | `SourceDrawLock.LockMessage` |
| broken-part refusals | `SubstanceBodyparts.CheckBreakBlock`, with the verb-specific tail from `MilkProcessor.MilkBreakBlockTail` / `SourceDrinkProcessor.DrinkBreakBlockTail` |
| self-target refusal | `SourceDrinkProcessor.SelfTargetText` — points at `!drink` |
| help fields | the two command classes. Both `LongDescription`s say plainly that this leaves no bottle |
| two title ladders | `ChateauSystemTitles`, 4 titles each |

`Utils.SubstanceToText` reads `Identifier.displayText`, so a new substance renders with no code
change. Never say "UTC day" — "Chateau day", or just "day".

---

## Differences from the original spec

The design review on 2026-08-03 checked the spec against the code before implementing. Four open
questions were decided, two of them against the spec's own recommendation, and two of the spec's
claims about the codebase turned out to be wrong.

**Decided against the spec's recommendation**

1. **Separate lock keys, not one shared key.** See [The per-direction lock](#the-per-direction-lock).
   The spec only noticed that `!drinkfrom` needed its own refusal wording; it missed that sharing
   the key makes `!milk`'s refusal claim a milking that never happened.
2. **The last drink lands whole, rather than clamping to the remaining budget.** See
   [The last drink lands whole](#the-last-drink-lands-whole). The day's ceiling is 4 points, not 3.

**Decided where the spec left it open**

3. **No bodypart eicon**, rather than making `BodypartEiconRule` role-aware or splitting the
   processor. See [Eicons](#eicons).
4. **"Fresher is stronger" means magnitude only**, confirming the spec's reading.

**Spec claims that were wrong**

5. **The eicon problem was smaller than reported.** `GetEiconSubject(interactionVerb, …)` already
   existed and already solved the custom-eicon half; only the bodypart half was ever stuck.
6. **`SourceOfDrink` was unnecessary.** Both helpers read a single field off the bottle, so they
   take parameters instead of a new type. `ShiftForTag` needed a magnitude overload the spec didn't
   mention.

**Discovered during implementation**

7. **`DoseStatusContributor.IsSatisfiedBy` needed both a `drinkfrom` case and a consumer-side
   rule**, or a hooked drinker would have read a craving fragment on the drink that satisfied
   them — and a source who swallowed nothing would have read as satisfied. The spec described this
   reuse as free. The fix reaches `!feed` too, and added
   `InteractionProcessorBase.ContributorInteractionType`.
8. **The interaction identifier has to stay the bare substance** — that same `IsSatisfiedBy` match
   is against a vice name, so the composite payload pattern `!milk` and `!climax` use would have
   broken satisfaction silently. The typed verb travels by other routes instead; see the class
   comment on `SourceDrinkProcessor`.
9. **`InteractionEiconSupport.NormalizeVerbKey` needed the `forcedrink` → `drinkfrom` fold**, not
   just a `TokenToVerbKeys` entry.

**Found after shipping**

10. **Both commands were missing from the no-arg `!help` listing.** The spec never mentioned the
    step, because listing a command was a second, hand-maintained thing: eight name arrays in
    `ChateauHelp.cs`. It is no longer a step at all — `ChateauHelp.SectionFor` derives the listing
    from each command's `Category`, so these two appeared under Involved Interactions the moment
    the arrays went away. See [Development-Guide](../Development-Guide.md#adding-a-new-command).

## Assumptions carried into the implementation

1. **One processor, two commands**, role-mapped and registered under both type keys like
   `!climax` / `!climaxfor`.
2. **Involved tier**, matching `!milk` and `!feed`.
3. **Self-target rejected** rather than shortcut, since `!drink` covers the useful case.
4. **Counts are new** (`drinkfromgive` / `drinkfromtake`) rather than folded into
   `milkgive` / `milktake`. Folding them in would let existing milk titles progress from drinking,
   which reads as a bug on `!dossier`.
5. **Shift of 2 and a 20% addiction chance** — double the bottled numbers, chosen because doubling
   is legible to residents and keeps the shared budget arithmetic clean.
6. **No aliases** initially. `!suckle` and `!nurse` remain one array entry each plus a
   `TokenToVerbKeys` entry if the owner wants them.
7. **No `!drinkfrom` for NPCs or self** — residents only, like every other interaction.
