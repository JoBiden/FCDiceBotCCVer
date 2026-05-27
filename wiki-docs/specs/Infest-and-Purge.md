# `!infest` + `!purge`

Infest the recipient with a parasite. Parasites have a chance to spread on each non-casual interaction; the originally-infested or newly-spread-to recipient can `!purge` at a cost.

**Status:** Implemented (2026-05-26).
**Investment level:** Consequence (`!infest`); `!purge` is a self-action with a cost.
**Reversal:** `!purge` (with cost).
**Depends on:** [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md), [Dose-and-Detox](Dose-and-Detox.md) (reuses the `PurgeCostType` / `PurgeCostApplier` infrastructure that shipped with `!detox`).

## Parasite catalog

Parasites are identifiers tagged `categories: ["parasite"]` in the existing `Identifiers` collection — no separate MongoDB collection. The shipped starter catalog: `paraslime`, `bimboslime`, `lustleeches`, `love`, `tentacles`, `nymites`.

Cost-per-parasite is encoded in `InfestProcessor.CostMap` (a static `IReadOnlyDictionary<string, PurgeCostType>`). Any parasite identifier without a `CostMap` entry falls back to `PurgeCostType.MissedWork`.

```csharp
public enum PurgeCostType   // defined in Dose-and-Detox.md infra
{
    MissedWork,        // (default) blocks !work for the next Chateau day
    RandomCurse,       // applies a random curse from the curse catalog
    RandomBreak,       // applies a random break (random part, 1-3 days)
    LostTrainingPoint, // -1 to a random training the user has at level >0
}
```

## `!infest` command syntax

```
!infest [user]Bob[/user] {parasiteName}
```

## `!infest` validation

- Recipient registered.
- `parasiteName` resolves to an identifier with `categories` containing `"parasite"`.
- Multiple parasites simultaneously **allowed** — no de-dup, but if the same parasite is already active on the recipient, reject ("Bob is already infested with paraslime — wait for them to !purge or pick a different parasite").

## `!infest` consent warning

Informative — the user must understand it can spread and can be cured easily if acted on quickly. The shipped wording calls out the parasite (via `ParasiteText`), the spread mechanic, and what `!purge` will cost; new-infestee grace period (24 h, free purge) is also surfaced.

## `!infest` processor logic (`InfestProcessor`)

1. Append a `ParasiteInstance` to `recipient.lists["parasites"]` with `SpreadFromContact = false` and no grace period.
2. Set `initiator.timers["infest_" + parasiteName + "_" + recipientUserName]` to 7 days (Consequence default, per-(initiator, recipient, parasite) pair-lock — matches the `OdorizeProcessor` / `DoseProcessor` shape).
3. Save interaction. Delete pending command.

## `ParasiteInstance` shape

```csharp
public class ParasiteInstance
{
    public string ParasiteName;
    public string InfestedBy;       // userName of the !infest initiator (or the spreader for spread cases)
    public DateTime InfestedAt;
    public bool SpreadFromContact;  // true if this came from spread, not direct !infest
    public DateTime GraceUntil;     // = InfestedAt + 24h (only meaningful if SpreadFromContact == true)
}
```

Stored JSON-encoded inside `Profile.lists["parasites"]` (mirrors the `ScentLayer` / `ViceInstance` pattern — same `LoadAll` / `SaveAll` helpers).

## Spread (`ParasiteSpreadEffect`, an `IPostInteractionEffect`)

Spread is cross-party (it can transfer from recipient to initiator or vice versa), which the existing single-profile `IStatusEffectContributor` couldn't model. The ship introduces a new sibling hook: **`IPostInteractionEffect`** receives BOTH profiles + the db handle, may mutate either, and returns completion fragments. Registered in `PostInteractionEffectRegistry`; wired into `InteractionProcessorBase.GetCompletionMessageWithStatusEffects` after status-effect aggregation.

Behavior of `ParasiteSpreadEffect`:

