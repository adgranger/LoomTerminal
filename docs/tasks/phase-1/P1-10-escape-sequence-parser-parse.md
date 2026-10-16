# P1-10 — `EscapeSequenceParser` (2/2): the parse loop

- **Status:** todo
- **Depends on:** P1-09
- **Size:** M

## Goal

Port `parse`: the table-driven loop with its fast paths for print runs, parameter digits and OSC/DCS payloads,
dispatching to the handlers registered in P1-09. Synchronous only.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/parser/EscapeSequenceParser.ts` | 519–792 | `parse` |
| | 464–518 | `_preserveStack` — **read only** to understand which branches to drop |

## Files to create or change

- `src/LoomTerminal.Core/Parser/EscapeSequenceParser.Parse.cs` — create (`partial`)

## Design decisions (already made — do not change)

- Signature: `public void Parse(uint[] data, int length)`. No `promiseResult`, no return value (ADR 0002).
- Port the loop **literally**: same `switch` on `transition >> TableAccess.TRANSITION_ACTION_SHIFT`, same fast-path
  inner loops (`PRINT` run until a non-printable, `PARAM` digits, `OSC_PUT` / `DCS_PUT` runs), same `_collect`
  bit packing, same handling of `ParserAction.ERROR` through the error handler and `Abort`.
- Where xterm.js checks a handler result for `Promise` (`handlerResult instanceof Promise` → `_preserveStack` and
  `return`), keep only the synchronous branch: `if (handler(...)) break;` semantics stay the same.
- Every `this._parseStack.state` check at the beginning of `parse` is dropped (no resumption).
- Do not optimize (no `unsafe`, no `Span` tricks beyond what is needed to compile). Phase 2 measures first.

## Tests

- [ ] Covered by P1-11 … P1-14. In this card, add one smoke test: a parser with a print handler collecting text and
      a CSI handler for `m`; `Parse("\x1b[1;31mHello")` (decoded with `StringToUtf32`) calls the CSI handler once
      with params `[1, 31]` and prints `Hello`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `parser/EscapeSequenceParser.ts` (✅ P1-10) and "Behavior differences":
      "parse is synchronous; no handler can pause the parser"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Porting the parser test suite (P1-11 … P1-14).

## Suggested commit message

`feat(core): port the escape sequence parse loop`
