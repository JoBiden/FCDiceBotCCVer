# Daily triage pass — playbook

You are running the unattended daily feedback pass. Nobody is watching: finish the whole
pass, leave the repo in a clean state, and put anything that needs a human into the queue
rather than guessing.

**This pass assesses. It does not decide and it does not implement.**

## Hard limits for this pass

- **Never write to the `Feedback` collection.** It is read-only here. (`FeedbackEntry` in
  `FChatDicebot/Model/ChateauDB.cs` has no `[BsonIgnoreExtraElements]`, so an extra field
  breaks `!feedbacklist` until the bot model changes and redeploys.) All state goes in
  `FeedbackTriage` via `ft.ps1`.
- **Never commit, push, branch, or open a PR.** The dossiers you write stay untracked in the
  working tree; the review pass commits them with the fix.
- **Never change bot code.** No fixes, not even one-line typo fixes, however obvious.
- **Never message a resident.** The bot is not driven from here; if a submission needs
  clarification, that is a `needs_info` item for the owner to relay.
- **Do not re-open closed rows.** A `declined` or `deferred` row stays closed unless the owner
  says otherwise. In particular `6a51dd0b` (breadcrumb titles / more random events) is the
  owner's own creative work and is declined on purpose.
- **No silent truncation.** If you cap the work (see step 3), say so in the notification and
  the log.

## 1. Get to the repo

Work in the main checkout, not a worktree:

```
cd C:\source\repos\FCDiceBotCCVer
git checkout main
git pull
```

If `scripts\feedback-triage\ft.ps1` does not exist, the feedback-triage branch has not been
merged yet. Stop, notify the owner with exactly that, and do nothing else.

## 2. Reconcile what is already in flight

```
powershell -ExecutionPolicy Bypass -File scripts\feedback-triage\ft.ps1 status -Open
```

For each open row:

- `pr_open` with a `prNumber` — check it: `gh pr view <n> --json state,mergedAt,title`. If
  merged, set `shipped` and move its dossier from `queue\` to `done\`. If closed unmerged, set
  the row back to `approved` and note it in the notification.
- `in_progress` older than 3 days with no PR — flag it in the notification as stalled. Do not
  restart the work yourself.
- `missing: true` and still pre-decision (`assessed` / `needs_info`) — the submission was
  deleted from the collection, which is how the owner resolves things by hand. Set the row to
  `obsolete` with a detail saying the submission is gone, and move the dossier to `done\`.
  Leave post-decision rows alone; work already approved stays approved.

## 3. Pick up new submissions

```
powershell -ExecutionPolicy Bypass -File scripts\feedback-triage\ft.ps1 new
```

Oldest first. If the list is empty and step 2 changed nothing, write the log line (step 7) and
finish **without notifying** — a quiet day should stay quiet.

Assess at most **6** new entries in one pass. Anything beyond that stays untriaged for
tomorrow (it has no ledger row, so it comes back automatically) and gets counted in the
notification.

## 4. Assess each entry

Read the submission carefully, then investigate the code before forming a verdict. For each
entry:

1. **Understand the claim.** Identify the commands and systems it names — residents write
   things like "`!seteicon pet` says the wrong thing", which points at a specific string in a
   specific processor.
2. **Search the code.** Find the behavior. Get to a `file:line` you can point at. For a bug,
   trace the actual cause rather than the first plausible-looking line; if you cannot confirm
   it from the code, say so and record what would confirm it (a test, a DB query, a repro in
   channel).
3. **Check it is not already handled.** `git log --since="<submittedAt>" --oneline` plus a
   grep of the relevant file. Feedback often lands before a fix ships.
4. **Check for duplicates** against the other rows (`ft.ps1 status`) and the dossiers in
   `queue\` and `done\`. If it duplicates an open item, verdict `duplicate` and name the
   sibling; do not write a second full assessment.
5. **Check the docs.** `wiki-docs\specs\` may already have a spec, a parked decision, or an
   explicit "not doing this" for the idea. `wiki-docs\Feature-Requests.md` is the backlog.
6. **Classify.**
   - `bug` — behavior contradicts the design.
   - `wording` — behavior is right, the text is wrong or misleading. Still a real fix.
   - `feature` — new capability or a design change. Needs a spec before code.
   - `duplicate`, `invalid` (mistaken premise — explain why), `owner_creative` (the owner's
     own creative territory: title flavor, event authoring, lore), `needs_info` (cannot be
     assessed without asking the submitter).
7. **Recommend** `fix`, `spec`, `defer`, `decline`, or `ask`, with an effort estimate.
8. **Write the open design questions.** Only genuine forks — each with 2–4 concrete options,
   recommended first. These become the question dialogs the owner answers in the review pass,
   so phrase them so they stand alone.
9. **List every user-facing string that would change**, before → after, drafted against
   `wiki-docs\Style-Guide.md`. The owner approves wording before anything ships, so having the
   drafts ready is most of the review.

## 5. Write the dossier

Copy `dossier-template.md` and fill every section. One file per entry:

```
scripts\feedback-triage\queue\<yyyy-mm-dd of submission>-<shortId>-<slug>.md
```

e.g. `queue\2026-07-29-6a69a8a2-pledge-fulfilled-by-wrong-interaction.md`. Leave the
**Owner decision** section empty.

Length follows the item: a wording fix is half a page; a suspected cross-system bug earns as
much detail as it takes to make the decision obvious. Quote the submission verbatim — never
paraphrase it in the "What they said" block.

## 6. Record it in the ledger

One `set` per entry, after its dossier is written:

```
powershell -ExecutionPolicy Bypass -File scripts\feedback-triage\ft.ps1 set ^
  -Id <full 24-char feedback id> ^
  -Fields "{\"status\":\"assessed\",\"verdict\":\"bug\",\"recommendation\":\"fix\",\"confidence\":\"high\",\"effort\":\"small\",\"title\":\"<short title>\",\"dossierPath\":\"scripts/feedback-triage/queue/<file>.md\",\"duplicateOf\":null}" ^
  -Event assessed -Detail "<one-line summary of the finding>"
```

Use `needs_info` as the status instead of `assessed` when the item cannot move without asking
the submitter. Set `duplicateOf` to the other short id and status `duplicate` for dupes — no
dossier needed for those beyond a two-line stub.

## 7. Log and notify

Append one line per run to `scripts\feedback-triage\triage-log.md` (create it if missing):

```
- 2026-07-30 — 2 assessed (6a69a8a2 bug/fix, 6a59933a wording/fix), 0 shipped, 0 stalled, 0 deferred to tomorrow.
```

Then, **only if the owner has something to act on** (a new `assessed`/`needs_info` row, a
stalled item, a merged PR to celebrate, or entries left over past the cap), send one
`PushNotification`: what is waiting and what to run. Keep it one line, under 200 characters:

```
2 feedback items need your call (1 bug, 1 wording fix). Run /feedback-review in FCDiceBotCCVer.
```

Finally, print a short summary of the pass: what you assessed, each verdict, each dossier
path, anything you could not determine, and anything left for tomorrow.
