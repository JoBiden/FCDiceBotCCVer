# Consent-Warning & Welcome Text Pass

A cross-cutting **user-facing text** pass covering three items from [Feature-Requests.md](../Feature-Requests.md):

- **B1** — remove the "under construction" warning from the `!joinchateau` welcome.
- **B2** — replace the vague "*…and can not be done frequently.*" boilerplate with the **actual** cooldown facts (who is bound, how often), plus a small normalization + single-source-of-truth refactor of the cooldown metadata.
- **B3** — make `!corrupt` / `!purify` disclose that corruption/purity is **visible in other interactions and in milked product**, on the consent prompt and in help text.

**Status:** Proposed (not yet implemented). This is not a single interaction — it's a wording/refactor pass spanning ~16 interactions plus the welcome message, so it lives here rather than in `Future-Interactions/`.
**Depends on:** nothing new. Touches `ChatBotCommand`, ~7 command classes, ~16 processors, `Chateaudatabase.cs`.
**Wording status:** every proposed string below is a **first draft pending the owner's review** (per the standing rule that all changed user-facing text is approved before merge). Tone follows [Style-Guide.md](../Style-Guide.md).

---

## B1 — Remove the construction warning

[`Chateaudatabase.cs:43`](../../FChatDicebot/Database/Chateaudatabase.cs:43) appends a bold sentence to the first-registration welcome. **Decision (confirmed): delete it outright**, no replacement.

**Before:**
> Welcome to the Chateau, {displayName}! We hope you enjoy your time here. Use !help for a list of commands, and feel free to ask around. I promise, we only bite if you want us to~ \n[b]The bot is currently under construction. If you are seeing this message, your profile will almost certainly be reset before release, so don't get too attached![/b]

**After:**
> Welcome to the Chateau, {displayName}! We hope you enjoy your time here. Use !help for a list of commands, and feel free to ask around. I promise, we only bite if you want us to~

One-line change. No test impact beyond any assertion that pins the old string (grep `under construction` in tests — none found at spec time).

---

## B2 — Real cooldown facts in the seriousness warning

### Problem

The canonical warning `[b]This should not be taken lightly, and can not be done frequently.[/b]` tells the reader **neither how often nor whom it binds**. The facts already exist on each command (`CooldownDuration` + `CooldownAppliesTo`) but are (a) free-text and drift-prone, (b) not surfaced to players, and (c) in 7 commands the warning is **inlined in the command** rather than composed by the processor, so there are two copies to keep in sync.

### The four limiter shapes

The "taken lightly" interactions do **not** share one cooldown model. They fall into four shapes, and the frequency wording differs per shape:

| Shape | Interactions | What the prompt can state |
|---|---|---|
| **A — Cooldown, both parties** | bond, breed (1 day) | the rule only (cooldown fires *after* consent) |
| **B — Cooldown, recipient** | employ, mark, objectify, plant, consume (1 day); monsterize, petrify, rename (7 days) | the rule only |
| **C — Per-axis cooldown, initiator** | break (per bodypart), curse (per curse), dose (per vice), infest (per parasite), odorize (per scent) — all 7 days | the rule, scoped to the axis |
| **D — Magnitude quota, initiator** | corrupt / purify (10 magnitude/day per recipient) | the rule **+ consumed-so-far** |

Note shapes A/B/C are **hard cooldowns that start only on consent**, so the prompt can only state the *rule* — there is no live countdown to show (nothing is spent yet). Only shape D (corrupt/purify) has a live spent/remaining value at consent time, because its quota is pre-clamped at command time.

### Frequency-style wording per shape (confirmed: frequency style, not duration style)

Reader is the **recipient** → addressed as "you"; the initiator is referred to by **display name** (not "they"). `{verb}` infinitive, `{verbed}` recipient-framed past participle. Period rendering: 1 day → "once per day", 7 days → "once per week".

- **A (both):** `You can only {verb-phrase} between the two of you once per day.`
  - bond → "You can only declare a bond between the two of you once per day." breed → "You can only breed between the two of you once per day."
