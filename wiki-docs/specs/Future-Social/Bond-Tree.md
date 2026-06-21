# Bond Tree — `!bondtree` / `!familytree`

**Status:** Proposed (design-complete, owner-reviewed 2026-06-21).
**Feature-Requests source:** "!bondtree and/or !familytree which would map out all users connected to someone by N degrees of separation via bonds" (B10).

---

## Overview

Two read-only commands that traverse the bond graph and show everyone connected to a resident within N degrees of separation:

- **`!bondtree`** — walks **all** bond types.
- **`!familytree`** — walks only the **family subset** (see below).

Both are public, consent-free reads (bonds are mutually declared, public facts), built around one shared static builder so they differ only by which bond types are included. This is not an interaction; it rides the existing `GetProfile` reads with **no DB-layer change**.

---

## The bond graph (how bonds are stored)

The bond interaction writes **both directions** as profile `lists` ([BondProcessor.cs:54-75](../../../FChatDicebot/InteractionProcessors/Commitment/BondProcessor.cs)):

- on the initiator: `bond{type}initiated` → list of recipient **userNames**
- on the recipient: `bond{type}received` → list of initiator **userNames**

So a person's **neighbors** = the union, over every bond type, of the userNames in their `bond{type}initiated` and `bond{type}received` lists. Because both ends are written, the graph is symmetric and fully reconstructable by reading one profile at a time.

**Bond types** (keys, with display role from [Utils.BondToText](../../../FChatDicebot/Utils.cs)): `marriage`(spouse), `offspring`, `disciple`, `sibling`, `piety`(deity), `protection`(ward), `fealty`(lord), `family`(kin), `roommate`, `coworker`, `submission`(submissive), `ally`, `thrall`, `partner`, `fuckbuddy`, `property`, `rival`, `inspiration`, `client`, `subordinate`(boss), `pet`.

**Family subset (B10.1)** = `{ marriage, offspring, sibling, family }`.
> Note: "family" and "kin" are the **same** bond — bond type `family` renders as the role "kin" ([Utils.cs:1354](../../../FChatDicebot/Utils.cs)). The owner's "marriage, offspring, sibling, family, kin" is four bond types, not five. Define the subset as a single `static readonly string[] FamilyBondTypes` so `!familytree` and any future family-only feature share it.

---

## Behavior

`!bondtree [user] [N]` and `!familytree [user] [N]`:

- **Root (B10.3):** default the invoker; an optional `[user]` argument roots the tree at someone else (public info, no consent). Resolve the target like other user-arg commands (`GetUserNameFromCommandTerms` / `MonDB.getProfile`); unknown user → the standard not-found reply.
- **Depth N (B10.2):** optional integer arg, **default 2**, **max 3** (clamp silently, or note the clamp). Degree 1 = direct bonds, degree 2 = their bonds, etc.
- **Node cap (B10.2):** hard cap of **100 distinct people** rendered. BFS outward; if the cap is hit, stop expanding and append a "…and more (limit reached)" note rather than ballooning the message or the profile-load count.
- **Dedup / cycles:** the graph has cycles (mutual and diamond bonds). BFS with a `visited` set keyed by userName; **each person appears once, at their shallowest degree** from the root. The root itself is degree 0 and not listed as a connection.

**Traversal.** Standard BFS from the root: for each frontier profile, enumerate neighbors by scanning its `lists` keys that match `bond{type}initiated` / `bond{type}received` (restricted to `FamilyBondTypes` for `!familytree`), load each unseen neighbor's profile (`MonDB.getProfile`) to continue and to resolve display names, and record the **edge role** for labeling. The 100-node cap bounds total profile loads.

**Edge role labels (B10.4).** Label each connection with the neighbor's role **relative to the person they were reached from**, using `Utils.BondToText` with the same perspective convention as [BondProcessor.GetCompletionMessage](../../../FChatDicebot/InteractionProcessors/Commitment/BondProcessor.cs):
- reached via the parent's `bond{type}initiated` (parent was the initiator) → neighbor is the parent's `BondToText(type, true)` (e.g. "pet").
- reached via the parent's `bond{type}received` (parent was the recipient) → neighbor is the parent's `BondToText(type, false)` (e.g. "owner").

