# Future Interactions — Spec Index

Self-contained design specs for **upcoming** Chateau Contract interactions. Each file is written so a fresh implementation session can act on it without needing to ask the user for design clarifications. Where a spec leaves something open, the **Assumptions** section flags defaults that the user can override before implementation begins.

Specs are removed from this folder once they ship — the as-implemented docs live one level up in [`specs/`](../). See [`specs/README.md`](../README.md) for the index of implemented features.

## How to use this folder

- **Implementing a feature:** read the target spec end-to-end, plus the infrastructure specs it lists in **Depends on**. The spec lists every file you need to create/modify, persistence keys, validation rules, and tests.
- **Adding a new feature:** copy any feature spec as a template. Mandatory sections: Investment level, Command syntax, Validation, Processor logic, Persistence shape, Reversal, Status-effect contribution (or "none"), Tests, Assumptions.
- **Background reading first:** [Interaction-System.md](../../Interaction-System.md), [Development-Guide.md](../../Development-Guide.md).

## Implementation order (dependency-respecting)

Infrastructure must land before the features that depend on it. Within each tier, features are independent and can be done in any order.

### Tier 0 — Infrastructure (do these first)

| Spec | Required by |
|------|-------------|
| [Conversational-Flows](Infrastructure/Conversational-Flows.md) | Infest (custom-parasite definition only) |
| [NPC-System](Infrastructure/NPC-System.md) *(low priority — Breed degrades gracefully without this)* | Breed (full naming behavior) |

The shipped Tier 0 infra ([Status-Effect-Hook](../Infrastructure/Status-Effect-Hook.md) and the milk-inventory/bottle-currency portion of [Currency-and-Milk-Inventory](../Infrastructure/Currency-and-Milk-Inventory.md)) lives one level up in `specs/Infrastructure/`.

### Tier 1 — Involved interactions

*(All currently-designed Involved specs have shipped. See [`specs/README.md`](../README.md) for the as-implemented docs.)*

### Tier 2 — Commitment interactions

*(All currently-designed Commitment specs have shipped. See [`specs/README.md`](../README.md) for the as-implemented docs.)*

### Tier 3 — Consequence interactions

| Spec | Reversal | Depends on |
|------|----------|------------|
| [Infest-and-Purge](Infest-and-Purge.md) | `!purge` | Status-Effect-Hook, Conversational-Flows (custom parasites only) |
| [Curse-and-Cleanse](Curse-and-Cleanse.md) | `!cleanse` | Status-Effect-Hook |

*(Dose-and-Detox shipped on 2026-05-26 — see [`../Dose-and-Detox.md`](../Dose-and-Detox.md). The shipped feature also added the shared `PurgeCostType` enum + `PurgeCostApplier` that the unshipped reversals above will reuse, with `RandomCurse` currently falling back to a random-break cost until Curse-and-Cleanse ships.)*

## Conventions all specs assume

- The processor pattern documented in [Development-Guide.md](../../Development-Guide.md): one `IInteractionProcessor` subclass per interaction, registered in `InteractionProcessorRegistry`, paired with a `ChatBotCommand` in `BotCommands/`.
- State on the recipient `Profile`: simple values in `characteristics["key"]`, lists in `lists["key"]`, cooldowns in `timers["key"]` (a `CoolDown` with `timerEnd`).
- Identifier-typed lookups via `MonDB.getIdentifier(string)`; categories already populated in MongoDB include monsters, scents, vices/substances, training types.
- Investment-level cooldowns: Casual 30 min, Involved 30 min, Commitment 24 hours, Consequence 7 days (unless a spec says otherwise).
- Reversal commands always carry **either a negative consequence or a time gate** — never free.
- Status effects affect interactions you !consent to. System commands like !work do not invoke the status-effect hook.
- User-facing wording avoids "UTC day" — use "Chateau day" or just "day". The day-boundary is internally UTC midnight but players shouldn't have to know that.
