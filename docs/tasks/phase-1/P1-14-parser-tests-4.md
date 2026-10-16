# P1-14 — Parser tests (4/4): function identifiers

- **Status:** done
- **Depends on:** P1-13
- **Size:** S (tests only)

## Goal

Port the function identifier tests (registration limits, identifier invocation) and close the parser.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/parser/EscapeSequenceParser.test.ts` | 1583–1743 | `function identifiers` › `registration limits`, `identifier invocation` |

## Files to create or change

- `tests/LoomTerminal.Core.Tests/Parser/EscapeSequenceParserTests.Identifiers.cs` — create
- `docs/PORTING.md` — list `EscapeSequenceParser - async` (lines 1744–2220) under "Tests not ported"

## Design decisions (already made — do not change)

- Expected exception messages: assert the exception type (`ArgumentException`) and, when the JS test checks the
  message, the same message text.

## Tests

- [x] Port every `it` of `function identifiers`.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` updated: the whole `src/common/parser/` folder ✅
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`test(core): port the parser function identifier tests`
