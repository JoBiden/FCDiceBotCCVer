# `!seteicon` — Custom Interaction & Bodypart Eicons

Let residents pin one of their own F-List eicons to an interaction, the way Chateau Contract itself has little "easter-egg" icons on some interactions. Once set, the eicon is appended to that interaction's completion message. Global replacement for the old single-purpose `!setmark`.

The same command also pins an eicon to one of the resident's **bodyparts**, which renders on any interaction that involves that part (see [Bodypart eicons](#bodypart-eicons)). Separately, each `Identifier` carries an optional bot-wide `eicon` surfaced in `!whatis` (see [Identifier eicons](#identifier-eicons-whatis)).

**Status:** Implemented.
**Investment level:** N/A — cross-cutting cosmetic feature over every consent-based interaction (plus solo `!birth`).
**Reversal:** N/A — clear an eicon by running `!seteicon {token}` with no eicon; clear an identifier eicon with `!setidentifiereicon {identifier}`.
**Depends on:** the completion pipeline in `InteractionProcessorBase.GetCompletionMessageWithStatusEffects` and `GroupInteractionResolver` (Group-Interactions).

## Command syntax

```
!seteicon {interaction} [eicon]YourEicon[/eicon]   → set
!seteicon {bodypart} [eicon]YourEicon[/eicon]      → set (see Bodypart eicons)
!seteicon {token}                                  → clear
!seteicon                                          → DM a list of everything you've set
```

`{interaction}` accepts every interaction command name across the Casual, Involved, Commitment, and Consequence categories, plus their aliases (`hug`, `dress`, `hire`). `{bodypart}` accepts any identifier in the `bodypart` category. Anything else — system, recovery, or dicebot commands — returns a "not an interaction or bodypart you can pin an eicon to" reply.

**Token resolution order** in `ChateauSeteicon.Run`: interaction names and aliases first, then `MonDB.getIdentifier(token)` with a `bodypart` category check. There's no overlap between the two sets today; the precedence rule is the guard if one ever appears.

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
- **Redirected subject:** a directional interaction can still point the single eicon at the *other* party by overriding `GetEiconSubject`. `!climax`/`!climaxfor` point it at whoever actually climaxed, and **`!pet`** points it at the recipient — residents keep a "my character being petted" eicon, so the natural icon is the pet's, not the petter's.

Self-interactions (e.g. solo `!climax`) collapse to a single eicon.

## Group behavior

The group path ([`GroupInteractionResolver`](../../FChatDicebot/InteractionProcessors/GroupInteractionResolver.cs)) appends `GetGroupEiconSuffix` after the combined completion message:

- Symmetric group casuals (`cuddle`/`kiss`/`handhold`): every participant who set an eicon shows it.
- Directional group casuals (`spank`/`bully`/`lick`/`boobhat`): only the initiator's.
- Recipient-subject group casual (`pet`): overrides `GetGroupEiconSuffix` to show each petted recipient's eicon (in consent order), mirroring its 1:1 `GetEiconSubject` redirect — the initiator's is not shown.
- **No de-duplication:** if several participants picked the same eicon it shows once per participant (e.g. three lipstick marks on a group kiss).

### Lap-stack per-position rule (totem pole)

`LapsitProcessor` overrides `GetGroupEiconSuffix` and renders the stack as a vertical **totem pole** — one figure per line, topmost sitter first — so the icons read like the physical stack (top of the pile on the top line). The bottom of the stack (position 0, a pure lap) shows their `lap` eicon; everyone stacked above — middle riders and the top — shows their `sit` eicon (a mid-stack rider is doing both, and we prefer the sit icon there).

Unlike every other group interaction, the lap stack **fills empty slots**: a participant who hasn't set the relevant eicon falls back to their character icon (`[icon]{userName}[/icon]`) so the pole always carries one figure per person and the stack stays visually intact. (This slot-filling is lap-stack-only; other group interactions still omit unset participants.)

## `!mark` / `!setmark` refactor

`!setmark` still works but is now a thin wrapper over the shared storage (`InteractionEiconSupport.SetInteractionEicon(profile, "mark", …)`), and:

- It is removed from the public `!help` command list (kept reachable via `!help setmark`, whose text marks it legacy and points to `!seteicon mark`).
- Its success message appends a one-line nudge toward `!seteicon`.
- `!seteicon mark` is the canonical way to set a mark eicon and writes the same slot.

The F-List profile documentation (maintained externally) should likewise treat `!setmark` as legacy.

## Queen Contract hardcoded-eicon migration

The single-party Queen Contract easter-egg icons were removed from the processors so they can be re-set through `!seteicon` without double-printing:

