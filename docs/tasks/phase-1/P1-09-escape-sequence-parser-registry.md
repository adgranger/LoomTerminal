# P1-09 — `EscapeSequenceParser` (1/2): fields, identifiers, handler registry

- **Status:** todo
- **Depends on:** P1-06, P1-07, P1-08
- **Size:** M

## Goal

Port the parser class without its parse loop: state fields, function identifiers (`_identifier`, `identToString`),
handler registration for print / execute / ESC / CSI / OSC / DCS with fallbacks, error handler, `reset`.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/parser/EscapeSequenceParser.ts` | 230–298 | fields and constructor |
| | 299–344 | `_identifier`, `identToString` |
| | 345–443 | `setPrintHandler` … `clearErrorHandler` (all registration methods) |
| | 444–463 | `reset` |
| `src/common/parser/Types.ts` | 147–206 | `IEscapeSequenceParser` (reference: port it as an interface, InputHandler tests may mock it later) |

## Files to create or change

- `src/LoomTerminal.Core/Parser/IEscapeSequenceParser.cs` — create
- `src/LoomTerminal.Core/Parser/EscapeSequenceParser.cs` — create (registry part; the parse loop goes to
  `EscapeSequenceParser.Parse.cs` in P1-10 — leave `Parse` throwing `NotImplementedException` until then)

## Design decisions (already made — do not change)

- `internal partial class EscapeSequenceParser : Disposable, IEscapeSequenceParser` (not sealed: tests subclass it
  as xterm.js does with `TestEscapeSequenceParser`).
- Handler collections: `Dictionary<int, List<CsiHandler>>` and `Dictionary<int, List<EscHandler>>`; execute
  handlers: `Dictionary<int, ExecuteHandler>`. Registration returns an `IDisposable` removing that handler only.
- `_identifier(FunctionIdentifier id, int finalStart = 0x40, int finalEnd = 0x7e)` keeps the JS validation and
  error messages (`throw new ArgumentException(...)` with the same text) and the same bit packing.
- `setExecuteHandler(string flag, ...)` takes a one-character string as in xterm.js (`"\x07"`), uses its char code.
- Keep the `PrecedingJoinState` field (used by `print` for Unicode joining, P1-37). Do not port `_parseStack`
  (ADR 0002).
- Default handlers and fallbacks are no-ops, like xterm.js; the default error handler returns the state unchanged.

## Tests

- [ ] Covered by P1-11…P1-14 (the parser tests need the parse loop). In this card, add only: registering and
      disposing a CSI handler leaves no entry; `IdentToString` of `_identifier(new(Prefix: "?", Intermediates: "$", Final: "p"))` is `"?$p"`;
      invalid identifiers throw (`Final` of two characters, prefix outside `0x3c–0x3f`, intermediates outside `0x20–0x2f`).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`parse` (P1-10), `_preserveStack` (not ported).

## Suggested commit message

`feat(core): port the escape sequence parser registry`
