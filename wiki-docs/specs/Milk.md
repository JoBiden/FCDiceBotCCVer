# `!milk`

Milk a substance (or other bodily fluid) out of the recipient. Produces tagged bottles in the initiator's milk inventory which can be kept as trophies or sold via `!sell` for copper plus refunded bottle-currency.

**Status:** Implemented.
**Investment level:** Involved.
**Reversal:** none (bottles are sold via `!sell`, not "unmilked").
**Depends on:** [Currency-and-Milk-Inventory](Infrastructure/Currency-and-Milk-Inventory.md), [Status-Effect-Hook](Infrastructure/Status-Effect-Hook.md).

## Command syntax

```
!milk [user]Bob[/user] {substance}
```

## Validation

- Recipient must be registered (base validation).
- `substance` must resolve via the existing identifier catalog with category `"substance"` **or** `"vice"`. Rejected with `ChateauInteractionHandler.typeNotFoundText("substance")` otherwise.
- Daily-pair lock: the (initiator, recipient) pair may only `!milk` once per day (regardless of substance). Locked symmetrically — both profiles get a `milk_pair_<other>` timer ending at the next day boundary. The command-time pre-check and the processor's TOCTOU recheck both gate on this; failure text comes from `MilkProcessor.PairLockMessage` so the two paths can't drift.
- The status-effect hook gates this interaction automatically: any future `IStatusEffectContributor` that yields a recipient-side blocker (e.g. a hypothetical `break:breast`) will gate `!milk` without code changes in this processor.
- Self-target is **allowed** but routed through a special shortcut path — see below.

There is **no** "empty bottle" precondition on the initiator. The Chateau always provides the empties needed to milk.

## Self-milk shortcut

Self-target is handled inline by `ChateauMilk.Run` and bypasses the consent flow entirely. Instead of producing a `MilkBottle` entry, the Chateau buys the bottle straight off the producer:

- Credits the initiator `currencies["copper"] += 1` and `currencies["bottle"] += 1`.
- Sets `milk_pair_<self>` to the next day boundary (the daily lock still applies — once per day, same as any other target).
- Sends a single channel message; no `PendingCommand` is created.

The `MilkProcessor` itself rejects self-target Interactions as a defensive guard, so a stray self-targeted Interaction can't sneak through and produce a self-sourced `MilkBottle`.

## Processor logic (`MilkProcessor`)

Constants live in [`ChateauCurrency.cs`](../../FChatDicebot/ChateauCurrency.cs) (`MilkRollMin = 1`, `MilkRollMax = 3`, `CorruptionTagThreshold = 10`).

1. Re-validate (TOCTOU): if the pair lock is now active, set `produced = 0` and skip the side-effects. The completion message detects `produced <= 0` and emits empty channel text.
2. Otherwise roll bottle quantity: `Rng.Next(MilkRollMin, MilkRollMax + 1)` → 1–3 bottles. RNG is `internal static` so tests can swap it for a `FixedSampleRandom`.
3. Determine corruption tag from recipient's corruption (`CorruptionProcessor.ReadCorruption`):
   - `corruption <= -10` → `"corrupt"`
   - `corruption >= +10` → `"purified"`
   - else → `null`
4. Append a `MilkBottle` to initiator's `milkInventory`:
   - `substance` = the substance identifier
   - `sourceName` = recipient's `userName` (not display name)
   - `milkedAt` = `DateTime.UtcNow`
   - `quantity` = rolled amount
   - `corruptionTag` = computed tag
5. Set symmetric pair lock: both `initiatorProfile.timers[PairTimerKey(recipient)]` and `recipientProfile.timers[PairTimerKey(initiator)]` to a `CoolDown` ending at `DateTime.UtcNow.Date.AddDays(1)` (next day boundary).
6. Stamp the produced quantity onto the in-memory `PendingCommand.pendingInteraction.identifier` as `"<substance>|<quantity>"` (same identifier-piggyback trick `CorruptionProcessor` uses) so `GetCompletionMessage` can read the truthful quantity.
7. `Database.AddInteraction`, `Database.SetProfile` for both, then `IncrementDifferentCounts(initiator, recipient, "milkgive", "milktake")`.
8. `Database.DeletePendingCommand`.

## Persistence

- Profile: `milkInventory` (`List<MilkBottle>`).
- Profile timers: `milk_pair_{otherUserName}` (24-hour daily lock per pair, set symmetrically on both profiles).
- Counts: `milkgive` for initiator, `milktake` for recipient.
- Currency (self-milk shortcut only): `currencies["copper"] += 1`, `currencies["bottle"] += 1`.

There is **no** `emptyBottles` field on `Profile`. The bottle-currency tally lives in the standard `currencies` dict under `ChateauCurrency.BottleCurrency` (`"bottle"`) and is credited by `!sell` (and the self-milk shortcut), never by `!milk`.

## Status-effect contributions

