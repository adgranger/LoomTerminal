# P1-56 — `TerminalWriter`: the single work loop of the emulator

- **Status:** done
- **Depends on:** P1-54
- **Size:** S

## Goal

Replace xterm.js `WriteBuffer` (ADR 0002, decision 6) with one blocking loop that consumes an ordered queue of work
items — pty output chunks and actions on the emulator — and calls a hook after each batch, so the owner can publish a
frame. It is the only code that touches `TerminalCore` once a session runs (ADR 0002, decision 5).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to take |
|---|---|---|
| `src/common/input/WriteBuffer.ts` | 1–247 | **ideas only** (`// Inspired by`): chunk batching, `handleUserInput` |

## Files to create or change

- `src/LoomTerminal.Core/TerminalWorkItem.cs` — create
- `src/LoomTerminal.Core/TerminalWriter.cs` — create
- `tests/LoomTerminal.Core.Tests/TerminalWriterTests.cs` — create

## Design decisions (already made — do not change)

- Core must not reference `LoomTerminal.Pty` (architecture rule): the writer reads a `ChannelReader<TerminalWorkItem>`,
  never an `IPtyConnection`.
- `internal readonly struct TerminalWorkItem` with two factories: `FromOutput(byte[] data)` and
  `FromAction(Action<TerminalCore> action)`; exactly one of `Data` / `Action` is set.
- `internal sealed class TerminalWriter(TerminalCore core)` with
  `void Run(ChannelReader<TerminalWorkItem> work, Action<int> afterBatch, CancellationToken cancellationToken)`:
  1. block until an item is available (`work.WaitToReadAsync(ct).AsTask().GetAwaiter().GetResult()`; `false` → return);
  2. process every item available right now (`TryRead`), in order: output → `core.Write(data)`, action → `action(core)`;
     stop the batch early once 512 KB of output were written;
  3. call `afterBatch(bytesWrittenInThisBatch)` once;
  4. loop. Cancellation throws `OperationCanceledException`.
- `Run` is synchronous on purpose: the owner runs it on its own dedicated thread (P3-06). No locks inside.
- An exception thrown by an action propagates out of `Run` (the owner decides); an exception from `core.Write` too.

## Tests

- [x] New: three output items queued before `Run` produce one `afterBatch` call with the total length and the expected
      screen; an action queued between two outputs runs between them (order kept); a UTF-8 sequence split across two
      items prints one character; completing the channel ends `Run`; cancelling ends it with `OperationCanceledException`;
      2 MB queued at once produce at least 4 `afterBatch` calls.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `WriteBuffer.ts` → "replaced by `TerminalWriter`"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The session that owns the thread and the queue (P3-06).

## Suggested commit message

`feat(core): add TerminalWriter, the single work loop of the emulator`
