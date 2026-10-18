# P1-50 — `InputHandler.WindowOptions`: XTWINOPS and title stacks

- **Status:** done
- **Depends on:** P1-49
- **Size:** S

## Goal

Port `CSI Ps t` (window manipulation reports, push/pop title), gated by the `windowOptions` option.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 69–105 | `paramToWindowOption`, `WindowsOptionsReportType` (if not already in P1-36) |
| | 2840–2897 | `windowOptions` |
| | constructor | CSI `t` |
| `src/common/InputHandler.test.ts` | 1459–1584 | `windowOptions` |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.WindowOptions.cs` — create (with `RegisterWindowOptionHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerWindowOptionTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. Pixel size reports (14, 16) fire `OnRequestWindowsOptionsReport`; the UI answers later (phase 3).
- `DECCOLM` (`CSI ? 3 h/l`, in `setModePrivate`, P1-45) only works with `SetWinLines`: the test at 1569 checks it
  here.

## Deviation from the card (found while porting)

`PopTitle` calls `setTitle` / `setIconName` and the tests drive the title with OSC 0, so this card also created
`InputHandler.Osc.cs` with the `_windowTitle` / `_iconName` fields, `SetTitle`, `SetIconName` and `RegisterOscHandlers`
(OSC 0, 1, 2 only). P1-51 adds the other OSC handlers to that file.

## Tests

- [x] Port `windowOptions` (all `it`s).

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(core): port XTWINOPS and the title stacks`
