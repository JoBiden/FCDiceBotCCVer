# 6a6efae4 — Drink straight from the source, skipping the bottle (`!drinkfrom` / `!forcedrink`)

| | |
|---|---|
| **Feedback id** | `6a6efae4f86b7e092f61a452` |
| **Submitted** | `2026-08-02T08:08:04Z` (today) by **Queen Contract** (`Queen Contract`) |
| **Source** | private message |
| **Tagged** | `idea` |
| **Verdict** | `feature` |
| **Recommendation** | `spec` |
| **Confidence** | high on what exists; the design is open |
| **Effort** | medium — two new interactions, but every effect they need already exists |

## What they said

> expand the drink and feed system to allow residents to consume directly from the source, gaining any corruption effects and fueling any addictions but skipping the !milk step. Consider the reverse interaction as well. Something like !drinkfrom and !forcedrink

## What it means

Today, consuming another resident's fluid is a two-step loop: `!milk` them into a numbered
bottle, then `!drink` that bottle. The request is a one-step path — drink from the person
directly, keeping the mechanical payload (corruption shift, craving satisfaction, addiction
deepening) but producing no bottle. `!forcedrink` is the mirror: the donor initiates, putting
their own fluid into someone else.

## Assessment

**Every mechanical effect this needs already exists and is already factored into reusable
pieces.** The gap is a delivery path, not new mechanics.

`ChateauDrink.Execute`
([ChateauDrink.cs:109](../../../FChatDicebot/BotCommands/ChateauDrink.cs:109)) is already a pure,
DB-injected static that applies exactly the three effects the submission names:

| Effect | Where | Tuning |
|---|---|---|
| Corruption / purity shift from the bottle's tag | `ChateauDrink.ApplyCorruption` ([:205](../../../FChatDicebot/BotCommands/ChateauDrink.cs:205)) | ±1 per bottle, 3/day (`DrinkCorruptionShiftPerBottle`, `DrinkCorruptionDailyLimit`) |
| Craving satisfaction | `SelfCommandStatusEffects.GetCompletionFragments` via `DoseStatusContributor.DrinkType` ([:137](../../../FChatDicebot/BotCommands/ChateauDrink.cs:137)) | — |
| Addiction deepening | `DoseProcessor.IntensifyExistingVices` ([:287](../../../FChatDicebot/BotCommands/ChateauDrink.cs:287)) | 10% roll (`DrinkAddictionChance`) |

The corruption tag those effects key off is derived from the **donor's** corruption at milking
time (`corrupt` at ≤ −10, `purified` at ≥ +10, `ChateauCurrency.cs:166`), so drinking from the
source can compute the same tag live from the donor's current corruption — no bottle needed.

### The reverse interaction largely exists already, minus the effects

`!feed` ([ChateauFeed.cs](../../../FChatDicebot/BotCommands/ChateauFeed.cs)) is already a
consent-gated Involved interaction — `!feed [user] {substance}`, 30-minute rate limit on both
parties, mouth eicon on the recipient. But `FeedProcessor.ProcessInteraction`
([FeedProcessor.cs:68](../../../FChatDicebot/InteractionProcessors/Involved/FeedProcessor.cs:68))
only records the interaction and increments `feedgive`/`feedtake`. **It applies no corruption, no
craving satisfaction, and no addiction roll** — it is flavor and bookkeeping. Its completion
message is "Was it yummy? I bet it was."

So `!forcedrink` as described is close to "`!feed`, but the substance comes from the initiator's
own body and it actually does something." Whether that is a new command or a promotion of `!feed`
is the central design fork, and it is the owner's call — `!feed` today accepts *any* substance
identifier (food, energy), which a source-based interaction cannot.

### What is genuinely new

- **Consent shape.** `!drink` is self-targeted and needs no consent
  ([ChateauDrink.cs:15](../../../FChatDicebot/BotCommands/ChateauDrink.cs:15)). `!drinkfrom`
  targets another resident, so it needs the two-phase `PendingCommand` flow like every other
  interaction.
- **No keepsake.** The bottle system's whole premise is that the numbered empty is permanent
  proof (`Bottle-Consumption-And-Transfer.md` §31, §35). A source drink leaves no serial, so it
  is deliberately the *ephemeral* option — worth stating in the spec so it does not read as an
  oversight.
- **Cooldown relationship with `!milk`.** `!milk` uses per-direction locks
  (`*_give_<recipient>` keys, shipped 2026-06-28). Whether drinking from someone consumes the
  same lock decides whether `!drinkfrom` is a shortcut around milk's rate limit or a separate
  budget — this is the balance question.
- **The daily corruption budget.** `DrinkCorruptionDailyLimit` is 3/day and keyed
  `drinkshift_<date>` on the drinker's profile. Source drinks should almost certainly draw from
  that same budget, or the bottle path becomes the strictly worse way to get corrupted.