None. (`!milk` is an action, not a passive status — it doesn't register an `IStatusEffectContributor`.)

## Status-effect consumption

The base `ValidateInteraction` calls `GetActiveStatusEffects(recipient, Consent, isInitiator: false)`, so any blocker fragments contributed by other status effects (a hypothetical `break:breast`, an active `curse`, etc.) automatically gate `!milk`. No custom blocker code in the processor.

Completion message reads the recipient's corruption tag at process time for flavor:

- Corrupt recipient: `" The {substanceText} has a faint dark sheen to it..."`
- Purified recipient: `" The {substanceText} practically glows."`
- Neutral recipient: no appendix.

`substanceText` comes from `Utils.SubstanceToText(identifier)` so the flavor line reads with the actual substance name rather than the literal word "substance".

## User-facing text

**Consent warning** (channel, neutral recipient):

> `{Initiator} wants to milk {Recipient} for some {substanceText}. Do you !consent to being milked?`

When the recipient is at the corrupt/purified threshold, the warning prepends a flavor line before "Do you !consent...":

> ` {Recipient}'s {substanceText} is going to be quite [b]corrupt[/b].`
> ` {Recipient}'s {substanceText} is going to be quite [b]pure[/b].`

**Completion message** (channel, after consent):

> `{Initiator} milks {Recipient} for {N} bottle(s) of {substanceText}. Bottled, sealed, and tagged.`

Plus the corruption appendix above.

**Self-milk shortcut** (channel, instant):

> `{Initiator} milks themself for a bottle of {substanceText} and trades it straight to the Chateau for [b]1 copper[/b] and [b]1 bottle[/b].`

**Pair lock failure** (private to initiator):

> `You've already milked {RecipientDisplay} today. You can milk them again in {time-until-next-day}.`

Self variant (via `PairLockMessage(..., isSelf: true)`):

> `You've already milked yourself today. You can milk yourself again in {time-until-next-day}.`

## Tests

[`Milkprocessortests.cs`](../../FChatDicebot.Tests/Unit/Milkprocessortests.cs):

- Validation: self-target rejected by processor (allowed at command layer instead); missing/unknown/wrong-category substance rejected; vice-category accepted; no `milkInventory` precondition (fresh profile passes); active pair lock blocks; expired pair lock allows.
- Process: appends `MilkBottle` with substance/source/quantity/tag; sets symmetric pair lock to next day boundary; increments `milkgive`/`milktake`; saves Interaction; deletes pending; corrupt/purified/neutral recipient → tag thresholds applied; copper balance untouched (Chateau provides empties).
- Identifier round-trip: `ComposeIdentifier` / `ParseSubstanceFromIdentifier` / `ParseQuantityFromIdentifier` preserve values; missing pipe returns sentinel `-1`.
- Channel text: completion line uses the substance name (not the word "substance"); consent warning template; corrupt/pure tag appendices use both recipient display and substance name; pair-lock message includes time, omits "UTC"; self-variant uses "yourself".

[`Chateaucurrencytests.cs`](../../FChatDicebot.Tests/Unit/Chateaucurrencytests.cs): pricing tiers, corruption multipliers, threshold band for tag assignment.

## Differences from the original spec

The spec was reshaped during implementation. Key changes:

- **Empty bottles are not a precondition.** The original spec required `profile.emptyBottles >= 1` and seeded new profiles with 5. Per user direction, the Chateau provides empties for free; the `emptyBottles` int field was dropped before merge.
- **Bottle currency is in the standard `currencies` dict.** `!sell` returns one `currencies["bottle"]` per sold bottle (the Chateau handing the empty back); the original `profile.emptyBottles` int doesn't exist.
- **Self-target is allowed via a self-sale shortcut.** Original spec rejected self-milk. Implementation routes self-target through an instant 1 copper + 1 bottle credit, bypassing the consent flow. Pair lock still applies.
- **No `break:breast` blocker is special-cased.** The status-effect hook handles future blockers generically; nothing in `MilkProcessor` references a specific status.
- **Sell-related changes** are in [Currency-and-Milk-Inventory](Infrastructure/Currency-and-Milk-Inventory.md).

## Files

- [`FChatDicebot/Model/MilkBottle.cs`](../../FChatDicebot/Model/MilkBottle.cs)
- [`FChatDicebot/Model/ChateauDB.cs`](../../FChatDicebot/Model/ChateauDB.cs) (added `milkInventory` to `Profile`)
- [`FChatDicebot/ChateauCurrency.cs`](../../FChatDicebot/ChateauCurrency.cs)
- [`FChatDicebot/InteractionProcessors/Involved/MilkProcessor.cs`](../../FChatDicebot/InteractionProcessors/Involved/MilkProcessor.cs)
- [`FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs`](../../FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs) (registers `MilkProcessor`)
- [`FChatDicebot/BotCommands/ChateauMilk.cs`](../../FChatDicebot/BotCommands/ChateauMilk.cs)
- [`FChatDicebot/BotCommands/ChateauSell.cs`](../../FChatDicebot/BotCommands/ChateauSell.cs)
- [`FChatDicebot.Tests/Unit/Milkprocessortests.cs`](../../FChatDicebot.Tests/Unit/Milkprocessortests.cs)
- [`FChatDicebot.Tests/Unit/Chateaucurrencytests.cs`](../../FChatDicebot.Tests/Unit/Chateaucurrencytests.cs)
- [`FChatDicebot.Tests/Unit/Chateauselltests.cs`](../../FChatDicebot.Tests/Unit/Chateauselltests.cs)
