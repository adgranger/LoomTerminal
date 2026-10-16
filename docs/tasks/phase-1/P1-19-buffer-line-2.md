# P1-19 — `BufferLine` (2/2): editing, resize, copy, strings

- **Status:** todo
- **Depends on:** P1-18
- **Size:** M

## Goal

Finish `BufferLine`: insert/delete/replace cells, resize (with memory cleanup), fill, copy, clone, trimmed lengths,
`copyCellsFrom` and `translateToString`.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/buffer/BufferLine.ts` | 256–551 | `insertCells`, `deleteCells`, `replaceCells`, `resize`, `cleanupMemory`, `fill`, `copyFrom`, `clone`, `getTrimmedLength`, `getNoBgTrimmedLength`, `copyCellsFrom`, `translateToString` |
| `src/common/buffer/BufferLine.test.ts` | 278–473 | `resize`, `getTrimLength`, `translateToString with and w'o trimming` |

## Files to create or change

- `src/LoomTerminal.Core/Buffer/BufferLine.Editing.cs` — create (`partial`)
- `tests/LoomTerminal.Core.Tests/Buffer/BufferLineTests.Editing.cs` — create (`partial`)

## Design decisions (already made — do not change)

- `resize` reallocates like JS: when shrinking a lot it keeps the larger array and only `cleanupMemory` shrinks it
  (`CLEANUP_THRESHOLD`). Port `Uint32Array.prototype.subarray/set` with `Array.Copy` on the same ranges.
- `translateToString(trimRight, startCol, endCol, outColumns)` → `string TranslateToString(bool trimRight = false,
  int startCol = 0, int endCol = -1, List<int>? outColumns = null)`; `-1` means "line length" (JS default).

## Tests

- [ ] Port lines 278–473: `resize`, `getTrimLength`, `translateToString with and w'o trimming`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Tests from line 474 (P1-20).

## Suggested commit message

`feat(core): port BufferLine editing, resize and strings`
