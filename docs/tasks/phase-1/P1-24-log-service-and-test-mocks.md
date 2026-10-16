# P1-24 — `ILogService` and the test mocks

- **Status:** todo
- **Depends on:** P1-21, P1-23
- **Size:** S

## Goal

Add the minimal logging service the services need, and port the xterm.js test mocks used by the InputHandler and
service tests.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/services/Services.ts` | 159–179 | `LogLevelEnum`, `ILogService` |
| `src/common/services/LogService.ts` | 1–124 | **read only**: we only keep a null logger and a list logger for tests |
| `src/common/TestUtils.test.ts` | 61–202 | `MockCoreMouseService`, `MockCharsetService`, `MockLogService`, `MockOptionsService`, `MockOscLinkService`, `MockUnicodeService` |

## Files to create or change

- `src/LoomTerminal.Core/Services/ILogService.cs` — create (`ILogService` with `LogLevel Level`, `Trace`, `Debug`,
  `Info`, `Warn`, `Error` taking a `string message`), `NullLogService` (singleton, does nothing)
- `tests/LoomTerminal.Core.Tests/TestUtils/MockLogService.cs`, `MockOptionsService.cs`, `MockUnicodeService.cs`,
  `MockCharsetService.cs`, `MockCoreMouseService.cs`, `MockOscLinkService.cs` — create **only the ones whose
  interface exists now** (`MockLogService`, `MockOptionsService`, `MockUnicodeService`). The other three are created
  by the cards that add their interfaces (P1-25, P1-34, P1-35) — note it in those cards' PRs.

## Design decisions (already made — do not change)

- Log methods take an already formatted `string` (no `params object[]`: callers use interpolated strings).
- `MockOptionsService` exposes `RawOptions` with a **public setter** so tests can write
  `options.RawOptions = options.RawOptions with { ConvertEol = true };` (the JS tests assign `options.rawOptions.x`).
  Setting it raises `OnOptionChange` for changed properties, like `SetOptions`.
- `MockUnicodeService` is V6-based, exactly as the JS mock (its comment says "defaults to V6 always to keep tests
  passing").

## Tests

- [ ] New: `MockOptionsService` setter raises `OnOptionChange` for each changed option.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`LogService` with console output, `logger` option.

## Suggested commit message

`feat(core): add the log service and the first test mocks`