So a line reads, e.g., `Bob (Alice's pet)` — the neighbor, then their role relative to whoever connected them.

**Layout (B10.5):** grouped by degree, PM'd to the invoker, `[spoiler]` fallback past the usual length:

```
Bond tree for {Root}, up to {N} degrees:
1st degree:
  {Name} ({role relative to Root})
  ...
2nd degree:
  {Name} ({role relative to the 1st-degree person who connected them})
  ...
```

**Empty state:** `"{Root} has no bonds yet."` for `!bondtree`; `"{Root} has no family bonds yet."` for `!familytree` *(both pending owner wording review).*

---

## Implementation

A shared static builder makes both commands one-liners and keeps the traversal unit-testable without a live DB:

```csharp
// Pure-ish over an injected profile lookup so tests can supply a synthetic graph.
static string BuildBondTree(string rootUserName, int degrees, bool familyOnly,
                            Func<string, Profile> getProfile);
```

- `!bondtree` → `BuildBondTree(root, n, familyOnly: false, MonDB.getProfile)`.
- `!familytree` → `BuildBondTree(root, n, familyOnly: true, MonDB.getProfile)`.

Two real command classes (not an alias of each other — they differ in the type filter), both `Category = "Information"`, `RequireChannel = false` (PM result), no cooldown, `RelatedCommands = { "bond", "dossier" }`. Names `bondtree` / `familytree` collide with nothing in the dispatch table.

Display names resolved at render via the loaded profile's `displayName`, falling back to the stored userName when a profile is missing.

---

## Files to create / modify

**Create:**
- `FChatDicebot/BotCommands/ChateauBondtree.cs` — `!bondtree` (+ the shared static `BuildBondTree`, or place the helper in a small support class).
- `FChatDicebot/BotCommands/ChateauFamilytree.cs` — `!familytree`.
- `FChatDicebot.Tests/Unit/Chateaubondtreetests.cs` — traversal/rendering tests over a synthetic `getProfile`.

**Modify:**
- `FChatDicebot/BotCommands/ChateauHelp.cs` — register both under Information.
- `wiki-docs/Feature-Requests.md` — B10 bullet retires on ship.

No DB-layer change — reads ride existing `GetProfile`.

---

## Tests (`BuildBondTree` over a synthetic graph)

- Root with no bonds → empty-state string (both all-bonds and family-only variants).
- Direct bonds only (N=1) → correct names + roles, correct perspective for an initiated vs a received edge.
- N=2 reaches second-degree people; a person bonded at both degree 1 and degree 2 appears **once**, at degree 1.
- Cycle (A↔B, and A–B–C–A) terminates and lists each person once.
- `familyOnly` includes only `{marriage, offspring, sibling, family}` edges and excludes others (e.g. `pet`, `rival`).
- Node cap: a graph wider than 100 stops at 100 with the "limit reached" note.
- Depth clamp: N>3 behaves as N=3.
- Missing neighbor profile → falls back to the stored userName, traversal continues.

---

## Decisions resolved (owner, 2026-06-21)

- **B10.1** `!familytree` is a **subset** of `!bondtree`: family bond types `{marriage, offspring, sibling, family}` (family == kin, one type).
- **B10.2** Default **N=2**, **max 3**, hard **100-node** cap.
- **B10.3** Root = **self by default**, optional `[user]` target (public, no consent).
- **B10.4** **Show the role** on each connection.
- **B10.5** **Grouped-by-degree** layout; PM with `[spoiler]` fallback.

## Open items

- Empty-state and header strings pending owner wording approval.
- Whether to also show the **bond type/role from the root's own perspective** vs the connecting person's (spec uses "relative to whoever connected them" — confirm that reads well, or switch all labels to be relative to the root).
- Optional later: a depth-0 self line, counts per degree, or an ASCII/indented tree rendering instead of grouped lists.

## Assumptions

- Bond lists only ever contain valid userNames written by `BondProcessor`; defensive skip if a referenced profile is gone.
- 100 nodes × up to 3 degrees is a tolerable number of `GetProfile` reads for an on-demand command (no caching layer added).
- F-Chat output constraints match existing outputs (4096-char cap, BBCode, `[spoiler]` fallback if long).
