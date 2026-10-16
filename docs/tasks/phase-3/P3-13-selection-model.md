# P3-13 — `SelectionModel`

- **Status:** todo
- **Depends on:** P3-01
- **Size:** S

## Goal

Port the selection model: start/end in buffer coordinates, word/line selection extents, reversed selections, trimming
when the scrollback drops lines.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/browser/selection/SelectionModel.ts` | 1–144 | `SelectionModel` |
| `src/browser/selection/Types.ts` | 1–15 | `ISelectionRedrawRequestEvent` (if used) |
| `src/browser/selection/SelectionModel.test.ts` | 1–140 | all |

## Files to create or change

- `src/LoomTerminal.Presentation/Selection/SelectionModel.cs` — create (internal)
- `tests/LoomTerminal.Presentation.Tests/Selection/SelectionModelTests.cs` — create

## Design decisions (already made — do not change)

- Coordinates `[number, number]` → `(int X, int Y)?` tuples (`null` for `undefined`).
- The model needs `IBufferService` (cols): use Core's `IBufferService` (internal access) and the Core
  `MockBufferService` pattern for tests (create a small Presentation-side test helper building a real `BufferService`).

## Tests

- [ ] Port all of `SelectionModel.test.ts`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Selection logic (P3-14), pointer handling (P3-15).

## Suggested commit message

`feat(presentation): port the selection model`
