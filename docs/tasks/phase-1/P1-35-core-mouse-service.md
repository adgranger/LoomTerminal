# P1-35 — `CoreMouseService` (mouse protocols and encodings)

- **Status:** done
- **Depends on:** P1-34
- **Size:** M

## Goal

Port the mouse reporting logic: protocols (NONE, X10, VT200, DRAG, ANY), encodings (DEFAULT, SGR, SGR_PIXELS), event
filtering and the bytes sent to the application. The UI backends only fill a `CoreMouseEvent` (ADR 0004).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/Types.ts` | 288–386 | `CoreMouseButton`, `CoreMouseAction`, `ICoreMouseEvent`, `CoreMouseEventType`, `ICoreMouseProtocol`, `CoreMouseEncoding` |
| `src/common/services/Services.ts` | 36–74 | `ICoreMouseService` |
| `src/common/services/CoreMouseService.ts` | 1–365 | `DEFAULT_PROTOCOLS`, `Modifiers`, `eventCode`, `DEFAULT_ENCODINGS`, `CoreMouseService` |
| `src/common/TestUtils.test.ts` | 61–77 | `MockCoreMouseService` |
| `src/common/services/CoreMouseService.test.ts` | 1–212 | all |

## Files to create or change

- `src/LoomTerminal.Core/Services/CoreMouseTypes.cs` — create (enums and `CoreMouseEvent` record)
- `src/LoomTerminal.Core/Services/ICoreMouseService.cs`, `CoreMouseService.cs` — create
- `tests/LoomTerminal.Core.Tests/TestUtils/MockCoreMouseService.cs` — create
- `tests/LoomTerminal.Core.Tests/Services/CoreMouseServiceTests.cs` — create

## Design decisions (already made — do not change)

- Protocols and encodings stay registered **by name** (`Dictionary<string, …>`), as xterm.js, because DECSET handlers
  select them by name (`'VT200'`, `'SGR'`…).
- `ICoreMouseEvent` → `sealed record CoreMouseEvent` with `Col`, `Row`, `X`, `Y` (pixels), `Button`, `Action`,
  `Ctrl`, `Alt`, `Shift`.
- `consumeWheelEvent(ev: WheelEvent, cellHeight, dpr)` takes a DOM event in JS. Port it as
  `int ConsumeWheelEvent(double deltaY, WheelDeltaMode mode, bool shift, bool alt, bool ctrl, double cellHeight, double dpr)`
  with `enum WheelDeltaMode { Pixel = 0, Line = 1, Page = 2 }` (DOM values), keeping the same arithmetic.
- `explainEvents` returns `Dictionary<string, bool>`.

## Tests

- [x] Port all of `CoreMouseService.test.ts` (`triggerMouseEvent`, `coords`, protocol/encoding tests).

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (✅ P1-35)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Pointer events from a UI (phase 3/4).

## Suggested commit message

`feat(core): port CoreMouseService`
