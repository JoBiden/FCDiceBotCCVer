# Future Social & Events — Spec Index

Self-contained design specs for **upcoming** Chateau Contract community, relationship, and engagement features — the "Social/events" cluster from [Feature-Requests.md](../../Feature-Requests.md). Same conventions as [Future-Interactions](../Future-Interactions/README.md) and [Future-Economy](../Future-Economy/README.md): each file is written so a fresh implementation session can act on it without re-asking the user for design clarifications, and specs move one level up to [`specs/`](../) once they ship.

## Status

### Planned

| Spec | Adds |
|------|------|
| [Feedback](Feedback.md) | `!feedback` (+ `!suggestion` alias) for residents to submit an idea or bug report into a new `Feedback` collection, plus an admin-only `!feedbacklist` to read recent submissions. (B9) |
| [Bond-Tree](Bond-Tree.md) | `!bondtree` and `!familytree`, read-only commands that walk the bond graph and render everyone connected to a resident within N degrees of separation. (B10) |
| [Random-Events](Random-Events.md) | An ambient event system: the bot periodically fires a random event into opted-in channels; residents join with the new `!random` command (with an optional anti-snipe argument) for a chance at currency, titles, training, corruption/purity, curses, or flavor. (B12) |

> **B11** (resurface chips/poker/other dicebot games over which Chateau Work was layered) stays parked pending the original dicebot developer pushing their latest changes to Git, for ease of merging — not specced here.

## Conventions

These specs assume the same house rules as the rest of `wiki-docs/specs/` — see [Future-Interactions/README.md](../Future-Interactions/README.md#conventions-all-specs-assume) for the full list (processor pattern, `characteristics`/`currencies`/`lists` storage, "Chateau day" wording, user-facing-string review before shipping, F-Chat 4096-char cap + `[spoiler]` fallback, etc.).

### Cluster-wide implementation note: aliases are separate command classes

Command dispatch matches **only** on `ChatBotCommand.Name` ([BotCommandController.cs:54](../../../FChatDicebot/BotCommandController.cs), fed by [`SeparateCommandTerms`](../../../FChatDicebot/BotMain.cs)). The `Aliases` string array on a command is **cosmetic** — it is consumed only by `!help` display/lookup ([ChateauHelp.cs:38,185](../../../FChatDicebot/BotCommands/ChateauHelp.cs)), never for routing. A working alias is therefore its **own command class** whose `Name` is the alias and whose `Run` instantiates the canonical command and delegates — see [ChateauW.cs](../../../FChatDicebot/BotCommands/ChateauW.cs) (`Name="w"` → `new ChateauWork().Run(...)`) and [ChateauC.cs](../../../FChatDicebot/BotCommands/ChateauC.cs). Every alias named in these specs (e.g. `!suggestion`) must be built that way.

> This also applies retroactively to the alias arrays proposed in the [Interactions cluster](../Future-Interactions/Group-Interactions-And-Pending-Lifecycle.md) (`!decline`/`!reject`/`!r`, etc.) — those aliases need delegating classes at build time, not just `Aliases = {...}` entries.
