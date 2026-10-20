# P2-07 — Close phase 2

- **Status:** done
- **Depends on:** P2-01 … P2-06 and any follow-up card they opened
- **Size:** S (docs)

## Goal

Every conformance fixture passes (no skip left), the numbers are written, and the docs say so.

## Files to create or change

- `docs/ROADMAP.md` — phase 2 ✅
- `docs/PORTING.md` — "Behavior differences" updated with anything the fixtures revealed
- `README.md` — status line

## Tests

- [x] No `Skip` left in `Conformance/`.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`docs: close phase 2`
