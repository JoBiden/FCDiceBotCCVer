# Status-Effect Hook

Shared mechanism that lets passive status effects (odorize, dose, break, infest, corrupt, curse) surface text and validation gates in *other* interactions' consent and completion phases.

**Status:** Implemented. Registry currently seeded with `OdorizeStatusContributor` (Tier 3) and `CorruptionStatusContributor` (Tier 2 corrupt/purify); the remaining consequence interactions (dose / break / infest / curse) will add their own contributors as they land.

## Goal

When user A initiates `!kiss` with user B, and B is `!odorize`d with "musk" and `!dose`d with "alcohol", the resulting consent and completion messages should naturally include those status mentions without `KissProcessor` knowing anything about odorize or dose. Each status-effect-producing interaction registers its contribution; consumers opt in with one line.

## API

On `InteractionProcessorBase`:

```csharp
// Aggregator. Walks StatusEffectRegistry, calls each contributor, merges results.
// isInitiator defaults to false (recipient) — the most common call shape.
protected StatusEffectFragments GetActiveStatusEffects(
    Profile profile,
    StatusEffectCallSite callSite,
    bool isInitiator = false);

// Spacing helper. Use in overridden GetConsentWarning / GetCompletionMessage to
// concatenate fragments onto a base message with one space separator per non-empty
// fragment. Contributors should NOT include their own leading whitespace.
protected static string AppendStatusFragments(string baseMessage, IEnumerable<string> fragments);

public enum StatusEffectCallSite { Consent, Completion }

public class StatusEffectFragments
{
    public List<string> ConsentWarnings = new List<string>();    // surfaced in GetConsentWarning
    public List<string> CompletionAppendix = new List<string>(); // surfaced in GetCompletionMessage
    public List<ValidationBlock> Blockers = new List<ValidationBlock>();
    public void MergeWith(StatusEffectFragments other);          // append-merge in place
}

public class ValidationBlock
{
    public string Reason;          // "Bob's mouth is broken and cannot be kissed."
    public string Source;          // "break:mouth" — diagnostic tag, not shown to users
    public bool BlocksInitiator;   // true if the initiator would be the blocked party
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

Contributors register in `StatusEffectRegistry.Initialize()` (parallel to `InteractionProcessorRegistry`). Tests can use `StatusEffectRegistry.Clear()` + `RegisterContributor(...)` for isolation.

`GetActiveStatusEffects` is a pure aggregator: it walks the registry, calls each contributor, and merges what each one returns. Contributors that throw are skipped silently so one misbehaving contributor cannot break the parent interaction.

**Contributors own their own state mutations.** Fade-by-mention semantics (e.g. odorize decrementing a use counter every time it contributes a non-empty fragment) live inside the contributor's `Contribute()` method — see [Odorize-and-Wash.md](../Odorize-and-Wash.md). The helper does not introspect contributor state.

## How processors consume it

In `GetConsentWarning`:

```csharp
var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Consent);
string baseWarning = $"...";
return AppendStatusFragments(baseWarning, effects.ConsentWarnings);
```

In `GetCompletionMessage`:

```csharp
var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Completion);
string baseMessage = $"...";
return AppendStatusFragments(baseMessage, effects.CompletionAppendix);
```

For an initiator-side check (e.g. a broken `dick` blocking the initiator of `!breed`), call the helper a second time with `isInitiator: true` against `initiatorProfile`.

Validation blockers are checked in `ValidateInteraction`:

```csharp
var effects = GetActiveStatusEffects(recipientProfile, StatusEffectCallSite.Consent);
var relevantBlocker = effects.Blockers.FirstOrDefault(b => b.BlocksRecipient);
if (relevantBlocker != null)
    return ValidationResult.Failure(relevantBlocker.Reason);
