# Future Social & Events — Spec Index

Self-contained design specs for **upcoming** Chateau Contract community, relationship, and engagement features — the "Social/events" cluster from [Feature-Requests.md](../../Feature-Requests.md). Same conventions as [Future-Interactions](../Future-Interactions/README.md) and [Future-Economy](../Future-Economy/README.md): each file is written so a fresh implementation session can act on it without re-asking the user for design clarifications, and specs move one level up to [`specs/`](../) once they ship.

## Status

### Planned

| Spec | Adds |
|------|------|
| [Bond-Tree](Bond-Tree.md) | `!bondtree` and `!familytree`, read-only commands that walk the bond graph and render everyone connected to a resident within N degrees of separation. (B10) |
| [Random-Events](Random-Events.md) | An ambient event system: the bot periodically fires a random event into opted-in channels; residents join with the new `!random` command (with an optional anti-snipe argument) for a chance at currency, titles, training, corruption/purity, curses, or flavor. (B12) |
| [Achievement-Titles](Achievement-Titles.md) | System titles beyond "one number crosses one threshold": combo/set-completion, meta (title count), wild-state, variety/breadth, relationship depth, streaks, and narrative arcs — seven new check methods layered onto `ChateauSystemTitles`. (Owner request, not a B-number.) |
| [Winner-Count-Agreement](Winner-Count-Agreement.md) | `{singular\|plural}` alternation in a random event's `resultText`, so an `allInWindow` outcome authored for a crowd still reads correctly when one person wins — plus the builder preview that stops authors getting it backwards. Extends the shipped Random-Events. (Resident feedback `6a6fb15d`.) |

> **B11** (resurface chips/poker/other dicebot games over which Chateau Work was layered) stays parked pending the original dicebot developer pushing their latest changes to Git, for ease of merging — not specced here.

## Conventions

These specs assume the same house rules as the rest of `wiki-docs/specs/` — see [Future-Interactions/README.md](../Future-Interactions/README.md#conventions-all-specs-assume) for the full list (processor pattern, `characteristics`/`currencies`/`lists` storage, "Chateau day" wording, user-facing-string review before shipping, F-Chat 4096-char cap + `[spoiler]` fallback, etc.).

### Cluster-wide implementation note: aliases are one array entry

An alias is a string in the canonical command's `Aliases` array ([ChatBotCommand.cs](../../../FChatDicebot/BotCommands/Base/ChatBotCommand.cs)) and nothing else. That array is the single source of truth: `BotCommandController.BuildAliasIndex` indexes it at load and `FindCommandByName` falls back to it, so `!hug` dispatches straight to the `ChateauCuddle` instance, and `!help` both resolves and prints from the same array. Guarded by [Commandaliastests.cs](../../../FChatDicebot.Tests/Unit/Commandaliastests.cs).

Two rules apply. A real command `Name` always beats an alias, and the first claim on an alias wins over a later one; both cases are logged at startup and both are test failures, so check a new alias against the live command table first. The one other place to touch is `InteractionEiconSupport.TokenToVerbKeys` — an alias of an *interaction* command needs an entry there too, or `!seteicon` won't recognise it (also covered by a test).

> Aliases used to need a delegating command class of their own (`Name="w"` → `new ChateauWork().Run(...)`). That pattern is gone — the 24 stub classes were deleted when dispatch started reading `Aliases`. Older specs, including the alias arrays proposed in the [Interactions cluster](../Future-Interactions/Group-Interactions-And-Pending-Lifecycle.md), may still describe the old rule.
