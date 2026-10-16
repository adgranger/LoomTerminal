# P1-45 — `InputHandler.Modes`: SM, RM, DECSET, DECRST (incl. alternate screen, mouse, bracketed paste)

- **Status:** todo
- **Depends on:** P1-44, P1-35
- **Size:** M

## Goal

Port the four mode handlers: ANSI modes (IRM, LNM…) and DEC private modes (DECCKM, DECOM, DECAWM, cursor visibility,
mouse protocols and encodings, focus events, alternate screen 47/1047/1048/1049, bracketed paste, synchronized
output…).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 1749–1876 | `setMode` |
| | 1877–2000 | `setModePrivate` |
| | 2001–2124 | `resetMode` |
| | 2125–2244 | `resetModePrivate` |
| | constructor | CSI `h`, `?h`, `l`, `?l` |
| `src/common/InputHandler.test.ts` | 239–250 | `setMode` |
| | 624–678 | `alt screen` |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.Modes.cs` — create (with `RegisterModeHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerModeTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. Modes 1048 and 1049 call `saveCursor` / `restoreCursor` (P1-44).
- Mouse modes set `ICoreMouseService.ActiveProtocol` / `ActiveEncoding` by name, as in JS.
- Unknown modes are ignored silently, as in JS.

## Tests

- [ ] Port `setMode` › `should toggle bracketedPasteMode`.
- [ ] Port `alt screen` (all six tests).
- [ ] New: `CSI ? 1 h` sets `DecPrivateModes.ApplicationCursorKeys`; `CSI ? 1000 h` + `CSI ? 1006 h` select `VT200`
      + `SGR`; `CSI ? 25 l` hides the cursor (`CoreService.IsCursorHidden`).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

DECRQM (P1-45).

## Suggested commit message

`feat(core): port the InputHandler mode handlers`
