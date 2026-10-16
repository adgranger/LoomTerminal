# P1-13 — Parser tests (3/4): examples, coverage, set/clear handlers

- **Status:** todo
- **Depends on:** P1-12
- **Size:** M (tests only)

## Goal

Port the end-to-end parser examples, the coverage tests and the custom handler tests (ESC, CSI, OSC, DCS).

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/parser/EscapeSequenceParser.test.ts` | 1033–1144 | `escape sequence examples` |
| | 1145–1187 | `coverage tests` |
| | 1188–1582 | `set/clear handler` (including `ESC custom handlers`, `CSI custom handlers`, `OSC custom handlers`, `DCS custom handlers`) |

## Files to create or change

- `tests/LoomTerminal.Core.Tests/Parser/EscapeSequenceParserTests.Examples.cs` — create
- `tests/LoomTerminal.Core.Tests/Parser/EscapeSequenceParserTests.Handlers.cs` — create

## Design decisions (already made — do not change)

- Tests in these suites that use `async` handlers or `Promise` results are **not ported**: list their names in
  `docs/PORTING.md` ("Tests not ported").
- `OscHandler` / `DcsHandler` from P1-06 / P1-07 are used as in the JS tests.

## Tests

- [ ] Port every synchronous `it` of the three suites above.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` "Tests not ported" updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`function identifiers` (P1-14), `EscapeSequenceParser - async` (not ported).

## Suggested commit message

`test(core): port the parser example and handler tests`
