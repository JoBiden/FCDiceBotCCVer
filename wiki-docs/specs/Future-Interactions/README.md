# Future Interactions — Spec Index

Self-contained design specs for upcoming Chateau Contract interactions. Each file is written so a fresh implementation session can act on it without needing to ask the user for design clarifications. Where a spec leaves something open, the **Assumptions** section flags defaults that the user can override before implementation begins.

## How to use this folder

- **Implementing a feature:** read the target spec end-to-end, plus the infrastructure specs it lists in **Depends on**. The spec lists every file you need to create/modify, persistence keys, validation rules, and tests.
- **Adding a new feature:** copy any feature spec as a template. Mandatory sections: Investment level, Command syntax, Validation, Processor logic, Persistence shape, Reversal, Status-effect contribution (or "none"), Tests, Assumptions.
- **Background reading first:** [Interaction-System.md](../../Interaction-System.md), [Development-Guide.md](../../Development-Guide.md).

## Implementation order (dependency-respecting)

Infrastructure must land before the features that depend on it. Within each tier, features are independent and can be done in any order.

### Tier 0 — Infrastructure (do these first)

| Spec | Required by |
|------|-------------|
| [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md) | Odorize, Dose, Break, Infest, Corrupt, Curse |
| [Currency-and-Milk-Inventory](Infrastructure/Currency-and-Milk-Inventory.md) | Milk |
| [Conversational-Flows](Infrastructure/Conversational-Flows.md) | Infest (custom-parasite definition only) |
| [NPC-System](Infrastructure/NPC-System.md) *(low priority — Breed degrades gracefully without this)* | Breed (full naming behavior) |

### Tier 1 — Involved interactions

| Spec | Reversal | Depends on |
|------|----------|------------|
| [Climaxfor](Climaxfor.md) | — | Status-Effect-Hook (reads !dose state) |
| [Milk](Milk.md) | — | Currency-and-Milk-Inventory |

### Tier 2 — Commitment interactions

| Spec | Reversal | Depends on |
|------|----------|------------|
| [Breed-and-Birth](Breed-and-Birth.md) *(implemented)* | `!birth` (paired) | NPC-System (optional) |
| [Train](Train.md) | — | — |
| [Corrupt-and-Purify](Corrupt-and-Purify.md) *(implemented)* | `!purify` (single processor) | Status-Effect-Hook |

### Tier 3 — Consequence interactions

| Spec | Reversal | Depends on |
|------|----------|------------|
| [Infest-and-Purge](Infest-and-Purge.md) | `!purge` | Status-Effect-Hook, Conversational-Flows (custom parasites only) |
| [Odorize-and-Wash](Odorize-and-Wash.md) *(implemented)* | `!wash` | Status-Effect-Hook |
| [Curse-and-Cleanse](Curse-and-Cleanse.md) | `!cleanse` | Status-Effect-Hook |
| [Break-and-Rest](Break-and-Rest.md) | `!rest` | Status-Effect-Hook |
| [Dose-and-Detox](Dose-and-Detox.md) | `!detox` | Status-Effect-Hook |

## Conventions all specs assume

- The processor pattern documented in [Development-Guide.md](../../Development-Guide.md): one `IInteractionProcessor` subclass per interaction, registered in `InteractionProcessorRegistry`, paired with a `ChatBotCommand` in `BotCommands/`.
- State on the recipient `Profile`: simple values in `characteristics["key"]`, lists in `lists["key"]`, cooldowns in `timers["key"]` (a `CoolDown` with `timerEnd`).
- Identifier-typed lookups via `MonDB.getIdentifier(string)`; categories already populated in MongoDB include monsters, scents, vices/substances, training types.
- Investment-level cooldowns: Casual 30 min, Involved 30 min, Commitment 24 hours, Consequence 7 days (unless a spec says otherwise).
- Reversal commands always carry **either a negative consequence or a time gate** — never free.
- Status effects affect interactions you !consent to. System commands like !work do not invoke the status-effect hook.
