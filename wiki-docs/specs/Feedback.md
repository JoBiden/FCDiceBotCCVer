# Feedback — `!feedback` / `!suggestion` + admin `!feedbacklist`

**Status:** Implemented. (design owner-reviewed 2026-06-21; shipped 2026-06-22)
**Feature-Requests source:** "!feedback (alias !suggestion) to submit an idea or report a bug" (B9).

---

## Differences from the original spec

The feature shipped as designed, with these as-built notes:

- **Category tagging kept.** The optional leading `bug`/`idea`/`other` keyword was retained (the spec OK'd dropping it if parsing got fiddly; it didn't). Implemented as a pure, unit-tested `ChateauFeedback.ParseFeedback(rawTerms)` returning `(category, text)`.
- **Test fixture is a real test DB, not an in-memory double.** [Testdatabasefixture.cs](../../FChatDicebot.Tests/Fixtures/Testdatabasefixture.cs) wraps a live `ChateauDatabase` against the test connection string (the Pledges precedent), so `AddFeedback`/`GetRecentFeedback` are exercised end-to-end. The fixture's `Reset()` just adds `Feedback` to the cleared collections.
- **Owner-reviewed wording (2026-06-22):** empty-submission hint is *"Be sure you actually include your feedback! Usage: !feedback <your idea or bug report>"*; the `!feedback` long-description references the **mods** (not "staff"). The success acknowledgement is unchanged from the spec.
- **`!feedbacklist`** renders one block per entry (`{relative} ago — {name} ({category}): {text}`) and truncates with a note before the 4096-char cap; default 10, max 50.

---

## Overview

Residents have no in-bot channel to send ideas or bug reports to the staff. `!feedback` (with the `!suggestion` alias) lets any registered resident submit free text, which is persisted to a new `Feedback` Mongo collection. Staff read submissions back with the admin-only `!feedbacklist`.

This is **not** an interaction (no consent, investment level, status hook, or reversal). It is a write command + an admin read command. Because the submitter invokes the bot themselves, persisting and acknowledging their submission is TOS-safe; staff are never push-notified (they pull via `!feedbacklist`), consistent with the rest of the bot's TOS posture.

---

## The `!feedback` command (submission)

New `ChatBotCommand`: [ChateauFeedback.cs](../../FChatDicebot/BotCommands/ChateauFeedback.cs) under `BotCommands/`.

| Field | Value |
|-------|-------|
| `Name` | `feedback` |
| `Aliases` | `{ "suggestion" }` (display only — see real alias below) |
| `Category` | `General` |
| `ShortDescription` | "Send the staff an idea or a bug report" |
| `Usage` | `!feedback <your idea or bug>`  •  `!feedback [bug\|idea\|other] <text>` |
| `RelatedCommands` | `modmessage`, `help`, `botinfo` |
| `CooldownDuration` / `AppliesTo` | `5 minutes` / `initiator` |
| `RequireChannel` | `false` (works in channel or PM) |
| `RequireBotAdmin` / `RequireChannelAdmin` | `false` / `false` |

