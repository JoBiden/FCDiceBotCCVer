# Chateau Contract — User-Facing Text Style Guide

A first-draft style guide for any string a resident will see — consent prompts, completion messages, errors, help text. Distilled from patterns already shipping in the bot, not prescribed from outside it. The bot's owner reviews every changed user-facing string before merge regardless; this guide is for getting the first draft close.

## 1. Voice and narrator

- **The Chateau is the narrator** — a place/institution that speaks for itself in first-person plural ("We welcome all monsters to our Chateau", "Our policy is...", "according to our records", "our clerks"). Use this voice for errors and meta commentary.
- **Channel-visible interactions are third-person narration.** Subject is always the initiator's `displayName`; the recipient is the object.
- **The reader is addressed as "you" only in two cases:** private error messages, and consent prompts (which the recipient receives).
- **Tone is warm, playful, and welcoming — even for hostile or transformative acts.** "We welcome all monsters", "May you enjoy a bright future together", "Wear it with pride~". The Chateau is benevolent and a little mischievous.
- **Players are "residents."** Not "users", not "players."

## 2. Names

- **Always use `displayName`, never `userName`,** in any string a player sees. `userName` is the F-Chat handle and may be wrapped in `[s]old[/s] new` after a rename.
- **No `[user]` wrap around names in narration** — they're interpolated bare. `[user]…[/user]` only appears in literal usage examples in help text.
- **Self-target uses "themself" / "themselves"** rather than repeating the display name. (Yes, "themself" reflexive — that's the established form, e.g. `GoldenProcessor.cs`, `CorruptionProcessor.cs`.) Self-paths often deserve unique wording — see `MilkProcessor.PairLockMessage` ("You've already milked yourself today" vs "You've already milked X today").

## 3. The consent-prompt template

Every prompt asks `Do you !consent to ___?` and closes with `(or !no)`:

> `{initiator} <intent verb> {recipient} <detail>! [b]<seriousness warning>[/b] Do you !consent to <recipient-framed action>? (or !no)`

- **Intent verb phrases** vary by tier flavor: "wants to" (casual, friendly), "is going to" (most involved/consequence), "is about to", "intends for", "is gearing up to", "would like to declare", "graciously offers to". Match the tone of the act.
- **The `to ___` blank rephrases the action from the recipient's perspective:** "to a smooch", "to that sting", "to becoming an object", "to being bred", "to this life-changing occasion", "to receiving this payment". It should read as something the recipient experiences, not what the initiator does.
- **The seriousness warning is canonical for commitment and consequence tiers** and is almost always the literal phrase:
  > `[b]This should not be taken lightly, and can not be done frequently.[/b]`

  Casual prompts skip it. Custom warnings exist (e.g. `EntitleProcessor`, `RenameProcessor`) when there's something specific the recipient needs to know — keep them bold.
- **Never write the `(or !no)` reminder yourself.** It's applied by `InteractionProcessorBase.GetConsentWarning` from `ConsentWarningText.DeclineHint`, so it stays identical everywhere and reaches group announcements too. It's there because a consent flow that only names the yes command isn't offering a real choice.
- **The reminder belongs with the question, not at the very end.** Anything that trails the prompt — status-effect fragments, small-print notes — goes *after* it: `Do you !consent to some cuddles? (or !no) Bob is carrying a heavy musk.` A builder that appends status fragments must use `ComposeConsentWarning(baseWarning, fragments)` to get this ordering.
- **Bookkeeping notes are `[sub]`, not parentheticals.** When a prompt has to report something about the initiator's input rather than the act itself, put it in small print after the reminder — see `!break`'s `[sub]Duration was adjusted to the minimum of 1 day.[/sub]`. Two parentheticals in a row read as a stutter.
- **A prompt that doesn't ask a yes/no question needs a spelled-out reminder.** `ConsentWarningText.DeclineHintVariant("to opt out entirely")` yields `(or !no to opt out entirely)` and suppresses the default. The `!sit` lap stack uses it: it announces a race to consent, so a bare `(or !no)` would read as if declining only forfeited the bottom spot.

## 4. Completion-message template

Channel-visible "the thing happened" line. Shape:

> `{initiator} <verb-phrase> {recipient}<detail>. <closing flavor sentence>`

- **Verb tense is present or present-perfect:** "has bestowed", "has petrified", "has bred", "milks", "winds up and gives", "share a kiss". Past-perfect ("has") for heavier/permanent acts; simple present for moment-to-moment casual ones.
- **Always close with a flavor sentence** — a benediction, a question, an ominous trailing thought. Examples in use:
  - Warm: "May you enjoy a bright future together.", "Enjoy your new life as a wolf~", "May they wear it with pride!"
  - Playful: "Was it yummy? I bet it was.", "Do a spin for everyone, let them admire your new garb!"
  - Ominous: "they were never heard from again... or at least, it will be quite some time before they manage to escape", "hopefully visitors enjoy the pose they're stuck in."
- **Casual completions use a random descriptor list** (see `KissProcessor`, `CuddleProcessor`, `SpankProcessor`, `BullyProcessor`, `HandholdProcessor`). Keep descriptors short, varied in register (cute, lewd, dry, breathless), and end with appropriate punctuation per descriptor — they're sentence-fragments appended to a setup.

## 5. Verb tense helpers

If you add a new interaction whose past/present/future form isn't a regular `-ed`/`-s`/`will -`, override `GetInteractionVerb(VerbTense)` with all four forms (Past, Present, Future, Infinitive). Pattern at `BullyProcessor.cs`. This feeds dossier/history rendering, so it has to read naturally in "X bullied Y." sentences.

## 6. Identifier rendering

- **Never interpolate the raw identifier** for substances, bodyparts, attire, jobs, bonds, locations. Use the matching `Utils.SubstanceToText` / `BodypartToText` / `AttireToText` / `JobToText` / `BondToText` / `LocationToText`.
- **For monsters/plants/objects use `Utils.AnOrA(id) + " " + id`** for "a wolf"/"an alraune". See `MonsterizeProcessor`.
- **For scents use `ScentText.ScentPhrase`** — it data-drives the form ("Alice's musk" vs "a scent of lemonade" vs "a wood scent") from the identifier's categories. Re-use this same helper in every place a scent surfaces so the rendering doesn't drift.
- **Missing-mapping fallback is a spoiler tag that calls out the admin to fix it.** Pattern: `"something ineffable [spoiler]that means [user]Queen Contract[/user] needs to update Utils.XToText to include " + foo + ", go tell her to fix it![/spoiler]"`. Preserve this exact tone — it doubles as a bug report.

## 7. BBCode conventions

| Tag | Use case | Example |
|-----|----------|---------|
| `[b]…[/b]` | Seriousness warnings; key numbers in payouts; one-word labels like `[b]corrupt[/b]`/`[b]pure[/b]`/`[b]thick[/b]` | "sold … for [b]7 copper[/b]" |
| `[i]…[/i]` | Concept words on first mention | "others will be allowed to [i]Interact[/i] with you" |
| `[s]…[/s]` | Strikethrough old name after rename | `[s]oldname[/s] newname` |
| `[eicon]…[/eicon]` | Special-case flourishes, mostly Queen Contract reactions; appended at the end of a message | `message += "[eicon]qckiss[/eicon]"` |
| `[noparse]…[/noparse]` | Wrap `[user]` and `[eicon]` examples in help text so they render literally | `Usage = "!milk [noparse][user]NameInUserTag[/user][/noparse] {substance}"` |
| `[spoiler]…[/spoiler]` | Hidden admin nudges in the missing-mapping fallback strings |  |

`[user]…[/user]` appears in literal usage examples and inside error text — not in narration.

### 7a. Colour

F-Chat resolves `[color=x]` to a **fixed hex regardless of theme** (`scss/_flist_derived.scss` in `f-list/exported`; only `blue` is redefined per theme, `#00f` → `#06f` on dark). The palette was tuned for a black background, so most of it has poor contrast on the light theme. Measured against the real backgrounds (`#000` dark, `#e5e5e5` light):

| Colour | Contrast dark / light | Use |
|--------|----------------------|-----|
| `blue`, `purple`, `brown`, `red` | ≥3:1 on both | the readout section palette — safe anywhere |
| `orange`, `green`, `yellow`, `cyan`, `pink`, `gray` | fails on light | sparingly, and only where losing it on one theme is acceptable |
| `black`, `white` | each invisible on one theme | **never** |

The `!help` tier line deliberately keeps green → yellow → orange → red because the stoplight ramp reads as escalating seriousness, which is worth more there than palette consistency. `ViceText` likewise keeps `[color=yellow]` for golden fluid.

### 7b. The readout grammar

Every informative readout (`!dossier`, `!bank`, `!statistics`, `!bottles`, `!titles`, `!pledges`, `!business`, `!help`, the drill-downs) builds its output from `Model/ReadoutText.cs`. Don't hand-assemble these tags — the helpers exist because five commands had drifted into five different conventions for the same data shape.

| Element | Helper | Form |
|---------|--------|------|
| Title line | `ReadoutText.Title(text)` | `[b][u]Title[/u][/b]` — exactly one, first line, the only bold-underline in the readout |
| Masthead | `ChateauDossier.Masthead` | `[sup]…[/sup]` letterhead above the title line. `!dossier` only — see below |
| Section header | `ReadoutText.Section(header, domain)` | `[b][color=x]Header:[/color][/b]` — Sentence case, colon applied by the helper |
| Row label | `ReadoutText.Label(label)` / `Row(label, value)` | `[u]Label:[/u] value` — Title Case |
| Number | `ReadoutText.Num(value)` | `[b]148[/b]` — the unit stays outside the tag |
| Small print | `ReadoutText.Small(text)` | `[sub]…[/sub]` |
| Footer | `ReadoutText.Footer(text)` | `[sub]…[/sub]` on its own last line, pointing at sibling commands |
| Empty slot | `ReadoutText.None()` | `[sub]None[/sub]` — never a bare body-weight "None" |
| Row indent | `ReadoutText.RowIndent` | two spaces, for rows under a section header |
| Inline separator | `ReadoutText.InlineSeparator` | three spaces, the only separator |
| One-line block | `ReadoutText.InlineSection(header, domain, cells)` | header plus cells on one line |
| Multi-line block | `ReadoutText.LineSection(header, domain, summary, rows)` | one indented row per line; collapses to `[spoiler]` past 6 rows with the count left visible |

`ReadoutDomain` picks the section colour so the same kind of fact wears the same colour in every readout:

| Domain | Colour | Covers |
|--------|--------|--------|
| `Affliction` | red | curses, parasites, breaks, scents, corruption |
| `Relationship` | purple | bonds, marks, employment rosters, populations, pledges |
| `Record` | blue | interaction counts, offspring, lifetime totals |
| `Economy` | brown | vaults, collections, payroll, workforce |
| `Reference` | purple | `!help` fields, identifier lookups |
| `None` | none | history and neutral sections |

**A section with no qualifying rows is hidden entirely, header included.** Both `InlineSection` and `LineSection` return empty for an empty row list; hand-rolled blocks must do the same. A bare coloured heading introducing nothing reads as a bug.

**Leave a blank line between colour changes.** Sections of the same `ReadoutDomain` sit flush against each other; a change of domain gets a blank line, so the colour shift reads as a new part of the document rather than a stray highlight. `ChateauDossier.BuildFullDossier` does this by grouping sections into clusters and joining them with `ReadoutText.JoinClusters`, which also drops empty clusters so an absent group leaves no gap.

**`!dossier` opens with a masthead, not its title line.** F-Chat prepends the sender name and timestamp to the first line of a message only, so whatever lands there is indented relative to everything below it. `!dossier` spends that line on `[sup]Official Chateau Contract Dossier[/sup]` and puts the resident's name on line two, flush left with the rest of the document. Other readouts are short enough that the indent on their title line doesn't matter; if that changes, reuse this pattern rather than inventing a second one.

## 8. Punctuation and flavor

- **Exclamation marks are abundant.** Nearly every completion and most consent prompts end with `!`. The Chateau is enthusiastic. Use them.
- **`~` at the end of a phrase signals affection or playfulness** — "Wear it with pride~", "Enjoy your new life as a wolf~". Use sparingly, for genuinely warm moments.
- **`...` carries the dramatic pause.** Ominous trailings ("they were never heard from again..."), suggestive lewdness ("That's kind of lewd..."), and uncertainty ("Who knows what's in store for them..."). Don't use em-dashes for this.
- **Questions in self-narration** are part of the voice — "Was it yummy? I bet it was.", "Who knows what's in store for them, but...", "When's the wedding?"
- **Quoted user input** (custom titles, names) uses straight double quotes inside the string: `"{title}"`.

## 9. Day-boundary language

- **Never write "UTC" in any user-facing string.** Use "today", "per day", "Chateau day", "1 day".
- **Time-remaining gates use `Utils.GetTimeSpanPrint`** ("2 days, 3 hours, 12 minutes, 5 seconds") and always come with a remedy phrasing: "You can X again in {remaining}.", "You'll be able to Y again in {remaining}."

## 10. Errors and private messages

- **Tone: friendly, instructive, never blaming.** Open with the situation, not the user's failure.
  - "It doesn't look like you have a job to work yet!"
  - "It doesn't appear as if you have set a mark yet!"
  - "We appreciate your enthusiasm, but…"
  - "We don't track corruption interactions that fail to move the needle."
- **Always include a remedy.** Tell the user the next step — the command to run, the !consent flow, when they can retry, where to look. See `ChateauInteractionHandler.notFoundText` (suggests Tab autocomplete) and `markNotSetText` (points at `!setmark` and suggests `[eicon]blank[/eicon]`).
- **The Chateau speaks as "we" / "our".** "Our policy is to assume…", "We have a firm policy of consent…", "according to our records".
- **"Mind your spelling!" / "Make sure you spelled it correctly!"** is a recurring sign-off for "not found" errors. Keep it light.
- **Centralize wording when the same error can fire from two paths.** `MilkProcessor.PairLockMessage`, `CorruptionProcessor.QuotaExhaustedPrivateMessage`, `ChateauInteractionHandler.typeNotFoundText` are the precedent: pre-check at the command layer and TOCTOU recheck at the processor draw from one helper.

## 11. Tier-flavor cheat sheet

| Tier | Consent-prompt voice | Completion-message voice | Closing flourish |
|------|---------------------|--------------------------|------------------|
| Casual | "wants to", "is about to", "is gearing up to" | Simple present, randomized descriptor list, sometimes onomatopoeia ("Mwah!", "Ooh,") | Random short fragment |
| Involved | "is going to", "wants to" | Present, slightly more descriptive | "Was it yummy? I bet it was." / "Bottled, sealed, and tagged." |
| Commitment | "is going to", "graciously offers", "would like to declare" + bold warning | Present-perfect "has X-ed" | "May they wear it with pride!" / "May you enjoy a bright future together." |
| Consequence | "is going to" + bold warning + recipient-framed prompt | Present-perfect, transformation framing | Welcome (monster/plant) or ominous (consume/petrify) |

## 12. Help-field conventions

For new `ChatBotCommand` subclasses:

- **`ShortDescription`** — single short sentence describing what the command does. (Trailing period is inconsistent in the codebase; pick one and don't worry about it.)
- **`LongDescription`** — full prose, 2–4 sentences, explains mechanics including cooldown shape and any nuance (e.g. "per pair", "once per Chateau day").
- **`Usage`** — literal command with `{identifier}` braces for category arguments and `[noparse][user]NameInUserTag[/user][/noparse]` for the user-tag slot. Multiple forms separated by `\nor\n` (see `ChateauSell`, `ChateauWash`).
- **`CooldownDuration` / `CooldownAppliesTo`** — short human strings: "1 day", "1 day, per-pair", "both initiator and recipient". Not formal time formats.

## 13. Things to avoid

- **No "UTC" or implementation jargon** (TOCTOU, identifier, characteristic, profile, pending command, processor) anywhere a resident might read it.
- **No emoji** in any user-facing string. (Eicons are the bot's emoji equivalent and they're tag-wrapped.)
- **No "users", "players", or "you all"** for residents. The Chateau addresses one person at a time, or names them.
- **No raw identifier names where a `*ToText` helper exists** — they'll render unstyled.
- **No em-dashes (`—`) in any user-facing string.** Use `...` for dramatic pauses, periods for hard breaks, or commas/parentheses for asides. Em-dashes may not render correctly in all clients.
- **No silent failures.** Every refusal path must say something — even the rare TOCTOU paths get a private heads-up message (see `CorruptionProcessor` `_lastInitiatorPrivateMessage`).
