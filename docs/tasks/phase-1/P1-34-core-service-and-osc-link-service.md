# P1-34 — `CoreService` and `OscLinkService`

- **Status:** done
- **Depends on:** P1-33
- **Size:** S

## Goal

Port the terminal modes service (ANSI and DEC private modes, data and binary events toward the pty) and the OSC 8
hyperlink registry.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/Types.ts` | 263–279 | `IModes`, `IDecPrivateModes` |
| `src/common/services/Services.ts` | 75–111 | `ICoreService` |
| `src/common/services/CoreService.ts` | 1–92 | `DEFAULT_MODES`, `DEFAULT_DEC_PRIVATE_MODES`, `CoreService` (`reset`, `triggerDataEvent`, `triggerBinaryEvent`, events) |
| `src/common/Types.ts` | 128–135 | `IOscLinkData` |
| `src/common/services/Services.ts` | 307–335 | `IOscLinkService` |
| `src/common/services/OscLinkService.ts` | 1–115 | `OscLinkService` |
| `src/common/TestUtils.test.ts` | 88–116, 162–173 | `MockCoreService`, `MockOscLinkService` |
| `src/common/services/CoreService.test.ts` | 1–31 | all |
| `src/common/services/OscLinkService.test.ts` | 1–44 | all |

## Files to create or change

- `src/LoomTerminal.Core/Services/TerminalModes.cs` — create (`Modes`, `DecPrivateModes` as mutable classes with
  `Clone()`; JS freezes the defaults: expose `static Modes CreateDefault()` instead of frozen objects)
- `src/LoomTerminal.Core/Services/ICoreService.cs`, `CoreService.cs` — create
- `src/LoomTerminal.Core/Services/IOscLinkService.cs`, `OscLinkService.cs` — create
- `tests/LoomTerminal.Core.Tests/TestUtils/MockCoreService.cs`, `MockOscLinkService.cs` — create
- `tests/LoomTerminal.Core.Tests/Services/CoreServiceTests.cs`, `OscLinkServiceTests.cs` — create

## Design decisions (already made — do not change)

- `DecPrivateModes.MouseTrackingMode` is `string` in JS (`'none' | 'x10' | 'vt200' | 'drag' | 'any'`): port as
  `enum MouseTrackingMode { None, X10, Vt200, Drag, Any }`.
- `OnData` is `IEvent<string>` (JS strings, may contain any char); `OnBinary` is `IEvent<string>` too (latin-1
  string, converted to bytes by `TerminalSession`, P3-06); `OnUserInput`, `OnRequestScrollToBottom` are
  `IEvent<EventArgs>`.
- `triggerDataEvent` honors `disableStdin` and `scrollOnUserInput` exactly as in JS.

## Tests

- [x] Port `CoreService` › `reset` and `OscLinkService` › `constructor` tests.
- [x] New: `TriggerDataEvent("a", wasUserInput: true)` fires `OnUserInput` then `OnData`; with `DisableStdin` it fires nothing.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (✅ P1-34)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`CoreMouseService` (P1-35).

## Suggested commit message

`feat(core): port CoreService and OscLinkService`
