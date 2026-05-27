# Future Interactions — Spec Index

Self-contained design specs for **upcoming** Chateau Contract interactions. Each file is written so a fresh implementation session can act on it without needing to ask the user for design clarifications. Where a spec leaves something open, the **Assumptions** section flags defaults that the user can override before implementation begins.

Specs are removed from this folder once they ship — the as-implemented docs live one level up in [`specs/`](../). See [`specs/README.md`](../README.md) for the index of implemented features.

## How to use this folder

- **Implementing a feature:** read the target spec end-to-end, plus the infrastructure specs it lists in **Depends on**. The spec lists every file you need to create/modify, persistence keys, validation rules, and tests.
- **Adding a new feature:** copy any feature spec as a template. Mandatory sections: Investment level, Command syntax, Validation, Processor logic, Persistence shape, Reversal, Status-effect contribution (or "none"), Tests, Assumptions.
- **Background reading first:** [Interaction-System.md](../../Interaction-System.md), [Development-Guide.md](../../Development-Guide.md).

## Status

As of 2026-05-27, **every currently-planned interaction has shipped**. The only remaining design docs in this folder are shelved infrastructure that no active feature depends on:

| Spec | Status |
|------|--------|
| [NPC-System](Infrastructure/NPC-System.md) | **Shelved.** Breed-and-Birth shipped without it and degrades gracefully (un-named offspring records). Pick this back up if a future feature needs persistent, named NPCs. |

The shipped Tier 0 infra ([Status-Effect-Hook](../Infrastructure/Status-Effect-Hook.md) and the milk-inventory/bottle-currency portion of [Currency-and-Milk-Inventory](../Infrastructure/Currency-and-Milk-Inventory.md)) lives one level up in `specs/Infrastructure/`. The shared `PurgeCostType` / `PurgeCostApplier` infra now lives inside [Dose-and-Detox](../Dose-and-Detox.md) and is reused by `!detox`, `!purge`, and `!cleanse`.

## Conventions all specs assume

- The processor pattern documented in [Development-Guide.md](../../Development-Guide.md): one `IInteractionProcessor` subclass per interaction, registered in `InteractionProcessorRegistry`, paired with a `ChatBotCommand` in `BotCommands/`.
- State on the recipient `Profile`: simple values in `characteristics["key"]`, lists in `lists["key"]`, cooldowns in `timers["key"]` (a `CoolDown` with `timerEnd`).
- Identifier-typed lookups via `MonDB.getIdentifier(string)`; categories already populated in MongoDB include monsters, scents, vices/substances, training types.
- Investment-level cooldowns: Casual 30 min, Involved 30 min, Commitment 24 hours, Consequence 7 days (unless a spec says otherwise).
- Reversal commands always carry **either a negative consequence or a time gate** — never free.
- Status effects affect interactions you !consent to. System commands like !work do not invoke the status-effect hook.
- User-facing wording avoids "UTC day" — use "Chateau day" or just "day". The day-boundary is internally UTC midnight but players shouldn't have to know that.
