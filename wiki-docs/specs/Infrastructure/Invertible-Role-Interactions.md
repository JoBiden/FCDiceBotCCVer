# Invertible Role Interactions — `RoleSpec`

**Status:** Implemented.

A refactor with no behavior change. It names a pattern that had been hand-copied twice, so the
third user declares it instead of copying it a third time.

**Used by:** `ClimaxforProcessor` (`!climaxfor` / `!climax`), `SourceDrinkProcessor`
(`!drinkfrom` / `!forcedrink`). Written for
[Collectibles](Collectibles.md), whose panties verb pair (`!panties` / `!givepanties`) is the
third and shipped on it.

---

## The problem

Most interactions have one actor: whoever typed the command. A few pair two verbs onto a single
processor and swap the roles between them — `!climaxfor` means "I climax", `!climax` means "I make
you climax"; `!drinkfrom` means "I drink from you", `!forcedrink` means "you drink from me". In
both pairs, the party the interaction is *about* — whose status effects surface, whose custom
eicon renders, who the counts credit — flips with the typed verb rather than sitting on the
initiator.

Both pairs implemented that independently, with no shared code, in six matching pieces each:

| Piece | Climax | Source-drink |
|---|---|---|
| Two verb constants on one processor | `ClimaxforType` / `ClimaxType` | `DrinkFromType` / `ForceDrinkType` |
| Verb carried on `Interaction.type` | yes | yes |
| Static resolver | `ResolveClimaxer` | `ResolveDrinker` |
| Its mirror | `ResolvePartner` | `ResolveSource` |
| An "is it the inverted one" predicate | inline in `ResolveClimaxer` | `IsForceDrink` |
| `GetStatusEffectSubject` + `GetEiconSubject` overrides re-deriving the same answer | both | both |

`GroupSpec.Directional(giveKey, takeKey)` is the nearest existing abstraction and does not cover
this: it hardcodes "initiator gives, recipients take" and cannot see the typed verb.

---

## The shape

`InteractionProcessors/RoleSpec.cs`. Deliberately parallel to `GroupSpec` — a small declarative
value a processor returns to describe itself, rather than behavior it inherits.

```
RoleSpec.Fixed()                                  // actor is always the initiator (the default)
RoleSpec.Invertible(primaryVerb, invertedVerb)    // actor flips with the typed verb
```

| Member | Answers |
|---|---|
| `IsInvertible` | is there a second verb at all |
| `IsInverted(typedVerb)` | was the inverting verb typed |
| `ResolveActor(typedVerb, initiator, recipient)` | who the interaction is about |
| `ResolveCounterpart(...)` | the other party |
| `ResolveActorProfile(...)` | profile-shaped, for the base class's redirects |
| `Verbs` | both type keys, for registry wiring |

**Vocabulary.** "Actor" is whoever the effects and flavor attach to — the climaxer, the drinker —
not necessarily whoever typed the command. Each processor keeps its domain-named wrappers
(`ResolveClimaxer`, `ResolveDrinker`) over the shared methods, because those names are what the
call sites and their tests read best; they are now one-liners.

**Unrecognized verbs read as primary.** Lenient on purpose, matching what the hand-written
predicates did: a pending consent record written before a verb existed still resolves to something
sane at completion time instead of throwing.

---

## Base-class integration

`InteractionProcessorBase` gains two members:

- **`public virtual RoleSpec Roles => RoleSpec.Fixed();`** — what a processor declares.
- **`protected virtual string ResolveTypedVerb(string identifier) => null;`** — how it recovers the
  typed verb at call sites not handed it directly. Null means "can't tell from here", which leaves
  the caller on its non-directional default rather than a guess.

Two existing hooks then resolve through `Roles` instead of being overridden per processor:

| Hook | Was | Now |
|---|---|---|
| `GetEiconSubject` | `return initiatorProfile;` | `Roles.ResolveActorProfile(interactionVerb, …)` |
| `GetStatusEffectSubject` | `return recipientProfile;` | recipient for a fixed spec; the actor for an invertible one, via `ResolveTypedVerb` |

For a `Fixed()` spec both reduce to exactly their previous values, which is why no other processor
is touched.

### Why verb recovery stays a hook

The two pairs recover the typed verb differently, and neither way generalizes:

- **Climax** encodes it in the interaction identifier (`"{typeKey}|{count}"`), so
  `ResolveTypedVerb` is `ParseTypeFromIdentifier(identifier)`.
- **Source-drink cannot.** Its identifier must stay a bare substance name, because
  `DoseStatusContributor` matches that identifier against a vice to decide whether a craving was
  satisfied — a composite payload would silently break it. It reads a per-call stash instead, so
  `ResolveTypedVerb` is `_lastOutcome?.TypedVerb` and returns null before `ProcessInteraction` has
  run.

That divergence is the reason this is an overridable hook rather than something the base class
works out for itself.

### What stayed put

- **`ContributorInteractionType`** is still overridden by `SourceDrinkProcessor` alone. It now
  reads through `ResolveTypedVerb`, so the stash has one reader, but it keeps its own
  `?? DrinkFromType` fallback — contributors have no identifier in hand and the base class should
  not pretend otherwise.
- **The climax self-target collapse** stays in `ResolveClimaxer` / `ResolvePartner`. Climax has a
  real solo path (bare `!climax`, no target) where "which side is the climaxer" is not a direction
  question; source-drink has no such path and must not inherit a collapse it never wanted. So
  `RoleSpec` takes no position on self-targets.

---

## Files

### New

| File | What |
|---|---|
| `InteractionProcessors/RoleSpec.cs` | the declaration |
| `FChatDicebot.Tests/Unit/Rolespectests.cs` | 18 tests — see below |

### Modified

| File | Change |
|---|---|
| `InteractionProcessors/InteractionProcessorBase.cs` | `Roles`, `ResolveTypedVerb`; `GetEiconSubject` and `GetStatusEffectSubject` resolve through `Roles` |
| `InteractionProcessors/Involved/ClimaxforProcessor.cs` | declares `ClimaxRoles`; `ResolveClimaxer`/`ResolvePartner` become wrappers; both subject overrides deleted, replaced by `ResolveTypedVerb` |
| `InteractionProcessors/Involved/SourceDrinkProcessor.cs` | declares `DrinkRoles`; `ResolveDrinker`/`ResolveSource`/`IsForceDrink` become wrappers; both subject overrides deleted, replaced by `ResolveTypedVerb` |

---

## Tests

The point of a no-behavior-change refactor is that its proof is the tests that already existed.
**The full suite passes unmodified — 1912 tests, no assertion touched**, including the climax,
source-drink, interaction-eicon and curse-contributor suites that exercise both directions of both
pairs end to end.

`Rolespectests.cs` adds coverage for the new type itself:

- `Fixed()` resolves the initiator whatever verb is passed, and reports no verbs.
- `Invertible(...)` rejects a missing verb on either side, and reports both in primary-first order.
- The primary verb resolves the initiator as actor; the inverted verb resolves the recipient.
- Verb matching is case-insensitive; unknown, null and empty verbs read as primary.
- Actor and counterpart are always mirrors, for every verb including unknown ones.
- `ResolveActorProfile` follows the verb, and falls back to the initiator when either profile is
  null.
- Both shipped pairs agree with their own domain-named wrappers, for both of their verbs.
- The climax self-target collapse is asserted to live in `ResolveClimaxer`, *not* in `RoleSpec` —
  pinning the boundary so a later "simplification" doesn't push it down into the shared type and
  silently change source-drink.

---

## User-facing text

**None.** No resident-visible string, message, or command changed.
