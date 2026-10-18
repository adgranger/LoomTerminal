# P1-40 — `InputHandler.Tabs`: TBC, CHT, CBT, HTS

- **Status:** done
- **Depends on:** P1-39
- **Size:** S

## Goal

Port the tab stop handlers.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 1081–1129 | `tabClear`, `cursorForwardTab`, `cursorBackwardTab` |
| | 3288–3301 | `tabSet` |
| | constructor | CSI `g`, `I`, `Z`; execute `C1.HTS`; ESC `H` |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.Tabs.cs` — create (with `RegisterTabHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerTabTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. ESC `H` and C1 HTS both call `tabSet`; register them here (not in P1-44).

## Tests

- [x] New (xterm.js has no dedicated tests): default stops every 8 columns (`\t` from column 0 → 8);
      `ESC H` at column 3 adds a stop (`\r\t` → 3); `CSI 0 g` clears the stop at the cursor; `CSI 3 g` clears all
      (`\t` then goes to the last column); `CSI 2 I` moves two stops forward; `CSI Z` moves one stop back.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(core): port the InputHandler tab stop handlers`
