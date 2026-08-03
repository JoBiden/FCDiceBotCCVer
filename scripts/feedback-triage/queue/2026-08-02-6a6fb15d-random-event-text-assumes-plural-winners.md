# 6a6fb15d — Random event result text says "each" when there's only one winner

| | |
|---|---|
| **Feedback id** | `6a6fb15d4770dbd4a8792faa` |
| **Submitted** | `2026-08-02T21:06:37Z` (today) by **Queen Contract** (`Queen Contract`) |
| **Source** | channel `ADH-ac1885cd73f31adfaefb` |
| **Tagged** | `bug` |
| **Verdict** | `wording` |
| **Recommendation** | `fix` |
| **Confidence** | high — exact string located in the live event data |
| **Effort** | trivial (data only) / small (if the engine gains a count-aware placeholder) |

## What they said

> event text still outputs 'each' with single participants, "[user]Fia Violafalki[/user] each happen to receive a taste of [b][color=black]corruption[/color][/b]. It tastes rich and decadent."

## What it means

A random event fired, exactly one resident won it, and the announcement read as if several
people had. The reported sentence is the "Just A Taste" slime-twins event's outcome text, with
`{winners}` substituted for the single winner — leaving the surrounding plural grammar ("each
happen to receive") stranded.

## Assessment

**This is authored event data, not engine output.** The string is not in the C# — no source
match for the phrase — and the `[color=black]corruption[/color]` markup is not a shape the
engine produces (its own corruption fragment is `"[b]" + amount + "[/b] corruption"`,
[RandomEventEngine.cs:589](../../../FChatDicebot/BotCommands/Support/RandomEventEngine.cs:589)).

The source is `RandomEvents` document `6a53696f814fcb51bb23cf4f`, both of its outcomes:

```
{winners} each happen to receive a taste of [b][color=black]corruption[/color][/b]. It tastes rich and decadent.
{winners} each happen to receive a taste of [b][color=white]purity[/color][/b].    It tastes pure and invigorating.
```

`SubstituteWinners`
([RandomEventEngine.cs:663](../../../FChatDicebot/BotCommands/Support/RandomEventEngine.cs:663))
does a straight `Replace("{winners}", ...)`. It joins the names correctly ("A, B and C") but has
no notion of winner count, so it cannot touch the author's surrounding grammar.

Worth noting the engine gets this right for *its own* line: the grouped reward line picks
`" receive "` vs `" receives "` off `tags.Count`
([RandomEventEngine.cs:352](../../../FChatDicebot/BotCommands/Support/RandomEventEngine.cs:352)).
So the message the resident saw is half correct-by-code and half wrong-by-data.

### This is not a one-string problem

`allInWindow` is the **only** winner rule with a variable winner count — `firstValid`, `nth`,
and `random` each resolve to exactly one winner by construction
([RandomEventEngine.cs:501-520](../../../FChatDicebot/BotCommands/Support/RandomEventEngine.cs:501)).
So every `allInWindow` event whose `resultText` names `{winners}` is exposed. Surveying all 12
authored events, three are affected — four outcome strings in total:

| Event | Rule | Current text | Reads as, with one winner |
|---|---|---|---|
| `6a53696f` "Just A Taste" | allInWindow | `{winners} each happen to receive a taste of …` (×2 outcomes) | the reported bug |
| `6a45bfc3` "Cutie says" | allInWindow | `{winners} are all officially cuties, …` | "Fia **are all** officially cuties" |
| `6a53651d` "Everybody dance now" | allInWindow | `… as {winners} bust it down on the dance floor.` | "Fia **bust** it down" |

The other nine are safe: single-winner rules with correctly singular text (`{winners} is the
6th to join…`, `only {winners} has the honor…`), vocatives (`It's your lucky day, {winners}!`),
or no placeholder at all.

**The authoring guidance actively causes this.** The builder's placeholder example is
`{winners} are showered in rose petals!`
([scripts/random-event-builder/ui.html:462](../../../scripts/random-event-builder/ui.html:462))
and its `allInWindow` help says "everyone wins together (use {winners} in result text)"
([ui.html:594](../../../scripts/random-event-builder/ui.html:594)) — both steering authors to
plural on the one rule where the count varies. The spec's own example is plural too
(`"{winners} are now glowing purple."`,
[Random-Events.md:195](../../../wiki-docs/specs/Future-Social/Random-Events.md:195)). Nothing
warns that `allInWindow` can resolve to one person. So the next authored event repeats this.

- **Reproduces / confirmed:** yes — the exact string is in the live `RandomEvents` document, and
  the single-winner path substitutes it verbatim
- **Related code:** `RandomEventEngine.cs:663` (`SubstituteWinners`), `:352` (the reward line
  that *does* agree), `:501` (winner rules), `scripts/random-event-builder/ui.html:462,594`
- **Related specs / docs:** `wiki-docs/specs/Future-Social/Random-Events.md` §195 — documents
  `{winners}` as a plain substitution and is silent on count agreement
- **Duplicate of:** none
- **Already fixed since submission:** no — no commits since `efa818c` (New icon), and the live
  data still carries the string
- **"still outputs"** — the submitter's word suggests they may have reported this before. There
  is no earlier row in the ledger and no matching entry in the `Feedback` collection, so this is
  the first written record of it; they may have raised it verbally.

## Recommended action

Reword the four outcome strings to be count-neutral. It is a data edit in the `RandomEvents`
collection (via `scripts/random-event-builder`, or a direct update), takes effect on the next
fire with **no code change and no redeploy**, and clears everything a resident can currently
see.

That leaves the authoring hazard open, which is the part worth a decision: the builder still
suggests plural text for `allInWindow`, so event #13 lands the same way. The cheap half of that
is a note in the builder's help; the durable half is giving authors a count-aware placeholder.
Recommend doing the data fix now and treating the engine affordance as a separate, specced item
rather than bundling them.

Note this item's fix does **not** flow through the normal implement-a-PR path — the strings live
in Mongo, not in the repo. If the engine option is chosen, that half is a normal PR.

## Decisions needed from the owner

1. **How far does the fix go?**
   - **Data only, now** (recommended) — reword the four strings; nothing ships, nothing
     redeploys, the visible bug is gone today. The hazard remains for future events.
   - **Data + a builder warning** — also reword the builder's example and `allInWindow` help so
     the next author is told the count can be one. Small, repo-side, no bot change.
   - **Data + engine placeholder (spec first)** — add count-aware authoring to
     `SubstituteWinners`, e.g. a paired form `{winners} {is|are} …` / `{winners}
     happen{s} …`, resolved on winner count. Durable, but it is new authored syntax: needs a
     spec, tests, a builder update, and a redeploy.

2. **Who writes the replacement flavor?** These are your event prose, not framework strings.
   - **You reword them** (recommended) — I have drafted minimal count-neutral edits below purely
     as a starting point; the voice is yours.
   - **Take the drafts as-is** — they change as few words as possible and keep the imagery.

3. **Should `allInWindow` events be authored to assume one winner, or many?** Answering this
   once settles the guidance the builder should give.
   - **Neutral phrasing always** (recommended) — safest, works at any count, costs some flourish.
   - **Plural, with the engine fixing agreement** — only viable if option 1c is taken.

## User-facing text that would change

All four are `resultText` in the `RandomEvents` Mongo collection, not the repo. Drafts keep the
imagery and change as few words as possible; each is count-neutral rather than newly plural.

| Where | Now | Proposed |
|---|---|---|
| `6a53696f` outcome 1 | `{winners} each happen to receive a taste of [b][color=black]corruption[/color][/b]. It tastes rich and decadent.` | `{winners} happen to get a taste of [b][color=black]corruption[/color][/b]. It tastes rich and decadent.` |
| `6a53696f` outcome 2 | `{winners} each happen to receive a taste of [b][color=white]purity[/color][/b]. It tastes pure and invigorating.` | `{winners} happen to get a taste of [b][color=white]purity[/color][/b]. It tastes pure and invigorating.` |
| `6a45bfc3` outcome 1 | `{winners} are all officially cuties, as recognized by the walls of the Chateau itself!` | `The walls of the Chateau itself recognize {winners} as officially certified cuties!` |
| `6a53651d` outcome 1 | `Everyone follows her lead, as {winners} bust it down on the dance floor. It's a lot of fun, and she has some awesome moves that are surprisingly easy to pick up.` | `Everyone follows her lead, and {winners} end up owning the dance floor. It's a lot of fun, and she has some awesome moves that are surprisingly easy to pick up.` |

`{winners} happen to get` and `{winners} end up` read correctly at one name or several — English
lets a bare name take the plural-form verb here without sounding wrong, which is what makes the
neutral phrasing possible at all.

If option 1b is taken, two more strings change:

| Where | Now | Proposed |
|---|---|---|
| `ui.html:462` placeholder | `{winners} are showered in rose petals!` | `{winners} end up showered in rose petals!` |
| `ui.html:594` allInWindow help | `Collects every correct responder until the window closes, then everyone wins together (use {winners} in result text).` | `Collects every correct responder until the window closes, then everyone wins together (use {winners} in result text). Only one person may answer, so avoid wording that assumes a crowd.` |

## Owner decision

**2026-08-02 — Approved, with the durable fix.**

Design answers:

1. *How far does the fix go?* — **Data + engine placeholder, spec first.** Not the recommended
   option: the dossier recommended the data-only edit now and treating the engine affordance as a
   separate item. The owner took the durable route, so `SubstituteWinners` gains count-aware
   authored syntax — new authored syntax, so it needs a spec, tests, a builder update, and a
   redeploy before any of it reaches residents.
2. *Who writes the replacement flavor?* — **The owner rewords them.** Drafts below are a starting
   point only; the final text is theirs and gets applied verbatim.
3. *Should `allInWindow` events be authored plural or neutral?* — **Plural, with the engine fixing
   agreement.** Consistent with (1), and it sets what the builder should tell authors: write for a
   crowd, mark the parts that have to agree.

4. *Sequencing* — the four live strings cannot be rewritten in the new paired syntax until the
   engine understands it, since doing so early renders the markup literally in-channel. Offered a
   count-neutral stopgap now versus waiting; the owner chose to **wait for the engine**. No Mongo
   edit happens in this pass, the wrong grammar stays live until the engine deploys, and the four
   strings are then written once, by the owner, in the new syntax.
5. *Start now?* — **Yes**, the engine and builder work started immediately rather than waiting on
   spec sign-off.

**Still owed by the owner after the engine ships:** the four `resultText` strings in the
`RandomEvents` collection. They are Mongo data, not repo files, so no PR closes this item — the
data edit is the last step and it is theirs to write.
