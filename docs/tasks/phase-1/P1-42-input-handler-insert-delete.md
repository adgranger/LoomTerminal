# P1-42 — `InputHandler.InsertDelete`: IL, DL, ICH, DCH, SU, SD, SL, SR, DECIC, DECDC

- **Status:** done
- **Depends on:** P1-41
- **Size:** M

## Goal

Port the line and character insertion/deletion handlers and the vertical/horizontal scroll handlers.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 1311–1574 | `insertLines`, `deleteLines`, `insertChars`, `deleteChars`, `scrollUp`, `scrollDown`, `scrollLeft`, `scrollRight`, `insertColumns`, `deleteColumns` |
| | constructor | CSI `@`, ` @`, ` A`, `L`, `M`, `P`, `S`, `T`, `'}`, `'~` |
| `src/common/InputHandler.test.ts` | 76–133 | `SL/SR/DECIC/DECDC` |
| | 1302–1315, 1330–1345 | clamping: `DCH`, `DCH - should delete last cell`, `ICH`, `ICH - should delete last cell` |
| | 1617–1634 | wide chars: `ICH`, `DCH` |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.InsertDelete.cs` — create (with `RegisterInsertDeleteHandlers`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerInsertDeleteTests.cs` — create

## Design decisions (already made — do not change)

- See P1-36. The `scroll margins` tests (lines 1378–1440) need DECSTBM: they are ported by P1-49 (DECSTBM).

## Tests

- [x] Port the tests listed in the sources table.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`insertChars` / `deleteChars` regression tests (P1-53).

## Suggested commit message

`feat(core): port the InputHandler insert, delete and scroll handlers`
