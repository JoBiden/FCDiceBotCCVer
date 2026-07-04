# FCDiceBot / Chateau Contract — Fix Spec

_Companion to [`AUDIT_REPORT.md`](AUDIT_REPORT.md). Every finding ID (C1, H2, M7, L5, R1, B12-1…) refers to that report. This document says **what to change, where, in what order, and how to prove it**. It folds in the owner dispositions recorded 2026-07-03 and the B12 random-events addendum findings._

> **B12 note:** the four B12-* findings live in code that only exists on/after the merge with `origin/VibeCodedSystems` (commit `7c67a53`). They're specced in Phases 1/5/6 below but can only be implemented once this fix branch contains that merge.

## How to read this
- Fixes are grouped into **phases** sized like PRs. Phases are ordered by leverage: the earliest phases close the most (and the most dangerous) findings per line changed.
- Each phase lists: **Closes** (finding IDs), **Root cause**, **The change** (with code sketches for criticals/highs), **Files**, **Tests to add**, **Risk**.
- The guiding principle from the architecture judge: **most bugs are mechanical consequences of three structural gaps, so fix them at the choke point, not one command at a time.** The three choke points are Phase 1 (atomic money), Phase 2 (the consent enforcement spine), Phase 3 (the rate-limit note).

## Sequencing at a glance

| Phase | Theme | Closes | Est. size | Status |
|-------|-------|--------|-----------|--------|
| **1** | Economy integrity (atomic guarded debit + zero-floor) | C1, C2, H1, M5, M6, L5, L6, B12-2† | ~1 day | ✅ Done |
| **2** | Consent enforcement spine | H2, H3, H4, M7, M8, M9 | ~1 day | ✅ Done |
| **3** | Rate-limit note correctness | H5, R1 | ~2 hr | ✅ Done |
| **4** | Startup / connection hardening | M1, M2, M3, M14 | ~half day | ✅ Done |
| **5** | Runtime / dispatch robustness | M13, M15, M16, L1, B12-1† | ~half day | ✅ Done |
| **6** | Dossier & data quality | disp. 3, M4, M10, M12, M17, L2–L4, L8–L15, B12-3†, B12-4† | ~1 day | ✅ Done |
| **7** | Mongo document validation | disp. 5 | ~2 hr | ✅ Done |
| **8** | Wager ↔ consent integration | disp. 2 | ~1 day | ✅ Done (untargeted case already covered by `!joingame`, no new command needed) |
| **9** | Cleanup / dead code / reuse (optional, do alongside) | R2–R6, dead code, `MonDB` foot-gun | ongoing | ✅ **Done (2026-07-03)** — dead code + `MonDB` foot-gun fixed; **R3/R4/R5/R6 reuse consolidations implemented**; R2 and build hygiene deliberately left (see Phase 9 section) |

**Do Phase 1 first and alone** — it's the only set of bugs that mints currency, it's independently shippable, and it's the highest-confidence change. Phases 2 and 3 can land in either order after it. 4–8 are independent of each other.

_† B12-* items require the `VibeCodedSystems` merge first (see the B12 note above); if the merge hasn't happened when a phase lands, carry its B12 item forward rather than blocking the phase._

---

## Phase 1 — Economy integrity (the money can never be minted)

**Closes:** C1 (self-pay mint), C2 (queued overdraw), H1 (sign-inverted bill), M5 (ReplaceOne reverts `$inc`), M6 (`!sell`/`!milk` stale snapshot), L5 (self-odorize clobber — same class), L6 (self-bond clobber — same class).

**Root cause (one sentence):** currency is moved by *read-modify-write of the whole profile* (two `GetProfile` calls, two `SetProfile`), which (a) overwrites itself on self-target, (b) reverts concurrent atomic `$inc`, and (c) has no floor — while the correct primitive, atomic `$inc` via `ChangeCurrency`, already sits in the same file at [Chateaudatabase.cs:288](FChatDicebot/Database/Chateaudatabase.cs:288).

