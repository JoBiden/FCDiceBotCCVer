# Triage log

One line per pass. Newest at the bottom.

- 2026-07-30 — first pass (run by hand during setup, from the feedback-triage worktree). 3 in the collection: 1 seeded closed (6a51dd0b, owner's creative work), 2 assessed — 6a69a8a2 bug/fix (pledge title timing, root cause confirmed from live data), 6a59933a wording/decline (already fixed in 6f23fdd). 0 shipped, 0 stalled, 0 left over.
- 2026-08-02 — 3 assessed (6a6fb15d wording/fix, 6a6efae4 feature/spec, 6a6f3d94 feature/spec), 0 shipped, 0 stalled, 0 deferred to tomorrow. Pass was blocked on start: `bin/ft.js` is matched by `.gitignore`'s `**/bin/` and was never committed, so `ft.ps1` on main throws. Restored it untracked from the `magical-morse-09753b` worktree (its only copy on disk) to finish the run — the real fix is a PR, flagged to the owner.
