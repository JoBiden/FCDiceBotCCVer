# `!break` + `!rest`

Break a part of the recipient so it can't be used for several days. `!rest` accelerates healing by skipping a day of work.

**Status:** Implemented.
**Investment level:** Consequence
**Reversal:** `!rest` (skipping work to heal); also auto-heals one level per day.
**Depends on:** [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md)

## Breakable parts

Break-tagged entries in the `Identifiers` catalog (category `"break"`). The "break" tag denotes "will block something if broken," not just "can be broken" — so the tagged set is curated to the bodyparts that drive actual mechanics.

Currently break-tagged (16):

```
pussy, wing, mouth, hand, foot, breast, ass, arm, leg,
nose, body, torso, dick, ball, tongue, mind
```

Validation reads the tag dynamically. Adding a new break-tagged bodypart later does not require code changes to validation; it does require an entry in the status-effect blocking rules below if it should drive a block.

## `!break` command syntax

```
!break [user]Bob[/user] {part} {days?}
```

- `days` defaults to **3** if omitted.
- `days` clamped to **1..30**. Clamp silently; surface in the consent flow that it was clamped.

## `!break` validation

- Recipient registered.
- `part` resolves to a break-tagged `Identifier`.
- `days` is an integer in [1, 30] after clamping.
- Recipient does not already have this part broken with more time remaining than the new break would set. (If recipient has `mouth` broken with 5 days left and Alice tries `!break mouth 3`, reject — "Bob's mouth is already broken for 5 more days.")

## `!break` processor logic (`BreakProcessor`)

1. Append/replace a `BreakInstance` on `recipient.lists["breaks"]`:
   ```csharp
   public class BreakInstance
   {
       public string Part;
       public int Severity;            // = days when broken; decrements 1/day
       public string BrokenBy;
       public DateTime BrokenAt;
       public DateTime LastTickedAt;   // for daily auto-heal
   }
   ```
2. Set 7-day cooldown on (initiator → recipient → part).
3. Save interaction. Delete pending command.

## Daily auto-heal

Lazy on-read tick: every read of `lists["breaks"]` first runs the tick:

- For each `BreakInstance` on the profile, if `LastTickedAt < UtcNow.Date`, decrement `Severity` by `(UtcNow.Date - LastTickedAt).Days` and set `LastTickedAt = UtcNow.Date`.
- If `Severity <= 0`, remove the instance.

(Lazy is preferred over a scheduled job. A scheduler is acceptable but not required.)

## `!rest` command syntax

```
!rest                    → tries to heal all active breaks by 1 additional level today
!rest {part}             → heals one specific break by 1 extra level
```

## `!rest` validation

- Self-targeted only.
- Caller has at least one active `BreakInstance` (or the named one).
- Caller has not used `!work` today: `caller.timers["work_used_today"]` not set/expired. (The cost is "give up today's work.")
- Caller has not used `!rest` today.

## `!rest` processor logic (`RestCommand`, system command)

1. For each break being rested (or the named one): decrement `Severity` by an additional 1 (on top of the daily auto-tick).
2. Set `caller.timers["rest_used"] = CoolDown { timerEnd = next UTC day midnight }`.
3. Set `caller.timers["work_blocked"] = CoolDown { timerEnd = next UTC day midnight }` so `!work` is also blocked today (the cost).
4. Remove any breaks that hit 0.

## Status-effect contributions — blocking matrix

`BreakStatusContributor` fires on every consent-driven interaction, **including casual** (breaks are a real-world impediment, like Curse disablers — they fire regardless of investment level).

**Semantics of "block":** the interaction does **not** proceed, but a public flavor message fires in the channel explaining why. Blocked interactions are not silent rejections — they always surface what was wrong.

### A. Targeted-bodypart interactions

Any interaction whose command takes a bodypart parameter blocks if that targeted part is broken on the recipient.

- `!golden {part}` — block if `{part}` is broken on the recipient.
- `!mark {part}` *(if/when !mark takes a bodypart parameter — verify during implementation)*
- Any future interaction that takes a bodypart parameter — same rule, by virtue of the contributor checking the parsed bodypart parameter.

### B. Anatomy-locked interactions

Each interaction declares which parts, broken on which role, block it.

| Interaction | Role | Blocks if broken |
|---|---|---|
| `!milk` (substance = `milk`) | recipient | breast |
| `!milk` (substance = `saliva`) | recipient | mouth, tongue |
| `!milk` (substance = `golden` / `pre` / `cum`) | recipient | dick, ball, pussy |
| `!kiss` | recipient | mouth, tongue |
| `!feed` | recipient | mouth, tongue |
| `!climax` | initiator (the climaxing party) | dick, ball, pussy, ass |
| `!climaxfor` | recipient (the climaxing party) | dick, ball, pussy, ass |
| `!cuddle` | recipient | body, torso, arm |
| `!handhold` | recipient | hand |
| `!spank` | recipient | ass, body, torso |
| `!bully` | initiator | body, torso |
| `!breed` | — | **never blocks; flavor only** (see Section D) |

