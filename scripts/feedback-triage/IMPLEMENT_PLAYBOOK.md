# Implementation pass — playbook

Runs after the owner approves an item in the review pass. A bug or wording fix goes straight to
a branch and a PR. A feature stops at a spec for sign-off before any code is written.

Everything here happens with the owner in the session. Nothing in this playbook is safe to run
unattended.

## Bug and wording fixes

1. **Branch from main** in the main checkout:

   ```
   git checkout main
   git pull
   git checkout -b claude/feedback-<shortId>-<slug>
   ```

2. **Mark it in flight:**

   ```
   powershell -ExecutionPolicy Bypass -File scripts\feedback-triage\ft.ps1 set ^
     -Id <full feedback id> -Fields "{\"status\":\"in_progress\",\"branch\":\"claude/feedback-<shortId>-<slug>\"}" ^
     -Event implement -Detail "started"
   ```

3. **Fix the root cause**, not the symptom the resident happened to see. Follow the conventions
   the codebase already uses — read the neighbours before writing:
   - `wiki-docs\Development-Guide.md` and `wiki-docs\Architecture.md` for structure.
   - Interactions are a processor + a command class + a registry entry + tests. Aliases are
     their own delegating command class; the `Aliases` array is display-only and does not route.
   - Shared text lives in one place: cooldown/consent warnings come from the `CooldownSpec` /
     `ConsentWarningText` single source of truth (override `BuildConsentWarning`, never
     `GetConsentWarning`); vices render through `ViceText.ViceName`; `Utils.*ToText` helpers read
     `Identifier.displayText` from the DB, so new identifiers need no code change.
   - Never expose "UTC day" to residents — say "Chateau day", or just "day".
   - `FChatDicebot.csproj` globs `**\*.cs`, so a new file builds without a csproj edit.

4. **Add or extend tests.** Pure helpers get unit tests (the `BuildFeedbackList` /
   `ParseFeedback` mold); DB-touching behavior uses `Testdatabasefixture`, which needs local
   Mongo running.

   ```
   dotnet build FChatDicebot.sln
   dotnet test FChatDicebot.Tests\FChatDicebot.Tests.csproj
   ```

   A red build or a failing test is a stop-and-report, not something to work around.

5. **Surface the wording.** Before calling anything done, list every changed user-facing string
   (before → after) for the owner's approval, even when the dossier already drafted it and even
   when the change is a single word. Apply their rewrites verbatim. First drafts come from
   `wiki-docs\Style-Guide.md`.

6. **Update the docs the change invalidates** — the relevant `wiki-docs\specs\*.md` as-shipped
   notes, `wiki-docs\Command-Reference.md` if help text or usage changed.

7. **Commit** (ask first; committing is gated), including the dossier and any ledger-adjacent
   files so the paper trail lands with the fix:

   ```
   git add -A
   git commit
   ```

   Message: what changed and why, referencing the feedback short id. End with the
   `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>` trailer.

8. **Open the PR** against `main` with `gh pr create`. The body states the resident's complaint,
   the root cause, the fix, test coverage, and a link to the dossier path. End with the
   `🤖 Generated with [Claude Code](https://claude.com/claude-code)` line. Never merge it —
   merging is the owner's.

9. **Record the PR:**

   ```
   powershell -ExecutionPolicy Bypass -File scripts\feedback-triage\ft.ps1 set ^
     -Id <full feedback id> -Fields "{\"status\":\"pr_open\",\"prNumber\":<n>,\"prUrl\":\"<url>\"}" ^
     -Event pr -Detail "<pr title>"
   ```

   The next daily pass notices the merge, sets `shipped`, and moves the dossier to `done\`.

## Features

1. **Write the spec first**, following the existing format and the conventions in
   `wiki-docs\specs\README.md`: pre-implementation specs live in the `Future-*` subfolder that
   fits (`Future-Interactions`, `Future-Economy`, `Future-Social`, `Infrastructure`), carry
   Assumptions / dependencies / file-scaffolding sections, and mark every user-facing string as
   pending owner wording review.
2. Point the ledger at it and pause for sign-off:

   ```
   powershell -ExecutionPolicy Bypass -File scripts\feedback-triage\ft.ps1 set ^
     -Id <full feedback id> -Fields "{\"status\":\"approved\",\"specPath\":\"wiki-docs/specs/Future-.../<Name>.md\"}" ^
     -Event spec -Detail "spec written, awaiting sign-off"
   ```

3. Once the owner signs off on the spec, implement it exactly as the bug flow above (branch →
   code → tests → wording review → PR). When it ships, the spec moves out of `Future-*` and
   gains its `**Status:** Implemented.` line, per the specs README.

## Do not

- Merge PRs, or push to `main` directly.
- Implement anything the owner declined or deferred, or expand scope past what they approved —
  a second finding along the way is a note in the PR body or a new dossier, not a bonus commit.
- Touch the `Feedback` collection. Ledger writes go through `ft.ps1`.
- Message residents as the bot.