- **Reproduces / confirmed:** n/a (feature)
- **Related code:** `ChateauDrink.cs:109` (reusable `Execute`), `FeedProcessor.cs:68` (the
  effect-free reverse), `MilkProcessor.cs`, `DoseProcessor.IntensifyExistingVices`,
  `ChateauCurrency.cs:54-82` (tuning)
- **Related specs / docs:** `wiki-docs/specs/Bottle-Consumption-And-Transfer.md`,
  `wiki-docs/specs/Milk.md`, `wiki-docs/specs/Dose-and-Detox.md`
- **Duplicate of:** none — not in `wiki-docs/Feature-Requests.md` and no `Future-*` spec covers it
- **Already fixed since submission:** no

## Recommended action

Spec before code — this is a new interaction pair with real balance questions (the milk cooldown
relationship above all), which is exactly what the specs folder is for. The implementation itself
should be unusually cheap for its size, because `ChateauDrink`'s effect helpers are already
static, DB-injected, and bottle-agnostic apart from taking a `MilkBottle`; the spec's main job is
deciding the rules, not inventing mechanics.

Recommend `wiki-docs/specs/Future-Interactions/Drink-From-Source.md` covering both directions
together — they share every effect and differ only in who initiates and who consents, so
speccing them apart would duplicate the whole balance section.

## Decisions needed from the owner

1. **Is `!forcedrink` a new command, or does `!feed` grow into it?**
   - **New command, `!feed` untouched** (recommended) — `!feed` keeps being generic-substance
     flavor for food and energy; `!forcedrink` is specifically from-the-body and carries effects.
     No existing behavior changes, no migration of `feedgive`/`feedtake` counts.
   - **Give `!feed` the effects when the substance is a bodily one** — no new command, but `!feed`
     silently becomes mechanically loaded, which changes what residents already consented to and
     retro-reads its existing history.
   - **Both: `!forcedrink` as an alias-with-effects onto a shared processor** — one processor,
     two entry points; more design work to keep the consent text honest for each.

2. **Does `!drinkfrom` consume the `!milk` cooldown for that pair/direction?**
   - **Yes, shares milk's per-direction lock** (recommended) — a resident can be drawn from at
     one rate regardless of whether it ends up in a bottle; closes the shortcut.
   - **Its own separate cooldown** — the two paths coexist and a donor can be milked *and* drunk
     from in the same window; more available, more to balance.
   - **No cooldown beyond consent** — simplest, but makes bottling pointless for anyone nearby.

3. **What does a source drink leave behind, if anything?**
   - **Nothing — no bottle, no serial** (recommended) — the bottle collection stays the
     "keepsake" path and this stays the immediate one; a clean, legible trade-off.
   - **A numbered empty, marked as drunk-from-source** — the collection records it, but this
     erodes the distinction the bottle spec is built on and needs a new `MilkBottle` field.

4. **Corruption source: the donor's live corruption, or a rolled tag?**
   - **Live donor corruption, same ±10 thresholds** (recommended) — reuses
     `ChateauCurrency.cs:166` exactly, and means a corrupt resident is *known* to be a corrupting
     drink, which is good social information.
   - **Something stronger than a bottle** — fresher-is-more-potent is thematically tempting but
     needs its own numbers and its own budget interaction.

## User-facing text that would change

No existing strings change under the recommended option 1a. Everything below is new and would be
drafted properly in the spec against `wiki-docs/Style-Guide.md` — listed here so the shape is
visible, not for approval yet.

| Where | Now | Proposed |
|---|---|---|
| `!drinkfrom` consent prompt | — | new — must end with the standing "(or !no)" decline reminder via `BuildConsentWarning` |
| `!drinkfrom` completion | — | new — carries the corruption/craving/addiction fragments the way `ChateauDrink.BuildChannelMessage` already assembles them |
| `!forcedrink` consent prompt | — | new |
| `!forcedrink` completion | — | new |
| `!drink` `RelatedCommands` | `bottles, milk, sell, dose, detox` | add the new commands |
| `wiki-docs/Command-Reference.md` | — | two new entries |

## Owner decision

**2026-08-02 — Approved, spec first.** Both directions specced together in
`wiki-docs/specs/Future-Interactions/Drink-From-Source.md`, for sign-off before any code.

Design answers:

1. *`!forcedrink` — new command or `!feed` grown into it?* — **New command, `!feed` untouched.**
   `!feed` stays generic-substance flavor; `!forcedrink` is from-the-body and carries effects.
2. *Does `!drinkfrom` consume the `!milk` cooldown?* — **Yes, shares milk's per-direction lock.**
   A resident can be drawn from at one rate whether or not it ends up in a bottle.
3. *What does a source drink leave behind, and where does its corruption come from?* —
   **Nothing left behind, and fresher is stronger.** Not the recommended option on the second
   half: the dossier proposed reusing `ChateauCurrency.cs:166`'s ±10 thresholds unchanged, and
   flagged that a potency bump needs its own numbers and its own interaction with the daily
   corruption budget. The owner took the bump, so the spec owes a worked answer for both — they
   are now the spec's central balance section, not a footnote.
