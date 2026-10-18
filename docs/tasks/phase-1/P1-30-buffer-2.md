# P1-30 — `Buffer` (2/3): resize without reflow, memory cleanup

- **Status:** done
- **Depends on:** P1-29
- **Size:** M

## Goal

Port `Buffer.resize` (rows and columns, scrollback trimming, cursor clamping, Windows mode branch) and the batched
memory cleanup; reflow is called but stays unimplemented until P1-31.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/buffer/Buffer.ts` | 150–303 | `resize`, `_batchedMemoryCleanup`, `_isReflowEnabled` |
| `src/common/buffer/Buffer.test.ts` | 110–236 | `resize` › column size reduced / increased, row size reduced (incl. no scrollback), row size increased, row and column increased |
| | 1054–1068 | `buffer marked to have no scrollback` |
| | 1182–1210 | `memory cleanup after shrinking` |

## Files to create or change

- `src/LoomTerminal.Core/Buffer/Buffer.Resize.cs` — create (`partial`)
- `tests/LoomTerminal.Core.Tests/Buffer/BufferTests.Resize.cs` — create (`partial`)

## Design decisions (already made — do not change)

- `_batchedMemoryCleanup` is scheduled in JS with `IdleTaskQueue` / `setTimeout`. **Do not schedule anything:**
  expose `internal bool BatchedMemoryCleanup()` (returns `true` when done) and a field `MemoryCleanupPending`. The
  phase 3 `TerminalSession` calls it when the parser thread is idle. Tests call it in a loop until it returns `true`
  (the JS tests wait for the idle queue).
- `_reflow` exists but is empty in this card (`// P1-31`), and `_isReflowEnabled` is ported exactly; tests that need
  reflow are in P1-31/P1-32, so the resize tests ported here must pass with reflow disabled or not triggered — the
  JS tests in lines 110–236 are written that way (they do not rely on wrapped lines).

## Tests

- [x] Port the tests of lines 110–236, 1054–1068 and 1182–1210.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` "Behavior differences": "memory cleanup after shrinking is driven by the session's idle loop"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Reflow (P1-31).

## Suggested commit message

`feat(core): port Buffer resize and memory cleanup`
