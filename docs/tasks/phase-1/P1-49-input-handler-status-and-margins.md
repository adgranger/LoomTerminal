# P1-49 — `InputHandler.Status`: DSR, DECSTR, DECSCUSR, DECSTBM (+ scroll margin tests)

- **Status:** todo
- **Depends on:** P1-42, P1-45, P1-48
- **Size:** M

## Goal

Port device status reports (cursor position report, private DSR), soft reset, cursor style selection and the scroll
region, and the scroll margin tests postponed from P1-42.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 2653–2719 | `deviceStatus`, `deviceStatusPrivate` |
| | 2720–2760 | `softReset` |
| | 2761–2793 | `setCursorStyle` |
| | 2794–2839 | `setScrollRegion` |
| | constructor | CSI `n`, `?n`, `!p`, ` q`, `r` |
| `src/common/InputHandler.test.ts` | 202–238 | `setCursorStyle` |
| | 1346–1377 | `DECSTBM - scroll margins` |
| | 1378–1440 | `scroll margins` (from P1-42) |
| | 1932–1980 | `DECSTR` |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.Status.cs` — create (with `RegisterStatusHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerStatusTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. `setCursorStyle` writes `CursorStyle` and `CursorBlink` through `IOptionsService.SetOptions` (JS writes
  `optionsService.options.cursorStyle`); in tests with `MockOptionsService` the JS assertions read
  `optionsService.options.*`: read `RawOptions.*`.

## Tests

- [ ] Deferred from P1-38: port the first `BS with reverseWraparound set/unset` block (InputHandler.test.ts 134–181,
      `should not reverse outside of scroll margins`; C# name `Backspace_reverse_wraparound_set_should_not_reverse_outside_of_scroll_margins`).

- [ ] Port the four suites listed in the sources table.
- [ ] New: `CSI 6 n` at row 3, column 5 sends `\x1b[4;6R`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

XTWINOPS (P1-50).

## Suggested commit message

`feat(core): port DSR, DECSTR, DECSCUSR and DECSTBM`
