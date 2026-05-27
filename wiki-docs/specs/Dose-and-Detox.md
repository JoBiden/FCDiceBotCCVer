# `!dose` + `!detox`

Dose the recipient with an addictive vice. Cravings appear in subsequent interactions; `!feed` of the matching substance or `!odorize` of the matching scent satisfies the craving without raising severity. Re-dosing the same vice escalates severity. `!detox` removes a vice at a cost.

**Status:** Implemented (2026-05-26).
**Investment level:** Consequence
**Reversal:** `!detox` (with cost).
**Depends on:** [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md) (extended for this feature). Modifies the shipped [Climax-and-Climaxfor](Climax-and-Climaxfor.md) processor to apply an automatic dose at completion. Interacts with the shipped [Odorize-and-Wash](Odorize-and-Wash.md) and `!feed` for satisfaction.

## Vice catalog

A "vice" is any identifier with category `"vice"` in the existing `Identifiers` collection. That set already includes the relevant scents (musk, seminal, lemonade, floral, sweet, savory, spicy, bitter, salty, liquor, perfume) and substances (golden, cum, goo, lustessence, pollen, sap, pre, saliva, sweat, drug, drink, food, energy). No new catalog, no umbrella-matching logic — vice/substance/scent overlap is already encoded as multi-category identifiers.

Validation calls `Database.GetIdentifier(arg)` and rejects if either the identifier doesn't resolve or its `categories` doesn't contain `"vice"` (case-insensitive, same shape as `OdorizeProcessor` checks for `"scent"`).

## `!dose` command syntax

```
!dose [user]Bob[/user] {vice}
!dose {vice}                  → self-target (allowed; you can give yourself an addiction)
```

## `!dose` validation

- Recipient registered (or self-target — self-target is explicitly allowed).
- `vice` resolves to a vice-category identifier.
- Multiple vices allowed simultaneously on one profile. Re-dosing the same vice escalates `AddictionLevel` rather than rejecting.

## `!dose` processor logic (`DoseProcessor`)

1. Find existing `ViceInstance` for `vice` on recipient. If present, increment `AddictionLevel` by 1 capped at `MaxAddictionLevel` (10). Otherwise append a new instance at level 1.
2. Set 7-day cooldown on initiator's `timers["dose_" + vice + "_" + recipient.userName]` (per-(initiator, recipient, vice) — matches `OdorizeProcessor`'s tuple-cooldown shape).
3. `Database.AddInteraction(...)`; `SetProfile(recipient, ...)`; `SetProfile(initiator, ...)`; `DeletePendingCommand(...)`.
4. Return type key.

### `ViceInstance` shape

```csharp
public class ViceInstance
{
    public string Vice;               // identifier name, lowercase
    public int AddictionLevel;        // 1..MaxAddictionLevel
    public string DosedBy;            // most recent doser's displayName
    public DateTime FirstDosedAt;
    public DateTime LastEscalatedAt;
}
```

Stored JSON-encoded inside `Profile.lists["vices"]` (mirrors the [`ScentLayer`](../../FChatDicebot/Model/ScentLayer.cs) pattern — same `LoadAll` / `SaveAll` helpers, same silent-drop on malformed entries). Constants and helpers live on `DoseProcessor`.

## Cravings (status effect)

`DoseStatusContributor` runs on every consent-driven completion. For each `ViceInstance` on the profile being passed (initiator OR recipient — see "Symmetric cravings" below):

1. **Satisfaction check first.** If the parent interaction satisfies this specific vice, emit a satisfaction fragment and skip the craving roll. Satisfaction matches:
   - `parentInteractionType == "feed"` AND `parentIdentifier == vice` (case-insensitive).
   - `parentInteractionType == "odorize"` AND `parentIdentifier == vice` (case-insensitive).
   - `parentInteractionType == "dose"` AND `parentIdentifier == vice` (case-insensitive) — the act of escalating also satisfies the moment.

   Satisfaction fragment template (first draft, wording subject to review):
   > `"…the craving for {vice} momentarily quieted."`

2. **Craving roll.** Otherwise roll a craving with probability `AddictionLevel * CravingProbabilityPerLevel` (default 10% per level → 100% at level 10).
3. On hit, emit one craving fragment. First-draft pool (wording subject to review):
   > `"{subject}'s hands tremble — the {vice} craving doesn't go quietly."`
   > `"A flash of {vice} hunger crosses {subject}'s face."`
   > `"{subject}'s thoughts drift back to {vice} for a beat."`

