# P1-54 — `TerminalCore` (port of `CoreTerminal`)

- **Status:** todo
- **Depends on:** P1-53
- **Size:** M

## Goal

Create the composition root of the emulator: it builds and wires every service and the `InputHandler`, and exposes
the API Presentation uses (write, resize, scroll, reset, events, buffers, options).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/CoreTerminal.ts` | 46–145 | fields, events, getters, constructor (service creation and event forwarding) |
| | 158–252 | `writeSync` (as `Write`, see below), `input`, `resize`, `scroll`, `scrollLines`, `scrollPages`, `scrollToTop`, `scrollToBottom`, `scrollToLine`, `register*Handler`, `_setup`, `reset` |
| | 145–157 | `write` — **not ported** (it goes through `WriteBuffer`, ADR 0002); read it to see the callback semantics |

## Files to create or change

- `src/LoomTerminal.Core/TerminalCore.cs` — create
- `tests/LoomTerminal.Core.Tests/TerminalCoreTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class TerminalCore : Disposable` (Presentation sees it through `InternalsVisibleTo`; the public
  API of the packages is designed in phase 3).
- Constructor: `TerminalCore(TerminalOptions? options = null)`. It creates, in the JS order: `OptionsService`,
  `BufferService`, `NullLogService`, `CoreService`, `CoreMouseService`, `UnicodeService`, `CharsetService`,
  `OscLinkService`, `InputHandler` — no instantiation service (ADR 0002). Services are exposed as `internal`
  properties (`BufferService`, `CoreService`, …).
- `void Write(string data)` and `void Write(ReadOnlySpan<byte> data)` call `InputHandler.Parse` synchronously (this
  is JS `writeSync` without `maxSubsequentCalls`). The UTF-8 decoder state lives in `InputHandler` (as in JS), so
  chunks may split multi-byte characters.
- Events forwarded like JS (`OnBinary`, `OnData`, `OnLineFeed`, `OnScroll`, `OnTitleChange`, `OnBell`, `OnResize`,
  `OnCursorMove`, `OnRequestRefreshRows`, `OnColor`…), as `IEvent<T>` properties.
- `scrollToBottom(disableSmoothScroll)` drops the parameter (no smooth scrolling in Core).

## Tests

- [ ] New: `Write("hello\r\nworld")` then the active buffer's lines 0 and 1 are `hello` / `world`; `Resize(10, 5)`
      fires `OnResize`; `Write("\x1b]0;title\x07")` fires `OnTitleChange("title")`; `Write("\x1b[6n")` fires `OnData`
      with a CPR; `Reset()` clears the screen and attributes; a UTF-8 `é` split across two `Write(byte[])` calls is
      printed once.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (🔶 `CoreTerminal.ts`, P1-54; Windows heuristics in P1-55)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Windows wrapping heuristics (P1-55), `TerminalWriter` (P1-56).

## Suggested commit message

`feat(core): add TerminalCore, the emulator composition root`
