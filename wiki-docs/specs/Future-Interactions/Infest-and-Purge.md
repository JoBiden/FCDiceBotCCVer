# `!infest` + `!purge`

Infest the recipient with a parasite. Parasites have a chance to spread on each non-casual interaction; the originally-infested or newly-spread-to recipient can `!purge` at a cost. Players may also define new parasites via a guided multi-step flow.

**Investment level:** Consequence (`!infest`); `!purge` is a self-action with a cost.
**Reversal:** `!purge` (with cost).
**Depends on:** [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md), [Conversational-Flows](Infrastructure/Conversational-Flows.md) (only for the custom-parasite definition flow — fall back to hand-authored parasites if that infrastructure isn't built yet).

## Parasite catalog

A new collection in MongoDB: `Parasites`.

```csharp
public class Parasite
{
    public string Id;                  // GUID
    public string Name;                // unique, lowercase
    public string Description;         // shown in consent warning
    public PurgeCost Cost;             // see PurgeCost
    public string CreatedBy;           // userName, or "system" for built-ins
    public DateTime CreatedAt;
}

public class PurgeCost
{
    public PurgeCostType Type;         // MissedWork, RandomCurse, RandomBreak, LostTrainingPoint
    public string Detail;              // optional override (e.g. specific curse type)
}

public enum PurgeCostType
{
    MissedWork,        // (default) blocks !work for the next UTC day
    RandomCurse,       // applies a random curse from the curse catalog
    RandomBreak,       // applies a random break (random part, 1-3 days)
    LostTrainingPoint, // -1 to a random training the user has at level >0
}
```

Built-in starter parasites (`CreatedBy = "system"`):

| Name | Description | Cost |
|------|-------------|------|
| `bloodworm` | A wriggling thread under the skin, vibrating subtly when touched. | MissedWork |
| `mindfluke` | A nervous-system colonist that leaves whispers at the edge of thought. | RandomCurse |
| `nestlings` | A clutch of small parasites that occasionally shift visibly. | RandomBreak |

## `!infest` command syntax

```
!infest [user]Bob[/user] {parasiteName}
```

## `!infest` validation

- Recipient registered.
- `parasiteName` resolves to a `Parasite` record.
- Multiple parasites simultaneously **allowed** — no de-dup, but if the same parasite is already active on the recipient, reject ("Bob is already infested with bloodworm — wait for them to !purge or pick a different parasite").

## `!infest` consent warning

Must be informative — the user must understand it can spread and can be cured easily if acted on quickly:

> "Alice wants to infest you with **bloodworm** — *{description}*. This parasite has a chance to spread to anyone you {non-casual} interact with. You can `!purge` at the cost of {cost description}, but new infestees have a grace period of {grace} hours during which `!purge` has no cost. Do you !consent?"

## `!infest` processor logic (`InfestProcessor`)

1. Append a `ParasiteInstance` to `recipient.lists["parasites"]`.
2. Set `timer["infest_cooldown_" + parasite.Name]` to 7 days (Consequence default).
3. Save interaction. Delete pending command.

## ParasiteInstance shape

```csharp
public class ParasiteInstance
{
    public string ParasiteName;
    public string InfestedBy;       // userName of the !infest initiator (or the spreader for spread cases)
    public DateTime InfestedAt;
    public bool SpreadFromContact;  // true if this came from spread, not direct !infest
    public DateTime GraceUntil;     // = InfestedAt + GracePeriod (only meaningful if SpreadFromContact == true)
}
```

Stored as JSON-encoded list entries in `lists["parasites"]` (existing pattern), or as a typed list field if the user prefers — the existing codebase uses both.

## Status-effect contributions

`ParasiteStatusContributor` runs on every non-casual consent-driven interaction. Behavior:

1. For each parasite on the recipient, roll spread independently — default chance **10%**.
2. If the spread roll succeeds **and the initiator does not already have that parasite**, append a new `ParasiteInstance` to initiator with `SpreadFromContact = true`, `InfestedBy = recipientName`, `GraceUntil = now + 24 hours`.
3. The interaction's completion message gains a status fragment: `"...and {parasite.Name} spreads from {recipient} to {initiator}. They have 24 hours to !purge without consequences."`
4. If multiple parasites spread in one interaction, fragments are concatenated.

**Casual interactions skip the spread roll entirely** (per user spec). Skip is decided by `interactionType` lookup against the casual list.

## `!purge` command syntax

```
!purge {parasiteName}
!purge                  → purges the oldest parasite
```

## `!purge` validation

- Caller has the named parasite. Otherwise: "You aren't infested with {name}."

## `!purge` processor logic (`PurgeCommand`, system command — no consent flow)

1. Find the parasite instance on the caller's profile.
2. **Grace check**: if `SpreadFromContact == true` and `now < GraceUntil`, purge has **no cost**.
3. Otherwise, apply the parasite's cost:
   - `MissedWork`: set `caller.timers["work_blocked"]` to next UTC day midnight.
   - `RandomCurse`: pick a random curse from the curse catalog and apply it (calls into [Curse-and-Cleanse](Curse-and-Cleanse.md)).
   - `RandomBreak`: pick a random part from the breakable list, duration 1–3 days, apply (calls into [Break-and-Rest](Break-and-Rest.md)).
   - `LostTrainingPoint`: pick a random training where caller has >0, decrement by 1.
4. Remove the parasite instance.
5. Output completion message describing the purge and any cost suffered.

## Defining a new parasite (custom)

Triggered by `!defineparasite` — starts a [Conversational Flow](Infrastructure/Conversational-Flows.md) of type `"define_parasite"`. Steps:

1. **Name** — must be unique, lowercase, single word. Reject collisions.
2. **Description** — free text, 20–500 chars.
3. **Cost** — present numbered list of `PurgeCostType` values (default highlighted: MissedWork). Optionally accept a detail string for `RandomCurse`/`RandomBreak`.
4. **Confirm** — show the assembled record. Options: `confirm` / `change name` / `change description` / `change cost` / `cancel`.
5. On `confirm`, write to `Parasites` collection with `CreatedBy = caller`.

## Persistence

- Profile: `lists["parasites"]` (list of JSON-encoded `ParasiteInstance` or typed list).
- MongoDB: `Parasites` collection.
- Profile timers: `infest_cooldown_{parasiteName}` (per-parasite 7-day pair lock from initiator), `work_blocked` (24-hour, from MissedWork cost).

## Tests

- `InfestProcessorTests.cs`: persists parasite, dedup, consent text contains parasite description.
- `ParasiteStatusContributorTests.cs`: spread chance honored over many trials; casual interactions skip spread; same-parasite-already-present skips; grace period set on spread.
- `PurgeCommandTests.cs`: each cost type applied; grace removes cost; non-existent parasite rejected.
- `DefineParasiteFlowHandlerTests.cs`: full happy path; unique name validation; cancel works; confirm-step branch back works.

## Assumptions

- Spread chance: 10% per parasite per non-casual interaction. **Override** by adding a `SpreadChance` field to `Parasite` for per-parasite tuning if 10% proves wrong.
- Grace period: 24 hours from spread. **Override:** make per-parasite if needed.
- Casual = the existing Casual investment level set. **Override:** add a per-interaction `SpreadEligible` flag if a casual interaction should opt in.
- Custom parasite name uniqueness is global (not per-creator).
- Custom parasites cannot use a non-default `Detail` slot in v1 (we land MissedWork/RandomCurse/RandomBreak/LostTrainingPoint without per-record overrides).

## Files to create/modify

- `FChatDicebot/Model/Parasite.cs` *(new)*
- `FChatDicebot/Model/ParasiteInstance.cs` *(new)*
- `FChatDicebot/InteractionProcessors/Consequence/InfestProcessor.cs` *(new)*
- `FChatDicebot/BotCommands/Infest.cs` *(new)*
- `FChatDicebot/BotCommands/Purge.cs` *(new)*
- `FChatDicebot/BotCommands/DefineParasite.cs` *(new)*
- `FChatDicebot/Conversational/Handlers/DefineParasiteFlowHandler.cs` *(new)*
- `FChatDicebot/InteractionProcessors/StatusEffectContributors/ParasiteStatusContributor.cs` *(new)*
- `FChatDicebot/Database/IChateauDatabase.cs` *(modify — add `GetParasite`, `SaveParasite`, `ListParasites`)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` *(modify — register)*
- `FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs` *(modify — register)*
- `FChatDicebot/Conversational/ConversationalFlowRegistry.cs` *(modify — register)*
- `FChatDicebot.Tests/Unit/InfestProcessorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/ParasiteStatusContributorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/PurgeCommandTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/DefineParasiteFlowHandlerTests.cs` *(new)*