```

## Which interactions opt in

- **Default consent + validation are ON.** The base class's default `GetConsentWarning` calls the helper and appends fragments via `AppendStatusFragments`; the default `ValidateInteraction` checks recipient blockers. Processors that don't override either get this for free.
- **`GetCompletionMessage` is abstract.** Every existing processor overrides it, so there is no usable base default. Overrides that want completion-time status text must call the helper + `AppendStatusFragments` themselves (see snippet above).
- **System commands skip the helper entirely.** Commands like `!work`, `!volunteer`, `!pay`, `!sell`, `!stats`, `!dossier`, `!showtitles` are not `InteractionProcessorBase` subclasses and never call into the registry.

## Persistence

No new collections. Status-effect state lives on the `Profile` of whichever interaction set it (e.g. `profile.characteristics["odorize_musk_uses"] = "3"` for odorize, `profile.characteristics["corruption"] = "-50"` for corrupt/purify). Each contributor reads from there.

## Out-of-band private notifications to the initiator

A separate channel for the case where a processor needs to message the initiator privately after a consent fires (e.g. corrupt/purify's TOCTOU "your queued interaction landed but daily quota was already spent" notice):

```csharp
// On InteractionProcessorBase
protected string _lastInitiatorPrivateMessage = string.Empty;
public string GetAndClearInitiatorPrivateMessage();   // also on IInteractionProcessor
```

`ChateauConsent` drains this after every `ProcessInteraction` and routes any non-empty result to a private message. Processors that don't need it leave the field empty; the channel adds no overhead.

## Tests

`StatusEffectHookTests.cs` covers, using inline `FakeContributor` / `FakeBreakContributor` / `FakeOdorizeContributor` doubles plus a `TestProcessor` that exposes the protected helper:

- **Aggregation:** empty registry → empty fragments; null profile → empty; contributor receives `interactionType` and `isInitiator`; multiple contributors merge in registration order; a throwing contributor is skipped without breaking the helper.
- **Call site routing:** the contributor sees the correct `StatusEffectCallSite` value.
- **Odorize-style fade:** a contributor that decrements a counter on each invocation does so on every call, stops emitting fragments at 0, and never goes negative.
- **Break-style blockers:** a recipient blocker for a matching `interactionType` shows up in `Blockers` with `BlocksRecipient = true`; same blocker for an unrelated `interactionType` does not.
- **Default `GetConsentWarning` integration:** appends fragments with a single space separator each, skips empty fragments, never produces double-space artifacts.
- **Default `ValidateInteraction` integration:** a recipient-blocking contributor fails validation with the contributor's `Reason`; no contributors → validation succeeds.

The original spec also listed "system command path returns empty" as a test — that case isn't testable here because system commands don't subclass `InteractionProcessorBase` and therefore cannot reach the helper; the responsibility is structural.

## Assumptions

- **Helper signature carries `isInitiator`.** The original spec showed `(Profile, callSite)`; an `isInitiator` parameter (default `false`) was added so the same helper can be called twice — once for the recipient, once for the initiator — which the spec's own `IStatusEffectContributor` already required.
- **Spacing convention: contributors emit fragments without leading whitespace.** The base class's `AppendStatusFragments` inserts exactly one space between the host message and each fragment, and between consecutive fragments. **Override:** if a contributor needs a different separator (e.g. newline), it has to build the full text in the host message before calling, since `AppendStatusFragments` is space-only.
- **Contributors own their own state mutations.** Fade-by-mention decrement (odorize's use counter) lives inside the contributor, not the helper. The helper is purely an aggregator. **Override:** if you want explicit, per-call consumption, factor it into a separate `ConsumeOdorizeMention(profile, scent)` call invoked by the consumer instead of the contributor.
- **Order of fragments is registration order.** Not configurable per spec. **Override:** if specific stacking order matters, add a `Priority` int on `IStatusEffectContributor` and sort in `GetAllContributors`.
- **ValidationBlock surface is recipient-only at the default call sites.** The default `ValidateInteraction` checks recipient blockers; initiator-blocking effects (e.g. broken `dick` blocking the *initiator* of `!breed`) require a second `GetActiveStatusEffects(initiatorProfile, ..., isInitiator: true)` call in the processor's overridden `ValidateInteraction`. Specs that need this list it explicitly.
- **Contributor exceptions are swallowed.** A `Contribute()` that throws is skipped and the remaining contributors still run. **Override:** if a contributor's failure should be visible (e.g. for debugging), wire logging into `GetActiveStatusEffects`.

## Files to create/modify

- `FChatDicebot/InteractionProcessors/StatusEffectFragments.cs` *(new)*
- `FChatDicebot/InteractionProcessors/IStatusEffectContributor.cs` *(new)*
- `FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs` *(new)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorBase.cs` *(modify — add helper, fold default into base messages)*
- `FChatDicebot.Tests/Unit/StatusEffectHookTests.cs` *(new)*
