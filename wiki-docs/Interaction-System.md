# Interaction System (Chateau Contract)

The Chateau Contract interaction system is the most complex feature of FCDiceBot, providing a consent-based roleplay interaction framework with persistence, statistics tracking, and transformative effects.

## Overview

The interaction system allows residents to interact with each other in roleplay scenarios, with all interactions:
- **Requiring consent** from the recipient
- **Being tracked** in a database
- **Having cooldowns** to prevent spam
- **Granting achievements** and titles
- **Creating lasting effects** (for some interactions)

The authoritative list of interactions and their exact usage lives in [Command-Reference](Command-Reference.md) (and in-app via `!help`); this page explains how the machinery works.

## Investment Levels

Interactions are organized into investment levels. The level names the *weight* of the act; cooldowns are set per interaction, not per level (see Rate Limiting below).

### 1. Casual Interactions

**Location:** `InteractionProcessors/Casual/` — kiss, cuddle, handhold, spank, bully, pet, lick, boobhat, lapsit (`!sit` / `!lap`)

- Simple, non-committal; no lasting game effects
- 30-minute cooldown (a missed count is flavor-texted, the act still narrates)
- **Group-capable**: `!cuddle bob smith jane doe` opens one seat per target; the group resolves when everyone consents/declines or the 10-minute window closes. Lapsit additionally stacks (a race for the lap, then a pile)

### 2. Involved Interactions

**Location:** `InteractionProcessors/Involved/` — feed, dressup, golden, milk, climax/climaxfor, payment (`!pay` give/receive)

- More roleplay investment; several take an identifier (substance to feed, attire to wear)
- Milk produces serial-numbered bottles (`!bottles`, `!drink`, `!sell`, transfer via `!pay`)
- Milk's cooldown is **per-direction**: the initiator is stamped with a `milk_give_<recipient>` key, so milking Alice doesn't block milking Bob

### 3. Commitment Interactions

**Location:** `InteractionProcessors/Commitment/` — mark, entitle, bond, employ, petrify, plant, objectify, consume, breed, train, corrupt/purify

- Lasting relationships, transformations, and state (marks, bonds, jobs, pregnancies, corruption)
- Typically 1-day cooldowns; petrify is 7 days; breed is per-direction like milk
- Corrupt/purify share one processor and use a **daily magnitude quota** (10 magnitude per recipient per day) instead of a hard cooldown

### 4. Consequence Interactions

**Location:** `InteractionProcessors/Consequence/` — rename, monsterize, odorize, break, dose, infest, curse

- Transformative, persist until reversed; typically 7-day cooldowns, most binding the **initiator per axis** (e.g. one `!dose` per vice per recipient per week)
- Several leave **status effects** that surface in *other* interactions' prompts and completions (scent layers, addictions, parasites, curses) via the `IStatusEffectContributor` hook

### Recovery commands

Most lasting effects have a themed reversal, each with its own cost or time gate: `!purge` (parasites), `!cleanse` (curses), `!rest` (broken bodyparts), `!detox` (addictions), `!purify` (corruption), `!birth` (finished pregnancies), `!wash` (scent layers). These are self-targeted commands, not consent flows.

## Consent Workflow

### Step 1: Initiate

```
!kiss [user]Bob[/user]        (or just: !kiss bob)
!cuddle bob smith jane doe    (group casual)
```

The initiating command validates both profiles, writes a `PendingCommand` to Mongo (`pendingInteraction`, `awaitingConsentFrom`), and posts the processor's consent prompt. Every prompt ends with the decline reminder:

```
Alice wants to give you a kiss, Bob. Do you !consent to a smooch? (or !no)
```

### Step 2: Recipient responds

| Command | Aliases | Effect |
|---------|---------|--------|
| `!consent` | `!c`, `!accept` | Accept. With several pending: `!consent {number}`, `!consent {name}`, or `!consent all` |
| `!no` | `!refuse`, `!decline` | Decline. In a group, the shared moment still resolves with whoever else consented |
| *(nothing)* | | The pending request expires after 10 minutes (`GroupInteractionResolver.PendingMinutesKeep`) |

The **initiator** can withdraw with `!oops` (aliases `!o`, `!withdraw`, `!cancel`) — for a group, that calls off the whole pile.

### Step 3: Process