4. Cravings do **not** mutate `AddictionLevel`. The vice persists at its current level until `!detox` clears it or another `!dose` escalates it.

### Symmetric cravings (infra change)

The Dose status hook fires for **both** the initiator and the recipient of the parent interaction. Today's `InteractionProcessorBase` default only calls `GetActiveStatusEffects(recipientProfile, …)` at the recipient — initiator-side calls are opt-in per-processor.

This spec promotes that to a base-class default at the completion call site: `GetCompletionMessage`-using overrides should consume both `effects.CompletionAppendix` lists. Concretely, a `GetActiveStatusEffectsBothSides(initiator, recipient, parentType, parentIdentifier)` helper on `InteractionProcessorBase` aggregates two passes (one with `isInitiator: true`, one with `isInitiator: false`) and returns a merged `StatusEffectFragments`. Existing overrides that already call the recipient-only variant keep working; the new helper is what processors switch to.

### Contributor interface change

`IStatusEffectContributor.Contribute(...)` gains a `string parentIdentifier` parameter (and `GetActiveStatusEffects` plumbs it through). Existing contributors (`OdorizeStatusContributor`, `CorruptionStatusContributor`, `BreakStatusContributor`) ignore the new arg — only `DoseStatusContributor` reads it. Update their signatures + every test contributor fake.

```csharp
public interface IStatusEffectContributor
{
    StatusEffectFragments Contribute(
        Profile profile,
        StatusEffectCallSite callSite,
        string interactionType,
        string parentIdentifier,     // new — the parent interaction's identifier
        bool isInitiator);
}
```

### Within-`!dose` behavior

When `DoseStatusContributor` runs during a `!dose` (parentInteractionType=="dose"):
- The vice instance matching `parentIdentifier` gets a satisfaction fragment (per the rule above).
- *Other* active vices on the same profile still roll cravings normally — the recipient is still addicted to them. (User decision: realism over uniform skip.)

## `!climaxfor` / `!climax` integration

Climax does **not** satisfy cravings. Instead, climaxing applies an auto-`!dose` of all three of `cum`, `pre`, and `seminal` to the **non-climaxer** (the partner). Solo (no-partner) climaxes target the climaxer themselves.

**Intensify-only:** the auto-dose only increments `AddictionLevel` by 1 (capped at `MaxAddictionLevel`) on vices that are **already present** on the target. It never creates a new `ViceInstance`. So the climax-dose is a slow-burn escalator that bites once you've been dosed at least once by another route — your first cum-climax doesn't addict an unsoiled partner.

Implementation lives in `ClimaxforProcessor.ProcessInteraction` (and the self-target `PerformSelfTarget` path):

1. After persisting the interaction and bumping `dailyClimaxCounts`, compute the dose target:
   - Other-target climax: the partner (non-climaxer).
   - Self-target / solo climax: the climaxer themselves.
2. Call `DoseProcessor.IntensifyExistingVices(targetProfile, new[] { "cum", "pre", "seminal" })`. The helper:
   - For each vice name, if a `ViceInstance` exists on the target, increment `AddictionLevel` (capped). Otherwise no-op (no creation).
   - Returns the list of vices that actually intensified.
3. If anything intensified, append one summary fragment to the completion message (first draft):
   > `"{target}'s addictions to {vice-list} sharpen."`
   ("vice-list" formatted via existing `Utils.OxfordCommaJoin` or similar.)
4. Persist the target profile if any intensification happened.

`DoseStatusContributor`'s cravings still fire on the climaxer and the partner per the symmetric-cravings rule — but the climax interaction itself never matches the satisfaction conditions, so any cravings present will roll.

## `!detox` command syntax

```
!detox {vice}             → detox the named vice
!detox                    → detox the highest-`AddictionLevel` vice (ties broken by lowest `FirstDosedAt`)
```

`!detox` is a self-command — no consent flow, no `PendingCommand`. Goes through `ChateauDetox : ChatBotCommand` and a pure `DetoxCommand.Execute(...)` helper for testing, mirroring how [`ChateauWash`](../../FChatDicebot/BotCommands/ChateauWash.cs) is structured.

## `!detox` validation