1. Skip if `parentInteractionType == "infest"` (otherwise the freshly-applied parasite would loop back to the initiator).
2. Skip on casual interactions entirely.
3. For each parasite on each side, roll spread independently — **10%** per parasite per side per non-casual interaction.
4. On a spread hit, if the partner does not already have that parasite, append a new `ParasiteInstance` to them with `SpreadFromContact = true`, `InfestedBy = {source}.userName`, `GraceUntil = now + 24 hours`.
5. Emit a completion fragment naming the parasite and the 24-hour grace window.

## Per-parasite flavor (`ParasiteFlavorContributor`, an `IStatusEffectContributor`)

A separate contributor adds flavor fragments to ordinary interactions (not the spread fragment):

- 25% per parasite per interaction, symmetric (both sides surface their own flavors via the symmetric-invocation routing landed in [Dose-and-Detox](Dose-and-Detox.md)).
- Fires on every investment level **including casual** (user-confirmed during wording pass).
- Skips `!infest` (completion already speaks about the parasite).
- Flavor templates live in `ParasiteFlavorContributor.FlavorMap`. Parasites without an entry contribute nothing (graceful degradation for future additions).

## `!purge` command syntax

```
!purge {parasiteName}
!purge                  → purges the oldest parasite
```

## `!purge` validation

- Caller has the named parasite (or any parasite for the bare form). Otherwise: "You aren't infested with {name}."

## `!purge` processor logic (`ChateauPurge`, system command — no consent flow)

1. Find the parasite instance on the caller's profile.
2. **Grace check**: if `SpreadFromContact == true` and `now < GraceUntil`, purge has **no cost**.
3. Otherwise, resolve the parasite's `PurgeCostType` via `InfestProcessor.CostMap` (or fall back to `MissedWork`) and apply via `PurgeCostApplier`.
4. Remove the parasite instance.
5. Output a private DM describing the purge and any cost suffered (matching `!detox` precedent — no channel announcement).

## `ParasiteText` rendering helper

Centralizes per-parasite display styling:
- `lustleeches` → `[color=purple]lust leeches[/color]`
- Default parasites render bare (lowercase identifier).
- Article inflection ("a"/"an") is suppressed when the name ends in `s`; `HasOrHave` does verb agreement against the same pluralization rule.

Use `ParasiteText` for any user-facing parasite display (mirrors `ViceText` for vices, `ScentText` for scents).

## Persistence

- Profile: `lists["parasites"]` (JSON-encoded `ParasiteInstance` entries).
- Profile timers: `infest_{parasite}_{recipient}` (7-day pair-lock on the initiator), `work_blocked` (24-hour, from `MissedWork` cost — set by `PurgeCostApplier`).
- **No new MongoDB collection** — parasite definitions live in `Identifiers` tagged `categories:["parasite"]`.

## Tests

- `Infestprocessortests.cs`: persists parasite, dedup rejection, consent text contains parasite description.
- `Parasitespreadeffecttests.cs`: spread chance honored over many trials; casual interactions skip spread; same-parasite-already-present skips; grace period set on spread.
- `Parasiteflavorcontributortests.cs`: 25% per parasite, symmetric invocation, casual interactions included, skips on `!infest`.
- `Chateaupurgetests.cs`: each cost type applied; grace removes cost; non-existent parasite rejected; oldest-first default selection.
- `Parasitetexttests.cs`: rendering for known parasites (with color overrides), pluralization rules for article and verb agreement.

## Differences from the original spec

The original `Future-Interactions/Infest-and-Purge.md` design was followed in its broad strokes but diverged in several places, all approved during implementation:

