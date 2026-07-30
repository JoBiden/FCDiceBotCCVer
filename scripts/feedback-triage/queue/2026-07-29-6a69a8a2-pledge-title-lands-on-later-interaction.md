# 6a69a8a2 — "Milking Fia fulfilled a pledge" — the pledge title fires on the next unrelated interaction

| | |
|---|---|
| **Feedback id** | `6a69a8a25b3b20a4bbe5cd64` |
| **Submitted** | `2026-07-29T07:15:46Z` by **Queen Contract** (`Queen Contract`) |
| **Source** | channel `ADH-ac1885cd73f31adfaefb` |
| **Tagged** | `bug` |
| **Verdict** | `bug` |
| **Recommendation** | `fix` |
| **Confidence** | high — reproduced from the live data, root cause identified |
| **Effort** | trivial (one call site, plus a sibling) |

## What they said

> pledge might be broken, milking Fia fulfilled a pledge after pledging to !bond pet Celeste

## What it means

They pledged to bond with Celeste, later milked Fia, and the bot announced a pledge-related
title right after the milk — which reads as "that milk fulfilled my pledge to Celeste".

**No pledge was actually fulfilled.** The pledge is still `active` in the DB and the profile has
no `pledgesfulfilled` count. What they saw was the *"Made A Promise"* title — earned by making
the pledge 40 minutes earlier — being granted and announced during the milk.

## Assessment

Timeline from the live `ChateauDb` (all 2026-07-29 UTC):

| Time | Event |
|---|---|
| 06:28:55 | `!pledge` → `Pledges` doc `6a699da7`: pledger Queen Contract, pledgee Celeste Kristoph, type `bond`, **identifier `null`**, status `active` (still active today). `pledgesactive` → 1. |
| 07:11:04 | `!milk` Queen Contract → Fia Violafalki (`Interactions` `6a69a78f`) |
| **07:11:11** | **title "Made A Promise" granted** (`RegisteredProfiles.titles`) — 7 seconds after the milk, 42 minutes after the pledge |
| 07:15:46 | this feedback submitted |

Root cause: [ChateauPledge.cs:115](../../../FChatDicebot/BotCommands/ChateauPledge.cs:115) increments
`pledgesactive` but never calls `ChateauSystemTitles.CheckAndGrantTitles`. The count-title sweep
only runs on the interaction consent path
([ChateauConsent.cs:270](../../../FChatDicebot/BotCommands/ChateauConsent.cs:270)) and in `!work`,
`!volunteer`, and the climax support. So a pledge's title sits unclaimed until the resident's
next consented interaction — whatever and whoever that is — and is then announced on top of it.

`!abandonpledge` has the same shape: it increments `pledgesabandoned` with no title check, so
"Broke Their Promise" will likewise surface on some later unrelated interaction.

Fulfilment itself is sound: `TryMarkPledgeFulfilled`
([ChateauInteractionHandler.cs:22](../../../FChatDicebot/ChateauInteractionHandler.cs:22)) only
fires for an interaction stamped with a `pledgeId` by `!fulfill`, and it runs just before the
title sweep on the same consent path, so fulfilment titles land correctly.

- **Reproduces / confirmed:** yes — grant timestamp lands 7s after the milk, 42m after the pledge
- **Related code:** `ChateauPledge.cs:115`, `ChateauAbandonPledge.cs`, `ChateauSystemTitles.cs:243`, `ChateauConsent.cs:270`
- **Related specs / docs:** none for pledges specifically
- **Duplicate of:** none
- **Already fixed since submission:** no

### Second, separate defect found along the way

`!pledge` throws away the identifier: [ChateauPledge.cs:105](../../../FChatDicebot/BotCommands/ChateauPledge.cs:105)
hardcodes `identifier = null` with a "we'll add identifier support later" comment, and the
`else` branch at line 89 that should read it is empty. The submitter pledged **`bond pet`**; the
stored pledge is plain `bond`. `!fulfill` then passes that null identifier into the interaction,
so fulfilling produces a bond with no identifier rather than the pet bond they promised. This
is why their report says "bond pet" while the DB says `bond`.

## Recommended action

Call `ChateauSystemTitles.CheckAndGrantTitles(characterName)` at the end of a successful
`!pledge` and append the notification to the channel message, exactly as `!work`
([ChateauWork.cs:164](../../../FChatDicebot/BotCommands/ChateauWork.cs:164)) and `!volunteer` do.
Same in `!abandonpledge`. Titles then land on the action that earned them, and nothing else about
pledges changes.

The dropped identifier is a real but distinct bug — bigger, since it touches `!pledge` parsing,
the stored `Pledge`, `!fulfill`'s consent warning, and `!viewpledges` display.

## Decisions needed from the owner

1. **How wide should the title-timing fix go?**
   - **`!pledge` and `!abandonpledge`** (recommended) — fixes the reported bug and its twin; two call sites, no behavior change beyond when the banner appears.
   - **`!pledge` only** — narrowest possible fix; leaves the abandon case to resurface as its own report later.
   - **Audit every count-changing command** — sweep all `incrementCount` callers for missing title checks and fix them together. Bigger diff, likely finds more of these, better done as its own item.

2. **The dropped pledge identifier — now or separately?**
   - **Separate item** (recommended) — keep this PR to the one-line timing fix; I write the identifier bug up as its own dossier with its own design questions (how `!fulfill` should match a pledge whose identifier differs, what `!viewpledges` shows).
   - **Same PR** — fix both together; the PR grows to pledge parsing, storage, fulfil matching, and display.
   - **Leave it** — the TODO stands and `!pledge <type> <identifier>` keeps silently dropping the identifier.

## User-facing text that would change

No new strings. The `!pledge` confirmation would gain the existing "Title Time!" notification
appended to it — the same banner `!work` already appends, unchanged in wording. The only visible
difference is *when* residents see it.

| Where | Now | Proposed |
|---|---|---|
| `ChateauPledge.cs:171` channel message | pledge confirmation only; any earned title appears later, attached to an unrelated interaction | pledge confirmation + the title notification it earned, in the same message |

## Owner decision
