# P1-57 — Close phase 1

- **Status:** done
- **Depends on:** all P1 cards
- **Size:** S (docs)

## Goal

Make the documentation match the code at the end of phase 1 and prepare phase 2.

## Files to create or change

- `docs/PORTING.md` — every file of `src/common` is ✅, "replaced", or "not ported" with a reason; "Behavior
  differences" and "Tests not ported" are complete
- `src/LoomTerminal.Core/README.md` — replace "Planned content" by what exists (folders, entry point `TerminalCore`)
- `docs/ROADMAP.md` — phase 1 ✅
- `docs/adr/0002-core-design.md`, `0003-testing-strategy.md` — status `accepted` (if they were accepted at P1-01, just check)
- `README.md` — status line

## Design decisions (already made — do not change)

- No code change in this card, except deleting dead code found while writing the docs (list it in the PR).

## Tests

- [x] `PortedFileHeaderTests` (P1-01) is green: every ported file has its header.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `rtk dotnet publish samples/LoomTerminal.PtyConsole -c Release -r win-x64` — 0 IL/AOT warnings (needs the VS C++ workload; otherwise check the CI run)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Any new feature.

## Suggested commit message

`docs: close phase 1`
