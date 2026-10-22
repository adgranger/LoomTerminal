# P3-10 — `TerminalSession` (2/2): frame publication and idle work

- **Status:** done
- **Depends on:** P3-06, P3-09
- **Size:** M

## Goal

After each batch, the parser thread builds an immutable `RenderFrame` (P3-09) and swaps it in under the session lock;
the UI thread takes the latest frame when it renders. Idle work (buffer memory cleanup, P1-30) runs when the queue is empty.

## Files to create or change

- `src/LoomTerminal.Presentation/Session/TerminalSession.cs` — frame publication, `FrameAvailable` event,
  `RenderFrame? TryTakeFrame()`, idle work
- `tests/LoomTerminal.Presentation.Tests/Session/TerminalSessionFrameTests.cs` — create

## Design decisions (already made — do not change)

- `afterBatch` (P1-56) calls `RenderFrameBuilder.Build(core, previousFrame, viewState)` (P3-09), then under the lock
  replaces `_latestFrame` and sets `_frameDirty = true`, then raises `FrameAvailable` **once per dirty transition**
  (if `_frameDirty` was already true, no new event: the UI has not consumed the previous one yet).
- `RenderFrame? TryTakeFrame()` (UI thread): under the lock, returns `_latestFrame` if `_frameDirty`, sets it false;
  otherwise returns `null`. Backends call it from their per-frame callback after `FrameAvailable`.
- Building a frame copies only the visible rows (plus the dirty-row set); a frame is never mutated after it is published.
- Idle work: when `Run`'s queue is empty after a batch, the session posts an action that calls
  `Buffer.BatchedMemoryCleanup()` if `MemoryCleanupPending`, at most once per 50 ms.

## Tests

- [x] New: writing output raises `FrameAvailable` once even for 10 batches before the first `TryTakeFrame`; the frame
      returned contains the text; a second `TryTakeFrame` returns `null`; a frame taken earlier is unchanged after
      more output (immutability); 1000 concurrent `SendText` calls from 4 threads while output flows do not throw and
      every character arrives (ordering per thread kept).

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing frames (P3-21).

## Suggested commit message

`feat(presentation): publish immutable render frames from the parser thread`
