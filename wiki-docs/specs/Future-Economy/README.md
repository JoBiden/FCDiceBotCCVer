# Future Economy — Spec Index

Self-contained design specs for **upcoming** Chateau Contract economy mechanics (currency, employment, work payouts). Same conventions as [Future-Interactions](../Future-Interactions/README.md): each file is written so a fresh implementation session can act on it without re-asking the user for design clarifications, and specs move one level up to [`specs/`](../) once they ship.

## Status

### Planned

| Spec | Adds |
|------|------|
| [Employer-Earnings](Employer-Earnings.md) | A MANOR kickback paid to an employer (25% of a worker's rolled reward, bonus on top) every time an employee `!work`s, a per-employee/per-currency earnings ledger on the employer's profile, and the read-only `!business` command to view it. |

## Conventions

These specs assume the same house rules as the rest of `wiki-docs/specs/` — see [Future-Interactions/README.md](../Future-Interactions/README.md#conventions-all-specs-assume) for the full list (processor pattern, `characteristics`/`currencies` storage, "Chateau day" wording, user-facing-string review before shipping, etc.).