- **No new `Parasites` MongoDB collection.** Parasite identifiers already live in the `Identifiers` collection tagged `categories: ["parasite"]` (`paraslime`, `bimboslime`, `lustleeches`, `love`, `tentacles`, `nymites`). The spec's table of `bloodworm` / `mindfluke` / `nestlings` was illustrative and never seeded. Cost lives in a static `InfestProcessor.CostMap` rather than on the Identifier — keeps flavor and cost code colocated and avoids a schema change.
- **Custom parasites (`!defineparasite`) removed from scope.** The spec described a multi-step conversational flow for player-defined parasites; that feature was dropped (and with it the `Conversational-Flows` infrastructure dependency). New parasites are added by editing `IdentifiersSnapshot.json` and (optionally) `InfestProcessor.CostMap` + `ParasiteFlavorContributor.FlavorMap`.
- **Spread runs through a new `IPostInteractionEffect` hook, not the status-effect contributor.** Spread is inherently cross-party — the existing single-profile `Contribute` signature couldn't reach the partner. Any future "contagion" mechanic should use this hook rather than extending the contributor signature.
- **Per-parasite flavor is a separate `IStatusEffectContributor` (`ParasiteFlavorContributor`)** and fires on **all** investment levels including casual, at 25% per parasite per interaction (user-confirmed during wording pass — the spec implied flavor and spread shared the same gating).
- **`!purge` output is a private DM**, matching the `!detox` precedent. The spec was ambiguous; channel-quiet is the chosen convention for self-administered consequences.

## Files (as-shipped)

**New:**
- [`FChatDicebot/Model/ParasiteInstance.cs`](../../FChatDicebot/Model/ParasiteInstance.cs)
- [`FChatDicebot/Model/ParasiteText.cs`](../../FChatDicebot/Model/ParasiteText.cs)
- [`FChatDicebot/InteractionProcessors/Consequence/InfestProcessor.cs`](../../FChatDicebot/InteractionProcessors/Consequence/InfestProcessor.cs)
- [`FChatDicebot/InteractionProcessors/StatusEffectContributors/ParasiteFlavorContributor.cs`](../../FChatDicebot/InteractionProcessors/StatusEffectContributors/ParasiteFlavorContributor.cs)
- [`FChatDicebot/InteractionProcessors/StatusEffectContributors/ParasiteSpreadEffect.cs`](../../FChatDicebot/InteractionProcessors/StatusEffectContributors/ParasiteSpreadEffect.cs)
- [`FChatDicebot/InteractionProcessors/IPostInteractionEffect.cs`](../../FChatDicebot/InteractionProcessors/IPostInteractionEffect.cs)
- [`FChatDicebot/InteractionProcessors/PostInteractionEffectRegistry.cs`](../../FChatDicebot/InteractionProcessors/PostInteractionEffectRegistry.cs)
- [`FChatDicebot/BotCommands/ChateauInfest.cs`](../../FChatDicebot/BotCommands/ChateauInfest.cs)
- [`FChatDicebot/BotCommands/ChateauPurge.cs`](../../FChatDicebot/BotCommands/ChateauPurge.cs)
- [`FChatDicebot.Tests/Unit/Infestprocessortests.cs`](../../FChatDicebot.Tests/Unit/Infestprocessortests.cs)
- [`FChatDicebot.Tests/Unit/Parasitespreadeffecttests.cs`](../../FChatDicebot.Tests/Unit/Parasitespreadeffecttests.cs)
- [`FChatDicebot.Tests/Unit/Parasiteflavorcontributortests.cs`](../../FChatDicebot.Tests/Unit/Parasiteflavorcontributortests.cs)
- [`FChatDicebot.Tests/Unit/Chateaupurgetests.cs`](../../FChatDicebot.Tests/Unit/Chateaupurgetests.cs)
- [`FChatDicebot.Tests/Unit/Parasitetexttests.cs`](../../FChatDicebot.Tests/Unit/Parasitetexttests.cs)

**Modify:**
- [`FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs`](../../FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs) — registers `InfestProcessor`.
- [`FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs`](../../FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs) — registers `ParasiteFlavorContributor`.
- [`FChatDicebot/InteractionProcessors/InteractionProcessorBase.cs`](../../FChatDicebot/InteractionProcessors/InteractionProcessorBase.cs) — invokes `PostInteractionEffectRegistry` after status-effect aggregation in `GetCompletionMessageWithStatusEffects`.
- `IdentifiersSnapshot.json` — adds the parasite identifiers.