On consent, `ChateauConsent` sweeps expired pendings, resolves the processor from `InteractionProcessorRegistry`, and calls `ProcessInteraction()`: counts are incremented (rate-limit aware), effects applied, the pending command deleted, and the interaction saved to history. The completion message is assembled with status-effect fragments, custom eicons, and any title grants, then queued to the channel.

## Rate Limiting

There is no single per-level cooldown table — each processor declares its own limit, and that declaration is the single source of truth for the player-facing text:

- **Casual/involved processors** hold a `RateLimit` constant (30 minutes as of mid-2026). Hitting it doesn't block the roleplay — the completion still posts, with a `[sub]` note that the clerks didn't record it.
- **Commitment/consequence processors** declare a `CooldownSpec` (`CooldownRule` property) with:
  - `PeriodDays` (typically 1 or 7),
  - `Binds` — initiator, recipient, pair, or both (so "Bob can't be marked again today" and "Alice can't dose anyone with this vice this week" are different shapes),
  - optional `Scope` for per-axis initiator cooldowns (vice, curse, parasite, scent, bodypart),
  - or a `MagnitudeQuota` (corrupt/purify's 10-per-day budget).

  `CooldownSpec.FormatDuration()` / `FormatAppliesTo()` generate the `!help` fields, and `ConsentWarningText` renders the same spec into the consent-prompt frequency clause — the two can never drift. **Never hand-write cooldown text.**

Implementation: timers live in the profile's `timers` dictionary as `CoolDown { timerStart, timerEnd }` (UTC), keyed `ratelimit_{countLabel}` — or per-direction keys such as `milk_give_<recipient>` for milk and breed.

## Statistics Tracking

- **Symmetric interactions** (kiss, cuddle, handhold…) increment the *same* count label for both parties (`counts["kiss"]` on each).
- **Directional interactions** use give/take labels via `IncrementDifferentCounts…` (e.g. `milkgive` / `milktake`, `spankgive` / `spanktake`).
- Every completed interaction is also saved to the `Interactions` collection (initiator, recipient, type, identifier, investmentLevel, interactionTime) — this history feeds `!dossier`, the `!statistics` family, and title grants.

## Processor Architecture

One `IInteractionProcessor` per interaction, registered explicitly in `InteractionProcessorRegistry.Initialize()`. A few processors register under two keys (lapsit: `!sit`/`!lap`; corruption: `!corrupt`/`!purify`; climax: `!climaxfor`/`!climax`).

Read the interfaces rather than trusting any summary here — both files are heavily commented:

- `InteractionProcessors/IInteractionProcessor.cs` — the full contract, including group methods (`ApplyGroupCounts`, `GetGroupCompletionMessage`, `GrantGroupTitles`), verb-tense rendering, eicon suffixes, and the rate-limit/private-message accessors.
- `InteractionProcessors/InteractionProcessorBase.cs` — the template implementation: count helpers, status-effect composition, group message builders, and the consent-warning entry point.

For a worked example of adding one, see the [Development Guide](Development-Guide.md#adding-a-new-interaction). The shortest real processor to read is `Casual/KissProcessor.cs`.

### Consent warnings

Override `BuildConsentWarning` (and, for group-capable casuals, `BuildGroupConsentWarning`) —
never `GetConsentWarning` itself. `GetConsentWarning` is the non-virtual channel-bound entry
point: it takes your body and applies `ConsentWarningText.DeclineHint` — `(or !no)` — so every
consent prompt in the Chateau names the decline command as well as the accept one. Don't write
that reminder into the body yourself.

The reminder sits directly after the prompt's own question, and anything that trails the prompt
follows it. If your builder appends status-effect fragments, return
`ComposeConsentWarning(baseWarning, effects.ConsentWarnings)` rather than
`AppendStatusFragments(...)` — otherwise the reminder ends up stranded behind the fragments:

```csharp
protected override string BuildConsentWarning(Profile init, Profile rec, string identifier)
{
    string baseWarning = /* ... */ " Do you !consent to this curse?";
    var effects = GetActiveStatusEffects(rec, StatusEffectCallSite.Consent, identifier, isInitiator: false);
    return ComposeConsentWarning(baseWarning, effects.ConsentWarnings);
}
```

Two escape hatches:

- Prompts composed outside a processor (e.g. `!break`, which needs the typed duration the
  processor can't see) apply `ConsentWarningText.WithDeclineHint` themselves. It's idempotent,
  keyed on the prompt naming `!no` anywhere, so double-wrapping is safe.
- A prompt that announces something other than a yes/no question can substitute
  `ConsentWarningText.DeclineHintVariant("to opt out entirely")`, which suppresses the default.
  `!sit`'s lap stack is the one user of this.

`ConsentDeclineHintTests` fails the build if a processor shadows the entry point.

### Validation

Processors that need an identifier or precondition override `ValidateInteraction(initiator, recipient, identifier)` and return `ValidationResult.Failure("...")` — the initiating command surfaces the error instead of posting a consent prompt.

## Title System

Interactions can grant titles (achievements).

- **System titles** are granted automatically (`ChateauSystemTitles.CheckAndGrantTitles(userName)`; group interactions use the processor's `GrantGroupTitles`). Format: `·Title Name·` (with dots).
- **Player-granted titles** come from `!entitle` (no dots).
- Titles are stored on the profile as `Title { titleText, givenBy, grantedTime }`; `givenBy` is `"system"` for system titles.
- Each profile has **9 display slots** (`displayedTitleSlots`): `!settitle {slot} {title}` to display (or clear), `!titles` to list everything earned.

## Job System

Jobs are assigned via `!employ` (commitment interaction; self-employment works by targeting yourself).

### Work Command

**Daily Duty (two-step, via PM):**

```
Bob: !work
Bot (PM): The head maid hands you a list of chores...
         !w 1: Dust the shelves | !w 2: Polish the silver
Bob: !w 2
Bot (PM): Gleaming! ...
         You received [b]12 copper[/b]!
```

**Implementation** (`BotCommands/ChateauWork.cs`):
1. Remove a stale pending duty left over from a previous Chateau day, if any
2. If a pending duty is waiting, the number is the player's choice: post its
   `resultText`, roll ONE reward from the choice's weighted `rewardList`,
   grant +1 job experience, set the daily work timer, delete the pending duty
   (a 25% MANOR kickback may go to the employer - see Employer earnings)
3. Otherwise pick a random duty for the player's job (falling back to the
   `default` job's duties), filter its choices by their conditionals (job
   experience / training / currency / species - see
   `BotCommands/Support/DutyConditionalSupport.cs`), PM the numbered choice
   list, and store a `PendingDuty`

Duty documents live in the `Duties` collection; see
[Database-and-Persistence](Database-and-Persistence.md) for the real schema
(label/job/startText/dutyResults). They are authored with the Work Duty
Builder (`scripts/work-duty-builder`).

### Volunteer

`!volunteer {job}` runs the same duty flow for a job you are NOT employed in
(answers use `!v N`). It has its own daily timer, separate from `!work`, rolls
the same duties for the same rewards, and grants job experience for the job
being tried.

## Transformations and Lasting State

Commitment/consequence interactions write lasting state onto the recipient's profile — simple values in `characteristics["key"]`, lists in `lists["key"]` (bonds, scent layers, parasites…), structured state in dedicated fields (`pregnancies`, `milkInventory`, `trainings`). Examples: monsterize sets the species characteristic; petrify records the statue and its location (browse with `!statues`); corruption tracks a magnitude that other interactions can see.

Reversals go through the themed recovery commands listed above — there is no generic "restore" command. What each transformation's exit looks like (cost, time gate, or none) is part of its spec in `wiki-docs/specs/`.

## Querying Interactions

- `!dossier [user]Name[/user]` (aliases `!profile`, `!bio`) — the public-facing summary document: record, holdings, titles, lasting effects.
- `!statistics` (alias `!stats`) — Chateau-wide statistics, with drill-downs: `!populations`, `!flora`, `!birthrates`, `!parasites`, `!payroll`, `!economics`, `!statues`.
- `!bondtree` / `!familytree` — bond-graph maps N degrees out from a resident.

## See Also

- [Command Reference](Command-Reference) - Full list of interaction commands
- [Database and Persistence](Database-and-Persistence) - How interactions are stored
- [Development Guide](Development-Guide) - Adding new interactions
- [Architecture](Architecture) - Processor pattern details
- [Style Guide](Style-Guide.md) - Wording rules for prompts and completions
- `wiki-docs/specs/` - Per-feature as-shipped specs