- **Removed** (now set via `!seteicon`): cuddle `qchug`, kiss `qckiss`, lick `qctongue` (each in both the 1:1 and group paths).
- **Kept** (can't be expressed as a single resident's eicon): the Queen Contract + Corrupted Rin combined `rin_lap` in both `CuddleProcessor` and `LapsitProcessor`.
- **Removed** (now set via `!seteicon ass`): spank `qcass`. It is recipient-side (fires when Queen Contract is *spanked*), which the initiator-keyed interaction eicon couldn't express — the bodypart eicons below close that gap, so both hardcodes (1:1 and group) came out. Queen Contract's profile needs `characteristics["eicon_part_ass"] = "[eicon]qcass[/eicon]"` for the easter egg to keep showing — set it in-chat with `!seteicon ass [eicon]qcass[/eicon]`, or run [`scripts/seed-queen-contract-ass-eicon.js`](../../scripts/seed-queen-contract-ass-eicon.js).
- **Left as-is** — `MarkProcessor` still writes `characteristics["queenMark"]` on the recipient; it's an orphaned value (never displayed) and not a double-print risk, so it was out of scope for this change.

## Bodypart eicons

`!seteicon {bodypart}` pins an eicon to one of the resident's own body parts; it renders whenever an interaction involves that part, on either side of the act.

**Storage.** `Profile.characteristics`, keyed `"eicon_part_" + bodypart` (`InteractionEiconSupport.BodypartKeyPrefix`), value = the full `[eicon]…[/eicon]` bbcode. Cannot collide with the `"eicon_" + verb` interaction keys because no verb starts with `part_`. `GetBodypartEicon` / `SetBodypartEicon` / `ClearBodypartEicon` / `GetAllBodypartEicons` mirror the interaction trio (null-safe, no persistence — the caller saves). Whether a token *is* a bodypart is a database question, so that check lives in `ChateauSeteicon`, keeping `InteractionEiconSupport` DB-free.

One slot per part: the same ass eicon shows whether you're spanked, marked, or hosed down. There is no per-interaction-per-part matrix.

**Which interactions render one, and whose.** A processor opts in by overriding `BodypartEiconRule` (default `null` = no bodypart eicon). The rule pairs a *source* (`FromIdentifier` — the part the resident typed; `Fixed` — a part the interaction always involves) with an *owner*.

| Interaction | Source | Part | Owner | Rationale |
|---|---|---|---|---|
| `!mark` | FromIdentifier | typed | Recipient | the mark lands on *their* part |
| `!consume` | FromIdentifier | typed | Initiator | the devourer consumes *with* their mouth/tail/… |
| `!golden` | FromIdentifier | typed | Recipient | poured over *their* part |
| `!break` | FromIdentifier | typed | Recipient | *their* part gets broken |
| `!spank` | Fixed | ass | Recipient | the spanked party's rear |
| `!feed` | Fixed | mouth | Recipient | fed into *their* mouth |
| `!lick` | Fixed | tongue | Initiator | the licker's tongue |
| `!boobhat` | Fixed | breast | Initiator | the hat-provider's chest |
| `!milk` | Fixed | breast | Recipient | the milked party |
| `!handhold` | Fixed | hand | Both | co-equal, matching its symmetric interaction-eicon rule |

Everything else declares no rule. `!kiss` → mouth-for-both was considered and **deferred**: kiss already carries symmetric interaction eicons, and two more mouths on every kiss risks icon soup. Giving `KissProcessor` a rule is all it would take.

**Rendering.** Bodypart eicons join the existing trailing flourish *after* the interaction eicons:

```
{completion message}{status fragments}{interaction eicon(s)}{bodypart eicon(s)}
```

- Both suffix builders (`BuildOneToOneEiconSuffix`, `GetGroupEiconSuffix`) take the identifier and share `AddBodypartEicons`.
- **Mark's self-rendered case:** `IsSelfRendered` now suppresses only the *interaction* eicon; the bodypart eicon still appends, so a marked ass reads `… Wear it with pride~ [mark eicon] [their ass eicon]`.
- **Group path:** Owner = Initiator → one icon; Recipient / Both → each consenting recipient's, in consent order, with the same **no de-duplication** rule as interaction eicons (three spanked residents with the same booty icon = three icons). `LapsitProcessor`'s totem pole declares no rule and is unchanged.
- **Graceful miss:** a part with no stored eicon appends nothing. That covers residents who never set one, and `break`-category identifiers that aren't bodyparts (`mind`).
- **No global fallback:** the identifier's bot-wide `eicon` (below) is deliberately *not* used as a fallback in completions — every `!spank` in the channel carrying the same stock ass icon would be noise, not expression.

**Bare-list output.** `!seteicon` alone DMs two sections; the bodypart one only appears when at least one `eicon_part_*` key exists (read by prefix, no DB scan). Parts display through `Utils.BodypartToText`, so `lowerback` reads "lower back".

## Identifier eicons (`!whatis`)

`Identifier` carries an optional `eicon` field — the Chateau's own bot-wide decorative icon for that identifier, distinct from any resident's personal one. `[BsonIgnoreIfNull]` keeps existing documents untouched, so there is no migration. `IdentifiersSnapshot.json` picks the values up whenever it is next re-exported.

`!whatis` / `!identifier` appends it to the title line, and — when the identifier is a bodypart and the asking resident has pinned one of their own — a final `[i]Your personal eicon: …[/i]` line. That reply is already a DM, so it's private and low-noise.

