# Owner review pass — playbook

The owner is here and deciding. Your job is to present each assessed item compactly, get a
decision plus any design answers, write both down where they survive, and then start the work
they approved.

Optional argument: a short id (e.g. `6a69a8a2`) to review just that item.

## 1. Load the queue

```
cd C:\source\repos\FCDiceBotCCVer
powershell -ExecutionPolicy Bypass -File scripts\feedback-triage\ft.ps1 status -AwaitingOwner
```

Read each row's `dossierPath`. Also run `ft.ps1 status -Open` once so you can mention anything
already approved or in flight if the owner asks.

If nothing is awaiting a decision, say so, mention what is in flight, and stop. Do not go
hunting for new feedback — `ft.ps1 new` is the daily pass's job (though if it reports entries
and the owner wants them assessed now, follow `TRIAGE_PLAYBOOK.md` for those first).

## 2. Present one item at a time

Oldest submission first. Before asking anything, give the owner a tight read — a few lines,
not the whole dossier:

- what the resident said (quoted, trimmed to the point)
- what you found, with the `file:line` that matters
- your recommendation and the effort estimate
- for a `needs_info` item: exactly what question they would need to relay to the submitter

Link the dossier so they can open the full assessment.

## 3. Ask

Use one `AskUserQuestion` call per item, up to 4 questions: the decision first, then that
item's design questions from the dossier (recommended option first, as written). Keep the
questions the dossier already framed — do not invent new forks at this stage.

The decision question always offers:

- **Approve** — do it now.
- **Defer** — worth doing, not now (goes to `deferred`; it will not be re-assessed).
- **Decline** — not doing it (goes to `declined`; ask for a one-line reason if they don't give
  one, since it is the only record of why).
- **Needs info** — cannot proceed until the submitter clarifies (`needs_info`).

If the owner's answer conflicts with something the dossier assumed, say so plainly in one
sentence and follow their call.

## 4. Record the decision

For each item, in this order:

1. Append to the dossier's **Owner decision** section — the date, the decision, every answer
   they gave verbatim-in-substance, and any instruction that changes the plan. Append; never
   overwrite an earlier decision.
2. Update the ledger:

```
powershell -ExecutionPolicy Bypass -File scripts\feedback-triage\ft.ps1 set ^
  -Id <full feedback id> ^
  -Fields "{\"status\":\"approved\",\"decidedAt\":null,\"decision\":{\"action\":\"fix\",\"notes\":\"<what they chose>\"}}" ^
  -Event decision -Detail "<one-line summary of the decision>"
```

`status` is one of `approved`, `deferred`, `declined`, `needs_info`. For `duplicate`, set
`duplicateOf`. Move the dossier to `done\` for anything closed out (`declined`, `duplicate`,
`obsolete`); `deferred` dossiers stay in `queue\` so they remain visible.

## 5. Start the approved work

Ask whether to start now or leave it queued. When starting, follow
[IMPLEMENT_PLAYBOOK.md](IMPLEMENT_PLAYBOOK.md) — bugs and wording fixes go to a branch and a
PR, features get a spec for sign-off first.

Handle approved items one at a time, finishing each (PR opened, or spec written) before
starting the next, unless the owner says otherwise.

## 6. Close the session out

Report: what was decided, what is now in flight (with PR links), what is deferred, and what
still needs the submitter contacted. The bot never PMs residents on its own — if the owner
promised someone an answer, that is theirs to send.
