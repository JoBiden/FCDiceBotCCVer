# `!break` + `!rest`

Break a part of the recipient so it can't be used for several days. `!rest` accelerates healing by skipping a day of work.

**Investment level:** Consequence
**Reversal:** `!rest` (skipping work to heal); also auto-heals one level per day.
**Depends on:** [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md)

## Breakable parts

User-confirmed list (no freeform). New `Identifier` records with category `"break"`:

```
ear, pussy, mouth, breast, ass, eye, nose, body, dick, ball
```

(Note: "mind" is **not** breakable, despite earlier discussion. Drop from any code/docs.)

## `!break` command syntax

```
!break [user]Bob[/user] {part} {days?}
```

- `days` defaults to **3** if omitted.
- `days` clamped to **1..30**.

## `!break` validation

- Recipient registered.
- `part` resolves in the break-tagged `Identifier` catalog.
- `days` is an integer in [1, 30] after clamping (clamp silently — the user can write 50 and the system caps at 30, but warn in the consent flow that it was clamped).
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

A scheduled job (or lazy on-read) runs once per UTC day:

- For each `BreakInstance` on every profile, if `LastTickedAt < UtcNow.Date`, decrement `Severity` by 1 and set `LastTickedAt = UtcNow.Date`.
- If `Severity <= 0`, remove the instance.

Lazy implementation is preferred: every read of `lists["breaks"]` first runs the tick. This avoids needing a scheduler.

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

## Status-effect contributions

`BreakStatusContributor` emits **validation blockers** based on the proposed part-to-interaction map below. **DRAFT — confirm with user before implementation.**

| Broken part | Blocks (recipient role) | Blocks (initiator role) | Modifier flavor (when not blocking) |
|-------------|------------------------|------------------------|-------------------------------------|
| ear | — | — | "...{recipient} mishears the rhythm a bit." |
| pussy | `!breed` (recipient), `!milk` (when substance is `cum`/`fluid` and gendered female) | — | "...{recipient} winces, careful with their tender pussy." |
| mouth | `!kiss`, `!feed`, `!climaxfor` (when other-target & oral framing), `!milk` (oral substances) | — | — |
| breast | `!milk` (substance `milk`/`breast_milk`) | — | "...{recipient} keeps their bandaged chest carefully out of the way." |
| ass | `!breed` (recipient when gendered male/futa) | — | — |
| eye | — | — | "...{recipient} squints, eye still tender." |
| nose | — (but suppresses Odorize fragments referring to *this profile*) | — | — |
| body | `!cuddle`, `!handhold`, `!spank` | — | "...{recipient} winces from the bruising." |
| dick | — | `!breed`, `!climaxfor` (when initiator has dick) | — |
| ball | — | `!breed`, `!climaxfor` (when initiator has ball) | — |

Notes on the table:

- "Modifier flavor" only fires when no blocker is triggered for that interaction — i.e. the interaction goes through but with the flavor appended.
- Gendered constraints (`pussy` blocking only when "substance is fluid and gendered female", etc.) require profile gender data. v1 fallback: ignore gender, apply the block based on substance string match alone. Improve later.
- `nose` is a special case: it does not block any interaction but **suppresses scent contributions from `OdorizeStatusContributor` on this profile** (a broken-nose person can't smell the room). Implementation: contributor checks for `nose` in the recipient's breaks; if present, returns no fragments for *that profile only*.

The full validation table needs the user's sign-off before this spec lands in code. Implementation Claude must surface this section and confirm with the user before writing `BreakStatusContributor`.

## Persistence

- Profile: `lists["breaks"]`.
- Profile timers: `rest_used`, `work_blocked`.

## Tests

- `BreakProcessorTests.cs`: persists break with correct fields; default 3 days; clamp at 30; 7-day cooldown set.
- `BreakAutoHealTests.cs`: lazy tick decrements severity once per UTC day; Severity 0 removes entry.
- `BreakStatusContributorTests.cs`: per the table above, each broken part emits the correct blockers/fragments. Nose suppresses Odorize fragments for the profile.
- `RestCommandTests.cs`: heals 1 extra level; blocks work for the day; once-per-day enforced.

## Assumptions

- The blocking matrix above is **draft** and requires user confirmation before code lands.
- Gendered blocks fall back to substance-string match in v1.
- Lazy tick on read; a scheduled daily job is acceptable but not required.
- `mind` is excluded — confirmed not breakable.

## Files to create/modify

- `FChatDicebot/Model/BreakInstance.cs` *(new)*
- `FChatDicebot/InteractionProcessors/Consequence/BreakProcessor.cs` *(new)*
- `FChatDicebot/BotCommands/Break.cs` *(new)*
- `FChatDicebot/BotCommands/Rest.cs` *(new)*
- `FChatDicebot/InteractionProcessors/StatusEffectContributors/BreakStatusContributor.cs` *(new)*
- `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs` *(modify — register)*
- `FChatDicebot/InteractionProcessors/StatusEffectRegistry.cs` *(modify — register)*
- Migration / seed: ensure the 10 break parts exist in the `Identifiers` catalog with category `"break"`.
- `FChatDicebot.Tests/Unit/BreakProcessorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/BreakAutoHealTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/BreakStatusContributorTests.cs` *(new)*
- `FChatDicebot.Tests/Unit/RestCommandTests.cs` *(new)*