`!setidentifiereicon {identifier} [eicon]…[/eicon]` (admin-only, replies by DM) sets or clears it, backed by a narrow `SetIdentifierEicon(type, eicon)` on `IChateauDatabase` — clearing `$unset`s the field rather than storing an empty string. Identifiers are otherwise curated directly in Mongo; this exists so a purely cosmetic field doesn't need database surgery.

Deferred readout candidates, none shipped: `!monsterize` / `!breed` completions and the dossier monster line; `!dressup` attire and `!feed` substance eicons; bodypart eicons on the dossier's marks section. `!category` listings are probably never — 129 monster icons in one DM is spam.

## `!birth` special-case

`!birth` is a solo resolution with no consent pipeline, so it does not flow through the shared completion hook. The carrier's `birth` eicon is appended directly in `ChateauBirth.BuildCompletionMessage`.

## Files

New:

- [`FChatDicebot/InteractionProcessors/InteractionEiconSupport.cs`](../../FChatDicebot/InteractionProcessors/InteractionEiconSupport.cs) — storage, token→verb-key mapping, self-rendered check.
- [`FChatDicebot/BotCommands/ChateauSeteicon.cs`](../../FChatDicebot/BotCommands/ChateauSeteicon.cs) — the `!seteicon` command (set / clear / list, interactions + bodyparts).
- [`FChatDicebot/InteractionProcessors/BodypartEiconRule.cs`](../../FChatDicebot/InteractionProcessors/BodypartEiconRule.cs) — the per-processor bodypart rule (source + owner).
- [`FChatDicebot/BotCommands/ChateauSetidentifiereicon.cs`](../../FChatDicebot/BotCommands/ChateauSetidentifiereicon.cs) — admin setter for `Identifier.eicon`.
- [`FChatDicebot.Tests/Unit/Interactioneicontests.cs`](../../FChatDicebot.Tests/Unit/Interactioneicontests.cs) — 22 tests.
- [`FChatDicebot.Tests/Unit/Bodyparteicontests.cs`](../../FChatDicebot.Tests/Unit/Bodyparteicontests.cs) — 41 tests covering bodypart eicons and identifier eicons.

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

Bodypart / identifier eicons additionally modified:

- `InteractionEiconSupport.cs` — bodypart storage quartet + `BodypartKeyPrefix`.
- `InteractionProcessorBase.cs` — virtual `BodypartEiconRule`, identifier threaded into both suffix builders, shared `AddBodypartEicons`.
- `IInteractionProcessor.cs` / `GroupInteractionResolver.cs` / `PetProcessor.cs` / `LapsitProcessor.cs` — `GetGroupEiconSuffix` identifier param.
- `MarkProcessor`, `ConsumeProcessor`, `GoldenProcessor`, `BreakProcessor`, `SpankProcessor` (+ both `qcass` hardcodes removed), `FeedProcessor`, `LickProcessor`, `BoobhatProcessor`, `MilkProcessor`, `HandholdProcessor` — declare rules.
- `Model/ChateauDB.cs` — `Identifier.eicon`; `IChateauDatabase` + `ChateauDatabase` + `MonDB` — `SetIdentifierEicon`.
- `ChateauIdentifier.cs` — eicon on the title line, personal-eicon line, message composition extracted for testing.

## Tests

`Interactioneicontests.cs` covers: storage round-trip; mark's legacy slot; clear; token/alias/`pay` resolution and rejection of unsupported tokens; `IsSelfRendered`; directional (initiator-only) vs symmetric (both) suffix; no de-duplication; mark excluded from the suffix; the lap-stack per-position totem rule for both `!lap` and `!sit` (top → bottom, newline-separated) and its unset-slot character-icon fallback; the 1:1 completion actually appending both symmetric eicons; and the birth special-case.

`Bodyparteicontests.cs` covers: bodypart storage round-trip / case-insensitivity / clear / null-safety; the key prefix never colliding with any canonical interaction key; bodypart token resolution (found, non-bodypart identifier, unknown, interaction-wins precedence); every row of the whose-part table on the 1:1 path; the graceful misses (`!break mind`, unset part); mark keeping its interaction eicon suppressed while the part eicon appends; ordering (bodypart after interaction); the group path (per-consenter, no de-dup; initiator-owned once; lapsit unchanged); the `qcass` migration in both directions; both `!seteicon` list sections; the `!whatis` title-line and personal-eicon lines; and `SetIdentifierEicon` set / clear / unknown plus the admin gate. It also pins the set-confirmation wording, including `!pet`'s reversed phrasing.

## Decisions

- **No de-dup** in groups — deliberate (see Group behavior).
- **Bond counts as symmetric** — both partners become each other's bond, so both eicons show.
- **`!setmark` kept but undocumented** rather than removed, to preserve muscle memory and existing wiki/profile references.
- **Shared-processor pairs are context-aware** — keyed on the resolved verb, so each direction can carry its own icon.
- **One shared bodypart slot per part** — no per-interaction-per-part matrix.
- **Bodypart eicons render even for self-rendered verbs** (mark) — the mark eicon and the part eicon are different decorations.
- **No global-identifier-eicon fallback** in completions — personal eicons only.
- **Group bodypart eicons are uncapped** — every consenter's part icon shows. A cap can be added later if icon soup becomes a problem in big groups.
