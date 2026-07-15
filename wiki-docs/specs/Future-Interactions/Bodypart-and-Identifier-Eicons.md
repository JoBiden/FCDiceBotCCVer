# Bodypart Eicons & Identifier Eicons

Expand the custom-eicon system in two directions:

- **Part A — Personal bodypart eicons.** `!seteicon` grows to accept bodypart identifiers as well as interaction names (`!seteicon ass [eicon]MyBooty[/eicon]`). Interactions that involve a specific bodypart — explicitly (`!mark`, `!consume`, `!golden`, `!break`) or implicitly (`!spank` → ass, `!feed` → mouth, …) — append the part-owner's bodypart eicon to their completion message, alongside the existing interaction eicons.
- **Part B — Identifier eicons.** Every `Identifier` document gains an optional `eicon` field, a bot-wide decorative icon for that identifier. First surfaced in `!whatis` / `!identifier`; other readouts can opt in later, one deliberate choice at a time.

**Status:** Spec — not implemented.
**Investment level:** N/A — cross-cutting cosmetic feature, like the parent Custom-Interaction-Eicons feature.
**Reversal:** N/A — clear a bodypart eicon by running `!seteicon {bodypart}` with no eicon; clear an identifier eicon via the admin command with no eicon.
**Depends on:** [Custom-Interaction-Eicons](../Custom-Interaction-Eicons.md) (shipped) — reuses its storage conventions, suffix pipeline, and `!seteicon` command.

---

## Part A — Personal bodypart eicons

### Command syntax

```
!seteicon {interaction} [eicon]YourEicon[/eicon]   → set (existing)
!seteicon {bodypart} [eicon]YourEicon[/eicon]      → set (new)
!seteicon {token}                                  → clear (existing behavior, now also for bodyparts)
!seteicon                                          → DM a list of everything you've set (both kinds)
```

`{bodypart}` accepts any identifier whose `categories` contains `bodypart` (currently 27: ass, mouth, breast, hand, tongue, tail, ear, armpit, pussy, horn, wing, midriff, foot, arm, leg, joint, neck, back, eye, nose, decolletage, lowerback, face, body, torso, dick, ball — the **live DB is source of truth**, not `IdentifiersSnapshot.json`).

**Token resolution order** in `ChateauSeteicon.Run`:

1. `InteractionEiconSupport.TryResolveTokenToVerbKeys(token)` — interaction names and their aliases win, exactly as today.
2. Otherwise `MonDB.getIdentifier(token)`; if found and `categories` contains `bodypart` (case-insensitive), treat as a bodypart eicon set/clear.
3. Otherwise the existing rejection message, reworded to mention bodyparts:
   > `'{token}' isn't an interaction or bodypart you can pin an eicon to. Try one like !cuddle, !kiss, or ass. See !help seteicon for the full list.`

There is no overlap between interaction tokens and bodypart identifier types today; the precedence rule (interaction wins) is the guard if one ever appears. Bodypart tokens are matched lowercase (identifier `type` values are stored lowercase; `terms` should be lowercased before lookup, mirroring `GetIdentifierFromCommandTerms`).

The existing `MaxEiconLength` (47) guard and the set/clear/list confirmation messages apply unchanged, with bodypart-flavored wording on success, e.g.:

> `Done! From now on, whenever an interaction involves your ass, onlookers will see [eicon]MyBooty[/eicon].`

### Storage model

On `Profile.characteristics`, keyed `"eicon_part_" + bodypart` (lowercase identifier type), value = the full `[eicon]…[/eicon]` bbcode, same as interaction eicons. The `part_` segment cannot collide with the existing `"eicon_" + verb` keys because no interaction verb starts with `part_`.

New helpers on `InteractionEiconSupport` (keep it the single source of truth):

```csharp
public static string GetBodypartEicon(Profile profile, string bodypart);
public static void SetBodypartEicon(Profile profile, string bodypart, string eicon);   // caller saves
public static void ClearBodypartEicon(Profile profile, string bodypart);               // caller saves
```

These mirror the existing interaction-eicon trio (null-safe, no persistence). Bodypart *validation* (is this token a bodypart?) stays in the command/processor layer where DB access lives — `InteractionEiconSupport` remains DB-free and unit-testable.

### Rendering: which interactions show a bodypart eicon, and whose

