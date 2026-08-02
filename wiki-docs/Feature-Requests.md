Features and changes, big and small, that have been proposed but not thoroughly scoped out for implementation.

When an item here gets specced, move it into `specs/` (Future-* subfolder) and delete it from this list; when it ships, the spec's as-implemented notes are the record — this page should only ever hold still-open ideas.

* Some way to resurface chips, poker, other games from the underlying dicebot functionality (currently, Chateau Work has overwritten the dicebot work). Save this for after original dice bot developer pushes their most recent dicebot changes to Git, for ease of merging. (Partial: dice games can now be wagered in Chateau currencies via `!joingame {game} {amount} {currency}`.)
* Author more !random events (see `wiki-docs/specs/Future-Social/Random-Events.md`). The engine/scheduler shipped 2026-06-29 but only one starter event ("Cutie says {word}") exists in the `RandomEvents` collection so far — needs a real pool (varied response types, winner rules, and reward types) so the ambient events don't feel repetitive. Authoring UI: `scripts/random-event-builder`.

Shipped since this list was started (see the matching specs in `wiki-docs/specs/` for as-implemented details): !joinchateau welcome cleanup, cooldown-accurate consent warnings (`CooldownSpec`/`ConsentWarningText`), corruption visibility disclosure, multi-person casual groups, `!no`/`!oops` lifecycle commands (with `!decline`/`!cancel` aliases), the lapsit/boobhat/lick casuals, and the command alias audit (aliases now route via the `Aliases` array).
