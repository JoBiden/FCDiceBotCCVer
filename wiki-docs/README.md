# Documentation Guide

How to edit and expand the docs in `wiki-docs/` without letting them rot. This page is written for whoever touches the docs next — human or coding agent.

## The one rule

**Docs describe the code that exists, verified against the code, at the moment you write them.** The 2025 generation of these pages documented commands that never existed (`!addtable`, `!reject`, `!restore`) and API sketches that were never real; it took a full audit (August 2026) to purge. Never write a fact you haven't checked, and never keep a fact you can't check cheaply.

## Doc map

| Page | Covers | Update when… |
|------|--------|--------------|
| [Home](Home.md) | Landing page, feature overview | a headline feature ships or a listed fact changes |
| [Architecture](Architecture.md) | Components, data flow, patterns, extension points | components/registries/hooks/timing constants change |
| [Development-Guide](Development-Guide.md) | How to build, add commands/interactions/games, test | the dev workflow, base-class contract, or test setup changes |
| [Interaction-System](Interaction-System.md) | Consent flow, investment levels, cooldowns, titles, jobs | interaction machinery changes (new tier behavior, lifecycle verbs, cooldown shapes) |
| [Command-Reference](Command-Reference.md) | Every command, by category | **any** command is added/renamed/re-aliased, or help text/usage changes |
| [Database-and-Persistence](Database-and-Persistence.md) | Mongo collections, models, file storage, MonDB | a collection, model field, or storage convention is added/changed |
| [Dice-and-Games](Dice-and-Games.md) | Legacy dicebot: dice, tables, decks, chips, games | rarely — the dicebot is stable |
| [F-List-Integration](F-List-Integration.md) | Protocol, auth, reconnect, rate limits | connection handling changes |
| [Installation-and-Setup](Installation-and-Setup.md) | Prereqs, config files, running the bot | build/config/run-mode changes |
| [Style-Guide](Style-Guide.md) | Rules for resident-facing strings | a new text pattern becomes canon (owner's call) |
| [Feature-Requests](Feature-Requests.md) | **Unscoped ideas only** | an idea is proposed; remove entries once specced/shipped |
| `specs/` | Per-feature design + as-shipped docs | every feature — see [specs/README](specs/README.md) for its own conventions |

Cross-cutting map: a **new interaction** touches `specs/` (as-shipped spec), Command-Reference (row in its tier table + aliases table if any), Interaction-System (tier list), and — only if it adds machinery — Architecture. A **help-text tweak** touches Command-Reference alone.

## Writing rules (anti-rot)

1. **Point, don't paste.** Name the file that owns a fact and summarize it; don't transcribe it. A code snippet is allowed only if it's copied from the repo (trim, don't invent) or is a deliberately minimal skeleton labeled as such. Never write speculative "this is roughly what the class looks like" C# — that's how `MonDB.setProfile(profile)` got documented with the wrong arity for a year.
2. **Name the source of truth.** The best doc sentence has the shape "X does Y — `path/File.cs` is authoritative." Examples already in use: "the `switch` in `BotMain.OnMessage` is authoritative", "grep `GetCollection<` for the current list". Give the reader the verification move, not just the claim.
3. **No volatile numbers.** Line counts, exact command counts, and processor tallies rot within weeks. Write "~210 as of mid-2026" style approximations at most, and only where the magnitude matters. Constants are fine when named (`MaximumDice` = 200) because the name tells the reader where to re-check.
4. **Don't restate values a code SSOT owns.** Cooldown strings come from `CooldownSpec`, consent-warning text from `ConsentWarningText`, readout formatting from `ReadoutText`. Docs should say *that* and point there — a doc that copies the current wording will be wrong after the next text pass.
5. **Defer to the bot for player-facing truth.** `!help` is generated from the command classes and cannot drift. Command-Reference explicitly ranks below it; keep that framing in anything you add.
6. **Delete confidently.** A wrong or unverifiable paragraph is worse than no paragraph. When you can't verify a claim cheaply, remove it or rewrite it as a pointer.
7. **Docs ride the same PR as the code.** A change and its doc update land together — "update the docs a change invalidates" is a review requirement (CLAUDE.md), not a follow-up task.
8. **Wiki prose is developer-facing** — plain technical writing, em-dashes welcome. The Style-Guide's rules (no em-dashes, Chateau voice, etc.) apply to *resident-facing strings in the bot*, not to these pages.

## Verification snippets

Run these from the repo root when auditing or updating:

```bash
# All command names in the codebase
grep -rh 'Name = "' FChatDicebot/BotCommands/*.cs | sed 's/.*Name = "\([^"]*\)".*/\1/' | sort -u

# Count command classes
grep -rl ': ChatBotCommand' FChatDicebot --include=*.cs | wc -l

# All registered interaction processors
grep -n 'RegisterProcessor' FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs

# All Mongo collections in use
grep -oh 'GetCollection<[^>]*>("[^"]*")' FChatDicebot/Database/Chateaudatabase.cs | grep -oh '"[^"]*"' | sort -u

# A command's real metadata (usage, aliases, category)
grep -n 'Name =\|Aliases =\|Usage =\|Category =\|ShortDescription =' FChatDicebot/BotCommands/ChateauKiss.cs
```

Before documenting any command, confirm it appears in the first list. Before documenting any method, open the file.

## Specs and Feature-Requests lifecycle

```
idea → Feature-Requests.md (one bullet, unscoped)
     → specs/Future-*/Name.md (pre-implementation template; delete the bullet)
     → ships: move spec up out of Future-*/, add "**Status:** Implemented." + Files section
              + as-shipped divergence notes; update the tables in specs/README.md
```

Full conventions in [specs/README](specs/README.md). Feature-Requests must never accumulate shipped items — the August 2026 cleanup found six.

## Checklist for a docs-touching PR

- [ ] Every new factual claim verified against code (not memory, not another doc)
- [ ] Command names checked against the live command list
- [ ] No new volatile counts or invented code samples
- [ ] Cross-page links still valid; the doc-map row for anything you restructured still accurate
- [ ] If the change altered resident-facing strings: those went through the owner-approval flow (Style-Guide / CLAUDE.md), which is separate from this checklist