Each processor declares an optional **bodypart eicon rule** with two pieces: *where the part comes from* and *whose body it is*.

```csharp
public enum BodypartEiconSource { None, FromIdentifier, Fixed }
public enum BodypartEiconOwner { Initiator, Recipient, Both }

public class BodypartEiconRule
{
    public BodypartEiconSource Source;   // None = feature off for this interaction (default)
    public BodypartEiconOwner Owner;
    public string FixedPart;             // only for Source == Fixed
}
```

`InteractionProcessorBase` gains `public virtual BodypartEiconRule BodypartEiconRule => null;` (null = no bodypart eicon, the default for every processor not listed below).

| Interaction | Source | Part | Owner (whose body / whose eicon) | Rationale |
|---|---|---|---|---|
| `!mark` | FromIdentifier | typed bodypart | **Recipient** | the mark lands on *their* part |
| `!consume` | FromIdentifier | typed bodypart | **Initiator** | the devourer consumes *with their* mouth/tail/… |
| `!golden` | FromIdentifier | typed bodypart | **Recipient** | poured over *their* part |
| `!break` | FromIdentifier | typed bodypart | **Recipient** | *their* part gets broken |
| `!spank` | Fixed | `ass` | **Recipient** | the spanked party's rear (generalizes the `qcass` easter egg — see migration below) |
| `!feed` | Fixed | `mouth` | **Recipient** | fed into *their* mouth |
| `!lick` | Fixed | `tongue` | **Initiator** | the licker's tongue |
| `!boobhat` | Fixed | `breast` | **Initiator** | the hat-provider's chest |
| `!milk` | Fixed | `breast` | **Recipient** | the milked party |
| `!handhold` | Fixed | `hand` | **Both** | co-equal, matching its symmetric interaction-eicon rule |

Everything else: `null`. (`!kiss` → mouth-for-both was considered and **deferred**: kiss already carries symmetric interaction eicons and adding two mouth icons on every kiss risks icon soup. Easy to enable later by giving `KissProcessor` a rule.)

Notes on the `FromIdentifier` rows:

- The identifier arrives lowercase from `GetIdentifierFromCommandTerms`, matching the storage key.
- `!break` accepts identifiers in the `break` category, which may include entries that aren't in `bodypart` (e.g. `mind`); the eicon lookup simply misses for those — no error, no icon. Same graceful miss for any identifier with no stored eicon.

### Where it renders (pipeline changes)

Bodypart eicons join the existing trailing-flourish suffix, **after** the interaction eicons:

```
{completion message}{status fragments}{interaction eicon(s)}{bodypart eicon(s)}
```

