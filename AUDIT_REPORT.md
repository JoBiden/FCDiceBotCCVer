# FCDiceBot / Chateau Contract — Full Codebase Audit

_Audited by a multi-agent Fable 5 pass: 10 scoped bug-finders + adversarial verification + architecture & test-quality judges. Branch `claude/affectionate-clarke-0f98fb` (fork point `e8e8e03`)._

## Scope & method
- **In scope:** the active **Chateau Contract** interaction system (InteractionProcessors, Chateau* commands, ChateauCurrency/wager, MonDB/ChateauDatabase, Profile model) plus shared infrastructure (dispatch, message queue, persistence, time).
- **Frozen (respected):** the legacy dice-games framework. Its TODOs/NYI/design debt were not reported; shared-infra bugs that reach the active system were.
- **Not present on this branch:** the B12 random-events code (RandomEventEngine / AllowRandomEvents) postdates the fork point. It was audited separately on 2026-07-03 against `origin/VibeCodedSystems` (commit `7c67a53`, PR #43) — see the **B12 addendum** section at the end of this report.
- Every finding below was read in the actual code; the load-bearing critical/high items were independently cross-confirmed by a second finder or an adversarial verifier. Refuted candidates are listed at the end so you know they were checked.

## Build & tests (baseline facts)
- Main project builds clean (only legacy-code warnings).
- **All 1355 tests pass** (14s, 0 skips). `.csproj` compile-include list matches disk (only the intentional exclusions: the deprecated handler + MonsterGenerator data).
- Wager/currency-**game** subsystem (ChateauWagerBank, WagerHouse, Roulette, pot split) audited and judged **sound** — atomic `$inc`, re-checks at commit/spin, all-or-nothing stake commit. The currency bugs below are all in the **payment/interaction** paths, not the wager engine.
- Active-system **time handling** audited and judged **clean and UTC-consistent** — no comparison-direction bugs, no local/UTC mixing that changes behavior, no day-boundary off-by-ones.

---

## CRITICAL — currency can be minted from nothing

### C1. Self-`!pay` doubles your money _(PaymentGiveProcessor.cs:65-84)_
`ProcessInteraction` loads initiator and recipient as two separate `GetProfile` calls, debits one copy, credits the other, then `SetProfile(initiator)` followed by `SetProfile(recipient)`. When you pay **yourself**, those are two copies of the same document; the second save overwrites the debit with `balance + amount`. `ChateauPay` has no self-target guard and `ValidateInteraction` isn't called at consent time.
- **Exploit:** `!pay [self] 100 gold` → `!consent` → balance 100→200, repeatable. Verified first-hand and by two independent finders.

### C2. Queued `!pay` overdraws — no balance recheck at consent _(PaymentGiveProcessor.cs:79)_
Affordability is checked only at command time (`ChateauPay.cs:78`); `ProcessInteraction` subtracts unconditionally with no floor when the recipient consents (up to 10 min later). Multiple pendings pass the same stale check.
- **Exploit:** with 100 gold, `!pay B 100` and `!pay C 100` both validate; both consent → payer at −100, 200 gold now exists. Two accounts loop this. Cross-confirmed by two finders.

---

## HIGH — wrong behavior players hit in normal play

### H1. Negative `!pay` (billing) guard is sign-inverted _(ChateauPay.cs:94)_
For a negative amount the check is `balance < paymentAmount` with `paymentAmount` negative — never true, so any bill validates and the "over 100" cap is likewise dead. `PaymentReceiveProcessor` applies it with no recheck. `!pay B -500` drives B to −500 and mints 500 to the initiator. (Another currency-mint vector.)

### H2. Break/curse blockers do essentially nothing _(consent path never calls `ValidateInteraction`)_
`ValidateInteraction` (the blocker pipeline: broken mouth blocks kiss/feed, cooties curse blocks kiss/cuddle, etc.) is invoked from only **5** request-time call sites (Entitle, Milk, Monsterize, Objectify, Climax). Every other command builds its pending with no validation, and `ChateauConsent.ProcessOneToOneSeat` calls `ProcessInteraction` directly. So of the eight disabler curses only chastity does anything, and broken-part blocks fire only for climax. Verifier note: even `!train`/`!golden`, which self-gate in their own `ValidateInteraction`, are never called — so their gating is dead too. Players who paid `!cleanse` costs or suffered `!break` see the effects simply don't restrict anything.

### H3. Pledge fulfillment is dead code _(ChateauConsent.cs:164)_
The code that marks a pledge fulfilled / increments `pledgesfulfilled` lives only in `ChateauInteractionHandler.addInteraction`'s `processor != null` branch, but `ProcessOneToOneSeat` only calls `addInteraction` in its `processor == null` fallback — unreachable for any registered type. Consenting to a `!fulfill` completes the interaction but the pledge stays `active` forever; the "Kept A Promise / Trustworthy / Whose Word Is Law" titles are unobtainable by anyone. Verified.

### H4. `!breed` daily lock is bypassable → double pregnancies _(BreedProcessor.cs:133)_
Unlike `MilkProcessor` (which rechecks `HasActiveDirectionLock` inside `ProcessInteraction` as a documented TOCTOU guard), `BreedProcessor` has no recheck. Fire `!breed B slime` twice before B consents, then B `!consent all` → two pregnancies from one day's per-direction allowance, farmable daily. Verified.

### H5. Rate-limit "didn't make the dossier" note fires at exactly the wrong times _(Chateaudatabase.cs:207)_
`IsCountRateLimited` returns `UtcNow > timerEnd` — i.e. "limited" only **after** the window expires, the inverse of the intent. The 1:1 consent flow drives its "clerks were still busy" note off this, so the note is suppressed while a count is actually being rate-limited and shown on interactions that actually counted. (The processors already compute the correct note into `_lastRateLimitMessage`, but nothing drains it — see reuse cluster R1.) Cross-confirmed by three sources.

---

## MEDIUM — realistic edge cases, downtime, and reward/economy skew

| # | Finding | Location | Note |
|---|---------|----------|------|
| M1 | Reconnect reuses the stale (expired) F-List API ticket because `ApiTicketResult` is never cleared → every disconnect after 30+ min uptime costs ~4 min downtime instead of ~2 | BotMain.cs:1057 | Verified |
| M2 | `GetNewApiTicket` timeout tests the constant `TickTimeMiliseconds` instead of the `timeout` var → dead timeout; a login-thread failure (non-JSON API response, or prolonged outage) can hang the loop or crash the process | BotMain.cs:1061 | Verified; I spotted this inline too |
| M3 | `LoadChannelSettings` leaves `SavedChannelSettings` null if the file exists but fails to parse → unhandled NRE at startup = **crash loop** on every restart until the file is hand-repaired | BotMain.cs:258 | Verified; corrupt file is producible via non-atomic writes |
| M4 | Payment specialist counts are always 0 — `GetTypeCount` lowercases the type but interactions are stored as `paymentGive`/`paymentReceive` (case-sensitive Mongo match) | Chateaudatabase.cs:425 | Verified |
| M5 | Full-document `ReplaceOne` profile saves revert concurrent atomic `$inc` currency/escrow updates from the op-command tick thread or Discord thread | Chateaudatabase.cs:105 | See architecture P2 |
| M6 | `!sell` / `!milk` write a stale whole-profile snapshot before crediting via `ChangeCurrency`, clobbering any concurrent credit | ChateauSell.cs:98, ChateauMilk.cs:90 | Same class as M5 |
| M7 | `!mark` cooldown is never checked (command or processor); marks also accumulate duplicate entries with no dedupe | ChateauMark.cs:44 | Verified |
| M8 | `!objectify` / `!entitle` promise a consent-time cooldown recheck in comments, but `ValidateInteraction` isn't called there → multi-pending bypass grants two titles/day | ObjectifyProcessor.cs:59 | Verified |
| M9 | `!employ` stamps a once-per-day timer that nothing enforces — re-employable all day | EmployProcessor.cs / ChateauEmploy | From reuse audit |
| M10 | `!rename`'s else-if chain lets any previously-renamed (expired-timer) player skip the 100-char name-length limit | ChateauRename.cs:54 | From reuse audit |
| ~~M11~~ | **NOT A BUG (owner confirmed):** `!rename` strikethrough uses the login `userName` intentionally — it's the default public-facing F-List name and is meant to be shown struck through. No change. | RenameProcessor.cs:63 | Dismissed |
| M12 | `!no` (last holdout declines a group) grants group-achievement titles but never announces them — diverges from the `!consent` path which shows the "Title Time!" banner | ChateauNo.cs:136 | Verified; titles do persist, only the banner is dropped |
| M13 | Currency/work/potion alias map uses `Dictionary.Add`; an admin `!set currencyname potion` produces a colliding key (`showpotions`) → `ArgumentException` breaks **all** alias dispatch in that channel until the setting changes (caught by OnMessage try/catch, so no crash) | BotCommandController.cs:113 | Verified & corrected |
| M14 | `ERR 28` ("already in channel") purges **all** queued channel joins, not just the duplicate → aborts a batch join/audit | BotMain.cs:1248 | Verified |
| M15 | In FlistPlusDiscord mode both run loops execute the shared per-tick work → future messages count down at ~2× speed and `UpdateAllGames` races itself over the unlocked `GameSessions` list | BotMain.cs:646/847 | Verified (production-mode only) |
| M16 | `HandleFutureMessagesTick` is called outside the per-tick try/catch on a background thread with no `AppDomain` handler → any throw inside kills the loop thread/process | BotMain.cs:646 | Verified structurally; the claimed concurrency cause was refuted, so downgraded |
| M17 | `ChateauConsume` checks `timers[timerString]` but formats `timers["consume"]` (Plant copy-paste) | ChateauConsume.cs:51 | From reuse audit |
| M18 | Declared `Aliases[]` arrays are display-only; dispatch requires a hand-maintained parallel class per alias, so any future alias added only to the array appears in `!help` but does nothing | BotCommandController.cs:83 | Design trap; all current aliases OK |

---

## LOW — minor logic / cosmetic / data-quality

| # | Finding | Location |
|---|---------|----------|
| L1 | Long channel-description updates (>~16k chars) throw `InvalidCastException` (`CDSclient` cast to `MSGclient`) and are silently dropped | Model/BotMessage.cs:38 |
| L2 | Several 1:1 casuals (spank/bully/lick/boobhat/lap/sit/entitle) persist `interactionTime = DateTime.MinValue` → dossier "Last seen/reported" can't pick them | ChateauSpank.cs:56 etc. |
| L3 | `InteractionCountMigration` doubles all counts if re-run despite its header saying it's safe, and skips the newer casual types | InteractionCountMigration.cs:19 |
| L4 | Dossier climax specialist ignores `!climaxfor` history and would mis-attribute give/take if naively added | ChateauDossier.cs:92 |
| L5 | Self-`!odorize` loses the scent (same dual-load clobber as C1) but consumes the 7-day cooldown | OdorizeProcessor.cs:60 |
| L6 | Self-`!bond` loses the "initiated" side of the bond (dual-load clobber) | BondProcessor.cs:59 |
| L7 | 1:1 casual commands have no self-target guard (the group path does) → farm both give+take count ladders solo | ChateauSpank.cs:45 etc. |
| L8 | `RandomBreak` reversal cost silently no-ops when the rolled part is already broken longer → costed reversal is free that roll | PurgeCostApplier.cs:117 |
| L9 | `!wash` resets `RemainingMentions` to a full layer, so washing a nearly-faded scent makes it linger **longer** | ChateauWash.cs:129 |
| L10 | `AnOrA` emits "an unicorn" for the shipped `unicorn` monster (letter-based, not sound-based) | Utils.cs:1113 |
| L11 | Dossier renders the raw scent token instead of `ScentText.ScentPhrase` (SSOT bypass) → "Liquor" vs "a scent of liquor" | ChateauDossier.cs:458 |
| L12 | `Utils.Capitalize` throws on empty string — reachable from the dossier with a null/empty scent or curse name, which would crash the whole dossier | Utils.cs:531 |
| L13 | `!statues <location>` NREs on a petrified resident whose profile was removed (unguarded null) | ChateauStatues.cs:81 |
| L14 | `!feedbacklist` never length-checks the first (newest) entry, so one ~3900-char submission can produce an over-cap PM that's dropped/truncated | ChateauFeedbackList.cs:80 |
| L15 | `interactionToVerb` returns "dose" for both tenses → `!pledges` prints "Pledged to dosed X"; `GetInteractionDescription`'s `break` case renders a sentence missing the recipient/part | Utils.cs:1422 / 454 |

---

## Recurring structural patterns (the "why", per the architecture judge — grade **C+**)

The Chateau **processor core** (InteractionProcessorBase + registry + the stateless `GroupInteractionResolver` + the two documented hook systems + the `CooldownSpec`/`ConsentWarningText` text SSOT) is genuinely well-designed — a B+ on its own. The grade is pulled down by three structural gaps, and **most of the bugs above are mechanical consequences of them**, not one-off mistakes:

1. **The consent pipeline has no enforcement spine.** `ValidateInteraction` is overridden ~25× but the consent path never calls it, and per-command cooldown checks are hand-copied — so blockers (H2), pledge marking (H3), and cooldowns (M7/M8/M9) are enforced by "whatever the last copy-paste remembered." **Fix (highest leverage, ~1 afternoon):** call `ValidateInteraction` once in `ProcessOneToOneSeat`; add one shared cooldown gate driven by `CooldownSpec`.
2. **Read-modify-write `ReplaceOne` on whole profiles is the default persistence idiom on a genuinely multi-threaded bot** (websocket + Discord gateway + two tick loops, all `LockCategory.NONE`). The self-target clobbers (C1/L5/L6) are the deterministic single-thread special case; M5/M6 are the concurrent case. **Fix:** route every balance/count/timer write through the atomic `$inc`/`$set` helpers that already exist (`ChangeCurrency` is right there in the same file), and adopt the self-target single-profile rule that dose/corruption already use.
3. **95 near-identical command classes, copy-pasted with drift** (one even has a pending variable named `pendingObjectify` inside `ChateauEmploy`). **Fix:** one `ChateauInteractionCommandBase` owning the request pipeline; you already did this for groups.

**Foot-gun worth fixing now:** `MonDB.GetDatabase()` silently connects to the **production** database when uninitialized — dangerous for any script/tool/test that touches a static registry. Make it throw when uninitialized.

**Dead code / vestigial artifacts to delete:** `ChateauInteractionHandler_depreciated.cs` (271 lines, same class+namespace as the live file — re-adding it breaks the build); `ChateauConsent.getInteractionMessage` (self-labeled defunct); the write-only `_lastRateLimitMessage`/`GetAndClearRateLimitMessage` pair; `MonDB.getInteractions` (`NotImplementedException`); root `Tests.cs` (437 lines compiled into the production exe); `FChatDiceBot.zip` (16 MB) and `libmongocrypt.dylib`/`.so` at the repo root. Consider migrating the main `.csproj` to SDK-style globbing (the test project already is) to end the "new file silently not built" trap.

---

## Reuse / "reinvented wheel" (top clusters — full detail available)

- **R1. Two rate-limit-note systems: the shared one is dead, the duplicate is broken.** The processors compute the correct note into `_lastRateLimitMessage`, but nothing drains it; `ChateauConsent` re-derives it via a hand-maintained count-key table and the inverted `IsCountRateLimited` (= bug H5). Wire the drain and delete the duplicate.
- **R2. Cooldown gate + "too recently" text copy-pasted ~20× across three layers**, which is how the unenforced cooldowns (M7/M9) and the `!rename` limit skip (M10) slipped in. Consolidate onto one gate next to `CooldownSpec`.
- **R3. Consent-warning text hand-rolled in ~11 commands** that duplicate the processor's `GetConsentWarning` — already diverged for `!pay` vs `!fulfill`.
- **R4. `PaymentGive`/`PaymentReceive` are ~95% identical twins**; the transfer math is the same lines with a sign flip. Merge and route through `ChangeCurrency`.
- **R5. Five `Model/*Instance` classes re-implement the identical ~50-line JSON-in-`profile.lists` load/save** ("Mirrors the ScentLayer pattern"). A generic `ProfileListStore<T>` collapses ~250 lines to ~40.
- **R6. Time-remaining formatting done 4+ ways** → players see "23h:59m:59s" from `!work` but "0 days, 23 hours, 59 minutes, 59 seconds" from `!rest` for the same timer.
- Deliberately-fine duplication (do **not** consolidate): per-processor flavor wording, alias command classes (your stated policy), per-processor cooldown-key helpers, `MonDB` static shim.

---

## Test suite (grade **C+**)

**Positive surprise:** the "test database" is **not** a fake — the fixture runs the real `ChateauDatabase` against a real local Mongo (`ChateauDb_Test`), so Mongo semantics (fresh copies per read, missing-field defaults, case sensitivity) are faithfully exercised. The processor/wager/group-resolver tests are exemplary. **But the altitude is wrong:** everything reachable through the injected `IChateauDatabase` is tested; everything behind `Run(BotMain …)` + static `MonDB` — the command and `!consent` layer where **every** critical/high bug lives — has **zero** tests. The integration tests _simulate_ the pipeline rather than drive it (`Pledgeflowtests` re-implements pledge fulfillment inline four times and asserts on its own copy — which is exactly why H3 went unnoticed), and one test asserts an overdraft to −10 as **correct** behavior.

Highest-value missing tests (each <30 lines, and they'd have caught the headline bugs): self-`!pay` leaves balance unchanged; consent-time balance recheck; `ProcessOneToOneSeat` calls `ValidateInteraction` (covers H2 + M7–M9 + H4 at once); expired-but-consented group seat isn't swept; `IsCountRateLimited` active vs expired; `!no` emits the same title banner as `!consent`; pledge fulfillment via the real consent path. ~~Also: B12 random-events shipped with zero tests, and `Reset()` doesn't clear the `RandomEvents` collection.~~ **CORRECTION (2026-07-03): both claims were wrong** — they were made blind, before the B12 code was examined. B12 shipped with 25 tests (461 lines) that drive the *real* engine with seeded RNG and injected profile stores, and `Reset()` does clear `RandomEvents` (Testdatabasefixture.cs:49). Full suite on the B12 tree: **1380/1380 pass** (verified by running it). See the B12 addendum.

---

## Owner dispositions (2026-07-03) — drives the fix spec

1. **Negative currency balances:** Intended *only* as an easter egg for the **IOU currency** (represents debt); accidental going-negative elsewhere was tolerated by extension. **Decision: patch it — add a zero-floor on debits for all currencies except the IOU currency.** This closes/mitigates C1, C2, H1, M5, M6 by removing the "mint via negative" amplifier (the self-pay dual-load clobber C1 still needs its own fix).
2. **`!consent`/`!c` + game wager:** **If a wager is _targeted_ at a player, `!consent` should accept it** (align `!consent`/`!c` with `!accept`). **If a wager is _untargeted_/open to the room, add a dedicated command** rather than routing it through consent.
3. **Casual interactions & dossier "Last seen":** Casuals are **intentionally** skipped for "Last seen" (so meaningful interactions aren't overwritten). The `DateTime.MinValue` persistence (L2) is the wrong mechanism, and the **group** path records real timestamps that *would* overwrite. **Decision: make "Last seen" explicitly select the most recent _non-casual_ interaction** (rather than relying on missing/MinValue timestamps). Group-path recording stays (needed for title tracking).
4. **Profile gate (`BotCommandController.cs:167`):** **Intended.** Unregistered users are blocked from every command until `!joinchateau`. Not a bug — leave as is.
5. **Mongo document validation:** **Deferred deliberately** until the data shape settled; **now welcome.** Add `[BsonIgnoreExtraElements]` + basic shape validation on `Profile` so a stale pre-refactor document can't `FormatException` out `!dossier`/escrow reconciliation. Do a one-line `mongosh` shape check of `RegisteredProfiles` first.

_(Original question text preserved in git history; the above are the owner's resolutions.)_

## Checked and refuted (so you know they were looked at)
- **"Bond dossier swaps roles"** — false positive; the local variables are misleadingly named but the rendered sentence is correct.
- **"COL op-list responses matched to the wrong channel"** — the buggy channel-agnostic line is already commented out; matching is channel-scoped.
- **"Future-message list corrupted by concurrent mutation"** — the list is consistently locked; the residual risk (M16) is only the uncaught-throw-kills-thread structural issue.

---

## B12 addendum — random-events audit (2026-07-03)

_Audited separately: the feature is one commit (`7c67a53`, PR #43) on `origin/VibeCodedSystems`, sitting directly on the fork point this report covered — so this addendum completes coverage of that branch. Every changed file was read in full; the tick-loop call-site structure, thread ownership of the mutated lists, and the test suite result were verified first-hand._

**Baseline:** builds clean; **all 1380 tests pass** on the B12 tree (25 new engine tests). Both blind claims in the test section above were wrong and are corrected there.

**Overall verdict: this is the best-engineered feature in the codebase.** The engine is a single-locked, delegate-injected, absolute-time scheduler whose design *inherently avoids* four of the audit's known bug patterns: absolute-time next-fire scheduling defuses the M15 double-tick class (a second tick just sees "not due"); only `RunLoopFList` calls the tick (no dual-loop execution); the two new `.cs` files are in the `.csproj` (the known silent-no-build trap avoided); and `AllowRandomEvents` correctly defaults false for pre-existing channel-settings JSON. Empty/dormant event set is handled at every entry point; weighted selection guards zero/negative weights; keyword/challenge validation is case-insensitive with one-shot dedupe; curse grants go through the case-insensitive `CatalogMap`; title dedupe matches how `IsSystemTitle` is actually derived; `!random`'s help/strings follow the style guide; UpdateSetting/ViewSettings wiring is consistent.

### Findings

| # | Sev | Finding | Location |
|---|-----|---------|----------|
| **B12-1** | Medium | **Eligibility scan can kill the run-loop thread (M16 class).** `HandleRandomEventsTick` is called at BotMain.cs:654 outside the loop's try/catch (same position as its sibling, M16). Its internal try/catch covers only the per-channel work; the `SavedChannelSettings.Where(...).ToList()` enumeration and `ChannelsJoined.Contains` run unguarded — and both lists are mutated on the websocket receive thread (`ChannelsJoined.Add` BotMain.cs:1218, `SavedChannelSettings.Add` BotMain.cs:1828). A join/settings-write landing mid-enumeration throws `InvalidOperationException` → uncaught → the run-loop thread dies. Low per-tick odds, but the scan runs every 10 s forever. | BotMain.cs:654, 1021-1024 |
| **B12-2** | Medium | **Reward grants are whole-profile read-modify-write (M5 class).** `ResolveLocked` applies rewards via `_getProfile` → mutate → `_setProfile` (= `MonDB.setProfile` → `ReplaceOne`). A concurrent atomic `$inc` on the same profile (wager payout, `!work`) between the read and the write is silently reverted. The engine's instance lock doesn't cover other writers. | RandomEventEngine.cs:237-249 |
| **B12-3** | Low | **Channel eligibility match is case-sensitive against convention.** `ChannelsJoined.Contains(cs.Name)` is ordinal, while the rest of the codebase compares channel-settings names case-insensitively (`.ToLower()` at BotMain.cs:1187/1768). If casings ever drift, events silently never fire in that channel. | BotMain.cs:1022 |
| **B12-4** | Low | **Anti-snipe instruction line suppressed by coincidental substring.** The "Quick, `!random <keyword>`" line is appended only when the announce text doesn't already contain the keyword — an authored template that happens to contain the day's random keyword (e.g. "rose") without using `{keyword}` suppresses the instruction; the `Contains` check is also case-sensitive while validation is case-insensitive. Authoring-data edge; no events authored yet. | RandomEventEngine.cs:316-317, 324-325 |

**Untested (accepted):** the BotMain integration layer (eligibility filter, activity gate, tick throttle) has no tests — consistent with the suite-wide altitude gap; the engine itself is well covered.

**Fixes are folded into FIX_SPEC.md:** B12-1 → Phase 5 (alongside M16), B12-2 → Phase 1's atomic-write rule, B12-3/B12-4 → Phase 6. Note the fix branch forked before B12, so B12 fixes apply on/after the merge with `VibeCodedSystems`.
