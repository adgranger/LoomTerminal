# P1-07 — `DcsParser` and `DcsHandler`

- **Status:** todo
- **Depends on:** P1-06
- **Size:** S

## Goal

Port the DCS sub-parser (hook / put / unhook with handler fallthrough) and the string-based `DcsHandler` helper.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/parser/DcsParser.ts` | 1–192 | `DcsParser` (`dispose`, `registerHandler`, `clearHandler`, `setHandlerFallback`, `reset`, `hook`, `put`, `unhook`), `EMPTY_PARAMS`, `DcsHandler` |
| `src/common/parser/DcsParser.test.ts` | 1–283 | `DcsParser` suite (handler registration, DcsHandlerFactory) |

## Files to create or change

- `src/LoomTerminal.Core/Parser/DcsParser.cs` — create
- `src/LoomTerminal.Core/Parser/DcsHandler.cs` — create
- `tests/LoomTerminal.Core.Tests/Parser/DcsParserTests.cs` — create
- `tests/LoomTerminal.Core.Tests/Parser/ParserTestHelpers.cs` — extend if the DCS test helpers (lines 1–72) need it

## Design decisions (already made — do not change)

- Same patterns as P1-06: `RegisterHandler` returns an `IDisposable`; `Unhook(bool success)` returns `void`; drop
  the `_stack` and `Promise` branches (ADR 0002).
- `DcsHandler(Func<string, IParams, bool> handler)`: clones the params on `Hook` (`params.clone()`), accumulates the
  payload up to `ParserLimits.PayloadLimit`, as in xterm.js. `EMPTY_PARAMS` is a `static readonly Params`.

## Tests

- [ ] Port `DcsParser` › `handler registration`, `DcsHandlerFactory` (lines 73–283).
- [ ] Not ported: `DcsParser - async tests` (lines 284–459). List them in `docs/PORTING.md`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (✅ P1-07)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Concrete DCS handlers (DECRQSS: P1-52).

## Suggested commit message

`feat(core): port the DCS sub-parser`
