# Winner-count agreement in random event text

**Status:** Design. Not implemented. Awaiting owner sign-off.

Origin: resident feedback `6a6fb15d` (2026-08-02), approved for spec on 2026-08-02. Dossier:
`scripts/feedback-triage/queue/2026-08-02-6a6fb15d-random-event-text-assumes-plural-winners.md`.

> event text still outputs 'each' with single participants, "[user]Fia Violafalki[/user] each
> happen to receive a taste of corruption"

Extends the shipped [Random-Events](Random-Events.md). Adds authored syntax that lets an event's
`resultText` be written for a crowd and still read correctly when exactly one person wins.

---

## The problem

`{winners}` is a plain `Replace` (`RandomEventEngine.SubstituteWinners`). It joins names correctly
("A, B and C") but has no notion of how many there are, so the author's surrounding grammar is
stranded at one winner:

| Authored | One winner reads as |
|---|---|
| `{winners} each happen to receive a taste of…` | the reported bug |
| `{winners} are all officially cuties` | "Fia **are all** officially cuties" |
| `as {winners} bust it down on the dance floor` | "Fia **bust** it down" |

`allInWindow` is the **only** winner rule with a variable count — `firstValid`, `nth` and `random`
each resolve to exactly one winner by construction (`ResolveWinners`). So every `allInWindow` event
whose `resultText` uses `{winners}` is exposed. Three of the twelve authored events are, across four
outcome strings.

The engine already gets this right for its own line: the grouped reward line picks `" receive "` vs
`" receives "` off `tags.Count`. The gap is only in authored text.

**The authoring guidance actively causes it.** The builder's placeholder example is
`{winners} are showered in rose petals!` and its `allInWindow` help says "everyone wins together" —
both steering authors to plural on the one rule where the count varies. This spec's own parent
([Random-Events.md](Random-Events.md) §Multi-winner resolution) uses a plural example too. Nothing
warns that `allInWindow` can resolve to one person, so the next authored event repeats this.

---

## Owner decisions this spec is built on

Answered during the 2026-08-02 feedback review.

1. **Data plus an engine placeholder, spec first.** Not the recommended option — the dossier
   proposed rewording the four live strings now and treating the engine as a separate item. The
   owner took the durable route.
2. **`allInWindow` events are authored plural, with the engine fixing agreement.** This is the
   authoring philosophy the builder should teach: write for a crowd, mark the parts that have to
   agree.
3. **The owner writes the replacement flavor** for the four live strings. Drafts are a starting
   point only.

---

## Syntax

**`{singular|plural}` — alternation, singular first.**

```
{winners} {happens|happen} to get a taste of corruption.
{winners} {is|are} now officially {a cutie|cuties}.
```

One rule covers verbs, nouns, articles, and pronouns, so an author never has to remember which
part of speech has special handling. The alternative — a suffix marker like `happen{s}` — is
compact for regular verbs and useless for *is/are*, *a cutie/cuties*, and *its/their*.

### Rules

- Resolution is on **winner count**: 1 → the left branch, anything else → the right branch.
- **Zero winners takes the plural branch**, matching English ("0 residents are"). `resultText` is
  not used on the no-winner path, so this is defensive only.
- **No nesting.** The first `|` inside a `{…}` separates the two branches; a second `|` is part of
  the right branch, verbatim.
- **A `{…}` with no `|` is left untouched.** This is what makes the change safe for every existing
  authored string: `{winners}`, `{keyword}`, `{challenge}`, `{window}` and any stray brace an author
  typed all pass through exactly as they do today.
- **No escape mechanism** in v1. An author who needs a literal `{a|b}` cannot have one. Worth
  adding only if that ever comes up.

### Where it applies

`resultText` only. `announceText` fires before anyone has responded, so there is no count to agree
with — applying it there would resolve every alternation to the plural branch and read as a silent
bug.

---

## Implementation

One new pure static on `RandomEventEngine`, applied inside the existing substitution path:

```
public static string ResolveCountAgreement(string template, int winnerCount)
```

Called **before** `{winners}` is substituted, not after. Names are inserted as
`[user]Name[/user]` tags, and resolving alternations first means the resolver never has to reason
about what a name might contain.

