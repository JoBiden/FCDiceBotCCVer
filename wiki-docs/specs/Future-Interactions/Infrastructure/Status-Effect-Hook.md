# Status-Effect Hook

Shared mechanism that lets passive status effects (odorize, dose, break, infest, corrupt, curse) surface text and validation gates in *other* interactions' consent and completion phases.

**Status:** Infrastructure spec — implement before any consequence interaction that contributes status text.

## Goal

When user A initiates `!kiss` with user B, and B is `!odorize`d with "musk" and `!dose`d with "alcohol", the resulting consent and completion messages should naturally include those status mentions without `KissProcessor` knowing anything about odorize or dose. Each status-effect-producing interaction registers its contribution; consumers opt in with one line.

## API

Add to `InteractionProcessorBase`:

```csharp
protected StatusEffectFragments GetActiveStatusEffects(Profile profile, StatusEffectCallSite callSite);

public enum StatusEffectCallSite { Consent, Completion }

public class StatusEffectFragments
{
    public List<string> ConsentWarnings = new List<string>();   // appended to GetConsentWarning output
    public List<string> CompletionAppendix = new List<string>(); // appended to GetCompletionMessage output
    public List<ValidationBlock> Blockers = new List<ValidationBlock>(); // see below
}

public class ValidationBlock
{
    public string Reason;          // "Bob's mouth is broken and cannot be kissed."
    public string Source;          // "break:mouth"
    public bool BlocksInitiator;   // true if the *initiator* would be the blocked party
    public bool BlocksRecipient;
}
```

Helper consumes the same registry pattern as processors — each status-effect contributor implements:

```csharp
public interface IStatusEffectContributor
{
    StatusEffectFragments Contribute(
        Profile profile,
        StatusEffectCallSite callSite,
        string interactionType,        // "kiss", "milk", etc. — for interaction-specific behaviour
        bool isInitiator);             // is this profile the initiator of the parent interaction?
}
```

Contributors register in `StatusEffectRegistry.Initialize()` (parallel to `InteractionProcessorRegistry`).

`GetActiveStatusEffects` walks the registry, calls each contributor, and merges the returned fragments. It also handles odorize's "fade-by-mention" semantics by **decrementing the use counter on the profile every time a non-empty fragment for that scent is returned** — see [Odorize-and-Wash.md](../Odorize-and-Wash.md).

## How processors consume it

In `GetConsentWarning`:

```csharp
var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Consent);
string baseWarning = $"...";
return baseWarning + string.Join("", effects.ConsentWarnings);
```

In `GetCompletionMessage`:

```csharp
var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Completion);
string baseMessage = $"...";
return baseMessage + string.Join("", effects.CompletionAppendix);
```

Validation blockers are checked in `ValidateInteraction`:

```csharp
var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Consent);
var relevantBlocker = effects.Blockers.FirstOrDefault(b => b.BlocksRecipient);
if (relevantBlocker != null)
    return ValidationResult.Failure(relevantBlocker.Reason);
```

## Which interactions opt in

- **Default ON for all consent-driven interactions** (Casual, Involved, Commitment, Consequence). The base class's default `GetConsentWarning` / `GetCompletionMessage` should already call the helper, so processors that don't override those get it for free.
- **OFF for system commands**: `!work`, `!volunteer`, `!pay`, `!sell`, `!stats`, `!dossier`, `!showtitles`, etc.

## Persistence

No new collections. Status-effect state lives on the `Profile` of whichever interaction set it (e.g. `profile.characteristics["odorize_musk_uses"] = "3"` for odorize). Each contributor reads from there.

## Tests

- `StatusEffectHookTests.cs`:
  - With no active effects, all collections are empty.
  - With one odorize active, `Completion` returns the scent fragment; `Consent` returns the warning. After the call, the use counter on the profile decremented.
  - With a `break:mouth` active, calling for a `kiss` interaction puts a `BlocksRecipient = true` blocker in the list.
  - Multiple effects merge in registration order.
  - System command path (call site flag) returns empty.

## Assumptions

- Decrement-on-mention is performed inside `GetActiveStatusEffects` rather than by each consumer. **Override:** if the user wants explicit consumption, factor it into a separate `ConsumeOdorizeMention(profile, scent)` call.
- Order of fragments is registration order; not configurable per spec. **Override:** if specific stacking order matters, add a `Priority` int on `IStatusEffectContributor`.
- ValidationBlock surface is recipient-only at the call sites listed; initiator-blocking effects (e.g. broken `dick` blocking the *initiator* of `!breed`) are surfaced by adding a second `GetActiveStatusEffects(initiatorProfile, ...)` call. Specs that need this list it explicitly.

## Files to create/modify

- `FChatDicebot/FChatDicebot/InteractionProcessors/StatusEffectFragments.cs` *(new)*
- `FChatDicebot/FChatDicebot/InteractionProcessors/IStatusEffectContributor.cs` *(new)*
- `FChatDicebot/FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs` *(new)*
- `FChatDicebot/FChatDicebot/InteractionProcessors/InteractionProcessorBase.cs` *(modify — add helper, fold default into base messages)*
- `FChatDicebot.Tests/Unit/StatusEffectHookTests.cs` *(new)*
