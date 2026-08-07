# Bond Tree — `!bondtree` / `!familytree`

**Status:** Implemented. Shipped 2026-06-22; the readout was reformatted through `ReadoutText` in the BBCode sweep that followed. The design below is as-shipped except where [Differences from the original spec](#differences-from-the-original-spec) says otherwise.
**Status (original):** Proposed (design-complete, owner-reviewed 2026-06-21).
**Feature-Requests source:** "!bondtree and/or !familytree which would map out all users connected to someone by N degrees of separation via bonds" (B10).

---

## Overview

Two read-only commands that traverse the bond graph and show everyone connected to a resident within N degrees of separation:

- **`!bondtree`** — walks **all** bond types.
- **`!familytree`** — walks only the **family subset** (see below).

Both are public, consent-free reads (bonds are mutually declared, public facts), built around one shared static builder so they differ only by which bond types are included. This is not an interaction; it rides the existing `GetProfile` reads with **no DB-layer change**.

---

## The bond graph (how bonds are stored)

The bond interaction writes **both directions** as profile `lists` ([BondProcessor.cs:54-75](../../FChatDicebot/InteractionProcessors/Commitment/BondProcessor.cs)):

- on the initiator: `bond{type}initiated` → list of recipient **userNames**
- on the recipient: `bond{type}received` → list of initiator **userNames**

So a person's **neighbors** = the union, over every bond type, of the userNames in their `bond{type}initiated` and `bond{type}received` lists. Because both ends are written, the graph is symmetric and fully reconstructable by reading one profile at a time.

**Bond types** (keys, with display role from [Utils.BondToText](../../FChatDicebot/Utils.cs)): `marriage`(spouse), `offspring`, `disciple`, `sibling`, `piety`(deity), `protection`(ward), `fealty`(lord), `family`(kin), `roommate`, `coworker`, `submission`(submissive), `ally`, `thrall`, `partner`, `fuckbuddy`, `property`, `rival`, `inspiration`, `client`, `subordinate`(boss), `pet`.

**Family subset (B10.1)** = `{ marriage, offspring, sibling, family }`.
> Note: "family" and "kin" are the **same** bond — bond type `family` renders as the role "kin" ([Utils.cs:1354](../../FChatDicebot/Utils.cs)). The owner's "marriage, offspring, sibling, family, kin" is four bond types, not five. Define the subset as a single `static readonly string[] FamilyBondTypes` so `!familytree` and any future family-only feature share it.

---

## Behavior

`!bondtree [user] [N]` and `!familytree [user] [N]`:

- **Root (B10.3):** default the invoker; an optional `[user]` argument roots the tree at someone else (public info, no consent). Resolve the target like other user-arg commands (`GetUserNameFromCommandTerms` / `MonDB.getProfile`); unknown user → the standard not-found reply.
- **Depth N (B10.2):** optional integer arg, **default 2**, **max 3** (clamp silently, or note the clamp). Degree 1 = direct bonds, degree 2 = their bonds, etc.
- **Node cap (B10.2):** hard cap of **100 distinct people** rendered. BFS outward; if the cap is hit, stop expanding and append a "…and more (limit reached)" note rather than ballooning the message or the profile-load count.
- **Dedup / cycles:** the graph has cycles (mutual and diamond bonds). BFS with a `visited` set keyed by userName; **each person appears once, at their shallowest degree** from the root. The root itself is degree 0 and not listed as a connection.

**Traversal.** Standard BFS from the root: for each frontier profile, enumerate neighbors by scanning its `lists` keys that match `bond{type}initiated` / `bond{type}received` (restricted to `FamilyBondTypes` for `!familytree`), load each unseen neighbor's profile (`MonDB.getProfile`) to continue and to resolve display names, and record the **edge role** for labeling. The 100-node cap bounds total profile loads.

**Edge role labels (B10.4).** Label each connection with the neighbor's role **relative to the person they were reached from**, using `Utils.BondToText` with the same perspective convention as [BondProcessor.GetCompletionMessage](../../FChatDicebot/InteractionProcessors/Commitment/BondProcessor.cs):
- reached via the parent's `bond{type}initiated` (parent was the initiator) → neighbor is the parent's `BondToText(type, true)` (e.g. "pet").
- reached via the parent's `bond{type}received` (parent was the recipient) → neighbor is the parent's `BondToText(type, false)` (e.g. "owner").

So a line reads, e.g., `Bob (Alice's pet)` — the neighbor, then their role relative to whoever connected them.

**Layout (B10.5):** grouped by degree, PM'd to the invoker, `[spoiler]` fallback past the usual length. As shipped the readout is assembled from `ReadoutText` rather than hand-written BBCode — a title of "Bond Tree of {Root}" (or "Family Tree of {Root}") over a subtext degree count, then one `Relationship`-domain section per degree with an indented `{Name} ({Connector}'s {role})` row each, names sorted within a degree. `BondTreeSupport.Render` is authoritative for the shape; `ReadoutText` owns the tags.

**Empty state:** `"{Root} has no bonds yet."` for `!bondtree`; `"{Root} has no family bonds yet."` for `!familytree`. Both shipped as specified, and both still carry a "pending owner review" note in `Render` — they have not had a wording pass.

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

## Files

| File | Holds |
|------|-------|
| [`BotCommands/Support/BondTreeSupport.cs`](../../FChatDicebot/BotCommands/Support/BondTreeSupport.cs) | The whole feature: `FamilyBondTypes`, the `DefaultDegrees` / `MaxDegrees` / `MaxNodes` / `SpoilerThreshold` constants, `ParseDegrees`, the BFS in `BuildBondTree`, and `Render` |
| [`BotCommands/ChateauBondtree.cs`](../../FChatDicebot/BotCommands/ChateauBondtree.cs) | `!bondtree` — resolves the root, then one `BuildBondTree(…, familyOnly: false, …)` call |
| [`BotCommands/ChateauFamilytree.cs`](../../FChatDicebot/BotCommands/ChateauFamilytree.cs) | `!familytree` — the same, `familyOnly: true` |
| [`Tests/Unit/Chateaubondtreetests.cs`](../../FChatDicebot.Tests/Unit/Chateaubondtreetests.cs) | Traversal and rendering over a synthetic `getProfile`; covers every case in [Tests](#tests-buildbondtree-over-a-synthetic-graph) below |

No DB-layer change — reads ride existing `GetProfile`. Both commands declare `Category = "Information"`, which is what puts them in the `!help` listing; there is no help-side registration step (see [Development-Guide](../Development-Guide.md#adding-a-new-command)).

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

---

## Differences from the original spec

The feature shipped close to the spec — the traversal, the caps, the family subset and the perspective convention are all as designed. Four things differ:

1. **The traversal lives in a support class, not in the `!bondtree` command.** The spec left this open ("or place the helper in a small support class"); `BondTreeSupport` won, so `!familytree` doesn't have to reach into a sibling command for the builder.
2. **The readout goes through `ReadoutText`.** The spec sketched a plain-text layout. The BBCode sweep that followed the ship routed every readout through `ReadoutText` / `ReadoutDomain`, so the degree headers are `Relationship`-domain sections and the role suffix is subtext. The `[spoiler]` fallback survived as a named `SpoilerThreshold` constant rather than a vague "past the usual length".
3. **Depth clamps at both ends.** The spec named a max of 3; `BuildBondTree` also floors at 1, so `!bondtree 0` and `!bondtree -5` render one degree instead of an empty tree. `ParseDegrees` takes the first term that parses as an integer, so the name and the depth can be given in either order — at the cost of reading a digit inside a name as the depth.
4. **`visited` is case-insensitive.** The spec keyed it on userName; the shipped set uses `OrdinalIgnoreCase`, because a name whose casing differs between the two ends of an edge would otherwise be walked twice and could keep a cycle alive.

Both open questions the spec left are still open, and one has an answer worth recording:

- **Role perspective** shipped as the spec proposed — relative to whoever connected the person, so a second-degree line reads `Carol (Bob's ward)`, not `Carol (your ward's ward)`. The alternative (everything relative to the root) was never tried; it stays a live option if the connector-relative reading proves confusing in play.
- **Empty-state and header wording** never had an owner pass. `Render` still carries the note.
- Never picked up: a depth-0 self line, per-degree counts, an indented tree rendering.

## Assumptions

- Bond lists only ever contain valid userNames written by `BondProcessor`; defensive skip if a referenced profile is gone.
- 100 nodes × up to 3 degrees is a tolerable number of `GetProfile` reads for an on-demand command (no caching layer added).
- F-Chat output constraints match existing outputs (4096-char cap, BBCode, `[spoiler]` fallback if long).
