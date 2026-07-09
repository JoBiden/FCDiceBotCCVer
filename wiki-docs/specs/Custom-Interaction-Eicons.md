# `!seteicon` — Custom Interaction Eicons

Let residents pin one of their own F-List eicons to an interaction, the way Chateau Contract itself has little "easter-egg" icons on some interactions. Once set, the eicon is appended to that interaction's completion message. Global replacement for the old single-purpose `!setmark`.

**Status:** Implemented.
**Investment level:** N/A — cross-cutting cosmetic feature over every consent-based interaction (plus solo `!birth`).
**Reversal:** N/A — clear an eicon by running `!seteicon {interaction}` with no eicon.
**Depends on:** the completion pipeline in `InteractionProcessorBase.GetCompletionMessageWithStatusEffects` and `GroupInteractionResolver` (Group-Interactions).

## Command syntax

```
!seteicon {interaction} [eicon]YourEicon[/eicon]   → set
!seteicon {interaction}                            → clear
!seteicon                                          → DM a list of everything you've set
```

`{interaction}` accepts every interaction command name across the Casual, Involved, Commitment, and Consequence categories, plus their aliases (`hug`, `dress`, `hire`). It does **not** accept system, recovery, or dicebot commands — those return a "not an interaction you can pin an eicon to" reply.

## Storage model

Eicons live on `Profile.characteristics`, keyed by the interaction **verb actually stamped on `Interaction.type`** at completion time (`"eicon_" + verb`). Keying on the resolved verb rather than the processor's canonical `InteractionType` is what lets shared-processor pairs carry distinct icons:

| Pair (one processor) | Distinct eicon keys |
|----------------------|---------------------|
| `!corrupt` / `!purify` | `corrupt` vs `purify` (follows the *effective* direction, so `!corrupt -3` shows the purify eicon) |
| `!climax` / `!climaxfor` | `climax` vs `climaxfor` |
| `!lap` / `!sit` | `lap` vs `sit` |

Aliases fold onto their canonical verb (`hug`→`cuddle`, `dress`→`dressup`, `hire`→`employ`). `!pay` writes **both** payment directions (`paymentGive` + `paymentReceive`) so it shows whether the resident is paying or billing.

`mark` is special: it keeps its historical `characteristics["mark"]` slot (read by the dossier and `MarkProcessor`'s own reveal), so it is **excluded from the generic suffix** (`InteractionEiconSupport.IsSelfRendered`) to avoid a double icon.

All storage/mapping lives in one place: [`InteractionEiconSupport`](../../FChatDicebot/InteractionProcessors/InteractionEiconSupport.cs).

## Whose eicon shows (directionality)

Driven by `InteractionProcessorBase.EiconAppliesToBothParties`:

- **Directional (default):** only the initiator's eicon shows (e.g. `!spank`, `!mark`, `!feed`, `!corrupt`).
- **Symmetric:** both parties' eicons show. Overridden to `true` on the mutual, co-equal interactions: **`cuddle`, `kiss`, `handhold`, `bond`**.

Self-interactions (e.g. solo `!climax`) collapse to a single eicon.

## Group behavior

The group path ([`GroupInteractionResolver`](../../FChatDicebot/InteractionProcessors/GroupInteractionResolver.cs)) appends `GetGroupEiconSuffix` after the combined completion message:

- Symmetric group casuals (`cuddle`/`kiss`/`handhold`): every participant who set an eicon shows it.
- Directional group casuals (`spank`/`bully`/`lick`/`boobhat`): only the initiator's.
- **No de-duplication:** if several participants picked the same eicon it shows once per participant (e.g. three lipstick marks on a group kiss).

### Lap-stack per-position rule

`LapsitProcessor` overrides `GetGroupEiconSuffix`: the bottom of the stack (position 0, a pure lap) shows their `lap` eicon; everyone stacked above — middle riders and the top — shows their `sit` eicon (a mid-stack rider is doing both, and we prefer the sit icon there). Rendered bottom → top.

## `!mark` / `!setmark` refactor

`!setmark` still works but is now a thin wrapper over the shared storage (`InteractionEiconSupport.SetInteractionEicon(profile, "mark", …)`), and:

- It is removed from the public `!help` command list (kept reachable via `!help setmark`, whose text marks it legacy and points to `!seteicon mark`).
- Its success message appends a one-line nudge toward `!seteicon`.
- `!seteicon mark` is the canonical way to set a mark eicon and writes the same slot.

The F-List profile documentation (maintained externally) should likewise treat `!setmark` as legacy.

## `!birth` special-case

`!birth` is a solo resolution with no consent pipeline, so it does not flow through the shared completion hook. The carrier's `birth` eicon is appended directly in `ChateauBirth.BuildCompletionMessage`.

## Files

New:

- [`FChatDicebot/InteractionProcessors/InteractionEiconSupport.cs`](../../FChatDicebot/InteractionProcessors/InteractionEiconSupport.cs) — storage, token→verb-key mapping, self-rendered check.
- [`FChatDicebot/BotCommands/ChateauSeteicon.cs`](../../FChatDicebot/BotCommands/ChateauSeteicon.cs) — the `!seteicon` command (set / clear / list).
- [`FChatDicebot.Tests/Unit/Interactioneicontests.cs`](../../FChatDicebot.Tests/Unit/Interactioneicontests.cs) — 21 tests.

Modified:

- `InteractionProcessorBase.cs` — `EiconAppliesToBothParties`, 1:1 suffix appended in `GetCompletionMessageWithStatusEffects` (new optional `interactionVerb` param), virtual `GetGroupEiconSuffix`, shared helpers.
- `IInteractionProcessor.cs` — `interactionVerb` param + `GetGroupEiconSuffix` on the interface.
- `KissProcessor.cs`, `CuddleProcessor.cs`, `HandholdProcessor.cs`, `BondProcessor.cs` — `EiconAppliesToBothParties => true`.
- `LapsitProcessor.cs` — per-position `GetGroupEiconSuffix` override.
- `ChateauConsent.cs` — threads `pendingInteraction.type` (the verb) into the 1:1 completion.
- `GroupInteractionResolver.cs` — appends the group eicon suffix.
- `Involved/ClimaxforProcessor.cs` — threads the verb through the self-climax completion.
- `ChateauSetmark.cs` — delegates to shared storage, legacy help text + deprecation nudge, lowercased "eicon".
- `ChateauBirth.cs` — appends the carrier's birth eicon.
- `ChateauHelp.cs` — swaps `!setmark` for `!seteicon` in the public command list.

## Tests

`Interactioneicontests.cs` covers: storage round-trip; mark's legacy slot; clear; token/alias/`pay` resolution and rejection of unsupported tokens; `IsSelfRendered`; directional (initiator-only) vs symmetric (both) suffix; no de-duplication; mark excluded from the suffix; the lap-stack per-position rule for both `!lap` and `!sit`; the 1:1 completion actually appending both symmetric eicons; and the birth special-case.

## Decisions

- **No de-dup** in groups — deliberate (see Group behavior).
- **Bond counts as symmetric** — both partners become each other's bond, so both eicons show.
- **`!setmark` kept but undocumented** rather than removed, to preserve muscle memory and existing wiki/profile references.
- **Shared-processor pairs are context-aware** — keyed on the resolved verb, so each direction can carry its own icon.
