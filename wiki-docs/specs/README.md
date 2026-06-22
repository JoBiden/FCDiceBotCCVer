# Chateau Specs

Design + as-implemented documentation for the Chateau Contract interaction system.

- **Shipped features** are documented in this folder. Each spec carries a `**Status:** Implemented.` line near the top and a "Files" section listing the implementation artifacts. Where the implementation diverged from the original design, the spec describes the as-shipped behavior with a "Differences from the original spec" section.
- **Upcoming features** live one level down in [`Future-Interactions/`](Future-Interactions/) and follow the pre-implementation template (Assumptions, dependencies, file scaffolding lists). Specs are moved out of `Future-Interactions/` and updated to as-implemented form when they ship.

## Implemented features

### Infrastructure

| Spec | Adds | Used by |
|------|------|---------|
| [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md) | `IStatusEffectContributor` + registry + base-class helpers | Corrupt/Purify, Odorize/Wash, future status interactions |
| [Currency-and-Milk-Inventory](Infrastructure/Currency-and-Milk-Inventory.md) | `MilkBottle` model, `Profile.milkInventory`, `ChateauCurrency` constants, `!sell` command, bottle side-currency | Milk |

### Reporting

| Spec | Adds |
|------|------|
| [Statistics-and-Dossier](Statistics-and-Dossier.md) | `!statistics` + 6 drill-down commands (`!populations`, `!flora`, `!birthrates`, `!parasites`, `!payroll`, `!economics`), expanded `!dossier` blocks, `JobToPlural` helper, purge-interaction logging |

### Involved

| Spec | Reversal | Depends on |
|------|----------|------------|
| [Milk](Milk.md) | none (sold via `!sell`) | Currency-and-Milk-Inventory, Status-Effect-Hook |

### Commitment

| Spec | Reversal | Depends on |
|------|----------|------------|
| [Breed-and-Birth](Breed-and-Birth.md) | `!birth` (paired, time-gated) | NPC-System (optional, currently unshipped) |
| [Corrupt-and-Purify](Corrupt-and-Purify.md) | `!purify` (single shared processor) | Status-Effect-Hook |
| [Train](Train.md) | none | — |

### Consequence

| Spec | Reversal | Depends on |
|------|----------|------------|
| [Odorize-and-Wash](Odorize-and-Wash.md) | `!wash` (one layer/day) | Status-Effect-Hook |
| [Break-and-Rest](Break-and-Rest.md) | `!rest` (skip a day's !work to heal one day faster) | Status-Effect-Hook |
| [Dose-and-Detox](Dose-and-Detox.md) | `!detox` (at a `PurgeCostType` cost) | Status-Effect-Hook (extended), `!climaxfor` (auto-dose hook) |
| [Infest-and-Purge](Infest-and-Purge.md) | `!purge` (free during 24h spread-grace, otherwise at a `PurgeCostType` cost) | Status-Effect-Hook, Dose-and-Detox (`PurgeCostType` infra). Adds new `IPostInteractionEffect` hook. |
| [Curse-and-Cleanse](Curse-and-Cleanse.md) | `!cleanse` (at a per-curse `PurgeCostType` cost) | Status-Effect-Hook, Dose-and-Detox (`PurgeCostType` infra) |

### Social

| Spec | Adds |
|------|------|
| [Feedback](Feedback.md) | `!feedback` (+ `!suggestion` alias) to submit an idea or bug report into a new `Feedback` collection, plus admin-only `!feedbacklist` to read recent submissions. |

## Upcoming features

See [`Future-Interactions/README.md`](Future-Interactions/README.md) for the design specs of features that haven't shipped yet, organized by tier and dependency.

## Conventions

- One `IInteractionProcessor` subclass per interaction, registered in `InteractionProcessorRegistry`, paired with a `ChatBotCommand` in `BotCommands/`.
- Profile state: simple values in `characteristics["key"]`, lists in `lists["key"]`, cooldowns in `timers["key"]` (a `CoolDown` with `timerEnd`).
- Identifier-typed lookups via `MonDB.getIdentifier(string)`; categories already populated in MongoDB include monsters, scents, vices/substances, training types.
- Investment-level cooldowns (defaults): Casual 30 min, Involved 30 min, Commitment 24 hours, Consequence 7 days.
- User-facing wording avoids "UTC day" — say "Chateau day" or just "day".
