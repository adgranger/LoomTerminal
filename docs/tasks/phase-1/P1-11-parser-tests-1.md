# P1-11 — Parser tests (1/4): test terminal, init, transitions GROUND … CSI

- **Status:** done
- **Depends on:** P1-10
- **Size:** M (tests only)

## Goal

Port the parser test harness and the first half of `state transitions and actions`. Fix the port if tests fail.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/parser/EscapeSequenceParser.test.ts` | 1–208 | helpers: `TestEscapeSequenceParser`, `testTerminal` (records `print`, `exe`, `csi`, `esc`, `osc`, `dcs` calls), `states`, `parse` helper, `r` |
| | 209–240 | `Parser init and methods` |
| | 241–622 | `state transitions and actions`, from `state GROUND execute action` (242) to `trans CSI_INTERMEDIATE --> GROUND with csi_dispatch action` (610) |

## Files to create or change

- `tests/LoomTerminal.Core.Tests/Parser/EscapeSequenceParserTestTerminal.cs` — create (the harness)
- `tests/LoomTerminal.Core.Tests/Parser/EscapeSequenceParserTests.Init.cs` — create
- `tests/LoomTerminal.Core.Tests/Parser/EscapeSequenceParserTests.Transitions1.cs` — create
  (one `partial class EscapeSequenceParserTests` across the files of P1-11 … P1-14)

## Design decisions (already made — do not change)

- The JS `testTerminal` records calls as arrays like `['csi', '?', [1, 2], 'h']`. Port the records as
  `List<object[]>` and compare with `Assert.Equal` — same shape as the JS arrays, so expectations can be copied.
- Tests that loop over many code points stay loops inside one `[Fact]`.
- A failing test means the port is wrong (P1-08 … P1-10): fix the port, never the expectation.

## Tests

- [x] Port `Parser init and methods` (all `it`s).
- [x] Port `state transitions and actions` lines 242–622 (GROUND, ANYWHERE, ESCAPE, ESCAPE_INTERMEDIATE,
      CSI_ENTRY, CSI_PARAM, CSI_INTERMEDIATE), one C# test per `it`.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Lines 623 and after (P1-12 … P1-14).

## Suggested commit message

`test(core): port the parser state transition tests (1/2)`
