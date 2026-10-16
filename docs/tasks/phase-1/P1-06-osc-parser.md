# P1-06 — `OscParser` and `OscHandler`

- **Status:** todo
- **Depends on:** P1-05, P1-03
- **Size:** S

## Goal

Port the OSC sub-parser (identifier parsing, handler dispatch with fallthrough, payload limit) and the string-based
`OscHandler` helper.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/parser/OscParser.ts` | 1–238 | `OscParser` (`registerHandler`, `clearHandler`, `setHandlerFallback`, `dispose`, `reset`, `_start`, `_put`, `start`, `put`, `end`), `OscHandler` |
| `src/common/parser/OscParser.test.ts` | 1–279 | `OscParser` suite (identifier parsing, handler registration, OscHandlerFactory) |

## Files to create or change

- `src/LoomTerminal.Core/Parser/OscParser.cs` — create
- `src/LoomTerminal.Core/Parser/OscHandler.cs` — create
- `tests/LoomTerminal.Core.Tests/Parser/OscParserTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class OscParser : IDisposable`. `RegisterHandler(int ident, IOscHandler handler)` returns an
  `IDisposable` that removes that handler (P1-01 `Disposables.Create`). Handlers registered later run first; a
  handler returning `true` stops the chain (same as xterm.js).
- `End(bool success)` returns `void` (no `promiseResult`, ADR 0002). Drop the `_stack` field and every branch that
  handles a `Promise` result; keep the synchronous path exactly.
- `OscHandler(Func<string, bool> handler)`: accumulates the payload with `TextDecoder.Utf32ToString` (P1-03), stops
  accumulating past `ParserLimits.PayloadLimit` and then calls the handler with `false` semantics as in xterm.js
  (`_hitLimit`).
- The fallback handler is `OscFallbackHandler` (P1-05), default no-op.

## Tests

- [ ] Port `OscParser` › `identifier parsing`, `handler registration`, `OscHandlerFactory` (lines 35–279).
      The test helper at the top of the file (lines 1–34: a `TestHandler` recording calls, `toUtf32`) goes to
      `tests/LoomTerminal.Core.Tests/Parser/ParserTestHelpers.cs` (shared with P1-07 and P1-11).
- [ ] Not ported: `OscParser - async tests` (lines 280–453), see ADR 0002. List them in `docs/PORTING.md`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (✅ P1-06) and "Behavior differences": "OSC handlers are synchronous"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Concrete OSC handlers (titles, colors, hyperlinks): P1-51.

## Suggested commit message

`feat(core): port the OSC sub-parser`
