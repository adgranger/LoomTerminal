# P3-23 — Close phase 3

- **Status:** done
- **Depends on:** all P3 cards
- **Size:** S (docs)

## Goal

Review the public API of Presentation as a whole (names, nullability, threading notes) before a UI uses it, and update
the docs.

## Files to create or change

- `src/LoomTerminal.Presentation/PublicAPI.Unshipped.txt` — reviewed; every entry has XML doc and threading notes
- `src/LoomTerminal.Presentation/README.md` — replace "Planned content" by the real structure and a usage example
  (create a session, take frames, render with `FrameRenderer`)
- `docs/ARCHITECTURE.md` — data flow and threading section matches the code
- `docs/ROADMAP.md` — phase 3 ✅
- `docs/PORTING.md` — browser/addon files ported in phase 3 listed

## Design decisions (already made — do not change)

- Renames are allowed in this card only (nothing ships yet); each rename is listed in the PR description.

## Tests

- [x] All tests green; no `Skip` in Presentation tests.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`docs: close phase 3 and review the Presentation API`