`SubstituteWinners` keeps its current job and its current `Replace`. The grouped reward line's
`tags.Count > 1` logic is already correct and stays exactly as it is — this spec adds nothing to
the engine's own output.

Everything is pure and needs no database, so it tests as a plain unit test rather than through the
fixture.

---

## Builder

`scripts/random-event-builder/ui.html` is where authors meet this, and it is currently the reason
the bug propagates. Three changes:

1. **The result-text placeholder** becomes an example that demonstrates the syntax:
   `{winners} {is|are} showered in rose petals!`
2. **The `allInWindow` rule hint** says the count can be one, and points at the alternation form.
3. **The preview renders both counts side by side** — one winner and three winners, simultaneously.

(3) is the part that actually prevents the bug. A fixed singular-first convention has a coin-flip
failure mode that renders fine half the time and is invisible until a real event fires with one
winner; a preview showing both makes getting it backwards impossible to miss while writing.

---

## The four live strings

These are authored data in the `RandomEvents` Mongo collection, **not in this repo**, and the owner
is writing the replacements. The drafts below are the dossier's minimal count-neutral versions,
kept here only as a starting point.

| Event | Current |
|---|---|
| `6a53696f` "Just A Taste" ×2 | `{winners} each happen to receive a taste of [b][color=…]…[/color][/b].` |
| `6a45bfc3` "Cutie says" | `{winners} are all officially cuties, as recognized by the walls of the Chateau itself!` |
| `6a53651d` "Everybody dance now" | `Everyone follows her lead, as {winners} bust it down on the dance floor.` |

### Sequencing — decided

The new syntax cannot be written into these strings until the engine understands it; doing it early
renders `{is|are}` literally in the channel, which is worse than the current bug. Offered a
count-neutral stopgap in the data now versus waiting, **the owner chose to wait for the engine**.

So: no Mongo edit happens before the deploy, the wrong grammar stays live until then, and the four
strings are written once, in the new syntax, by the owner. **That data edit is the last step of
this item and no PR closes it** — the strings live in Mongo, not in this repo.

---

## Files

### Modified

| File | Change |
|---|---|
| `BotCommands/Support/RandomEventEngine.cs` | `ResolveCountAgreement`, called ahead of `SubstituteWinners` |
| `scripts/random-event-builder/ui.html` | placeholder, rule hint, dual-count preview |
| `wiki-docs/specs/Future-Social/Random-Events.md` | §Multi-winner resolution documents the syntax; its plural example gains the markup |
| `FChatDicebot.Tests/Unit/Randomeventenginetests.cs` | see Tests |

No new files, no model change, no migration. A redeploy is required — this is engine code.

---

## Tests

Extend `Randomeventenginetests.cs`. All pure.

- one winner takes the left branch; two and three take the right
- zero takes the right branch
- multiple alternations in one string all resolve, independently
- a `{…}` with no `|` is untouched — assert specifically for `{winners}`, `{keyword}`,
  `{challenge}` and `{window}`, since silently eating one of those is the worst failure this change
  could cause
- a string with no braces at all is returned unchanged
- an unclosed `{` is left verbatim rather than throwing or swallowing the rest of the line
- a second `|` stays in the right branch verbatim
- end to end: an `allInWindow` outcome authored plural resolves correctly at one winner and at
  three, with `{winners}` substituted in the same pass
- alternation runs before name substitution — a name containing a brace or pipe cannot affect it
  (F-List names cannot, but the ordering guarantee is what the test pins)

---

## User-facing text

**Every string below is pending owner wording review.**

| Where | Change |
|---|---|
| four `resultText` strings in Mongo | owner-written; see above |
| builder placeholder | `{winners} are showered in rose petals!` → demonstrates the syntax |
| builder `allInWindow` hint | gains a sentence saying the count can be one |
| builder syntax help | new — documents `{singular\|plural}` |

Nothing player-facing changes in the C#. The engine's own reward line is untouched.

---

## Assumptions

1. **Singular first.** Reversible, but it has to be one or the other and be documented; the dual
   preview is what keeps authors honest either way.
2. **Zero winners takes the plural branch.** Defensive; unreachable on the shipped paths.
3. **No escaping** for a literal `{a|b}`.
4. **`resultText` only.**
5. **No nesting.**
6. **The four live strings are the owner's to write**, and the sequencing question above is open.