- `InteractionProcessorBase.BuildOneToOneEiconSuffix` gains the `identifier` parameter (already available at its one call site in `GetCompletionMessageWithStatusEffects`). After the existing interaction-eicon collection, it resolves `BodypartEiconRule`: picks the part (typed identifier or `FixedPart`), then appends the owner profile's `GetBodypartEicon` (both profiles' for `Both`, initiator first, skipping the duplicate on self-interactions — reuse `IsSameProfile`).
- **Mark's self-rendered special case:** `IsSelfRendered` continues to suppress only the *interaction* eicon; the bodypart eicon still appends. So a marked ass reads: `X emblazons their mark upon Y's ass. Wear it with pride~ [mark eicon] [Y's ass eicon]`.
- **Group path:** `GetGroupEiconSuffix` (base + the `IInteractionProcessor` interface + `GroupInteractionResolver` call site) gains the `identifier` parameter. Owner = Initiator → one icon; Owner = Recipient/Both → each consenting recipient's bodypart eicon in consent order, keeping the existing **no de-duplication** rule (three spanked residents with the same booty icon = three icons). `LapsitProcessor`'s totem-pole override updates its signature but declares no rule, so its rendering is unchanged.
- **No global fallback in completions:** if the owner hasn't set a personal eicon for the part, nothing is appended. Falling back to the identifier's bot-wide `eicon` (Part B) was considered and **rejected** — every `!spank` in the channel would carry the same stock ass icon, which is noise, not expression.

### `!seteicon` bare-list output

Two sections; the bodypart section only queries/prints when at least one `eicon_part_*` key exists (scan the profile's `characteristics` keys by prefix — no DB identifier scan needed):

```
Here are the interaction eicons you've set:
[b]!kiss[/b]: [eicon]…[/eicon]

Here are the bodypart eicons you've set:
[b]ass[/b]: [eicon]…[/eicon]
```

Display bodyparts through `Utils.BodypartToText` (e.g. `lowerback` → "lower back").

### Queen Contract `qcass` migration

The recipient-side spank easter egg (`SpankProcessor.GetCompletionMessage` and `GetGroupCompletionMessage`, hardcoded `[eicon]qcass[/eicon]` when Queen Contract is spanked) is exactly a bodypart eicon: **remove both hardcodes** and have Queen Contract run `!seteicon ass [eicon]qcass[/eicon]` (or seed `characteristics["eicon_part_ass"]` directly). The prior Custom-Interaction-Eicons spec kept it hardcoded only because the initiator-keyed system couldn't express it — this feature closes that gap. Update the "Queen Contract hardcoded-eicon migration" section of that spec when this ships.

### Help / docs updates

- `ChateauSeteicon` `LongDescription` + `Usage`: document the bodypart form, whose icon shows where (the per-interaction table above, condensed), and that `!category bodypart` lists valid parts.
- `wiki-docs/Command-Reference.md` entry for `!seteicon`; move/merge this spec into `Custom-Interaction-Eicons.md` as-implemented sections on ship.

---

## Part B — Identifier eicons (`!whatis` first)

### Model

`Identifier` (`FChatDicebot/Model/ChateauDB.cs`) gains:

```csharp
[BsonIgnoreIfNull]
public string eicon { get; set; }   // full "[eicon]…[/eicon]" bbcode, or null
```

`[BsonIgnoreIfNull]` keeps existing documents untouched — no migration. Stored as full bbcode for consistency with every other stored eicon. `IdentifiersSnapshot.json` is a reference dump; it picks up `eicon` values whenever it is next re-exported (no need to hand-edit it as part of this feature).

### `!whatis` / `!identifier` readout

In `ChateauIdentifier.Run`, when the identifier has an eicon, append it to the title line:

```
[b]ass[/b] [eicon]stockbooty[/eicon]
The rear end...
```

Additionally, when the identifier is a bodypart and the **asking resident** has a personal eicon set for it (Part A), append a final line:

```
[i]Your personal eicon: [eicon]MyBooty[/eicon][/i]
```

(`!whatis` already replies via DM, so this is private and low-noise.)

### Admin setter

There is currently no identifier-editing command at all (identifiers are edited directly in Mongo), so add a minimal one rather than requiring DB surgery for a cosmetic field:

```
!setidentifiereicon {identifier} [eicon]TheEicon[/eicon]   → set
!setidentifiereicon {identifier}                           → clear
```

- New `ChateauSetidentifiereicon` command, `RequireBotAdmin = true`, `Category = "Admin"`, replies via DM.
- Validates the identifier exists (`MonDB.getIdentifier`), reuses `GetEIconFromCommandTerms` + the `MaxEiconLength` guard.
- Needs a DB write path: add `UpdateIdentifier(Identifier)` (or a narrower `SetIdentifierEicon(type, eicon)`) to `IChateauDatabase` / the Mongo implementation / `MonDB` — the interface currently has no identifier update.

### Future readout candidates (deferred — each is a later one-line opt-in)

Listed so we don't lose the ideas; **none ship with this spec**:

- `!monsterize` / `!breed` completions and the dossier's monster line showing the monster's eicon.
- `!dressup` completion showing the attire eicon; `!feed` showing the substance eicon.
- `!dossier` marks section: bodypart eicon next to each part header (this one is a Part A candidate too — the recipient's own part eicon).
- `!category` listings — probably never: 129 monster icons in one DM is spam.

---

## Validation & error cases (summary)

| Case | Behavior |
|---|---|
| `!seteicon nonsense` | rejection message naming both interactions and bodyparts |
| `!seteicon ass` (no eicon, one set) | clears, confirmation DM |
| `!seteicon ass` (no eicon, none set) | clears harmlessly, same confirmation (matches current interaction-clear behavior) |
| eicon > 47 chars | existing "way too long" rejection |
| completion where part-owner has no eicon | no suffix appended, no error |
| `!break mind` (break-category, not bodypart) | completes normally, no bodypart eicon |
| `!setidentifiereicon` by non-admin | standard admin-gate rejection |
| `!setidentifiereicon unknownthing …` | "not found in our records" |

## Tests

Extend `FChatDicebot.Tests/Unit/Interactioneicontests.cs` (or a sibling `Bodyparteicontests.cs`):

1. Bodypart storage round-trip / clear / null-safety (`GetBodypartEicon` on null profile etc.).
2. Storage key is `eicon_part_{part}` and never collides with an interaction key for every canonical verb.
3. Token resolution: interaction token still wins; bodypart token resolves via fake DB; unknown token rejected with the new wording.
4. Suffix, FromIdentifier rules: mark/golden/break append the **recipient's** part eicon; consume appends the **initiator's**; nothing when unset.
5. Mark: interaction eicon still suppressed (self-rendered), bodypart eicon still appended.
6. Suffix, Fixed rules: spank→recipient ass, feed→recipient mouth, lick→initiator tongue, boobhat→initiator breast, milk→recipient breast, handhold→both hands (initiator first; single icon on self-interaction).
7. Ordering: bodypart eicons after interaction eicons in the combined suffix.
8. Group: spank appends each consenter's ass eicon in consent order, no de-dup; initiator-owned rules append once; lapsit totem pole unchanged.
9. Spank: `qcass` hardcode gone; recipient with `eicon_part_ass` set reproduces the old behavior (1:1 and group).
10. `!seteicon` bare list shows both sections; bodypart section absent when none set; `lowerback` displays as "lower back".
11. `!whatis`: eicon on title line when set; absent when null; personal-eicon line for bodyparts when the asker has one.
12. `!setidentifiereicon`: admin gate, set, clear, unknown identifier, length guard.

## Files

New:

- `FChatDicebot/BotCommands/ChateauSetidentifiereicon.cs`
- `FChatDicebot.Tests/Unit/Bodyparteicontests.cs` (or grow `Interactioneicontests.cs`)

Modified:

- `FChatDicebot/InteractionProcessors/InteractionEiconSupport.cs` — bodypart storage trio, key prefix constant.
- `FChatDicebot/InteractionProcessors/InteractionProcessorBase.cs` — `BodypartEiconRule` type + virtual, identifier threaded into both suffix builders, bodypart append logic.
- `FChatDicebot/InteractionProcessors/IInteractionProcessor.cs` — `GetGroupEiconSuffix` signature.
- `FChatDicebot/InteractionProcessors/GroupInteractionResolver.cs` — pass identifier through.
- Processors declaring rules: `MarkProcessor`, `ConsumeProcessor`, `GoldenProcessor`, `BreakProcessor`, `SpankProcessor` (+ remove `qcass` ×2), `FeedProcessor`, `LickProcessor`, `BoobhatProcessor`, `MilkProcessor`, `HandholdProcessor`; `LapsitProcessor` signature only.
- `FChatDicebot/BotCommands/ChateauSeteicon.cs` — bodypart resolution branch, two-section list, help text.
- `FChatDicebot/Model/ChateauDB.cs` — `Identifier.eicon`.
- `FChatDicebot/BotCommands/ChateauIdentifier.cs` — eicon on title line + personal-eicon line.
- `IChateauDatabase` + Mongo implementation + `MonDB` — identifier update path.
- `wiki-docs/Command-Reference.md`, `wiki-docs/specs/Custom-Interaction-Eicons.md`, spec indexes — on ship.

## Assumptions (defaults the user can override before implementation)

1. **Whose-part table** above (esp. consume = initiator, milk = recipient, handhold = both) is the intended directionality.
2. **Kiss deferred** — no mouth eicons on kiss for now.
3. **No global-identifier-eicon fallback** in interaction completions — personal eicons only.
4. **Bodypart eicons render even for self-rendered verbs** (mark) — the mark eicon and the part eicon are different decorations.
5. **One shared bodypart slot per part** — the same ass eicon shows whether you're spanked, marked, or hosed down; there is no per-interaction-per-part matrix.
6. **Admin setter ships with Part B** rather than waiting for a general identifier-editing command.
7. **Group spank/milk show every consenter's part icon** (no cap). If icon soup becomes a problem in big groups, a cap can be added later.
