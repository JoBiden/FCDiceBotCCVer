# `!curse` + `!cleanse`

Apply a consequential curse to the recipient. Curses fall into three buckets and are reversed by `!cleanse` at a cost.

**Investment level:** Consequence
**Reversal:** `!cleanse` (with cost).
**Depends on:** [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md)

## Bucket taxonomy

User-confirmed buckets:

| Bucket | Description | Implementation |
|--------|-------------|----------------|
| **Disablers** | Block specific interactions outright | Status-effect blocker entries |
| **Modifiers** | Alter completion text without changing mechanics | Status-effect completion fragments |
| **Restrictors** | Narrow what's allowed in `!work` / `!volunteer` (e.g. only sex-related jobs) | Read by the work system |

(User dropped "isolation" curses as antithetical to the system's social purpose.)

## Curse catalog

A new MongoDB collection `Curses`. v1 ships with these starter curses:

### Disablers

| Name | Effect |
|------|--------|
| `chastity` | Blocks `!climaxfor` (recipient cannot climax). |
| `mute` | Blocks `!kiss`, `!feed` (recipient mouth-locked at the curse level — distinct from `break:mouth`). |
| `frigid` | Blocks `!cuddle`, `!handhold`. |
| `barren` | Blocks `!breed` recipient role. |

### Modifiers

| Name | Fragment template |
|------|-------------------|
| `mooing` | "...and {recipient} can't help but emit a {moo|low moo|long moo} mid-sentence." |
| `stuttering` | "...{recipient} st-stutters through a th-thank y-you." |
| `third-person` | "...{recipient} acknowledges the gesture in third person, as is their nature." |
| `blushing` | "...{recipient} flushes a deep red, helpless to it." |
| `tail-wag` | "...{recipient}'s tail betrays their excitement." |

### Restrictors

| Name | Restriction |
|------|------------|
| `sex-addicted` | `!work` / `!volunteer` only allowed for jobs flagged `sex_related = true` in the duties catalog. |
| `caffeine-addicted` | `!work` / `!volunteer` only allowed for jobs flagged `service_related = true`. |
| `pious` | `!work` / `!volunteer` only allowed for jobs flagged `religious = true`. |

## Curse data model

```csharp
public class Curse
{
    public string Id;
    public string Name;                 // unique, lowercase
    public string Bucket;               // "disabler" | "modifier" | "restrictor"
    public List<string> BlockedInteractions;     // for disablers
    public string ModifierFragmentTemplate;      // for modifiers
    public Dictionary<string, string> RestrictorFlags; // for restrictors, e.g. {"required_job_flag": "sex_related"}
    public string CleanseCostType;      // PurgeCostType reused — see Infest spec
    public string CleanseCostDetail;
    public string CreatedBy;
}
```

Curse instances on the recipient profile:

```csharp
public class CurseInstance
{
    public string CurseName;
    public string AppliedBy;
    public DateTime AppliedAt;
}
```

Stored at `recipient.lists["curses"]`.

## `!curse` command syntax

```
!curse [user]Bob[/user] {curseName}
```

## `!curse` validation

- Recipient registered.
- `curseName` resolves in the `Curses` catalog.
- Recipient does not already have this curse active. (Multiple distinct curses allowed; same curse twice rejected.)

## `!curse` processor logic (`CurseProcessor`)

1. Append `CurseInstance` to `recipient.lists["curses"]`.
2. Set 7-day cooldown timer on (initiator, recipient, curseName).
3. Save interaction. Delete pending command.

## Status-effect contributions

`CurseStatusContributor` runs on every consent-driven interaction. For each curse on the recipient:

- **Disabler**: emits a `ValidationBlock` for any interaction in `BlockedInteractions`. The blocker fires in `ValidateInteraction`, preventing consent from being requested.
- **Modifier**: emits a completion fragment from `ModifierFragmentTemplate`.
- **Restrictor**: contributes nothing to interactions; effect surfaces only in `!work` / `!volunteer` (those system commands check `recipientProfile.lists["curses"]` directly).

## Buckets in the consent warning

When `!curse` consent is requested, the warning **must include**:

> "Alice wants to curse you with **{curseName}** — a {bucket} curse: {effect description}. To remove it, you will need to `!cleanse` at the cost of {cleanseCostDescription}. Do you !consent?"

The recipient should always know what they're agreeing to before saying yes.

## `!cleanse` command syntax

```
!cleanse {curseName}
!cleanse                  → cleanses oldest curse
```

## `!cleanse` validation

- Self-targeted only.
- Caller has the named curse (if specified) or any curse (default).

## `!cleanse` processor logic (`CleanseCommand`, system command)

1. Find the curse instance.
2. Apply the cleanse cost via the `PurgeCostType` mechanism (see [Infest-and-Purge](Infest-and-Purge.md) for the full type list — same enum reused).
3. Remove the curse instance.
4. Output completion message.

## Persistence

- Profile: `lists["curses"]`.
- MongoDB: `Curses` collection.

## Custom curse definition

Out of v1 scope unless the user requests it. Pattern: same conversational flow approach as `!defineparasite`, with bucket-specific question branches. Track in the `Future-Infrastructure` folder if/when needed.

## Tests

- `CurseProcessorTests.cs`: persists curse, cooldown set, dup-rejected.
- `CurseStatusContributorTests.cs`:
  - Disabler emits validation blocker for the listed interactions only.
  - Modifier emits completion fragment.
  - Restrictor emits nothing on interactions.
- `CleanseCommandTests.cs`: each cost type applied; specific vs default selection.
- `CurseCatalogTests.cs`: starter curses load, schema valid.
- `WorkRestrictorIntegrationTests.cs`: !work refuses non-matching jobs when `sex-addicted` is active. (In the work module if/when modified.)

## Assumptions

- v1 curses are hand-authored. Custom curse definition is out of scope.
- Disablers compose: if a recipient has both `chastity` and `mute`, both blockers register.
- Modifier fragments compose: multiple modifier curses all append fragments to a single message.
- Restrictor flags hardcoded to known job-type tags. The duties catalog needs `sex_related`, `service_related`, `religious` boolean fields. **Confirm:** the duty catalog can carry these tags.
- `frigid` blocks casual interactions despite the spec's "casual interactions skip status effects" rule. Disabler curses are the *one* exception — they're reasons-to-not-do-the-thing-at-all, not flavor. Implementation: `Status-Effect-Hook` validation blockers fire on all consent-driven interactions, regardless of investment level.

## Files to create/modify

- `FChatDicebot/FChatDicebot/Model/Curse.cs` *(new)*
- `FChatDicebot/FChatDicebot/Model/CurseInstance.cs` *(new)*
- `FChatDicebot/FChatDicebot/InteractionProcessors/Consequence/CurseProcessor.cs` *(new)*
- `FChatDicebot/FChatDicebot/BotCommands/Curse.cs` *(new)*
- `FChatDicebot/FChatDicebot/BotCommands/Cleanse.cs` *(new)*
- `FChatDicebot/FChatDicebot/InteractionProcessors/StatusEffectContributors/CurseStatusContributor.cs` *(new)*
- `FChatDicebot/FChatDicebot/Database/IChateauDatabase.cs` *(modify — Curse accessors)*
- `FChatDicebot/FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` *(modify — register)*
- `FChatDicebot/FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs` *(modify — register)*
- Seed data file: `FChatDicebot/Data/curses.starter.json` *(new — loaded into MongoDB on first run)*
- `FChatDicebot.Tests/Unit/CurseProcessorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/CurseStatusContributorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/CleanseCommandTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/CurseCatalogTests.cs` *(new)*