- Self-targeted only.
- Caller has the named vice (if specified) — reject with "You aren't dosed with {vice}." plus a list of their actual vices.
- Caller has at least one vice (default-target). If none: "You're already clean. If that's not what you want, consider asking someone to !dose you…" (Style-Guide tone matching `ChateauWash`'s empty-state message.)

## `!detox` processor logic

1. Find the target `ViceInstance` (named, or highest-level).
2. **Apply the vice's detox cost** via the new `PurgeCostType` enum (see below).
3. Remove the `ViceInstance` entirely. No graduated detox in v1 — one `!detox` clears the vice regardless of `AddictionLevel`.
4. `Database.SetProfile(caller, callerProfile)`.
5. Return a channel message describing the detox and the cost suffered.

### `PurgeCostType` enum (new shared infra)

Introduced here and since reused by [Infest-and-Purge](Infest-and-Purge.md) and [Curse-and-Cleanse](Curse-and-Cleanse.md) (both shipped 2026-05-26). Lives at `FChatDicebot/InteractionProcessors/PurgeCostType.cs`.

```csharp
public enum PurgeCostType
{
    MissedWork,         // sets caller.timers["work_blocked"] until next UTC day midnight
    RandomCurse,        // *future* — applies a random curse (no-op + log until Curse ships)
    RandomBreak,        // applies a random break (random part, 1–3 days) via BreakProcessor
    LostTrainingPoint   // -1 to a random training where caller has level > 0
}

public static class PurgeCostApplier
{
    public static PurgeCostResult Apply(
        IChateauDatabase database,
        Profile callerProfile,
        PurgeCostType type);
}

public class PurgeCostResult
{
    public string Description;     // user-facing string: "Bob takes a random break (1 day on the arm)"
    public bool Applied;           // false if the cost type isn't fully wired yet
}
```

`DoseProcessor.DefaultDetoxCost` is `PurgeCostType.RandomBreak` (the body rebels at withdrawal). Future tuning could put a per-vice cost on the `ViceInstance` itself, but v1 is one cost for all vices.

The `RandomCurse` branch logs a "couldn't apply RandomCurse — Curse-and-Cleanse not yet shipped, falling back to RandomBreak" warning and degrades to `RandomBreak` so detox is never free. Tracked in [Curse-and-Cleanse.md](Future-Interactions/Curse-and-Cleanse.md) as a follow-up: when it ships, replace the fallback with the real curse application.

## Persistence

- Profile: `lists["vices"]` — list of JSON-encoded `ViceInstance` (see `ScentLayer` for the pattern).
- Profile timers: `dose_{vice}_{recipientUserName}` (7-day, on the initiator after a successful `!dose`); `work_blocked` (24-hour, written by `PurgeCostApplier.Apply(MissedWork)`).

## Tests

- `DoseProcessorTests.cs`:
  - First `!dose` creates `ViceInstance` at level 1, sets timestamps, sets initiator-side cooldown.
  - Second `!dose` of same vice → level 2.
  - Cap at `MaxAddictionLevel` (= 10) — eleventh `!dose` stays at 10.
  - Validation rejects non-vice identifiers (e.g. an identifier with `scent` but no `vice`).
  - Self-dose is allowed: initiator == recipient produces a single profile write at level 1.
  - `IntensifyExistingVices` intensifies present vices, no-ops on absent vices, returns the list it actually touched.
  - Persistence round-trip through `ViceInstance.LoadAll` / `SaveAll`.
- `DoseStatusContributorTests.cs`:
  - Craving probability scales linearly with `AddictionLevel` (statistical test across many trials).
  - `!feed` of matching substance → satisfaction fragment, no craving roll, no `AddictionLevel` change.
  - `!odorize` of matching scent → satisfaction.
  - `!dose` of same vice (parentIdentifier matches) → satisfaction.
  - `!dose` of *different* vice → other vices still craving-roll.
  - Climaxfor / climax interactions never trigger satisfaction (they dose instead).
  - Contributor is called once per side; `isInitiator` is honored.
  - Throws-safe: a malformed `ViceInstance` blob doesn't break the contributor.
- `DetoxCommandTests.cs`:
  - Named vice: vice removed, cost applied, channel message describes the cost.
  - Default-target picks highest `AddictionLevel`; ties broken by lowest `FirstDosedAt`.
  - Empty-state message when caller has no vices.
  - "You aren't dosed with {x}" lists the actual vice inventory.
  - `RandomCurse` cost type degrades to `RandomBreak` and logs a warning.
- `ClimaxforProcessorTests.cs` *(modify)*:
  - Other-target climax intensifies the partner's pre-existing `cum`/`pre`/`seminal` vices; non-present vices are untouched (no creation).
  - Self-target / solo climax intensifies the climaxer's own present vices.
  - Completion message gains the "addictions sharpen" fragment when something intensified, omits it otherwise.
- `PurgeCostApplierTests.cs`:
  - `MissedWork` writes `work_blocked` timer with end == next UTC midnight.
  - `RandomBreak` applies a `BreakInstance` via the existing break mechanism, duration 1–3 days, breakable-part list.
  - `LostTrainingPoint` decrements a training the caller has > 0; no-op if all trainings are at 0.
  - `RandomCurse` returns `Applied = true` (after degrading to RandomBreak) and the description mentions the fallback.
- `StatusEffectHookTests.cs` *(modify)*:
  - Contributor receives the new `parentIdentifier`; multiple-contributor merge preserves it.
  - The new both-sides helper aggregates initiator + recipient passes in one call.

## Files (as-shipped)

**New:**
- `FChatDicebot/Model/ViceInstance.cs`
- `FChatDicebot/Model/ViceText.cs` — vice-name rendering (mirrors `!odorize`'s scent rendering for scent-category vices; color overrides for `lustessence` → `[color=purple]lust essence[/color]` and `golden` → `[color=yellow]golden fluid[/color]`).
- `FChatDicebot/InteractionProcessors/Consequence/DoseProcessor.cs`
- `FChatDicebot/InteractionProcessors/StatusEffectContributors/DoseStatusContributor.cs`
- `FChatDicebot/InteractionProcessors/PurgeCostType.cs`
- `FChatDicebot/InteractionProcessors/PurgeCostApplier.cs`
- `FChatDicebot/BotCommands/ChateauDose.cs`
- `FChatDicebot/BotCommands/ChateauDetox.cs`
- `FChatDicebot.Tests/Unit/Doseprocessortests.cs`
- `FChatDicebot.Tests/Unit/Dosestatuscontributortests.cs`
- `FChatDicebot.Tests/Unit/Chateaudetoxtests.cs`
- `FChatDicebot.Tests/Unit/Purgecostappliertests.cs`

**Modify:**
- `FChatDicebot/InteractionProcessors/IStatusEffectContributor.cs` — adds `string parentIdentifier` to `Contribute` and a `bool SymmetricInvocation` property routing two-sides invocation at completion.
- `FChatDicebot/InteractionProcessors/InteractionProcessorBase.cs` — threads `parentIdentifier` through `GetActiveStatusEffects`; `GetCompletionMessageWithStatusEffects` now routes per-contributor: subject-only (corruption/break/odorize) keeps the prior single-subject behavior, symmetric contributors (dose) are called once per side. Self-target collapses to a single invocation per contributor.
- `FChatDicebot/InteractionProcessors/StatusEffectContributors/OdorizeStatusContributor.cs` / `CorruptionStatusContributor.cs` / `BreakStatusContributor.cs` — accept the new `parentIdentifier` (ignored) and declare `SymmetricInvocation = false`.
- `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` — registers `DoseProcessor`.
- `FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs` — registers `DoseStatusContributor` with a `MonDB.GetDatabase()` reference so it can resolve vice identifiers for scent rendering.
- `FChatDicebot/InteractionProcessors/Involved/ClimaxforProcessor.cs` — calls `DoseProcessor.IntensifyExistingVices` on the partner (other-target) or the climaxer (solo); stashes the rendered intensification fragment on `_lastClimaxDoseFragment` for `GetCompletionMessage` to consume.
- `FChatDicebot.Tests/Unit/Statuseffecthooktests.cs` — covers the new `parentIdentifier` param, symmetric vs subject-only invocation routing, and the self-target collapse.
- `FChatDicebot.Tests/Unit/Climaxforprocessortests.cs` — covers auto-dose behavior.
- Existing contributor-test files (`Breakstatuscontributortests.cs`, `Corruptionstatuscontributortests.cs`, `Odorizestatuscontributortests.cs`) updated to pass `parentIdentifier` to the new `Contribute` signature.
- `FChatDicebot.Tests/Unit/ClimaxforProcessorTests.cs` — cover auto-dose behavior.

## User-facing text (approved)

All `{vice}` slots below are rendered through `Model.ViceText.ViceName(identifier, name, dosedBy)` so:
- Scent-category vices mirror `!odorize` formatting (`Alice's musk`, `a scent of liquor`, `a salty scent`).
- `lustessence` → `[color=purple]lust essence[/color]`.
- `golden` → `[color=yellow]golden fluid[/color]`.
- Everything else renders as the raw vice name.

**`!dose` consent warning:**
> `{Initiator} wants to dose {Recipient} with {vice}! [b]This should not be taken lightly, and can not be done frequently. Addictive cravings can show up in any interaction until you !detox at a hefty cost. Everyone will know when you're satisfying an addictive craving.[/b] Do you !consent?`

**`!dose` completion:**
> `{Initiator} has dosed {Recipient} with {vice}. [sub](addiction N/10)[/sub]`

**`!dose` cooldown rejection (private):**
> `You've already dosed {Recipient} with {vice} too recently. Please respect that 'Consequence' interactions are meant to be meaningful, and not spammed. You'll be able to dose {Recipient} with {vice} again in {time}.`

**Craving fragment pool** (one picked at random when the roll fires):
- `{subject} has a sudden craving for {vice}.`
- `{subject} looks like they're distracted, daydreaming about {vice}.`
- `Some {vice} would really hit the spot about now, right {subject}?`

**Satisfaction fragment:** `{subject} gets the {vice} they've been craving.`

**Climax intensification fragment** (appended to the climax completion when one or more of cum/pre/seminal intensified):
> `...{target} is getting more and more hooked on {vice-list}.`

**`!detox` success (private — detox isn't a channel announcement):**
> `{Caller} manages to avoid {vice} for a whole day. {cost-description}`

`{cost-description}` is one of:
- `Withdrawals leave them too unwell to !work for the rest of the day.` (MissedWork)
- `It looks like in the process, they abused their {part} to the point of being exhausted for N day(s).` (RandomBreak)
- `They weren't able to do their usual {training} practice though... it feels like they've gotten worse at it as a result.` (LostTrainingPoint)
- `They had to ask a witch for help though, and the cost was a broken {part} for N day(s).` (RandomCurse temp-substitute until Curse-and-Cleanse ships)

**`!detox` empty state (private):**
> `You don't currently have any addictive vices. If you'd like to change that, consider asking someone to !dose you...`

**`!detox` wrong-vice (private):**
> `You aren't currently addicted to {vice}, but you are addicted to {vice-list-with-levels}. Maybe you'd like to !detox from one of those instead?`

**Help text:**
- `!dose` short: `Hook another resident on an addictive vice`
- `!dose` long: `Administer a large enough dose of an addictive vice to get a resident addicted, or deepen their existing addiction. Once addicted, their cravings will be notable even in unrelated interactions. When their craving is satisfied, everyone will know. Requires !consent.`
- `!detox` short: `Liberate yourself from an addiction, but at what cost?`
- `!detox` long: `Suffer the cost of withdrawal in order to end your addiction to a vice. This cost might be a missed day of work, a broken body part, loss of training prowess, or a curse, decided at random.`

## Assumptions / overrides

- **Vice catalog = identifiers tagged `vice`.** Override by adding a separate "addictive" category if vice/addictive should diverge.
- **`MaxAddictionLevel = 10`, linear 10%-per-level craving probability.** Both `const` on `DoseProcessor`. Override for a nonlinear curve or a different cap.
- **Cooldown = 7 days per-(initiator, recipient, vice) on the initiator's timers**, matching `OdorizeProcessor`.
- **Default detox cost = `RandomBreak`.** Override by adding a `PurgeCost` field to `ViceInstance` so each dose can carry its own cost (would mirror the `Parasite.Cost` field the future Infest spec describes).
- **v1 detox is full-clear.** Graduated detox (level decrement) is a future extension.
- **Climax auto-dose only intensifies present vices; never creates new ones.** Confirmed by user. Override only if a "first cum-climax addicts you" mechanic is later desired.
- **Solo climaxes self-dose** (climaxer's own existing cum/pre/seminal vices intensify). Confirmed by user.
- **`RandomCurse` currently falls back to a random break** with "they had to ask a witch for help though…" framing, until Curse-and-Cleanse ships. When it lands, swap the branch for the real curse and update the wording accordingly.
- **Symmetric cravings are routed by the contributor itself.** `IStatusEffectContributor.SymmetricInvocation` decides whether the completion-time wrapper calls a contributor once on the interaction's subject (corruption / break / odorize keep their prior single-subject behavior) or once per side (dose). No per-processor migration needed.
