# Group Interactions, Pending Lifecycle, and New Casuals

**Status:** Shipped 2026-06-22 (B4 group system, B5 lifecycle + `!consent <name>`, the lapsit lap-stack). The new casuals (B7) had already shipped 1:1 on 2026-06-21. Build green, 15 new tests. The lifecycle verbs were renamed at owner request to dodge the `!w`/`!work` clash: recipient-side is **`!no`** (aliases `!refuse`, `!decline`), initiator-side is **`!oops`** (aliases `!o`, `!withdraw`, `!cancel`). **Group-achievement titles shipped 2026-06-22** (a follow-up): size-based titles for the group-capable casuals and the lapsit per-position "height" titles, owner-provided — see [Group-achievement titles](#group-achievement-titles-by-resolved-size--lap-stack-position) below.

This spec covers the "Interactions" cluster from [Feature-Requests.md](../../Feature-Requests.md):

- **B4** — a system to include multiple people in an interaction.
- **B5** — `!withdraw` (initiator-side) and `!refuse` (recipient-side) commands to clear a pending interaction early, on top of the existing 10-minute timeout.
- **B7** — three new casual interactions: **lapsit** (`!lap` / `!sit`, a "lap stack"), **boobhat** (`!boobhat`), and **lick** (`!lick`).

The three are bundled because they interlock: the new casuals are the first consumers of the group system, and `!withdraw`/`!refuse` have to understand group pendings.

---

## Background: the current 1:1 model

- An `Interaction` is strictly one `initiator` + one `recipient` ([ChateauDB.cs:160](../../../FChatDicebot/Model/ChateauDB.cs)).
- A command (e.g. `!cuddle Bob`) builds one `Interaction`, wraps it in one `PendingCommand` with `awaitingConsentFrom = recipient`, sends a channel announcement, and stores the pending ([ChateauCuddle.cs](../../../FChatDicebot/BotCommands/ChateauCuddle.cs)).
- The only pending lookup that exists filters on `awaitingConsentFrom` — there is **no initiator-side query** ([Chateaudatabase.cs:426](../../../FChatDicebot/Database/Chateaudatabase.cs)).
- `!consent` (run by the recipient) sweeps pendings older than 10 minutes, then handles bare / `all` / `#` / numbered-list disambiguation, runs `processor.ProcessInteraction`, emits the completion message, and checks titles ([ChateauConsent.cs](../../../FChatDicebot/BotCommands/ChateauConsent.cs)).
- Counts are **per-user lifetime totals**, gated by a 30-minute rate-limit per user per key. Casual counters are either **symmetric** (kiss/cuddle/handhold → both parties get the same key, via `IncrementBothCountsWithRateLimit`) or **directional** (spank → `spankgive`/`spanktake`, bully → `bullygive`/`bullytake`, via `IncrementDifferentCountsWithRateLimit`).
- The dossier casual block is a single line that appends one `[u]Label:[/u] N` chip per casual key with count > 0 ([ChateauDossier.cs:593](../../../FChatDicebot/BotCommands/ChateauDossier.cs)).
- `PendingCommand` does **not** record the channel it was raised in — any follow-up message lands in whatever channel the follow-up command is typed in.

---

## B4 — Multi-person interactions

### Two tiers

| Tier | Applies to | Shape |
|------|-----------|-------|
| **Hybrid group** | Casual interactions only | One shared interaction "moment" with a combined completion message; each recipient still consents individually with plain `!consent`. |
| **Fan-out** | All other (non-casual) interactions, if/when multi-target is wanted there | `!verb A B C` simply creates N independent 1:1 pendings; each resolves on its own exactly as today. No shared moment, no model change. |

Only the **hybrid group** tier is specced in depth here. Fan-out is documented as the cheap fallback for non-casuals and is out of scope for the initial build unless a specific non-casual asks for it.

### Hybrid group model

Add to `PendingCommand`:
- `groupId` (string; null/empty for ordinary 1:1 pendings).
- `consentState` (enum: `Pending` / `Consented`).
- `consentedOrder` (int; 0 for non-group; assigned `1,2,3…` as group members consent — drives lapsit stack ordering and serial-comma name order).

A multi-target command (`!cuddle Bob Carol Dave`) creates **one `PendingCommand` per recipient**, all sharing a freshly minted `groupId`, all `awaitingConsentFrom` their own recipient. The shared interaction parameters (type, initiator, investment level, the initiator's role for directional/lapsit) are duplicated onto each, or stored once on a small group record — implementer's choice; the spec only requires that resolution can recover them.

New DB queries (both also used by B5):
- `GetPendingCommandsByGroupId(groupId)`
- `GetPendingCommandsByInitiator(initiator)`

### Consent and resolution flow

1. Each recipient runs plain `!consent` (or `!consent <initiatorName>`, see B5.8). For a **group** member, `!consent` does **not** immediately process — it sets `consentState = Consented`, stamps `consentedOrder`, and saves.
2. After any seat changes state (consent, refuse, or lazy 10-min expiry), check the group: **if no `Pending` seats remain**, resolve.
3. **Resolution** = "fire with whoever consented" (B4.2): gather the `Consented` set, run the group-aware processing (count math below + one combined completion message), then delete every `PendingCommand` for that `groupId`.
   - If **zero** recipients consented, nothing fires — the group expires silently, exactly like a lapsed 1:1 today.
   - For directional/lapsit, a single consenter is enough to fire (initiator + 1).

> **Implementation note — background sweep (updated 2026-07, feedback 6a55cfa9).** Originally expiry was *lazy* (swept only when someone ran `!consent`/`!no`), so a group whose 10-minute window ran out sat unresolved until the next command touched it. Players read that as "the timeout doesn't work", so `BotMain.HandleGroupTimeoutsTick` now sweeps every ~30 seconds: any group holding an un-consented seat past the window is fed through the same `GroupInteractionResolver.CheckAndResolve` path, firing the moment with whoever consented (or clearing a group nobody answered). Seats carry a `sourceChannel` so the sweep knows where to post the completion message; the lazy path still runs on `!consent`/`!no` as before.

### Count accounting (B4.3)

Let **M** = number of people in the resolved interaction (initiator + consenting recipients), **R** = consenting recipients (so M = R + 1).

- **Non-directional / symmetric** (kiss, cuddle, handhold): every participant gets **+(M − 1)** to the symmetric key — i.e. +1 per *other* participant. (A 3-person cuddle → +2 each.) This mirrors the directional rule's "+1 per counterparty" and is cross-checked by the lapsit math below, where each person's total increments equal people-above + people-below = M − 1.
- **Directional one-to-many** (spank, bully, boobhat, lick): one initiator, many recipients (B4.4). Initiator gets **+R** to the *give* key; each recipient gets **+1** to the *take* key.
- **Lapsit**: special per-position rule — see B7.

New increment helper needed: a `+N` variant (e.g. `IncrementCountBy(user, key, n, RateLimit)`), since the existing helpers add 1. **Rate-limit semantics for groups:** run one rate-limit check per participant per group event; if not limited, apply the full `+N`; if limited, apply 0 and surface the existing "clerks were busy" sub-note for that participant.

### Self-target handling (B4.6)

In a multi-target command, if the initiator names themselves: **silently drop** them from the channel-visible target list, and **whisper the initiator** (allowed — they invoked the bot):

> "You're already interacting with the other residents, you don't need to also interact with yourself!"

### Cap (B4.6)

Max **10 recipients** per invocation, for now (easily tuned). Combined with F-Chat's 4096-char message limit and that renamed display names can exceed 100 characters, the completion-message builder must tolerate long name lists (fall back to `[spoiler]` like `!consent` already does at >500 chars when needed).

### Completion-message format

- Symmetric, M ≥ 3: serial comma — "Alice, Bob, and Carol cuddle up together. {descriptor}". M = 2 keeps today's "Alice and Bob…".
- Directional: "Alice licks Bob, Carol, and Dave. {descriptor}".
- Lapsit: render the stack bottom→top (see B7).

### `!consent` disambiguation display

The numbered list in `!consent` currently shows `type initiated by X`. For group seats, also surface that it's a group (e.g. append the other participants or "(group)") so a recipient can tell a pile apart from a 1:1. Minor, cosmetic.

---

## B5 — `!withdraw` / `!refuse`

Two new commands that clear a pending early, on top of the 10-minute timeout (which is unchanged).

### Commands and aliases (B5.9)

| Primary | Aliases | Who runs it | Clears |
|---------|---------|-------------|--------|
| `!refuse` | `!decline`, `!reject`, `!r` | the **recipient** | a pending awaiting *their* consent |
| `!withdraw` | `!cancel`, `!retract`, `!w` | the **initiator** | a pending *they* initiated |

> ⚠ **Alias collision check required.** `!r` and `!w` are short and may already be taken (`!c` is already a `!consent` alias). Verify against the live command table before wiring these, and fold the result into the **alias audit** backlog item (added to Feature-Requests.md).

### Behavior

- **`!refuse`** mirrors `!consent`'s disambiguation in reverse: bare (your only/most-recent), `all`, `#`, numbered list, and `!refuse <name>` to target a specific initiator (B5.8). Uses the existing `awaitingConsentFrom` query. For a **group** seat, refusing clears only *that* recipient's seat; the group resolves with whoever else consented.
- **`!withdraw`** needs the new `GetPendingCommandsByInitiator` query. Same disambiguation surface: bare / `all` / `#` / `!withdraw <name>`. For a group, withdrawing nukes **all** seats of that `groupId` (the initiator is cancelling the whole pile).
- Both should opportunistically sweep expired pendings while they're in there.

### Announcements (B5.7)

**TOS constraint:** the bot may only PM a user who *themselves* invoked it. When a recipient refuses, the *initiator* didn't invoke anything, so they can't be PM'd — hence both messages are **public in the channel** where the command was run.

Owner-provided strings:
- **Withdraw/cancel:** "Nevermind! {initiator} says that was a clerical error."
- **Refuse/decline:** "Looks like {recipient} isn't in the mood. Remember, it's not true consent if you can't say 'no'!"

### Also: `!consent <name>` (B5.8)

Add the same by-name targeting to `!consent` — `!consent <initiatorName>` consents to the request from that initiator — so all three lifecycle verbs (`!consent` / `!refuse` / `!withdraw`) share a consistent bare / `all` / `#` / `<name>` grammar. Especially useful now that one initiator can have several pendings out at once via groups.

---

## B7 — New casuals

All three are **directional** (B7.10). lapsit additionally has the lap-stack group mechanic; boobhat and lick are standard directional one-to-many.

### boobhat — `!boobhat`

The initiator rests their chest on the recipient(s)' heads. `boobhatgive` = the one providing the hat (the chest); `boobhattake` = the one wearing it. Standard directional one-to-many: initiator +R `boobhatgive`, each recipient +1 `boobhattake`.

### lick — `!lick`

The initiator licks the recipient(s). `lickgive` = licker, `licktake` = licked. Standard directional one-to-many: initiator +R `lickgive`, each recipient +1 `licktake`.

### lapsit — `!lap` / `!sit` (the lap stack)

One `LapsitProcessor` fed by **two** commands (the pair shares a processor the way corrupt/purify do):

- **`!lap [targets]`** — the initiator is the **bottom** of the stack (the foundation everyone sits on). Initiator starts at position 0.
- **`!sit [targets]`** — the initiator is a **sitter** at position 1; the bottom (position 0) starts **untaken**.

As recipients consent, in consent order, they fill the stack: each "claims the untaken bottom first, otherwise takes the next-highest position." Concretely, at resolution over the consented set (stack size M, positions `0` = bottom … `M−1` = top):

- **`!lap`:** initiator = 0; consenters get 1, 2, 3… in consent order.
- **`!sit`:** first consenter = 0 (claims the open bottom); initiator = 1; remaining consenters get 2, 3… in consent order.

**Per-position counts (B7.12):** a person at position `k` gets **+(M−1−k) `lapsittake`** (people above them, sitting on them) and **+k `lapsitgive`** (people below them, whom they're sitting on).

**Worked example — `!lap Bob Carol`, consent order Bob then Carol:**
Stack bottom→top: Initiator(0), Bob(1), Carol(2). M = 3.
- Initiator: `lapsittake` +2, `lapsitgive` +0
- Bob: `lapsittake` +1, `lapsitgive` +1
- Carol: `lapsittake` +0, `lapsitgive` +2

**Worked example — `!sit Bob Carol`, consent order Bob then Carol:**
First consenter Bob claims the bottom. Stack bottom→top: Bob(0), Initiator(1), Carol(2). M = 3.
- Bob: `lapsittake` +2, `lapsitgive` +0
- Initiator: `lapsittake` +1, `lapsitgive` +1
- Carol: `lapsittake` +0, `lapsitgive` +2

(2-person degenerate cases: `!lap Bob` → initiator is the lap (`lapsittake` +1), Bob sits (`lapsitgive` +1). `!sit Bob` → Bob is the lap (`lapsittake` +1), initiator sits (`lapsitgive` +1).)

**Completion message:** built from the bottom two outward, then each additional sitter, then a height tally. The opener depends on which command was used (i.e. who is the bottom):

- **`!sit` opener** (initiator sits on the bottom person): *"{initiator} uses {bottom} as a comfy lap to sit on."* / *"{initiator} pulls themselves onto {bottom}'s lap."* — pick from variants.
- **`!lap` opener** (initiator is the bottom): *"{initiator} pulls {firstSitter} onto their lap."*
- **Group extension (stack ≥ 3):** append *"Then {next} takes a seat, then {next}, then {next}"* for each person above the opening pair, in stack order, ending with *", forming a lap stack {M} people tall!"*.
- **2-person:** opener only, plus a random descriptor (no "Then…/forming a lap stack" tail — though the "Does it count as a stack if it's only two?" descriptor leans into it).

Example — `!lap Bob Carol David Erwin`, all consent: *"Alice pulls Bob onto their lap. Then Carol takes a seat, then David, then Erwin, forming a lap stack 5 people tall!"* + descriptor.

**Lap-stack height titles** award titles for a participant's position in a resolved stack — by how many others sit **below** them and how many sit **above** them. These gate on a single stack's size/position rather than lifetime counts, so they live outside the count ladders. **Shipped** with the rest of the group-achievement titles — see [Group-achievement titles](#group-achievement-titles-by-resolved-size--lap-stack-position).

### Dossier, specialist text, and counts

New casual count keys: `boobhatgive`, `boobhattake`, `lickgive`, `licktake`, `lapsitgive`, `lapsittake`.

- **CountDisplayNames** ([ChateauDossier.cs:26](../../../FChatDicebot/BotCommands/ChateauDossier.cs)) — add display labels for each (chips only show when count > 0; B7.11 leaves the single-line casual block uncapped for now).
- **CasualCountSpecialistText** ([ChateauDossier.cs:54](../../../FChatDicebot/BotCommands/ChateauDossier.cs)) — add a "specialization" verb for each so they feed the most-performed-casual line.
- **ChateauConsent.GetCountKeys** — add mappings (note: the *group* resolution path computes counts directly via the processor and the `+N` helper; `GetCountKeys` still feeds the rate-limit clerk-note path for 1:1).

### Title ladders (B7.12, owner-provided). Thresholds `1 / 10 / 50 / 100 / 500`.

```
lapsitgive:  1 Will Sit On You   10 Supported           50 S is for Sitter   100 Lap Pet                    500 Disciple of Rin
lapsittake:  1 Lap               10 Supportive          50 Chair             100 Comfy                     500 Throne

boobhatgive: 1 Boo(b)            10 Good Hat            50 Umbrella          100 Speaks With Their Chest    500 Heavy is the Crown
boobhattake: 1 Guess Who?        10 Nice Hat            50 Boobrest          100 Didn't Skip Neck Day       500 Heavy is the Head

lickgive:    1 :P                10 Mlem                50 Talented Tongue   100 Licksalot                  500 Got to the Center of the Tootsie Pop
licktake:    1 Licked            10 Tasty               50 Salt Lick         100 Spit Shined                500 Tootsie Pop
```

Add all to the `interactionMilestones` table in [ChateauSystemTitles.cs:64](../../../FChatDicebot/BotCommands/ChateauSystemTitles.cs). (The lap-stack *height* titles above are a separate title type — see the next section.)

### Group-achievement titles (by resolved size / lap-stack position)

**Shipped 2026-06-22 (follow-up).** A second, event-driven title type, distinct from the lifetime-count ladders above: awarded once when a *group* moment resolves, based on the size of the resolved group (or, for lapsit, a participant's stack position). Thresholds are **cumulative** — a resolved group of a given size grants every tier at or below it, the same way the count ladders backfill. Group size **M counts every participant including the initiator** (so a 3-person group = initiator + 2 recipients).

Who is credited:

- **Symmetric** (`kiss` / `cuddle` / `handhold`): **every participant** earns the size titles — participating is enough.
- **Directional one-way** (`spank` / `bully` / `lick` / `boobhat`): **only the initiator** earns them.
- **Lapsit:** **per position** — each participant earns "below" titles for the number of people stacked under them and "above" titles for the number over them, so a mid-stack rider can earn both.

Size titles (M = participants incl. self), owner-provided:

```
kiss      3 Kiss Kiss   5 Kisstacular   7 Kissimanjaro   9 Kisspocalypse   11 Spartakiss
handhold  3 Triangle    5 Pentagon      7 Septagon       9 Nonagon         11 Undecagon
cuddle    3 Cuddle Puddle  5 Cuddle Pond  7 Cuddle Lake   9 Cuddle Sea      11 Cuddle Ocean
spank     3 Echo        5 Thunder Storm  8 Seven Spanks   11 Strike!
bully     3 Intimidating  5 Hazing       7 Demands Respect  9 Unbullyvable  11 Decibully
lick      3 Free Sample  5 Indecisive   7 Can Tie A Knot In A Cherry Stem  9 Tongue In Chic  11 Lick A Ton
boobhat   4 Hat Trick    7 Mad Hatter   11 Capping
```

Lapsit per-position titles, owner-provided:

```
others below:  2 Elevated   4 Laps All The Way Down   6 Can See Their House From Here   8 King Of The World   10 Lap of Babel
others above:  2 Shortstacked   5 Buried   10 Foundational
```

`boobhat`'s **Hat Trick** is the former `objectifygive` tier-3 title, **renamed to "Toy Maker"** there (the lone holder had earned it both ways, so no migration was needed).

**Implementation.** Tables + cumulative lookups live in [ChateauSystemTitles.cs](../../../FChatDicebot/BotCommands/ChateauSystemTitles.cs) (`GetGroupSizeTitles`, `GetLapsitPositionTitles`). Granting mirrors the count math: `InteractionProcessorBase.GrantGroupTitles` (symmetric/directional) and a `LapsitProcessor` override (per-position) persist through the injected database and return per-participant newly-granted titles; `GroupInteractionResolver` calls them during resolution and exposes the grants on `GroupResolutionResult.GroupTitleGrants`.

**One consolidated banner.** A group moment surfaces a *single* "Title Time!" notification, **grouped by title**: each newly-earned title is listed once with everyone who earned it in a serial-comma name list ("Alice, Bob, and Carol earned the title ·Cuddle Puddle·!"). `ChateauConsent` folds in each participant's lifetime-count title wins (via `ChateauSystemTitles.CheckAndGrantCountTitles`, which now returns the same `GroupTitleGrant` shape) alongside the group-achievement grants, then renders them through `ChateauSystemTitles.FormatGroupTitleNotification`. The 1:1 consent path keeps its per-person banner.

### Descriptors (owner-provided)

Random completion descriptors per the casual pattern. Tokens: `{lapsitgiver}` = the topmost sitter; `{lickgiver}` = initiator; `{licktaker}` = a recipient (for multi-target lick, resolve to one — first or random; confirm at build).

- **lapsit:** "Does it count as a stack if it's only two?" · "Lap! Lap! Lap!" · "We have normal chairs too, in case you didn't know..." · "That means {lapsitgiver} is on top, literally." · "Always nice to have some support."
- **boobhat:** "Nice hat!" · "How heavy are they?" · "Can you still see?" · "Soft, warm..." · "How forward!"
- **lick:** "How many more licks to get to the center?" · "I bet they taste good." · "In cat culture, that means {lickgiver} is in charge." · "In bunny culture, that means {licktaker} is in charge." · "Is this what it means to be groomed?" · "Mlem!"

---

## Files to create / modify

**New processors** (`FChatDicebot/InteractionProcessors/Casual/`): `BoobhatProcessor`, `LickProcessor`, `LapsitProcessor`.

**New commands** (`FChatDicebot/BotCommands/`): `ChateauBoobhat`, `ChateauLick`, `ChateauLap`, `ChateauSit` (B7); `ChateauRefuse`, `ChateauWithdraw` (B5).

**Modify:**
- `InteractionProcessorRegistry` — register the 3 new processors.
- `Model/ChateauDB.cs` — `PendingCommand` gains `groupId`, `consentState`, `consentedOrder`.
- `Database/Chateaudatabase.cs` (+ `Ichateaudatabase.cs`) — add `GetPendingCommandsByGroupId`, `GetPendingCommandsByInitiator`.
- `InteractionProcessorBase` — add the `+N` increment helper + group-resolution count entry points.
- `BotCommands/ChateauConsent.cs` — group-aware consent (defer processing for group seats; resolve on last seat), `!consent <name>` targeting, group line in the disambiguation list.
- Multi-target parsing in the casual commands (a `GetUserNamesFromCommandTerms` returning a list) + self-target drop/whisper.
- `BotCommands/ChateauDossier.cs` — new `CountDisplayNames` + `CasualCountSpecialistText` entries.
- `BotCommands/ChateauSystemTitles.cs` — new title ladders.

---

## Tests

- **Group resolution:** N-person symmetric cuddle awards +(M−1) each; directional group awards +R initiator / +1 each recipient; partial consent ("fire with whoever consents") resolves with the consented subset; zero-consent group expires silently.
- **Lapsit math:** `!lap` and `!sit` position assignment by consent order; per-position `lapsittake`/`lapsitgive`; 2-person degenerate cases.
- **Self-target:** initiator dropped from a multi-target list; whisper sent; interaction still fires for the rest.
- **Cap:** >10 recipients rejected/truncated.
- **Lifecycle:** `!refuse` clears only the caller's seat (group resolves with the rest); `!withdraw` clears all seats of a group; by-name targeting for all three verbs; expired-sweep behavior.
- **Dossier/titles:** new casual chips render only when count > 0; new title ladders grant at thresholds.

---

## Decisions — all resolved (owner, 2026-06-20)

- **B4.1** Hybrid (shared moment, individual consent) for casuals; fan-out (independent 1:1) for any future non-casual multi-target.
- **B4.2** Fire with whoever has consented at resolution.
- **B4.3** Non-directional: +1 per other participant (M−1 each). Directional: initiator +R, each recipient +1. (Lapsit: per-position, see B7.12.)
- **B4.4** Directionals are one initiator → many recipients.
- **B4.5** Hybrid is casual-only; non-casuals use fan-out.
- **B4.6** Cap 10 recipients. Silently drop initiator self-targets in channel; whisper the explanation.
- **B5.7** Both announcements public (TOS: can't PM a non-invoking user). Strings provided.
- **B5.8** Add `<name>` targeting to `!refuse`, `!withdraw`, and `!consent`.
- **B5.9** Primaries `!refuse` / `!withdraw`; aliases as tabled. Alias audit added to backlog.
- **B7.10** All three new casuals directional.
- **B7.11** Casual dossier line stays uncapped for now.
- **B7.12** Title ladders (thresholds `1/10/50/100/500`) + lapsit lap-stack mechanic + completion-message form + descriptors all as specified.

## Open items (confirm/decide at build time)

1. ~~**Tall-stack height titles** (design TODO, owner)~~ — **Resolved/shipped 2026-06-22.** Owner provided the heights and wording; shipped as part of the broader [group-achievement titles](#group-achievement-titles-by-resolved-size--lap-stack-position) (size-based titles for every group-capable casual, plus the lapsit per-position "below"/"above" ladders).
2. ~~**No background scheduler** for timeout-fire of partially-consented groups~~ — **Resolved 2026-07 (feedback 6a55cfa9):** built the timer; see the updated Implementation note above.
3. **Group state storage** — duplicate shared params on each seat vs. one small group record. Either is fine; pick whichever is cleaner against the existing DB layer.
4. **`!r` / `!w` alias availability** — verify before wiring (part of the alias audit).
5. **`{licktaker}` token in multi-target lick** — pick first vs. random recipient.
