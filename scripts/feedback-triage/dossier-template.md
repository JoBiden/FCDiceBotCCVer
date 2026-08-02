# <shortId> — <one-line title>

| | |
|---|---|
| **Feedback id** | `<full 24-char _id>` |
| **Submitted** | `<ISO timestamp>` (<relative>) by **<display name>** (`<login name>`) |
| **Source** | channel `<name>` / private message |
| **Tagged** | `<category as submitted>` |
| **Verdict** | `bug` \| `wording` \| `feature` \| `duplicate` \| `invalid` \| `owner_creative` \| `needs_info` |
| **Recommendation** | `fix` \| `spec` \| `defer` \| `decline` \| `ask` |
| **Confidence** | high \| medium \| low |
| **Effort** | trivial (<1h) \| small \| medium \| large |

## What they said

> <verbatim submission text, unedited>

## What it means

<Plain restatement of the complaint or request, naming the actual commands and systems
involved. If the submission is ambiguous, say what the readings are and which one this
assessment assumes.>

## Assessment

<What the code actually does today, with file:line references. For a bug: the root cause if
found, or the narrowed-down suspects and what would confirm it. For a feature: what exists
already, what would have to change, and which existing systems it rides on. Note whether
anything since the submission date already changed this (git log check).>

- **Reproduces / confirmed:** yes | no | can't tell from code alone — <why>
- **Related code:** <file:line>, <file:line>
- **Related specs / docs:** <wiki-docs/... links, or "none">
- **Duplicate of:** <shortId, or "none">
- **Already fixed since submission:** no | yes — <commit>

## Recommended action

<One paragraph: what should be done, and why that scope. If the recommendation is decline or
defer, say plainly what the resident loses by that.>

## Decisions needed from the owner

<Numbered. Each one is a real fork where the answer changes the work — not a rubber stamp.
Give 2–4 concrete options, recommended one first, and state what each implies. Omit this
section entirely if the fix is unambiguous.>

1. **<Short question>**
   - **<Option A>** (recommended) — <what it implies>
   - **<Option B>** — <what it implies>

## User-facing text that would change

<Every string a resident would read, before → after, per the standing rule that changed
wording gets owner approval before anything is called done. Draft from
wiki-docs/Style-Guide.md. Write "none" if the change is invisible to residents.>

| Where | Now | Proposed |
|---|---|---|
| `<file:line>` | <current> | <proposed> |

## Owner decision

<Left empty by the daily pass. The review pass appends the decision, the answers to the
questions above, and the date. Never overwrite an earlier decision — append.>