Notes on substance mapping for `!milk`: substances in the catalog map to bodyparts as above. `golden`/`pre`/`cum` collapse to a single recipient-side blocker check across dick/ball/pussy (any broken member of that set blocks).

### C. !train {type} blocking map

| Training | Blocks if broken |
|---|---|
| corset | breast, torso |
| heel | foot |
| anal | ass |
| deepthroat | mouth, tongue |
| ponygirl | body, foot, leg, ass |
| obedience | *(none — willpower-based)* |
| mathematics | mind |
| magic | mind |
| flight | wing |
| instrument | hand |
| dance | foot, leg |

### D. Special cases

- **`nose` broken** → suppresses `OdorizeStatusContributor` fragments referring to that profile. Implementation: `OdorizeStatusContributor` checks for `nose` in the relevant profile's breaks; if present, returns no fragments for that profile.
- **`!breed` flavor** → never blocks. When either party has any of `pussy, ass, dick, ball` broken, append flavor listing those parts (across both parties together):
  > "...who knows how, with their {parts} in the state {it's/they're} in."

  Singular/plural agreement: "it's" when one part, "they're" when two or more.

### E. Untouched by breaks

These interactions never block on breaks and emit no break-related fragments:
`!consume, !petrify, !plant, !corrupt, !bond, !objectify, !entitle, !employ, !monsterize, !rename, !dressup`.

### F. Pass-through flavor (interaction not blocked)

For every consent-driven interaction in scope (i.e., not in section E and not blocked), if the recipient has **any** active breaks, append:

> "...their {parts} are still sore from earlier."

Listing comma-separated, in canonical order (the order they appear in the bodypart catalog), with "and" before the last when there are 2+ parts. Singular/plural verb agreement: "is" for one, "are" for two or more.

This is intentionally **wide** — every active break on the recipient is listed regardless of whether the interaction has any anatomical connection. (Bodyparts not on the break-tagged list can't be broken in the first place, so the list is naturally bounded.)

### Block-replacement flavor wording

When an interaction is blocked under sections A/B/C, the contributor emits a single public message instead of completing the interaction. Template:

> "{recipient/initiator}'s {parts} {is/are} too broken for {interaction-verb}."

Examples:
- "Bob's mouth is too broken for kissing."
- "Bob's body and torso are too broken for cuddling."
- "Alice's dick is too broken to climax."

Per-interaction verb mapping is hardcoded in the contributor (`!kiss` → "kissing", `!feed` → "feeding", `!climax` → "to climax", etc.). All final wording is subject to a style-guide review pass before merge — see [Style-Guide.md](../../Style-Guide.md).

## Persistence

- Profile: `lists["breaks"]`.
- Profile timers: `rest_used`, `work_blocked`.

## Tests

- `BreakProcessorTests.cs`: persists break with correct fields; default 3 days; clamp at 30; 7-day cooldown set; rejects "more time remaining than new break" case.
- `BreakAutoHealTests.cs`: lazy tick decrements severity once per UTC day; multi-day gap collapses correctly; Severity 0 removes entry.
- `BreakStatusContributorTests.cs`:
  - Section A: `!golden eye` doesn't block; `!golden mouth` with mouth broken blocks.
  - Section B: each row of the table — verify the blocker fires for listed parts and not for unlisted parts.
  - Section C: each training, broken-part combo blocks correctly.
  - Section D: nose suppresses Odorize fragments; !breed never blocks but emits "who knows how..." flavor when relevant parts are broken on either party.
  - Section E: each interaction in E proceeds without any break-related fragment even when breaks are active.
  - Section F: pass-through flavor lists all active breaks, comma-separated with "and", correct singular/plural.
  - Casual interactions (`!cuddle`, `!handhold`, `!spank`, `!kiss`, `!bully`) fire blockers normally (no casual exemption).
- `RestCommandTests.cs`: heals 1 extra level; blocks work for the day; once-per-day enforced.

## Assumptions

- Lazy tick on read; scheduled daily job acceptable but not required.
- Block-replacement flavor wording is a draft — final strings go through style-guide review.
- The break tag set is read live from the `Identifiers` catalog. Adding a new break-tagged part later is data-only unless it needs blocking rules, in which case sections A/B/C grow.
- Pass-through flavor lists every active break; no relevance filter and no cap.
- `!breed` is the only interaction with a special non-block flavor template; everything else uses the generic pass-through flavor.

## Files to create/modify

- `FChatDicebot/Model/BreakInstance.cs` *(new)*
- `FChatDicebot/InteractionProcessors/Consequence/BreakProcessor.cs` *(new)*
- `FChatDicebot/BotCommands/Break.cs` *(new)*
- `FChatDicebot/BotCommands/Rest.cs` *(new)*
- `FChatDicebot/InteractionProcessors/StatusEffectContributors/BreakStatusContributor.cs` *(new)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` *(modify — register)*
- `FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs` *(modify — register)*
- `FChatDicebot/InteractionProcessors/Consequence/OdorizeProcessor.cs` or its contributor *(modify — nose suppression hook)*
- `FChatDicebot.Tests/Unit/BreakProcessorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/BreakAutoHealTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/BreakStatusContributorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/RestCommandTests.cs` *(new)*
