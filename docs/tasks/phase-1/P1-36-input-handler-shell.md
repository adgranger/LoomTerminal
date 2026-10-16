# P1-36 — `InputHandler` shell: fields, events, `Parse`, dirty rows, test harness

- **Status:** todo
- **Depends on:** P1-10, P1-22, P1-35
- **Size:** M

## Goal

Create the `InputHandler` class with its fields, events, constructor wiring (fallback handlers and print handler),
`Parse` (string and bytes, synchronous), the dirty row tracker, and the InputHandler test harness. The individual
handlers are added by P1-37 … P1-52, each registering its own sequences.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.ts` | 1–117 | imports (for reference), `GLEVEL`, `MAX_PARSEBUFFER_LENGTH`, `STACK_LIMIT` (not needed: async), `WindowsOptionsReportType`, `SLOW_ASYNC_LIMIT` (not needed) |
| | 118–169 | fields and emitters |
| | 170–247 | constructor: base wiring and the five fallback handlers, `setPrintHandler` |
| | 408–424 | `_getCurrentLinkId` |
| | 425–511 | `parse` |
| | 3436–3492 | `markRangeDirty`, `IDirtyRowTracker`, `DirtyRowTracker` |
| `src/common/Types.ts` | 451–545 | `IInputHandler` (reference: do not port the interface) |
| `src/common/InputHandler.test.ts` | 1–75 | `getCursor`, `getLines`, `TestInputHandler`, the `beforeEach` setup |

## Files to create or change

- `src/LoomTerminal.Core/InputHandling/InputHandler.cs` — create (fields, events, constructor, `Parse`)
- `src/LoomTerminal.Core/InputHandling/DirtyRowTracker.cs` — create
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerTestBase.cs` — create (setup, `GetCursor`, `GetLines`,
  `TestInputHandler` subclass exposing `CurAttrData`, `WindowTitleStack`, `IconNameStack`)
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerShellTests.cs` — create

## Design decisions (already made — do not change) — apply to every InputHandler card

- `internal partial class InputHandler : Disposable`. One partial file per concern, created by the card that ports
  it: `InputHandler.Print.cs`, `.Controls.cs`, `.Cursor.cs`, `.Tabs.cs`, `.Erase.cs`, `.InsertDelete.cs`,
  `.DeviceAttributes.cs`, `.Modes.cs`, `.RequestMode.cs`, `.Sgr.cs`, `.Status.cs`, `.WindowOptions.cs`, `.Esc.cs`,
  `.Osc.cs`, `.Dcs.cs`.
- **Registration lives with the handler**: each card adds a `private void Register<Concern>Handlers()` method in its
  partial file, with the `registerCsiHandler` / `setExecuteHandler` / `registerEscHandler` / `registerOscHandler` /
  `registerDcsHandler` lines of the constructor (lines 248–386) that call its handlers, in the JS order; the
  constructor calls those methods in the order the JS constructor registers them. This card creates the constructor
  with the calls commented out (`// RegisterControlHandlers(); — P1-38`) so each card only uncomments its line.
- Constructor parameters, in JS order: `IBufferService`, `ICharsetService`, `ICoreService`, `ILogService`,
  `IOptionsService`, `IOscLinkService`, `ICoreMouseService`, `IUnicodeService`, `IEscapeSequenceParser? parser = null`.
- `Parse(string data)` and `Parse(ReadOnlySpan<byte> data)` return `void`; drop `promiseResult`, `_parseStack` and the
  "paused" branches (ADR 0002). Keep `_parseBuffer` growth and the `MAX_PARSEBUFFER_LENGTH` chunking loop.
- Emitters (P1-01): `OnRequestBell`, `OnRequestRefreshRows` (`IEvent<RowRange?>` with
  `readonly record struct RowRange(int Start, int End)`), `OnRequestReset`, `OnRequestSendFocus`,
  `OnRequestSyncScrollBar`, `OnRequestWindowsOptionsReport`, `OnA11yChar`, `OnA11yTab`, `OnCursorMove`, `OnLineFeed`,
  `OnScroll`, `OnTitleChange`, `OnColor` (payload type created by P1-51; declare it then).
- Every handler method is `public` on the `internal` class (tests call them directly, as JS tests do).
- Logging calls of the fallbacks use `ILogService.Debug($"Unknown CSI code: {…}")`.

## Tests

- [ ] New: `Parse("abc")` with only the print handler wired does not throw and marks row 0 dirty
      (`OnRequestRefreshRows` fires with `RowRange(0, 0)`) — print itself comes in P1-37, so assert only the dirty
      range and that unknown CSI sequences reach the fallback (use a `MockLogService` that records messages).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `InputHandler.ts` (🔶 P1-36) with the list of partial files
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Every handler (P1-37 … P1-52).

## Suggested commit message

`feat(core): add the InputHandler shell, parse entry points and dirty row tracking`
