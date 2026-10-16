# P6-08 — Close phase 6

- **Status:** todo
- **Depends on:** all P6 cards
- **Size:** S (docs)

## Goal

Record what the Avalonia port taught about the architecture, and document the control.

## Files to create or change

- `src/LoomTerminal.Avalonia/README.md` — usage, differences with WinUI
- `docs/ARCHITECTURE.md` — the backend section lists both backends
- `docs/ROADMAP.md` — phase 6 ✅, and "Abstraction leaks found" (list any Presentation card opened because of phase 6;
  "none" is the expected answer)
- `README.md` — status

## Tests

- [ ] `docs/manual-tests.md` Avalonia column ticked.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`docs: close phase 6`
