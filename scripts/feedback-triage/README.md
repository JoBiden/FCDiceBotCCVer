# Feedback triage

A daily pass over the bot's `Feedback` collection (what residents send with `!feedback` /
`!suggestion`): assess each new submission against the actual code, write it up, ask the owner
for a decision, then take the approved ones through to a PR.

Three phases, each with its playbook in this folder:

| Phase | When | Playbook | Ends with |
|-------|------|----------|-----------|
| **Triage** | daily, unattended | [TRIAGE_PLAYBOOK.md](TRIAGE_PLAYBOOK.md) | a dossier per new submission + a notification |
| **Review** | when you run `/feedback-review` | [REVIEW_PLAYBOOK.md](REVIEW_PLAYBOOK.md) | your decision + design answers recorded |
| **Implement** | straight after approval | [IMPLEMENT_PLAYBOOK.md](IMPLEMENT_PLAYBOOK.md) | a PR (bugs) or a spec for sign-off (features) |

The playbooks live in the repo on purpose — they are the actual instructions the automation
follows, so improving the system is a normal PR.

## What it never does

- Writes to the `Feedback` collection. `FeedbackEntry` in `FChatDicebot/Model/ChateauDB.cs` has
  no `[BsonIgnoreExtraElements]`, so any extra field there would break `!feedbacklist`
  deserialization until the bot model changes and redeploys. All state lives in a separate
  `FeedbackTriage` collection.
- Commits, branches, or changes code in the unattended pass.
- Merges a PR.
- Messages residents as the bot. A submission needing clarification becomes a `needs_info`
  item for you to relay.

## Daily run

Installed as a Claude Code scheduled task (`feedback-triage` in
`C:\Users\blazi\.claude\scheduled-tasks\`), firing daily at 08:12 local. It runs while the
desktop app is open; if the app was closed at that time, it runs on next launch.

You get a push notification **only when something needs you** — a new assessment, a stalled
item, a merged fix, or entries left over past the per-run cap. Quiet days stay quiet, and the
run still appends a line to `triage-log.md`.

To run the pass by hand: `/feedback-triage`. To review what is waiting: `/feedback-review`
(optionally with a short id, e.g. `/feedback-review 6a69a8a2`).

## The ledger

`ft.ps1` is the only thing that touches state. Every verb prints JSON.

```bash
powershell -ExecutionPolicy Bypass -File scripts\feedback-triage\ft.ps1 info
```

| Verb | Does |
|------|------|
| `info` | counts by status, plus how many submissions have never been assessed |
| `new` | submissions with no ledger row yet — the work list, oldest first |
| `status` | ledger rows; `-AwaitingOwner`, `-Open`, `-Status approved,pr_open` filter it |
| `set` | upsert one row: `-Id <24-char hex> -Fields '<json>' -Event <name> -Detail <text>` |
| `seed` | insert rows only where none exists — for pre-marking history, never clobbers |

Connection overrides: `-Conn mongodb://... -Database OtherDb -MongoSh C:\path\mongosh.exe`.
Requires [mongosh](https://www.mongodb.com/try/download/shell) on PATH.

A row is keyed by the feedback `_id` hex, snapshots the submission (so it stays readable after
you delete the original), and appends a `history` entry on every write.

### Statuses

```
assessed ─┬─> approved ──> in_progress ──> pr_open ──> shipped
          ├─> deferred          (kept in queue/, not re-assessed)
          ├─> declined          (closed; the detail is the only record of why)
          └─> needs_info ──> (you relay the question) ──> approved | declined

duplicate   another row covers it, see duplicateOf
obsolete    the submission was deleted from the collection before a decision
```

`assessed` and `needs_info` are the two that mean "waiting on you".

## Files

```
ft.ps1                  ledger CLI (the only writer)
bin/ft.js               mongosh backend
dossier-template.md     the per-submission write-up format
queue/                  dossiers awaiting a decision, or deferred
done/                   dossiers for shipped, declined, and obsolete items
triage-log.md           one line per daily run (created on first run)
```

Dossiers are written untracked by the daily pass and get committed alongside the fix, so the
assessment and the owner decision land in the PR that resolves them.

## Troubleshooting

- **`mongosh not found`** — install it, or pass `-MongoSh`.
- **Connection refused** — the bot's Mongo (`mongodb://localhost:27017`) is not running.
- **The task reports the playbook is missing** — it ran against a checkout that predates this
  folder. `git pull` in `C:\source\repos\FCDiceBotCCVer`.
- **An item you handled by hand keeps coming back** — it has no ledger row. Close it out with
  `ft.ps1 seed`, e.g. `-Rows '[{"feedbackId":"<id>","fields":{"status":"shipped"},"detail":"fixed manually in <commit>"}]'`.
- **A dossier got lost** — the ledger row still has the verdict and history. Delete the row's
  status back to `assessed` only if you want the whole assessment redone.