**Owner disposition folded in (#1):** negative balances are an intended easter egg **only** for the `ious` currency (and, pre-existing, the `nothing` joke currency). Every other currency gets a **zero-floor on debit**.

### 1a. New DB primitive: atomic guarded debit

Add to `IChateauDatabase` / `ChateauDatabase` next to `ChangeCurrency`:

```csharp
/// <summary>
/// Atomically remove `amount` (a positive magnitude) of a currency from a profile.
/// Unless allowNegative is true, the debit only applies when the balance is >= amount,
/// so a concurrent/stale caller can never drive a normal currency below zero. Returns
/// true iff the debit was applied. allowNegative is for the `ious`/`nothing` easter eggs.
/// </summary>
public bool TryDebitCurrency(string userName, string currencyLabel, int amount, bool allowNegative)
{
    if (amount <= 0) return false; // callers pass a positive magnitude
    var collection = Database.GetCollection<Profile>("RegisteredProfiles");
    var fb = Builders<Profile>.Filter;
    var filter = fb.Eq("userName", userName);
    if (!allowNegative)
        filter = fb.And(filter, fb.Gte("currencies." + currencyLabel, amount)); // missing field fails $gte → correct
    var update = Builders<Profile>.Update.Inc("currencies." + currencyLabel, -amount);
    return collection.UpdateOne(filter, update).ModifiedCount == 1;
}
```

Notes:
- `$gte` does **not** match a missing field, so a player with no balance at all correctly fails the guarded debit — no special-casing needed.
- Credit side stays `ChangeCurrency(user, currency, +amount)` — `$inc` already upserts the key.
- Add a small shared helper for the exemption so the rule lives in one place:
  ```csharp
  public static class CurrencyRules {
      public const string IouCurrency = "ious";
      public const string NothingCurrency = "nothing";
      public static bool AllowsNegative(string currency) =>
          currency == IouCurrency || currency == NothingCurrency;
  }
  ```
  **Decision to confirm:** include `nothing` in the exemption? It's a pre-existing joke currency the code already lets you "pay" without having (capped at 100/txn). Flooring it silently kills that easter egg, so the default here **keeps it exempt**. If you want `nothing` floored too, drop it from `AllowsNegative`. (`ious` is exempt per your disposition regardless.)

### 1b. Rewrite the payment processors to move money atomically

`PaymentGiveProcessor.ProcessInteraction` ([PaymentGiveProcessor.cs:54](FChatDicebot/InteractionProcessors/Involved/PaymentGiveProcessor.cs:54)) and its twin `PaymentReceiveProcessor.ProcessInteraction` ([PaymentReceiveProcessor.cs:55](FChatDicebot/InteractionProcessors/Involved/PaymentReceiveProcessor.cs:55)) both do the dual-load/dual-save. Replace the body of the transfer (keep `AddInteraction`, keep `DeletePendingCommand`) with a **guarded-debit-then-credit**, and have `ProcessInteraction` report failure up so the consent layer can tell the user:

```csharp
// paymentGive: initiator pays recipient `amount` (amount > 0).
// paymentReceive: recipient pays initiator; store the magnitude and flip the payer.
string payer   = isGive ? initiator : recipient;
string payee   = isGive ? recipient : initiator;
int    magnitude = Math.Abs(amount);

bool debited = Database.TryDebitCurrency(payer, currency, magnitude, CurrencyRules.AllowsNegative(currency));
if (!debited)
{
    // Insufficient funds at consent time (C2/H1): do NOT credit. Surface a note, drop the pending.
    _lastInitiatorPrivateMessage = payer + " no longer has enough " + currency + " for that transfer.";
    Database.DeletePendingCommand(command.Id);
    return "paymentFailed"; // consent layer prints the completion only on success
}
Database.ChangeCurrency(payee, currency, magnitude);
Database.DeletePendingCommand(command.Id);
```

- This **kills C1**: on self-pay `payer == payee`, the guarded `$inc -magnitude` then `$inc +magnitude` net to zero — no mint — and with the self-target guard in 1c it never even queues.
- This **kills C2**: the debit is now conditional on live balance at consent time; a second queued payment finds the balance already gone and fails cleanly.
- This **kills H1** for the money side: the payer is always the party who owes, and they can't go below zero (except `ious`/`nothing`).
- **R4 opportunity:** the two processors are now ~identical with an `isGive` flag — merge them into one `PaymentProcessor` (optional, Phase 9). Leave the two `InteractionType` strings registered either way so old pendings still resolve.

### 1c. Self-target guard at command creation

In `ChateauPay.Run` ([ChateauPay.cs:35](FChatDicebot/BotCommands/ChateauPay.cs:35)), after resolving `recipient`, reject self-pay before building the pending:

```csharp
if (string.Equals(recipient, characterName, StringComparison.OrdinalIgnoreCase)) {
    bot.SendPrivateMessage("You can't pay yourself — that's just moving money from one pocket to the other.", characterName);
    valid = false;
}
```

Adopt the same self-target rule for the other dual-load clobbers the audit flagged: **L5 (`!odorize`)** and **L6 (`!bond`)**. The generalizable rule (already used correctly by dose/corruption): _when initiator == recipient, load the profile once and mutate one copy._ For odorize/bond specifically, the simplest correct fix is the same command-level self-target guard (a self-scent / self-bond has no meaning), matching the group path which already guards self-target.

### 1d. Fix the request-time affordability check (H1 UX half)

`ChateauPay`'s negative branch at [ChateauPay.cs:94](FChatDicebot/BotCommands/ChateauPay.cs:94) compares `recipientProfile.currencies[currency] < paymentAmount` with `paymentAmount` negative — never true. Compare against the **magnitude**:

```csharp
int magnitude = -paymentAmount; // paymentAmount < 0 here
if (!recipientProfile.currencies.ContainsKey(currency) || recipientProfile.currencies[currency] < magnitude) { ... }
else if (magnitude > 100) { /* the "nothing" inflation cap */ }
```

This is the friendly early-out only; the authoritative guard is 1a/1b at consent time.

### 1e. `!sell` / `!milk` stale-snapshot (M6)

`ChateauSell.cs:98` and `ChateauMilk.cs:90` write a whole-profile `SetProfile` snapshot and *then* credit via `ChangeCurrency`, so the snapshot clobbers the credit. Fix: do **all** mutations for these commands through the atomic helpers (`ChangeCurrency`, `ChangeCount`, `SetTimer`) and delete the intervening `SetProfile(wholeProfile)`. Where a command legitimately needs to change several fields, use the targeted per-field helpers rather than one whole-document replace.

### 1f. M5 as a standing rule

M5 ("`ReplaceOne` reverts concurrent `$inc`") is the general form. It's fully closed only when balance/escrow/count/timer writes stop going through whole-profile `SetProfile`. Phase 1 fixes the money paths; **file the remaining `SetProfile(wholeProfile)` call sites as Phase 9 cleanup** (grep `SetProfile(` — each is a candidate for a targeted helper). Add a code comment on `SetProfile` warning that it clobbers concurrent atomic updates and should not be used for currency/escrow/count/timer.

### 1g. B12-2 — random-event reward grants (post-merge only)

`RandomEventEngine.ResolveLocked` (RandomEventEngine.cs:237-249) applies winner rewards via `_getProfile` → mutate → `_setProfile`, the same M5-class whole-profile `ReplaceOne`. The clean fix that keeps the engine's delegate-injected testability: split the reward application so the **currency** branch goes through an injected atomic `changeCurrency(user, key, amount)` delegate (prod: `MonDB` → `ChangeCurrency`; tests: in-memory dict), and only the branches that genuinely need document fields (title/training/corruption/curse) keep the profile write. The existing engine tests keep passing with the extra delegate wired in the fixture.

**Files:** `Database/IChateauDatabase.cs`, `Database/Chateaudatabase.cs`, `InteractionProcessors/Involved/PaymentGiveProcessor.cs`, `.../PaymentReceiveProcessor.cs`, `BotCommands/ChateauPay.cs`, `BotCommands/ChateauSell.cs`, `BotCommands/ChateauMilk.cs`, `InteractionProcessors/.../OdorizeProcessor.cs` (+ its command), `InteractionProcessors/Commitment/BondProcessor.cs` (+ `ChateauBond`).

**Tests to add (each <30 lines, all against the real test Mongo):**
- self-`!pay 100 gold` leaves balance unchanged (was: doubled).
- two queued `!pay B 100` / `!pay C 100` from 100 gold: exactly one succeeds, payer ends at 0, no currency created (sum conserved).
- `!pay B -500 gold` when B has 200: rejected at request time; if forced through, B floors at 0 and nothing is minted.
- `ious` debit **may** go negative; `gold` debit may **not**.
- `!sell` / `!milk` credit survives a concurrent `ChangeCurrency` (assert sum).
- self-`!odorize` keeps the scent / doesn't consume the cooldown on a no-op; self-`!bond` rejected.

**Risk:** low-medium. The behavior change players will notice: overdrafts and self-pay stop working (intended). Confirm no legitimate flow relies on a currency other than `ious`/`nothing` going negative (grep for direct negative writes; the wager engine already floors via escrow).

---

## Phase 2 — The consent enforcement spine

**Closes:** H2 (break/curse blockers unenforced), H3 (pledge fulfillment dead code), H4 (breed double-pregnancy), M7 (`!mark` no cooldown), M8 (`!objectify`/`!entitle` multi-pending), M9 (`!employ` re-employable all day).

**Root cause:** `ValidateInteraction` is overridden ~25× but the consent path never calls it, and cooldown checks are hand-copied per command. `ChateauConsent.ProcessOneToOneSeat` ([ChateauConsent.cs:160](FChatDicebot/BotCommands/ChateauConsent.cs:160)) calls `ProcessInteraction` directly.

### 2a. Call `ValidateInteraction` once, at the top of `ProcessOneToOneSeat`

```csharp
private string ProcessOneToOneSeat(BotMain bot, PendingCommand toConsent)
{
    string channelMessage = string.Empty;
    var interaction = toConsent.pendingInteraction;
    var processor = InteractionProcessorRegistry.GetProcessor(interaction.type);
    if (processor != null)
    {
        // Enforcement spine: re-validate at consent time. Catches break/curse blockers that
        // appeared after the request (H2), per-direction locks (H4), and lets processors
        // gate themselves (train/golden). ValidateInteraction is side-effect free.
        var validation = processor.ValidateInteraction(interaction.initiator, interaction.recipient, interaction.identifier);
        if (!validation.IsValid)
        {
            MonDB.removePendingInteraction(toConsent.Id);
            bot.SendPrivateMessage(validation.Reason, toConsent.awaitingConsentFrom);
            return string.Empty; // nothing happens in-channel
        }
        processor.ProcessInteraction(toConsent);
        ...
    }
    ...
}
```

- `ValidateInteraction`'s base already runs the blocker pipeline with `StatusEffectCallSite.Consent` ([InteractionProcessorBase.cs:298](FChatDicebot/InteractionProcessors/InteractionProcessorBase.cs:298)), so this immediately makes **all** disabler curses and broken-part blocks real at consent (H2), and activates the previously-dead `train`/`golden` self-gates.
- **The group path needs the same treatment.** `GroupInteractionResolver` resolves seats without validation too; add the equivalent check where each seat is applied so a blocked participant is dropped from the resolution rather than force-processed. (Same `ValidateInteraction` call, per participant, at apply time.)
- **UX decision (defaulted):** on a failed consent, send the reason privately to the consenter and delete the pending. If you'd rather surface it in-channel as a `[sub]` note, swap the `SendPrivateMessage` for a channel fragment — but private keeps the "why can't I" quiet.

### 2b. Shared consent-time cooldown gate (M7/M8/M9, and belt-and-suspenders H4)

Right after 2a's validation passes, gate on the processor's `CooldownSpec` before processing:

```csharp
if (processor.CooldownRule != null && CooldownGate.IsOnCooldown(MonDB.GetDatabase(), interaction, processor.CooldownRule, out string coolMsg))
{
    MonDB.removePendingInteraction(toConsent.Id);
    bot.SendPrivateMessage(coolMsg, toConsent.awaitingConsentFrom);
    return string.Empty;
}
```

- Introduce one `CooldownGate` helper next to `CooldownSpec` that reads the spec's `Binds`/`PeriodDays` and checks the right party's timer (this is the consolidation R2 asks for). Because `!consent all` processes seats **sequentially**, seat 1 stamping the lock makes seat 2 fail the gate — that closes **H4** at the spine level.
- **Populate the missing specs:** verify each of `MarkProcessor`, `EmployProcessor`, `ObjectifyProcessor`, `EntitleProcessor` actually defines `CooldownRule` (M7/M9 exist because the check was never wired *and* some specs may be null). Any that are null get a `CooldownSpec` matching their intended cadence. `!mark` also needs **dedupe** (don't append a duplicate mark entry) — add that in `MarkProcessor.ProcessInteraction`.

### 2c. Breed defense-in-depth (H4)

Even with 2b, mirror `MilkProcessor` exactly so breed is safe regardless of call path. `MilkProcessor` rechecks `HasActiveDirectionLock` inside `ProcessInteraction` at [MilkProcessor.cs:216](FChatDicebot/InteractionProcessors/Involved/MilkProcessor.cs:216). Add the same to `BreedProcessor.ProcessInteraction` ([BreedProcessor.cs:133](FChatDicebot/InteractionProcessors/Commitment/BreedProcessor.cs:133)) using its own `DirectionTimerKey(recipient)`:

```csharp
if (initiatorProfile.timers != null
    && initiatorProfile.timers.TryGetValue(DirectionTimerKey(recipient), out var lockTimer)
    && DateTime.UtcNow < lockTimer.timerEnd)
{
    Database.DeletePendingCommand(command.Id);
    return "breedBlocked"; // already bred this direction today; no second implant
}
```

Give `BreedProcessor` a `HasActiveDirectionLock(profile, recipient)` mirroring Milk's, and use it in both `ValidateInteraction` and `ProcessInteraction`.

### 2d. Make pledge fulfillment reachable (H3)

The pledge-fulfilled marking (set fulfilled, `pledgeHonored`, increment `pledgesfulfilled`) lives only in `ChateauInteractionHandler.addInteraction`'s `processor != null` branch, but `ProcessOneToOneSeat` only reaches `addInteraction` in the `processor == null` fallback — dead for every registered type.

Fix: move pledge fulfillment out of `addInteraction` into the **success path** of `ProcessOneToOneSeat`, gated on "this pending is a pledge fulfillment." After `ProcessInteraction` succeeds:

```csharp
if (toConsent.IsPledgeFulfillment /* or: the fulfill flag already on the pending */)
{
    ChateauPledges.MarkFulfilled(toConsent.pendingInteraction, /* pledgeId */);
    // sets fulfilled, stamps pledgeHonored, increments pledgesfulfilled → unlocks the title line
}
```

- Confirm how `!fulfill` tags the pending as a pledge (it routes through `ChateauFulfill`); reuse that tag rather than inventing a new flag.
- This unlocks the "Kept A Promise / Trustworthy / Whose Word Is Law" titles, which are currently unobtainable by anyone.
- Delete the now-vestigial pledge code from `addInteraction` (and consider deleting the whole `processor == null` fallback — every registered type has a processor; see Phase 9 dead-code list).

**Files:** `BotCommands/ChateauConsent.cs`, `InteractionProcessors/GroupInteractionResolver.cs`, new `InteractionProcessors/CooldownGate.cs`, `InteractionProcessors/Commitment/BreedProcessor.cs`, `.../MarkProcessor.cs`, `.../EmployProcessor.cs`, `.../ObjectifyProcessor.cs`, `.../EntitleProcessor.cs`, pledge marking in `ChateauPledges`/`ChateauInteractionHandler`.

**Tests to add:**
- broken-mouth recipient: `!kiss` request queues, consent is **rejected**, no interaction recorded (H2).
- `!breed B slime` twice, then `!consent all`: exactly **one** pregnancy (H4); assert the second seat is dropped.
- `!mark`, then `!mark` again same day: second is blocked; a single mark entry (no dupe) (M7).
- `!objectify`/`!entitle` two pendings same day → one grant (M8); `!employ` twice → one (M9).
- `ProcessOneToOneSeat` calls `ValidateInteraction` (a direct unit test — this single assertion guards H2 + M7–M9 + H4 from regressing).
- `!fulfill` via the **real** consent path marks the pledge fulfilled and increments `pledgesfulfilled` (H3) — replaces the four inline re-implementations in `Pledgeflowtests` that hid this bug.

**Risk:** medium. This changes consent semantics (some consents now legitimately fail). The blast radius is exactly the interactions that *should* have been gated. Watch for processors whose `ValidateInteraction` override does something surprising at consent time — audit the ~25 overrides once for side effects (base is side-effect-free; identifier existence re-checks are harmless).

---

## Phase 3 — Rate-limit note correctness

**Closes:** H5 (`IsCountRateLimited` inverted), R1 (two rate-limit-note systems; shared one dead, duplicate broken).

### 3a. Fix the inversion (one character)

[Chateaudatabase.cs:207](FChatDicebot/Database/Chateaudatabase.cs:207): `return DateTime.UtcNow > profile.timers[timerKey].timerEnd;` → `<`. "Rate-limited" means the timer hasn't expired yet (now is **before** `timerEnd`).

### 3b. Retire the duplicate; drain the shared message (R1)

The casual processors already compute the correct note into `_lastRateLimitMessage` (e.g. `SpankProcessor.cs:55` via `IncrementDifferentCountsWithRateLimit`), but `ProcessOneToOneSeat` ignores it and re-derives via its own `GetCountKeys` table + `IsCountRateLimited`. Replace `CheckRateLimitsAndGetMessage(...)` in `ProcessOneToOneSeat` with:

```csharp
channelMessage += processor.GetAndClearRateLimitMessage();
```

Then delete `ChateauConsent.CheckRateLimitsAndGetMessage` and `GetCountKeys` (the hand-maintained, drift-prone count-key table). Keep 3a regardless — other callers use `IsCountRateLimited` and the inversion is a latent bug for all of them.

**Files:** `Database/Chateaudatabase.cs`, `BotCommands/ChateauConsent.cs`.

**Tests to add:** `IsCountRateLimited` true while timer active, false once expired; a rapid second casual interaction shows the "clerks were busy" note; a first interaction does not.

**Risk:** low. Mostly deletes code. Verify every processor whose interactions surface a rate-limit note actually populates `_lastRateLimitMessage` (the involved/commitment tiers may not — if a type produces no note, `GetAndClearRateLimitMessage` returns empty, which is correct).

---

## Phase 4 — Startup / connection hardening

**Closes:** M1 (stale API ticket on reconnect), M2 (dead timeout can hang/crash login loop), M3 (corrupt channel-settings crash loop), M14 (`ERR 28` purges all queued joins).

- **M1 — [BotMain.cs:1057](FChatDicebot/BotMain.cs:1057):** clear `ApiTicketResult` (set to null) on disconnect so reconnect fetches a fresh ticket instead of retrying the expired one for ~4 min. Add: on reconnect, if the ticket is older than the F-List TTL, force a refresh.
- **M2 — [BotMain.cs:1061](FChatDicebot/BotMain.cs:1061):** the timeout check tests the constant `TickTimeMiliseconds` instead of the local `timeout`. Change to `if (timeout <= 0)`. Verify the loop actually breaks/aborts the login attempt on timeout rather than spinning.
- **M3 — [BotMain.cs:258](FChatDicebot/BotMain.cs:258):** `LoadChannelSettings` leaves `SavedChannelSettings` null when the file exists but fails to parse → NRE crash loop. Wrap the parse in try/catch; on failure, log and fall back to a fresh empty settings object (and back up the corrupt file rather than overwriting it). Consider atomic writes (write temp + rename) where these files are saved to prevent the corruption in the first place.
- **M14 — [BotMain.cs:1248](FChatDicebot/BotMain.cs:1248):** on `ERR 28` ("already in channel") only drop the **duplicate** channel from the join queue, not the whole queue. Match the erroring channel and remove just that entry.

**Files:** `BotMain.cs`.

**Tests / drills:** these are in the untested `BotMain` layer. Add whatever unit coverage is feasible around the pure helpers (timeout comparison, the join-queue purge filter, the settings-parse fallback), and script manual drills: (1) force a reconnect after >30 min uptime, confirm ~2 min recovery; (2) hand-corrupt the channel-settings file, confirm clean startup; (3) queue joins including a duplicate, confirm only the dup is dropped.

**Risk:** medium (touches the connection lifecycle). Land behind careful manual verification; these paths have zero automated coverage today.

---

## Phase 5 — Runtime / dispatch robustness

**Closes:** M13 (alias-map `Dictionary.Add` collision breaks all alias dispatch), M15 (FlistPlusDiscord double-ticks future messages + races `UpdateAllGames`), M16 (`HandleFutureMessagesTick` outside try/catch kills the loop thread), L1 (long channel descriptions dropped by `CDSclient`→`MSGclient` cast).

- **M13 — [BotCommandController.cs:113](FChatDicebot/BotCommandController.cs:113):** building the currency/work/potion alias map with `Dictionary.Add` throws `ArgumentException` on a colliding key (e.g. admin sets a currency name to `potion` → collides with `showpotions`), disabling alias dispatch in that channel. Use `dict[key] = value` (last-wins) or guard with `ContainsKey`, and log the collision.
- **M15 — [BotMain.cs:646/847](FChatDicebot/BotMain.cs:646):** in `FlistPlusDiscord` mode both run loops execute the shared per-tick work, so future messages count down ~2× and `UpdateAllGames` races over the unlocked `GameSessions` list. Make the shared per-tick work run from exactly one loop (guard by mode, or a single owning loop), and/or lock `GameSessions` during `UpdateAllGames`.
- **M16 — [BotMain.cs:646](FChatDicebot/BotMain.cs:646):** wrap `HandleFutureMessagesTick` in the same per-tick try/catch the rest of the loop uses so a throw logs instead of killing the background thread. (The concurrency cause was refuted; this is the structural uncaught-throw fix only.)
- **B12-1 — BotMain.cs:654 (post-merge only):** `HandleRandomEventsTick` sits at the same unprotected call site as M16, and its eligibility scan (`SavedChannelSettings.Where(...)` + `ChannelsJoined.Contains`) enumerates lists the websocket thread mutates (`ChannelsJoined.Add` :1218, `SavedChannelSettings.Add` :1828) outside its internal per-channel try/catch. Fix both halves: include the call in the same per-tick try/catch as M16, **and** snapshot the two lists before filtering (e.g. `ChannelsJoined.ToArray()` / `SavedChannelSettings.ToArray()` under the appropriate lock, or wrap the scan itself in the try) so a mid-enumeration mutation can't throw.
- **L1 — [Model/BotMessage.cs:38](FChatDicebot/Model/BotMessage.cs:38):** long channel-description updates (>~16k chars) throw `InvalidCastException` casting `CDSclient` to `MSGclient` and are silently dropped. Handle the `CDS` message type explicitly (or guard the cast) so long descriptions aren't lost.

**Files:** `BotCommandController.cs`, `BotMain.cs`, `Model/BotMessage.cs`.

**Risk:** medium; M15 is production-mode (`FlistPlusDiscord`) only. Smoke-test in that mode.

---

## Phase 6 — Dossier & data quality

**Closes:** owner disposition #3 ("Last seen" non-casual selection), M4, M10, M12, M17, L2, L3, L4, L8, L9, L10, L11, L12, L13, L14, L15.

### 6a. "Last seen" selects the most recent non-casual interaction (disposition #3)

The intent: casual interactions must not overwrite the dossier's "Last seen / reported" line, so meaningful interactions stay visible. Today this is (wrongly) approximated by persisting `interactionTime = DateTime.MinValue` for some 1:1 casuals (L2), and the **group** path records real timestamps that *would* overwrite. Correct mechanism:

- **Stop writing `DateTime.MinValue`.** Persist real `interactionTime` for all interactions (fixes L2's data corruption and lets other features use casual timestamps).
- **Change the "Last seen" query** to explicitly select the most recent interaction whose type is **not casual** (filter by investment tier / a `casual` type set), rather than relying on missing/min timestamps. One place: the dossier "Last seen" lookup in `ChateauDossier`.
- Group-path recording stays as-is (needed for title tracking); it's simply excluded from "Last seen" by the same non-casual filter.

### 6b. Remaining dossier/data fixes

| ID | Fix |
|----|-----|
| **M4** | `GetTypeCount` ([Chateaudatabase.cs:425](FChatDicebot/Database/Chateaudatabase.cs:425)) lowercases the type but interactions are stored `paymentGive`/`paymentReceive` (case-sensitive Mongo). Match the stored casing (don't lowercase, or store canonical-case consistently). Restores payment-specialist counts. |
| **M10** | `!rename` else-if chain ([ChateauRename.cs:54](FChatDicebot/BotCommands/ChateauRename.cs:54)) lets an expired-timer player skip the 100-char limit. Restructure so the length check always runs. |
| **M12** | `!no` group resolution ([ChateauNo.cs:136](FChatDicebot/BotCommands/ChateauNo.cs:136)) grants group titles but never announces them. Mirror `ChateauConsent`'s banner: read `resolution.GroupTitleGrants` + fold count-title wins + `FormatGroupTitleNotification`. Best fixed by extracting the shared "resolve a touched group → banner" block from `ChateauConsent` (lines 116–138) into a helper both call. |
| **M17** | `ChateauConsume` ([ChateauConsume.cs:51](FChatDicebot/BotCommands/ChateauConsume.cs:51)) checks `timers[timerString]` but formats `timers["consume"]` (Plant copy-paste). Use `timerString` in both. |
| **L3** | `InteractionCountMigration` ([InteractionCountMigration.cs:19](FChatDicebot/…/InteractionCountMigration.cs:19)) double-counts on re-run despite its "safe" header, and skips newer casual types. Make it idempotent (guard by a migration marker) and include the new casual types — or mark it one-shot and retire it. |
| **L4** | Dossier climax specialist ignores `!climaxfor` history; document/guard so a future edit doesn't mis-attribute give/take. |
| **L8** | `RandomBreak` reversal cost no-ops when the rolled part is already broken longer ([PurgeCostApplier.cs:117](FChatDicebot/…/PurgeCostApplier.cs:117)) → free reversal. Charge the cost regardless, or reroll to an unbroken part. |
| **L9** | `!wash` ([ChateauWash.cs:129](FChatDicebot/BotCommands/ChateauWash.cs:129)) resets `RemainingMentions` to a full layer, making a nearly-faded scent linger longer. Clamp to `min(current, fullLayer)` (never extend). |
| **L10** | `AnOrA` ([Utils.cs:1113](FChatDicebot/Utils.cs:1113)) emits "an unicorn". Special-case sound, not letter — at least handle `unicorn`/`u`-as-"you" words. |
| **L11** | Dossier ([ChateauDossier.cs:458](FChatDicebot/BotCommands/ChateauDossier.cs:458)) renders the raw scent token instead of `ScentText.ScentPhrase`. Route through the SSOT helper. |
| **L12** | `Utils.Capitalize` ([Utils.cs:531](FChatDicebot/Utils.cs:531)) throws on empty string, reachable from the dossier with an empty scent/curse name → whole dossier crashes. Return `""` for null/empty. |
| **L13** | `!statues <location>` ([ChateauStatues.cs:81](FChatDicebot/BotCommands/ChateauStatues.cs:81)) NREs on a petrified resident whose profile was removed. Null-guard and skip. |
| **L14** | `!feedbacklist` ([ChateauFeedbackList.cs:80](FChatDicebot/BotCommands/ChateauFeedbackList.cs:80)) never length-checks the newest entry → one ~3900-char submission produces an over-cap dropped PM. Truncate/paginate the first entry too. |
| **L15** | `interactionToVerb` returns "dose" for both tenses ([Utils.cs:1422](FChatDicebot/Utils.cs:1422)) → "Pledged to dosed X"; `GetInteractionDescription`'s `break` case ([Utils.cs:454](FChatDicebot/Utils.cs:454)) renders a sentence missing the recipient/part. Fix the tense table and the break sentence template. |
| **B12-3** | _(post-merge)_ Random-events channel eligibility `ChannelsJoined.Contains(cs.Name)` (BotMain.cs:1022) is ordinal case-sensitive; the codebase convention is `.ToLower()` comparison (BotMain.cs:1187/1768). Use `Contains(cs.Name, StringComparer.OrdinalIgnoreCase)` so a casing drift can't silently disable events in a channel. |
| **B12-4** | _(post-merge)_ `OpenEventLocked`'s "Quick, `!random <keyword>`" instruction line is suppressed when the announce text coincidentally contains the keyword (RandomEventEngine.cs:316-317, same for challenge :324-325), and the `Contains` check is case-sensitive while validation isn't. Only treat the placeholder as satisfied when the template actually used `{keyword}`/`{challenge}` (track substitution success from `Substitute` instead of re-scanning the output). |

**Files:** `ChateauDossier.cs`, `Chateaudatabase.cs`, `ChateauRename.cs`, `ChateauNo.cs` (+ shared helper in `ChateauConsent.cs`), `ChateauConsume.cs`, `InteractionCountMigration.cs`, `PurgeCostApplier.cs`, `ChateauWash.cs`, `Utils.cs`, `ChateauStatues.cs`, `ChateauFeedbackList.cs`.

**Tests to add:** "Last seen" ignores a newer casual and shows the older involved interaction (disposition #3); payment specialist count is non-zero after a payment (M4); `!no`-resolved group emits the same banner as `!consent` (M12); dossier renders with an empty scent without throwing (L12); `!wash` never increases `RemainingMentions` (L9).

**Risk:** low; mostly isolated. The "Last seen" query change and the M12 shared-helper extraction are the only ones touching shared code.

---

## Phase 7 — Mongo document validation (disposition #5)

**Closes:** disposition #5 (a stale pre-refactor `Profile` document can `FormatException` out `!dossier`/escrow reconciliation).

- Add `[BsonIgnoreExtraElements]` to `Profile` (and other persisted models read from Mongo) so an old document carrying since-removed fields deserializes instead of throwing.
- Add basic shape validation / defaulting on load: null dictionaries (`currencies`, `counts`, `timers`, `lists`, `characteristics`, `jobExperience`, `pregnancies`) default to empty rather than null, so downstream code never NREs on a document that predates a field.
- **Before shipping:** run a one-line `mongosh` shape check over `RegisteredProfiles` to see what stale shapes actually exist in prod (fields present/absent, null vs missing dictionaries) and size the defaulting to reality.

**Files:** `Model/Profile.cs` (+ any sibling persisted models), possibly `GetProfile` post-deserialize normalization.

**Tests to add:** deserialize a document with an unknown extra field (doesn't throw); deserialize a document with a missing dictionary field (comes back empty, not null); `!dossier` on a minimal/legacy profile doesn't throw.

**Risk:** low; `[BsonIgnoreExtraElements]` is strictly more permissive.

---

## Phase 8 — Wager ↔ consent integration (disposition #2) — CLOSED 2026-07-03

**Closes:** disposition #2 — the audit's "consent-vs-wager INTENT" question (`results-commands-layer.json`).

**Targeted wagers → `!consent`:** done. `ChateauConsent.Run` now calls `WagerGameSupport.TryAcceptWager` first, exactly mirroring what `Accept.cs` already did — so `!consent`/`!c`/`!accept` are all interchangeable for accepting a targeted wager proposal. `Accept.cs` simplified to a pure delegate now that the check lives in one place.

**Untargeted/open-to-room wagers → already covered, no new command needed.** Owner clarified the intended scenario ("Alice opens {game} and stakes 7 copper, who wants to join?") and confirmed it: `!joingame {gametype}` already lets any player join and automatically match the opener's declared stake with no named opponent and no consent-queue involvement at all (`WagerGameSupport.HandleWagerJoin`, the `myStake.Matches(openerStake)` branch). The `!accept`/`!consent` targeted flow only exists for the different case — a second player proposing a *different* stake, which needs the opener's explicit accept. There is no "open wager anyone can claim without matching the stake" concept in the current design, so no dedicated command was needed.

---

## Phase 9 — Cleanup, dead code, reuse (do opportunistically alongside 1–8)

Not bugs on their own, but they're *why* the bugs happened; clearing them prevents recurrence. None are urgent; fold each into whichever phase touches the same file.

**Dead code / artifacts to delete — ✅ DONE (2026-07-03):**
- `ChateauInteractionHandler_depreciated.cs` (271 lines, same class+namespace as the live file — re-adding it breaks the build). **Deleted.**
- `ChateauConsent.getInteractionMessage` (self-labeled defunct). **Deleted.**
- The write-only `_lastRateLimitMessage`/`GetAndClearRateLimitMessage` duplicate path: `CheckRateLimitsAndGetMessage`/`GetCountKeys` **deleted** in Phase 3 (also fixed Kiss/Cuddle/Handhold, which turned out to never populate `_lastRateLimitMessage` at all — see Phase 3 commit).
- `MonDB.getInteractions` (`NotImplementedException`). **Deleted.**
- Root `Tests.cs` (437 lines compiled into the production exe). **Deleted.**
- `FChatDiceBot.zip` (16 MB), `libmongocrypt.dylib`/`.so` at repo root. **Deleted** (the real Windows `mongocrypt.dll` stays — it's already correctly wired).
- The `processor == null` fallback in `ProcessOneToOneSeat` and the now-fully-unreachable `ChateauInteractionHandler.addInteraction` it called. **Deleted**; the fallback branch now logs and drops a pending whose type doesn't resolve to a processor at all (e.g. a corrupted document) instead of calling into ~30 lines of dead legacy code.

**Foot-gun — ✅ DONE:** `MonDB.GetDatabase()` used to silently connect to the **production** DB when uninitialized. It now **throws** `InvalidOperationException` instead. Verified the full test suite still passes (it goes through `TestDatabaseFixture`, which calls `MonDB.Initialize`) and that `BotMain`'s constructor always calls `MonDB.Initialize` at real startup, so a live bot is unaffected.

**Reuse consolidations (R2–R6):**
- **R2** — one cooldown gate + "too recently" text. **Not delivered as a shared class (intentional, unchanged).** M7/M9's missing rechecks were fixed in Phase 2b by adding the recheck directly to `MarkProcessor`/`EmployProcessor.ValidateInteraction`, mirroring the pattern `ObjectifyProcessor`/`EntitleProcessor` already used — i.e. the *bugs* are closed, but the ~20 hand-copied cooldown checks across the codebase (including `!rename`, M10) are still hand-copied, not routed through one shared gate. Revisit only if the duplication itself starts causing drift.
- **R3** — ✅ **DONE (2026-07-03).** Found 11 command classes hand-rolling consent-warning text that duplicated (or, for `!pay`, diverged from) their processor's `GetConsentWarning`: `ChateauPay`, `ChateauBoobhat`, `ChateauBully`, `ChateauCuddle`, `ChateauLick`, `ChateauHandhold`, `ChateauSpank`, `ChateauFeed`, `ChateauDressup`, `ChateauGolden`, `ChateauTrain`. All now call `processor.GetConsentWarning(...)` instead. `ChateauPay`'s hand-rolled text mentioned the amount, which `GetConsentWarning`'s single `identifier` slot didn't carry — fixed by folding the magnitude into the identifier string passed in (`"100 gold"`), and (owner-approved) `PaymentGiveProcessor`/`PaymentReceiveProcessor.GetConsentWarning` were reworded to reproduce the original `!pay` text exactly ("Alice is going to pay Bob 100 gold! ...") rather than the processor's previous, more generic wording — so `!pay`'s announcement is byte-identical to before, just now sourced from one place. The other 10 commands' wording was already byte-identical to their processor's `GetConsentWarning`, so no visible text change there — only the duplication was removed.
- **R4** — ✅ **DONE (2026-07-03).** Extracted the shared debit/credit transfer logic (the part Phase 1 made mint-proof) into an abstract `PaymentProcessorBase` in `InteractionProcessors/Involved/PaymentProcessorBase.cs`; `PaymentGiveProcessor`/`PaymentReceiveProcessor` are now thin subclasses supplying only `IsGive`, `InsufficientFundsSuffix`, and their distinct wording (`GetInteractionVerb`/`GetCompletionMessage`/`GetConsentWarning`). Deliberately did **not** collapse them into one instance registered under two type keys (the pattern `CorruptionProcessor`/`LapsitProcessor`/`ClimaxforProcessor` use) — `GetInteractionVerb(VerbTense)` takes no interaction context, so a single shared instance couldn't tell "paid" from "billed" apart. Two classes stay separately registered under `paymentGive`/`paymentReceive` (existing pendings unaffected). Full Phase-1 test suite (`Trydebitcurrencytests.cs`, `Paymentgiveprocessortests.cs`, `Paymentreceiveprocessortests.cs`) re-run and passes unchanged (1427/1427 full suite).
- **R5** — ✅ **DONE (2026-07-03).** Added generic `ProfileListStore<T>` (`Model/ProfileListStore.cs`) with `LoadAll(profile, listKey, isValid)` / `SaveAll(profile, listKey, entries)`. `BreakInstance`, `CurseInstance`, `ParasiteInstance`, `ViceInstance`, `ScentLayer` are now thin wrappers over it (each keeps its own list key constant and any field-level validity predicate); `BreakInstance.LoadAllWithTick` unchanged. ~250 lines of near-identical JSON-in-`lists` load/save collapsed to ~40 (the shared class) + ~5 lines/class.
- **R6** — ✅ **DONE (2026-07-03), owner-approved wording change.** `ChateauWork.cs`, `ChateauVolunteer.cs`, and `ChateauFeedback.cs`'s `CooldownMessage` had their own inline `"{0:D2}h:{1:D2}m:{2:D2}s"` formatting instead of the `Utils.GetTimeSpanPrint` used by every other timer message (`!rest`, `!bond`, `!breed`, etc.). All three now call `Utils.GetTimeSpanPrint`. `!work`/`!volunteer` cooldown notes now read "0 days, 23 hours, 59 minutes, 59 seconds" instead of "23h:59m:59s"; `!feedback`'s cooldown note gains the same "days, hours, minutes, seconds" phrasing.
- **Do NOT consolidate** (intentional): per-processor flavor wording, alias command classes (your policy), per-processor cooldown-key helpers, the `MonDB` static shim.

**Build hygiene — still not started (deliberately deferred).** Migrating the main `.csproj` from explicit `<Compile Include>` to SDK-style globbing (the test project already is) would end the "new `.cs` silently not built" trap, but touches every file in the project and risks pulling in unwanted items (build artifacts, data files) if not scoped carefully — left for a dedicated, isolated change rather than folded in here. (Until then, remember: new main-project files must be added to `FChatDicebot.csproj` — this pass added three: `ProfileListStore.cs`, `PaymentProcessorBase.cs`.)

---

## Test strategy (applies across phases)

The audit's core test finding: the injected-`IChateauDatabase` layer is well tested against a **real** local Mongo, but the **command/`!consent` layer where every critical/high lives has zero tests**. So the highest-value work is **raising test altitude**, not adding more DB-layer tests.

1. **Add a thin harness to drive commands through `Run(BotMain…)` + the real test Mongo** (or, minimally, to drive `ChateauConsent.ProcessOneToOneSeat` and the group resolver directly). Every headline bug has a <30-line regression test that only becomes possible at this altitude — they're listed per phase above.
2. **Delete the self-fulfilling integration fakes:** `Pledgeflowtests` re-implements pledge fulfillment inline four times and asserts on its own copy — which is exactly why H3 went unnoticed. Replace with a test that drives the real consent path. Fix the test that asserts an overdraft to −10 as "correct" (it encodes C2).
3. **Guard the choke points with one assertion each:** "`ProcessOneToOneSeat` calls `ValidateInteraction`", "payment routes through `TryDebitCurrency`", "`IsCountRateLimited` is active before expiry". These three tiny tests protect the three structural fixes from silent regression.

---

## Open decisions — resolved 2026-07-03 (kept for history)

1. **`nothing` currency floor (Phase 1a):** ✅ resolved — kept `nothing` exempt from the zero-floor, per the default (`ious` exempt either way).
2. **Failed-consent UX (Phase 2a):** ✅ resolved — sends the block/cooldown reason privately to the consenter and drops the pending, per the default.
3. **Untargeted-wager command (Phase 8):** ✅ resolved — no new command needed. Owner clarified the scenario ("Alice opens {game} and stakes 7 copper, who wants to join?"), which `!joingame {gametype}` already handles.
4. **Payment twin merge (Phase 1b / R4):** ✅ resolved 2026-07-03 — shared transfer logic extracted into `PaymentProcessorBase`; the two classes stay separately registered (see Phase 9's R4 entry for why a single shared instance wasn't safe here).

Nothing here changes the two **no-change** dispositions: the profile gate (disposition #4) stays, and the `!rename` strikethrough `userName` (M11) stays.
