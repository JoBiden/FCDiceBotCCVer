# Employer Earnings — `!work` kickback + `!business`

**Status:** Proposed (design-complete, owner-reviewed 2026-06-21).
**Feature-Requests source:** "if someone is !employed by another (not themselves), give their employer some currency when the employee works… should also include some sort of command for employers to see how much employees have earned them" (B6).

> The sibling request B8 ("in-system flow to walk users through creating a !work duty") was **resolved out-of-band** — there is now a short tutorial paragraph on the Chateau Contract profile, also reachable via `!modmessage duty`, and no in-bot builder will be implemented. It is intentionally not specced here.

---

## Overview

Today `!work` ([ChateauWork.cs](../../../FChatDicebot/BotCommands/ChateauWork.cs)) credits the **worker's** wallet only. The `!employ` interaction already records the employer on the recipient as `characteristics["employer"]` ([EmployProcessor.cs:53](../../../FChatDicebot/InteractionProcessors/Commitment/EmployProcessor.cs)). This feature adds:

1. **A MANOR kickback** — when a resident employed by *someone else* completes a `!work` duty, their employer is credited a bonus **on top of** the worker's reward (the worker is unaffected). Lore-consistent: `!bank` already tells players that employers are never billed and "additional resident payroll is covered by the Chateau's *MANOR*".
2. **A per-employer ledger** of how much each employee has earned them, broken down by employee and by currency.
3. **`!business`** — a read-only command an employer runs to see that ledger.

This is **not** an interaction: no consent flow, no investment level, no status-effect hook, no reversal command. It is a passive hook on `!work` plus an information command, in the mold of `!payroll` / `!economics`.

---

## Behavior — the `!work` kickback hook

Lives inside the reward-granting block of `ChateauWork.cs` (currently [lines 79–104](../../../FChatDicebot/BotCommands/ChateauWork.cs), the `foreach … rewardList` that rolls `rewardAmount` / `rewardEntry.Value.currency`). After the worker's own reward is resolved:

**Eligibility.** Pay the employer only when **all** hold:
- `userProfile.characteristics` contains `"employer"`, and
- `characteristics["employer"] != characterName` (self-employed earns nobody a cut — B6 "not themselves"), and
- the employer's profile still exists (`Database.GetProfile(employer) != null` — defensive; skip silently if gone).

**Amount.** A flat **25 %** of the worker's *rolled* reward, floored, minimum 1 when the roll was positive, in the **same currency** the worker just rolled:

```
employerCut = rewardAmount > 0
    ? Math.Max(1, (int)Math.Floor(rewardAmount * 0.25))
    : 0;
```

- The cut is computed from the **rolled** `rewardAmount`, **independently of the worker's poverty curse.** If the worker is poverty-cursed (their own reward vanishes), the employer is *still* paid — "only the person with the poverty curse is impacted" (B6.3). The worker's curse impacts the worker; it does not reach the employer.
- A rolled reward of 0 produces no cut.

**Employer's own poverty curse.** Load the employer's curses (`CurseInstance.LoadAll(employerProfile)`, same check already used for the worker at [ChateauWork.cs:77](../../../FChatDicebot/BotCommands/ChateauWork.cs)). If the **employer** carries `"poverty"`, the kickback vanishes for them: **no wallet credit, no ledger increment,** and the worker's PM omits the kickback line (we neither pay it nor leak the employer's curse to the worker).

**Effects when paid (employer not cursed):**
1. Credit the employer's wallet: `employerProfile.currencies[currency] += employerCut` (add the key if absent — same pattern as the worker credit).
2. Increment the ledger (see Persistence): `employeeEarnings[workerUserName][currency] += employerCut`.
3. `Database.SetProfile(employer, employerProfile)` — a **second** profile save in addition to the worker save already at [ChateauWork.cs:125](../../../FChatDicebot/BotCommands/ChateauWork.cs). (Command handling is serial in this bot, consistent with every other multi-profile write here; no new concurrency guard needed.)

