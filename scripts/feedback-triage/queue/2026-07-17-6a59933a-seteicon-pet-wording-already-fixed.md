# 6a59933a — `!seteicon pet` confirmation described the wrong direction (already fixed)

| | |
|---|---|
| **Feedback id** | `6a59933ae91eb416bbf7b3ed` |
| **Submitted** | `2026-07-17T02:28:10Z` by **Queen Contract** (`Queen Contract`) |
| **Source** | private message |
| **Tagged** | `bug` |
| **Verdict** | `wording` |
| **Recommendation** | `decline` — nothing left to do, the fix already shipped |
| **Confidence** | high |
| **Effort** | none |

## What they said

> !seteicon pet says 'whenever you pet someone' even though display behavior is 'whenever you are pet by someone'

## What it means

Setting a custom `!pet` eicon confirmed with "whenever you pet someone", but the `pet` eicon that
actually surfaces belongs to the resident *being* petted. The confirmation described the opposite
direction from the behavior.

## Assessment

Fixed on 2026-07-24 in commit `6f23fdd` ("Add bodypart eicons and identifier eicons"), a week
after this was submitted.

[ChateauSeteicon.cs:135](../../../FChatDicebot/BotCommands/ChateauSeteicon.cs:135) now routes the
confirmation through `SetConfirmationClause(token)`, which special-cases `pet` and returns
**"whenever someone pets you"**; everything else keeps "whenever you {token} someone". Its
doc-comment records why, and flags that any future interaction redirecting `GetEiconSubject` to
the recipient needs an entry too.

The direction is the right way round: `PetProcessor.GetEiconSubject`
([PetProcessor.cs:108](../../../FChatDicebot/InteractionProcessors/Casual/PetProcessor.cs:108))
returns the recipient profile, and `GetGroupEiconSuffix` mirrors it for group pets — so the
displayed icon is the petted resident's, which is what the confirmation now says.

- **Reproduces / confirmed:** no — cannot reproduce on current `main`
- **Related code:** `ChateauSeteicon.cs:135`, `PetProcessor.cs:108`
- **Related specs / docs:** [Custom-Interaction-Eicons.md](../../../wiki-docs/specs/Custom-Interaction-Eicons.md)
- **Duplicate of:** none
- **Already fixed since submission:** yes — `6f23fdd`, 2026-07-24

## Recommended action

Close it. Nothing to implement. Worth a word to the submitter that it was fixed on the 24th,
since they reported it a week before and would have no way to know.

## Decisions needed from the owner

None — unless you want the fix generalized. Right now the recipient-side phrasing is a hardcoded
`pet` special case; a future interaction that redirects its eicon to the recipient will silently
get the wrong clause again until someone adds it to that method. Making the clause read from the
processor (asking it who the eicon subject is) would remove that trap, but it is speculative work
for a single known case today.

## User-facing text that would change

None.

## Owner decision