**`!suggestion` alias** = its own delegating class [ChateauSuggestion.cs](../../FChatDicebot/BotCommands/ChateauSuggestion.cs), `Name="suggestion"`, whose `Run` calls `new ChateauFeedback().Run(...)` (the [ChateauW.cs](../../FChatDicebot/BotCommands/ChateauW.cs) pattern — the `Aliases` array does **not** route; see the [cluster README](Future-Social/README.md#cluster-wide-implementation-note-aliases-are-separate-command-classes)).

**Optional leading category (B9.2).** If the first term is one of a small known set (`bug`, `idea`, `other` — extend freely), strip it and store it as `category`; otherwise `category = "general"` and the whole message is the text. Keep this trivial: a single first-token check. **If it complicates parsing in practice, drop it entirely** and store every submission as `general` — the owner has explicitly OK'd dropping the category rather than fighting the parser.

**Validation.**
- **Reject empty** — if, after removing any category token, the text is empty/whitespace, reply with a short usage hint and do **not** write a row or start the cooldown. *(For owner wording review:* `"Tell me what's on your mind! Usage: !feedback <your idea or bug report>."`*)*
- **Cooldown** — a standard `CoolDown` timer under `profile.timers["feedback"]`, **5 minutes**, set only on a successful submission (mirrors how other commands gate via `timers`; reuse the existing cooldown-check helper rather than hand-rolling). On a too-soon retry, reply with the standard remaining-cooldown wording used elsewhere.
- No max length beyond the F-Chat message cap; store the text as received.

**On success:**
1. Build a `FeedbackEntry` (see Persistence) — submitter `userName` + `displayName`, `category`, `text`, the `sourceChannel` (the channel name, or `null`/empty when sent by PM), and `submittedAt = DateTime.UtcNow`.
2. `Database.AddFeedback(entry)` (InsertOne — the [Pledges](../../FChatDicebot/Database/Chateaudatabase.cs) write precedent).
3. Set the 5-minute cooldown timer and save the profile.
4. Acknowledge. Mirror [ModMessage.cs](../../FChatDicebot/BotCommands/ModMessage.cs)'s reply routing: reply in-channel if it came from a channel, else PM. Only the acknowledgement is emitted — never echo the submitted text publicly.

> **For owner wording review — acknowledgement (owner-supplied, final):**
> `Thank you for your feedback! A mod might reach out to you with further inquiry, and we'll do our best to let you know how we used your feedback, one way or the other.`

---

## The `!feedbacklist` command (admin read)

New `ChatBotCommand`: [ChateauFeedbackList.cs](../../FChatDicebot/BotCommands/ChateauFeedbackList.cs). A dedicated admin command (rather than overloading `!feedback`) so there is **no ambiguity** between "submit the word 'list' as feedback" and "list feedback."

| Field | Value |
|-------|-------|
| `Name` | `feedbacklist` |
| `Category` | `General` (admin) |
| `ShortDescription` | "Staff: view recent feedback submissions" |
| `Usage` | `!feedbacklist`  •  `!feedbacklist [count]` |
| `RequireBotAdmin` | `true` |
| `RequireChannel` | `false` (PM the result to the admin) |
| `CooldownDuration` | `null` |

**Render.** Pull the most recent `N` entries (default 10, optional `[count]` arg, sane cap e.g. 50) via `Database.GetRecentFeedback(n)`, newest first. One block per entry: relative timestamp (`Utils.TimeDifferenceText(submittedAt, UtcNow)` → "2 hours ago"), submitter display name, category, and the text. PM to the requesting admin; `[spoiler]` fallback / truncation past the F-Chat cap as elsewhere. Empty-state: *(owner review)* `"No feedback has been submitted yet."`

Built around a static `BuildFeedbackList(List<FeedbackEntry>)` helper for unit testing (the `!payroll`/`!business` mold).

---

## Persistence shape

New model `FeedbackEntry` (in [ChateauDB.cs](../../FChatDicebot/Model/ChateauDB.cs), alongside `Pledge`):

```csharp
public class FeedbackEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    public string submitterUserName { get; set; }   // login name (survives renames)
    public string submitterDisplayName { get; set; } // snapshot at submit time
    public string category { get; set; }             // "general" | "bug" | "idea" | "other"
    public string text { get; set; }
    [BsonIgnoreIfNull]
    public string sourceChannel { get; set; }        // channel name, or null if PM
    public DateTime submittedAt { get; set; }
}
```

Stored in a new **`Feedback`** Mongo collection. Unlike B6, this **does** touch the DB layer (it is the first user-driven write to a brand-new collection):

- `Ichateaudatabase.cs`: `void AddFeedback(FeedbackEntry entry);` and `List<FeedbackEntry> GetRecentFeedback(int count);`
- `Chateaudatabase.cs`: implement both against `Database.GetCollection<BsonDocument>("Feedback")` — `AddFeedback` = `InsertOne(entry.ToBsonDocument())` (Pledges pattern); `GetRecentFeedback` = `Find(empty).SortByDescending(submittedAt).Limit(count)` deserialized to `FeedbackEntry`.
- `MonDB.cs`: thin static pass-throughs (`addFeedback` / `getRecentFeedback`) matching the existing `MonDB` wrapper style.
- Test double [Testdatabasefixture.cs](../../FChatDicebot.Tests/Fixtures/Testdatabasefixture.cs) gains an in-memory `Feedback` list + the two methods.

---

## Files to create / modify

**Create:**
- `FChatDicebot/BotCommands/ChateauFeedback.cs` — `!feedback` submission.
- `FChatDicebot/BotCommands/ChateauSuggestion.cs` — `!suggestion` delegating alias.
- `FChatDicebot/BotCommands/ChateauFeedbackList.cs` — admin `!feedbacklist` (+ static `BuildFeedbackList`).
- `FChatDicebot.Tests/Unit/Chateaufeedbacklisttests.cs` — `BuildFeedbackList` rendering tests; submission validation tests (empty rejected, category parse, cooldown set).

**Modify:**
- `FChatDicebot/Model/ChateauDB.cs` — add `FeedbackEntry`.
- `FChatDicebot/Database/Ichateaudatabase.cs` + `Chateaudatabase.cs` — `AddFeedback` / `GetRecentFeedback`.
- `FChatDicebot/MonDB.cs` — static wrappers.
- `FChatDicebot.Tests/Fixtures/Testdatabasefixture.cs` — in-memory Feedback support.
- `FChatDicebot/BotCommands/ChateauHelp.cs` — register `!feedback`, `!suggestion`, `!feedbacklist` in their categories.
- `wiki-docs/Feature-Requests.md` — B9 bullet retires on ship.

---

## Tests

- `BuildFeedbackList`: empty → empty-state string; N entries render newest-first with relative time, name, category, text; `count` cap respected.
- Submission: empty/whitespace text → rejected, no write, no cooldown; category token stripped and stored, default `general` otherwise; successful submit writes one `FeedbackEntry` and sets the 5-min cooldown; PM vs channel ack routing.
- `GetRecentFeedback` ordering (newest first) against the test fixture.

---

## Decisions resolved (owner, 2026-06-21)

- **B9.1** Mod retrieval via an **admin-only in-bot read command** (approach (a)) — speced as the unambiguous dedicated `!feedbacklist`.
- **B9.2** Optional leading **category** keyword, defaulting to `general`; **drop it** if it causes parsing trouble.
- **B9.3** **5-minute** cooldown and **reject empty** submissions.
- **B9.4** Acknowledgement wording supplied by owner (above), final.

## Open items

- `!feedbacklist` / empty-state / reject-empty strings are first drafts pending owner wording approval (house rule). The success acknowledgement (B9.4) is final.
- Category set (`bug`/`idea`/`other`) is a starting list — add/rename freely.
- Possible later: `!feedbacklist [category]` filter; marking entries handled/resolved; notifying the submitter of resolution (would need a TOS-safe pull channel, e.g. surfacing status the next time *they* run a command).

## Assumptions

- The 5-minute cooldown uses the same `profile.timers` + cooldown-check machinery every other gated command uses.
- Admins are identified the same way as everywhere else (`Utils.IsCharacterAdmin(AccountSettings.AdminCharacters, ...)` / `RequireBotAdmin`).
- F-Chat output constraints match existing list outputs (4096-char cap, BBCode, `[spoiler]` fallback if long).