**Scope: `!work` only, not `!volunteer`.** Volunteering is the worker exploring *other* careers as a personal side-gig ("On top of working their primary job, residents can also !volunteer once a day to explore other careers"), not doing their job for their boss — so [ChateauVolunteer.cs](../../../FChatDicebot/BotCommands/ChateauVolunteer.cs) gets **no** kickback hook. *(Decision made by default — flag if you want volunteer income to kick back too.)*

**Worker-facing line.** When the employer is paid, append one short line to the worker's existing `!work` PM (the worker invoked the bot, so this is TOS-safe; the *employer* is never push-notified and learns via `!business`). Suppressed entirely when the employer's curse voids the cut.

> **Owner provided wording:**
> "Your employer {employer} also gets [b]{N} {currency}[/b] for your diligent work, courtesy of the Chateau MANOR."

TOS note: we may **not** PM the employer about this in real time (they didn't invoke the bot). `!business` is the pull-based channel by which they discover it.

---

## Persistence shape

One new field on `Profile` ([ChateauDB.cs:12](../../../FChatDicebot/Model/ChateauDB.cs)), nested so the view's "by employee, then by currency" grouping is direct and no key-delimiter parsing is needed:

```csharp
// Lifetime MANOR kickbacks this profile has earned as an EMPLOYER, keyed by
// employee userName -> currency -> cumulative amount. Written by !work when an
// employee (employed by someone other than themselves) completes a duty; read
// by !business. Persists across employment changes (it is history), so a former
// employee's entry survives if they later change jobs or bosses.
[BsonIgnoreIfNull]
public Dictionary<string, Dictionary<string, int>> employeeEarnings { get; set; }
    = new Dictionary<string, Dictionary<string, int>>();
```

- Keyed by the employee's **`userName`** (login name), not `displayName`, so it survives renames. The view resolves display names at render time via `MonDB.getDisplayName(userName)`, falling back to the stored key if the profile is gone.
- `[BsonIgnoreIfNull]` keeps the field out of existing profile documents until first written — no migration needed (same approach as `dailyMagnitudes` / `dailyClimaxCounts`).
- Lifetime cumulative; never decremented or reset in v1.

---

## The `!business` command

New `ChatBotCommand`: [ChateauBusiness.cs] under `BotCommands/`, modeled on `!payroll` / `!economics` (static `BuildBusiness(Profile)` helper for unit-testing, thin `Run` that PMs the result).

| Field | Value |
|-------|-------|
| `Name` | `business` |
| `Aliases` | `{ }` (none in v1; subject to the standing alias audit — `business` itself collides with nothing) |
| `Category` | `Information` |
| `ShortDescription` | "See what your employees have earned you" |
| `Usage` | `!business` |
| `RelatedCommands` | `employ`, `work`, `bank`, `payroll` |
| `CooldownDuration` / `AppliesTo` | `null` (read-only) |
| `RequireChannel` | `false` (PM result) |

**Scope:** self-only in v1 — you see your own `employeeEarnings`. *(Admin-queryable `!business [user]` is a possible later extension; flag if you want it now.)*

**Render.** Group the invoker's `employeeEarnings`:
- One row per employee (display name resolved), listing each currency they've earned you as `[b]{n} {currency}[/b]` chips joined with ` | `, sorted by that employee's total descending then name.
- A closing **totals** line summing across all employees, per currency.
- Drop any employee/currency entry whose amount is `<= 0`.

**Empty state** (no ledger entries — you've never had an employee earn you anything, including if you've only ever self-employed):
> "None of your employees have earned you anything yet. Use !employ to put someone on the payroll, and they'll start sending MANOR kickbacks your way every time they !work."

> **For owner wording review** — populated form (illustrative):
> ```
> Here's what your employees have earned you:
>   Alice: [b]42 copper[/b] | [b]3 silver[/b]
>   Bob: [b]18 copper[/b]
> Total earned from all employees: [b]60 copper[/b] | [b]3 silver[/b]
> ```

(Standard F-Chat constraints apply: 4096-char cap, `[spoiler]` fallback past ~500 chars if a prolific employer's list runs long — match whatever the `!bank`/`!payroll` outputs already do.)

---

## Files to create / modify

**Create:**
- `FChatDicebot/BotCommands/ChateauBusiness.cs` — the `!business` command (+ static `BuildBusiness(Profile)` helper).
- `FChatDicebot.Tests/Unit/Chateaubusinesstests.cs` — `BuildBusiness` rendering tests.

**Modify:**
- `FChatDicebot/Model/ChateauDB.cs` — add `employeeEarnings` to `Profile`.
- `FChatDicebot/BotCommands/ChateauWork.cs` — the kickback hook in the reward block; worker-facing kickback line.
- `FChatDicebot/BotCommands/ChateauHelp.cs` (and any help/category registration the other Information commands use) — register `!business`.
- `FChatDicebot.Tests/Builders/Profilebuilder.cs` — optional `WithEmployeeEarnings(...)` helper for tests.
- `wiki-docs/Feature-Requests.md` — B6 bullet retires on ship (B8 already removed).

No DB-layer (`Chateaudatabase.cs` / `Ichateaudatabase.cs`) changes — `employeeEarnings` rides on the existing `GetProfile` / `SetProfile` serialization, and `!business` reads only the invoker's profile.

---

## Tests

`BuildBusiness(Profile)`:
- Empty ledger → empty-state string.
- Single employee, single currency → one row + matching total.
- Multiple employees, overlapping + disjoint currencies → per-employee rows, correct per-currency grand totals, sorted by employee total desc.
- Zero-valued entries pruned.
- Display-name resolution falls back to the stored userName when the profile is missing.

`!work` kickback (extend [Chateauworktests] if present, else a focused test around the extracted helper):
- Employed-by-other, employer uncursed → employer wallet +`max(1, floor(0.25*reward))`, ledger +same, worker reward unchanged.
- Self-employed → no kickback, no ledger entry.
- No employer key → no kickback.
- Worker poverty-cursed, employer not → worker voided, **employer still paid** off the rolled amount.
- Employer poverty-cursed → no wallet credit, no ledger entry, no worker-facing kickback line.
- Rolled reward 0 → cut 0.
- `floor`/`min 1`: reward 1 → cut 1; reward 2 → cut 1; reward 100 → cut 25.

> The kickback math is currently inline in `ChateauWork.Run`. Extract `(employerCut, currency)` computation into a small testable helper (e.g. `static int EmployerCut(int rewardAmount)`) so the rounding rules can be unit-tested without standing up a full work session.

---

## Decisions resolved (owner, 2026-06-21)

- **B6.1** Kickback is a MANOR-funded **bonus on top**; worker keeps their full reward.
- **B6.2** **25 %** of the worker's rolled reward, `floor`, **min 1**, same currency.
- **B6.3** Only the poverty-cursed party is impacted: worker's curse → worker loses reward, **employer still paid**; employer's curse → employer's cut voided, worker unaffected.
- **B6.4** Ledger broken down by **both employee and currency**, summable to totals at display; lifetime cumulative; persists across employment changes.
- **B6.5** View command is **`!business`**, self-only, per-employee × per-currency with totals.

## Open items

- **Worker-facing kickback line** and **`!business` strings** are first drafts pending owner wording approval (house rule: surface every changed user-facing string before declaring done).
- **`!volunteer` exclusion** decided by default (work-only) — override if volunteer income should also kick back.
- **Admin-queryable `!business [user]`** left out of v1 — add if wanted.
- Possible later **employer-milestone titles** (e.g. lifetime currency earned from employees) — not in scope.

## Assumptions

- `CurseInstance.LoadAll(profile)` + `"poverty"` match is the canonical poverty check (already used in `ChateauWork`).
- Kickback currency follows whatever the duty rolled; no currency conversion (denominations stay independent, per `ChateauCurrency`).
- F-Chat output constraints match the existing `!bank` / `!payroll` outputs (4096 char cap, BBCode, `[spoiler]` fallback if long).
