# P1-28 — `Marker`, `BufferRange`, reflow helper functions

- **Status:** todo
- **Depends on:** P1-02, P1-20
- **Size:** M

## Goal

Port the small building blocks of `Buffer`: line markers, range length, and the pure reflow functions that compute
new line layouts when the terminal width changes.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/buffer/Marker.ts` | 1–43 | `Marker` (`id`, `line`, `isDisposed`, `onDispose`, `dispose`, `register`) |
| `src/common/Types.ts` | 257–262 | `IMarker` |
| `src/common/buffer/BufferRange.ts` | 1–13 | `getRangeLength` |
| `src/common/buffer/BufferReflow.ts` | 1–226 | `INewLayoutResult`, `reflowLargerGetLinesToRemove`, `reflowLargerCreateNewLayout`, `reflowLargerApplyNewLayout`, `reflowSmallerGetNewLineLengths`, `getWrappedLineTrimmedLength` |
| `src/common/buffer/BufferRange.test.ts` | 1–32 | all |
| `src/common/buffer/BufferReflow.test.ts` | 1–93 | all |

## Files to create or change

- `src/LoomTerminal.Core/Buffer/Marker.cs`, `IMarker.cs` — create
- `src/LoomTerminal.Core/Buffer/BufferRange.cs` — create (`readonly record struct BufferCellPosition(int X, int Y)`,
  `readonly record struct BufferRange(BufferCellPosition Start, BufferCellPosition End)`, `static int GetRangeLength(BufferRange, int bufferCols)`)
- `src/LoomTerminal.Core/Buffer/BufferReflow.cs` — create (`internal static class BufferReflow`)
- `tests/LoomTerminal.Core.Tests/Buffer/BufferRangeTests.cs`, `BufferReflowTests.cs` — create

## Design decisions (already made — do not change)

- `Marker` ids come from a static counter as in JS (`Marker._nextId`). `Marker.Line` is a public field (`int`,
  `-1` once disposed, as JS sets it).
- `IMarker : IDisposable` with `int Id`, `bool IsDisposed`, `int Line`, `IEvent<EventArgs> OnDispose`.
- The reflow functions take `CircularList<IBufferLine>` and `BufferLine[]` exactly as in JS; results are `int[]`.
  `INewLayoutResult` → `readonly record struct NewLayoutResult(int[] Layout, int CountRemoved)`.

## Tests

- [ ] Port `BufferRange` › `getRangeLength`.
- [ ] Port `BufferReflow` › `reflowSmallerGetNewLineLengths`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (✅ P1-28)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`Buffer` (P1-29).

## Suggested commit message

`feat(core): port Marker, BufferRange and the reflow helpers`
