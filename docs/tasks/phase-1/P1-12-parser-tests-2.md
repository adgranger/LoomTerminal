# P1-12 — Parser tests (2/4): transitions CSI_IGNORE … DCS_PASSTHROUGH

- **Status:** done
- **Depends on:** P1-11
- **Size:** M (tests only)

## Goal

Port the second half of `state transitions and actions`.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/parser/EscapeSequenceParser.test.ts` | 623–1032 | from `trans CSI_ENTRY --> CSI_PARAM for ":" (0x3a)` (623) to `state DCS_PASSTHROUGH ignore` (1012): CSI_IGNORE, SOS_PM_APC_STRING, OSC_STRING, DCS_ENTRY, DCS_PARAM, DCS_IGNORE, DCS_INTERMEDIATE, DCS_PASSTHROUGH |

## Files to create or change

- `tests/LoomTerminal.Core.Tests/Parser/EscapeSequenceParserTests.Transitions2.cs` — create

## Design decisions (already made — do not change)

- Reuse the harness of P1-11. Two JS tests share the name `trans CSI_PARAM --> CSI_IGNORE` (630 and 641): suffix the
  C# names `_1` and `_2`.

## Tests

- [x] Port every `it` between lines 623 and 1032.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Lines 1033 and after.

## Suggested commit message

`test(core): port the parser state transition tests (2/2)`