- **B (recipient):** `You can only be {verbed} once per {day|week}.`
  - employ → "You can only be employed once per day." · mark → "once per day" · objectify → "once per day" · plant → "once per day" · consume → "once per day" · monsterize → "once per week" · petrify → "once per week" · rename → "once per week".
- **C (per-axis, initiator):** `{initiator} can only {verb} you with a given {axis} once per week.`
  - dose → "{Alice} can only dose you with a given vice once per week." · curse → "{Alice} can only afflict you with a given curse once per week." · infest → "{Alice} can only infest you with a given parasite once per week." · odorize → "{Alice} can only saturate you with a given scent once per week." · break → "{Alice} can only break a given part of you once per week."
- **D (quota, initiator):** `{initiator} can only {corrupt|purify} you by 10 per day.` + (when already spent) ` {initiator} has already {corrupted|purified} you by {used} today.`

> Initiator referred to by display name in shapes C and D (the initiator-bound clauses) per owner direction — no "they" pronoun. Shapes A/B are recipient-framed ("you") and carry no initiator reference.

### Bespoke implication clauses (kept, folded into the bold block)

Five interactions already carry a hand-written implication beyond the cooldown. These stay (they're the "much more unique" details), now sitting **inside** the same bold block, after the frequency clause:

| Interaction | Existing implication clause (verbatim today) | Disposition |
|---|---|---|
| break | "While broken, {recipient}'s sore {part} will prevent them from {blockedVerbs}, and might be noticed during other interactions as well." | keep; re-point to "your sore {part} will keep you from…" (recipient-framed) |
| infest | "This parasite has a chance to spread to anyone you have a non-casual interaction with, and signs of your infection might be noticeable in any interaction. You can !purge at the cost of {cost}, with a {grace}-hour grace period during which !purge has no cost." | keep verbatim |
| curse | "To remove this curse, the recipient will need to !cleanse at the cost of {cost}." | keep; re-point to "you'll need to !cleanse…" |
| dose | "Addictive cravings can show up in any interaction until you !detox at a hefty cost. Everyone will know when you're satisfying an addictive craving." | keep verbatim |
| odorize | "The scent will linger on them through several interactions before it fades. {recipient} may !wash once per day…" (currently **outside** the bold) | move inside bold, recipient-framed |
| monsterize | "The capabilities of your new body might be referenced in flavor text and !work checks." | keep verbatim |
| rename | "The new name will show almost every time the Chateau mentions you." | keep verbatim |

### Resulting bold-block grammar

```
[b]This should not be taken lightly. {frequencyClause}{ consumedClause?}{ implicationClause?}[/b]
```

`"…, and can not be done frequently."` is **replaced** by the concrete `{frequencyClause}`. The non-bold parts of every prompt (intent verb, recipient-framed `to ___?` ending) are **unchanged**.

**Worked example (corrupt, merges B2 + B3):**
> {Alice} wants to corrupt {Bob}, increasing their corruption by 3! [b]This should not be taken lightly. Corruption will be visible in most interactions, and in anything milked from you. {Alice} can only corrupt you by 10 per day. {Alice} has already corrupted you by 2 today.[/b] Do you !consent to being corrupted?

### B2.3 — Normalize the cooldown metadata

Replace the two free-text fields with a structured value object so wording can be generated and never drifts:

```csharp
enum CooldownKind   { None, Cooldown, MagnitudeQuota }
enum CooldownBinds  { None, Initiator, Recipient, Both, Pair }   // Pair = the specific (init,recip) pair, e.g. milk
class CooldownSpec {
    CooldownKind  Kind;
    CooldownBinds Binds;
    int           PeriodDays;     // 1, 7, …  (renders day/week)
    string        Scope;         // null | "vice" | "curse" | "parasite" | "scent" | "bodypart"  → "per {Scope}"
    int           QuotaMagnitude; // quota shapes only (corrupt/purify = 10)
}
```

Free-text → structured migration for the warned set (and the stray casual/odd ones for consistency, even though they don't show the warning):

| Interaction | Current `CooldownDuration` / `CooldownAppliesTo` | → Kind / Binds / Period / Scope |
|---|---|---|
| bond | "1 Day" / "both initiator and recipient" | Cooldown / Both / 1 / — |
| breed | "1 Day" / "both" | Cooldown / Both / 1 / — |
| employ, mark, objectify, plant, consume | "1 Day" / "recipient" | Cooldown / Recipient / 1 / — |
| monsterize, petrify, rename | "7 days" / "recipient" | Cooldown / Recipient / 7 / — |
| break | "7 days" / "initiator (per bodypart per recipient)" | Cooldown / Initiator / 7 / bodypart |
| curse | "7 days" / "initiator (per curse per recipient)" | Cooldown / Initiator / 7 / curse |
| dose | "7 days" / "initiator (per vice per recipient)" | Cooldown / Initiator / 7 / vice |
| infest | "7 days" / "initiator (per parasite per recipient)" | Cooldown / Initiator / 7 / parasite |
| odorize | "7 days" / "initiator (per scent per recipient)" | Cooldown / Initiator / 7 / scent |
| corrupt / purify | "Daily quota (10 magnitude per recipient)" / "initiator" | MagnitudeQuota / Initiator / 1 / — (Quota 10) |
| (casual: kiss/cuddle/etc.) | "30 Minutes" / "both…" | Cooldown / Both / — (no warning shown) |
| milk | "1 day, per-pair" / "both…" | Cooldown / Pair / 1 / — |
| spank | "30 minutes (but can still interact…)" | normalize note carried separately; out of scope for the bold block |

`CooldownDuration` / `CooldownAppliesTo` strings (used in help/`!whatis`) become **derived** from `CooldownSpec` via a formatter so help text and the consent warning can't diverge.

> ⚠ **Scope to verify at implementation:** for shapes A/B I read `CooldownAppliesTo` but not the exact `timers[...]` key granularity (per-user vs per-pair vs per-other-party). The wording above assumes the natural reading; confirm each timer key before finalizing the string (e.g. is bond's "both" cooldown global-per-user or per-pair?). The shape-C/D scopes are already explicit in code.

### B2.4 — Single source of truth

1. **Kill the 7 inlined warnings.** These commands build the consent prompt inline instead of delegating: `ChateauEmploy`, `ChateauMark`, `ChateauPlant`, `ChateauPetrify`, `ChateauConsume`, `ChateauBreed`, `ChateauRename`. Convert each to call `processor.GetConsentWarning(...)` the way [`ChateauBond.cs:70`](../../FChatDicebot/BotCommands/ChateauBond.cs:70) already does. After this, **`GetConsentWarning` on the processor is the only place a consent prompt is composed.**
2. **One warning composer.** Add a static `ConsentWarningText.SeriousnessBlock(CooldownSpec, initiatorName, recipientName, sideContext)` that returns the bold block (frequency + consumed + caller-supplied implication). Each `GetConsentWarning` calls it and appends its own bespoke implication clause.
3. **`CooldownSpec` lives on the processor (confirmed).** Author it on the **processor** (the canonical interaction unit, already holds `InvestmentLevel`), expose via `InteractionProcessorRegistry`, and have the command's help fields format from it. System commands without a processor (`!work`, `!volunteer`, `!rest`, `!wash`) keep their own free-text — they show no consent warning.

---

## B3 — Corruption visibility disclosure

### How corruption surfaces today (confirmed)

- **Aura on other interactions:** [`CorruptionStatusContributor`](../../FChatDicebot/InteractionProcessors/StatusEffectContributors/CorruptionStatusContributor.cs) appends an "aura of corruption/purity" fragment to the **completion** message of *other* interactions, in bands, once |corruption| ≥ 10.
- **Milk product:** [`MilkProcessor`](../../FChatDicebot/InteractionProcessors/Involved/MilkProcessor.cs) tags bottles with `CorruptTag` / `PurifiedTag` from the recipient's corruption (threshold ±10), giving milked product corrupt/purified flavor. **Symmetric** — both a corrupt tag and a purified tag exist.

### Decisions (confirmed)

- **Where:** the corrupt/purify **consent prompt** and the `!corrupt` / `!purify` **help text** (`LongDescription`). Not the completion message.
- **How spoilery:** just say it's **visible** — **no** threshold (±10) or banding detail.
- **Other implications:** the **only** extra implication is `!milk`. (Corruption is otherwise cosmetic — no `!work` conditional reads it. The warning must not over-promise mechanical consequences.)
- **Direction:** warn about the **direction currently in play** only. `!corrupt {+}` / `!purify {-}` move toward **corruption** → speak of "corruption". `!corrupt {-}` / `!purify {+}` move toward **purity** → speak of "purity". The effective verb is already resolved before `GetConsentWarning` runs.

### Proposed clause (the implication slot of the bold block)

- Corrupting direction: `Corruption will be visible in most interactions, and in anything milked from you.`
- Purifying direction: `Purity will be visible in most interactions, and in anything milked from you.`

### Proposed help-text addition (`LongDescription`, both commands)

Append: `Once it builds up, a resident's corruption (or purity) becomes visible in most interactions and flavors anything milked from them.`

### Clamp behavior (confirmed: unchanged)

The over-budget clamp stays a **private message to the initiator** at command time ([`CorruptionCommandSupport.cs:83`](../../FChatDicebot/InteractionProcessors/Commitment/CorruptionCommandSupport.cs:83)) with the TOCTOU twin at process time. The recipient's consent prompt only ever shows the already-clamped magnitude. The `{consumedClause}` ("already corrupted you by X today") is **informational** and separate from the clamp.

---

## Confirmed defaults

1. **`{consumedClause}` suppressed when used = 0** (confirmed) — show only the rule line ("{Alice} can only corrupt you by 10 per day.") until there's actual prior usage, avoiding a clunky "already corrupted you by 0".
2. **7-day cooldown → "once per week"** (confirmed) — frequency phrasing, not "once every 7 days".
3. **4096-char limit:** consent prompts already interpolate display names, and rename targets can exceed 100 chars. The frequency clause is short and adds no name repetition, so headroom is fine; no truncation logic proposed.
4. **No "UTC"** anywhere — "per day" / "per week" only.

## Decisions — all resolved with the owner

- B1: delete, no replacement. ✓
- B2 frequency wording (A/B/C/D): approved; shape A uses "between the two of you"; shapes C/D use the initiator's display name, not "they". ✓
- B2 `CooldownSpec`: lives on the processor. ✓
- B3: consent prompt + help text, direction-adaptive, milk mention, no thresholds, initiator named. ✓
- Defaults #1 and #2: accepted. ✓

**Only remaining implementation-time caveat:** for shapes A/B, confirm each interaction's exact `timers[...]` key granularity (per-user vs per-pair vs per-other-party) before finalizing its string — `CooldownAppliesTo` gives the bound party but not always the key scope. Shapes C/D scopes are already explicit in code.

## Files in scope

- `FChatDicebot/Database/Chateaudatabase.cs` — B1.
- `FChatDicebot/BotCommands/Base/ChatBotCommand.cs` — `CooldownSpec` + derived help formatter.
- `FChatDicebot/BotCommands/{Employ,Mark,Plant,Petrify,Consume,Breed,Rename}.cs` — drop inline warnings, delegate to processor (B2.4).
- ~16 processors' `GetConsentWarning` + a new `ConsentWarningText` helper — B2 composition.
- `CorruptionProcessor.GetConsentWarning` + `ChateauCorrupt`/`ChateauPurify` `LongDescription` — B3.
- Tests: update any assertion pinning "and can not be done frequently" (e.g. `Curseprocessortests.cs:299`); add coverage for each frequency shape and the corrupt/purify visibility clause (both directions, consumed = 0 and > 0).
