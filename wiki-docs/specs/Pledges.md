# Pledges

**Status:** Implemented.

A pledge is a promise to perform an interaction later. Making one takes no consent — it is only a promise — while fulfilling one runs the ordinary consent flow for whatever interaction was promised. The Chateau keeps score of how reliably a resident keeps their word, and says so publicly every time they make a new pledge.

Commands and usage live in [Command-Reference](../Command-Reference.md#pledges); `!help` is authoritative over both. This page covers the machinery behind them.

## Lifecycle

A `Pledge` document carries a `status` string. `Pledge.IsActive` (in `Model/ChateauDB.cs`) is `status == "active"`, and every lookup path keys off it.

| Status | Set by | Meaning |
|--------|--------|---------|
| `active` | `!pledge`, on creation | Outstanding; listable and fulfillable |
| `fulfilled` | `ChateauInteractionHandler.TryMarkPledgeFulfilled` | The promised interaction was consented to and completed |
| `abandoned` | `!abandonpledge`, second invocation | The pledger formally withdrew |

`ChateauDatabase.GetActivePledges(pledger, pledgee, interactionType)` filters on all four fields including `status == "active"` — it is the lookup both `!fulfill` and `!abandonpledge` use to find the pledge a resident is naming. `!pledges` fetches by pledger and by pledgee and filters with `IsActive` in the command.

### The abandon confirmation

`!abandonpledge` warns on the first invocation and completes on the second. The warning is recorded on the separate `Pledge.abandonWarned` flag, deliberately **not** as a status: the pledge stays `active` throughout, so a pledger who reconsiders still sees it in `!pledges` and can still `!fulfill` it. Only the second invocation writes `abandoned`.

This is a correction. The confirmation originally moved the pledge to a `warned` status, which every lookup filtered out — the pledge became invisible to `!pledges`, unfulfillable, and unfindable by the second `!abandonpledge`, so the abandonment could never complete and `pledgesactive` was never decremented. Any status-filtered query added later should assume the same trap: a pledge mid-confirmation is an ordinary active pledge.

**Only non-casual interactions can be pledged.** `ChateauPledge` rejects any interaction whose processor reports `InvestmentLevel` `casual`, so the pledgeable set is the involved, commitment, and consequence tiers.

## Fulfillment

`!fulfill` does not perform the interaction itself. It builds a normal `PendingCommand` for the promised interaction and stamps the pledge's id as the first `extraParameters` entry, as `{ "pledgeId": "<ObjectId>" }`. The consent prompt is the target processor's own, with a `[sub]` note appended saying how long ago the pledge was made.

When the recipient consents, `ChateauConsent.ProcessOneToOneSeat` calls `ChateauInteractionHandler.TryMarkPledgeFulfilled`, which reads that `pledgeId` back, flips the pledge to `fulfilled`, and stamps `fulfilledTime`. `ChateauInteractionHandler.cs` is authoritative for the honor rule; as shipped, a pledge is honored (and counted) only when at least one day separates `pledgeTime` from `fulfilledTime`.

`TryMarkPledgeFulfilled` swallows its own exceptions on purpose: `extraParameters[0]` belongs to whatever interaction is being consented to, and for some (payment, for instance) it is not a pledge-shaped document at all.

## Honor accounting

Three `Profile.counts` keys drive both the reputation messaging and the title ladders:

| Count | Incremented by |
|-------|----------------|
| `pledgesactive` | `!pledge`, on every new pledge |
| `pledgesfulfilled` | `TryMarkPledgeFulfilled`, only for honored fulfillments |
| `pledgesabandoned` | `!abandonpledge`, on the completing invocation (which also decrements `pledgesactive`) |

`ChateauPledge` compares abandoned-to-fulfilled and active-to-fulfilled ratios to pick one of several warnings appended to the public pledge announcement, ranging from "always follows through on their word" to an open accusation of oathbreaking. The cascade in `ChateauPledge.Run` is authoritative — the thresholds are ordered and fall through, so read it top to bottom rather than treating any one band in isolation.

All three counts also feed system-title ladders (`ChateauSystemTitles.cs`), which is why they are profile counts rather than derived queries over the `Pledges` collection.

## Data and files

The `Pledges` collection and the `Pledge` model are summarized in [Database-and-Persistence](../Database-and-Persistence.md); `Model/ChateauDB.cs` is the schema.

| Piece | File |
|-------|------|
| Commands | `BotCommands/ChateauPledge.cs`, `ChateauPledges.cs`, `ChateauFulfill.cs`, `ChateauAbandonPledge.cs` |
| Fulfillment hook | `ChateauInteractionHandler.TryMarkPledgeFulfilled`, called from `BotCommands/ChateauConsent.cs` |
| Model | `Model/ChateauDB.cs` (`Pledge`) |
| Persistence | `Database/Chateaudatabase.cs` pledge operations, exposed on `IChateauDatabase` and wrapped by `MonDB` |
| Titles | `BotCommands/ChateauSystemTitles.cs` |
| Tests | `FChatDicebot.Tests/Integration/Pledgeflowtests.cs`, `Unit/Pledgedatabasetests.cs`, `Unit/Pledgecommandtexttests.cs` |

There is no pledge interaction processor. Pledges are a command-layer feature that borrows another interaction's processor at fulfillment time.

## Known gaps

Verified against the code as of August 2026. None of these are design decisions — they are defects or unfinished edges that the code currently has.

- **Automatic fulfillment does not exist.** `!fulfill`'s `LongDescription` says a performed interaction will be detected as fulfilling a pledge, but nothing except `ChateauFulfill` ever writes a `pledgeId`, so only the explicit command fulfills anything.
- **Same-day fulfillments are invisible to the ledger.** `pledgesfulfilled` increments only when the honored (one day or more) test passes, and no path decrements `pledgesactive` on fulfillment. A resident who fulfills promptly gets the pledge marked `fulfilled` but no credit in the ratio messaging or the title ladders.
- **Pledges carry no identifier.** `ChateauPledge` writes `identifier = null` unconditionally; the command takes no identifier argument. A pledged `!feed` does not record what food, and `!fulfill` therefore starts the interaction with a null identifier.

## Not built yet

Three proposed extensions are tracked as open ideas in [Feature-Requests](../Feature-Requests.md): a pledge summary block on `!dossier`, a way to disambiguate several same-type pledges outstanding to the same person, and a pledgee-side release that ends a pledge without the abandonment penalty.
