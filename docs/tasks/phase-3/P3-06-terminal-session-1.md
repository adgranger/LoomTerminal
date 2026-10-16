# P3-06 — `TerminalSession` (1/2): pty, parser thread, input, resize, exit

- **Status:** todo
- **Depends on:** P3-01, P1-56
- **Size:** M

## Goal

The public object an app creates per terminal: it owns a `TerminalCore`, a pty connection and the parser thread, and
moves data both ways. Snapshots for rendering come in P3-10.

## Files to create or change

- `src/LoomTerminal.Presentation/Session/TerminalSession.cs` — create (public)
- `src/LoomTerminal.Presentation/Session/TerminalSessionOptions.cs` — create (public record: `TerminalOptions`-like
  subset exposed publicly: `Scrollback`, `CursorStyle`, `CursorBlink`, `TabStopWidth`, `WordSeparator`,
  `WindowsPty` (default from `Environment.OSVersion.Version.Build` when on Windows), `TermProgram` (env var))
- `tests/LoomTerminal.Presentation.Tests/Session/FakePtyConnection.cs` — create (an `IPtyConnection` over two channels)
- `tests/LoomTerminal.Presentation.Tests/Session/TerminalSessionTests.cs` — create

## Design decisions (already made — do not change)

- Construction: `public static TerminalSession Start(PtyStartOptions pty, TerminalSessionOptions? options = null)`
  (starts the shell with `PseudoTerminal.Start`) and `public static TerminalSession Attach(IPtyConnection connection,
  int columns, int rows, TerminalSessionOptions? options = null)` (tests, custom transports).
- One **work queue**: a bounded `Channel<TerminalWorkItem>` (P1-56), capacity 64, `FullMode.Wait`.
  - A pump task copies `IPtyConnection.Output` chunks into it with `WriteAsync` (so a slow parser still backpressures
    the shell through the bounded pty channel).
  - Every public method that needs Core posts a `TerminalWorkItem.FromAction(...)` to the **same** queue, so output
    and input stay strictly ordered. Because the queue is bounded, post with `WriteAsync(...)` and do not await it
    (fire and forget, exceptions observed and logged), so UI calls never block.
- **Parser thread**: one dedicated thread (`new Thread(..., IsBackground = true, Name = "LoomTerminal parser")`)
  running `new TerminalWriter(core).Run(queue.Reader, afterBatch, cancellationToken)`. `afterBatch` is where P3-10
  publishes the frame; in this card it does nothing.
- Input from Core (`CoreService.OnData`: UTF-8 encode; `OnBinary`: Latin-1 bytes) is written to the pty on the parser
  thread (`IPtyConnection.Write` never blocks).
- Public methods (thread-safe, return immediately): `SendText(string)` (typed text), `SendKey(TerminalKeyEvent)`
  (through `KeyInputHandler`, P3-04, and returns its `KeyInputOutcome` synchronously — the decision only needs the
  current modes: `afterBatch` publishes an immutable `ModesSnapshot` record — application cursor keys, bracketed
  paste, mouse tracking — in a `volatile` field that `SendKey` reads), `Paste(string)`, `Resize(int columns, int rows)`
  (resizes Core and the pty, ignores sizes < 2×1), `WriteToDisplay(string vtText)` (writes text to the **emulator
  only**, as if the shell had printed it — for host messages such as VIGO's dimmed `[exited 0]` line; works after
  the shell exited), `Dispose()` (disposes the pty, stops the thread, idempotent).
- Public events (raised on the parser thread; documented as such): `Exited` (`int` exit code), `TitleChanged`
  (`string`), `Bell`. Backends marshal them to the UI thread.
- `KeyPreview` is a public `Func<TerminalKeyEvent, bool>?` property, called on the caller's thread by `SendKey`.

## Tests

- [ ] New (with `FakePtyConnection`): output written to the fake appears in Core (query through an internal
      `RunOnParserThreadAsync(Func<TerminalCore, T>)` helper); `SendText("ls\r")` reaches the fake's input as UTF-8;
      a CPR request from the "shell" (`\x1b[6n`) is answered on the fake's input; `Resize` resizes both;
      completing the fake output and setting its exit code raises `Exited`; `Dispose` twice is safe.
- [ ] New (Windows only, real ConPTY): `Start(new PtyStartOptions("cmd.exe /c echo hi"))` ends with `Exited(0)` and
      the screen contains `hi`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Public API files updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Render snapshots (P3-10), theme (P3-07).

## Suggested commit message

`feat(presentation): add TerminalSession with its parser thread`
