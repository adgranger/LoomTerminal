# P1-37 — `InputHandler.Print`: printing, wrapping, wide and combining characters

- **Status:** todo
- **Depends on:** P1-36
- **Size:** M

## Goal

Port `print`: charset translation, Unicode width and joining, insert mode, auto-wrap, wide characters at the right
margin, hyperlink ids on cells.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 512–656 | `print` |
| `src/common/InputHandler.test.ts` | 598–623 | `print` |
| | 1585–1608 | `should correctly reset cells taken by wide chars` › `print` (the setup of the describe at 1585–1590 too) |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.Print.cs` — create
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerPrintTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36 for the shared InputHandler rules. `print` is wired by the constructor already (`setPrintHandler`).
- `print` reads `PrecedingJoinState` from the parser (P1-09) and `UnicodeService.charProperties`: port the
  `_parser.precedingJoinState` reads/writes exactly.
- The `onA11yChar` emitter is fired only when `screenReaderMode` is on, as in JS.

## Tests

- [ ] Port `print` › `should not cause an infinite loop (regression test)`, `should clear cells to the right on early wrap-around`.
- [ ] Port `should correctly reset cells taken by wide chars` › `print` (the EL/ICH/DCH/ECH tests of that describe
      come with their handlers in P1-41 and P1-42).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`REP` (P1-43).

## Suggested commit message

`feat(core): port InputHandler.print`
