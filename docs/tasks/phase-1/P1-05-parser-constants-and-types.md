# P1-05 — Parser constants and handler types

- **Status:** done
- **Depends on:** P1-04
- **Size:** S

## Goal

Port the parser states, actions, OSC states, the handler interfaces and delegate types that the OSC/DCS sub-parsers
and the escape sequence parser share.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/parser/Constants.ts` | 1–58 | `ParserState`, `ParserAction`, `OscState`, `PAYLOAD_LIMIT` |
| `src/common/parser/Types.ts` | 50–275 | `IParsingState`, handler delegate types, `IDcsHandler`, `IOscHandler`, `IFunctionIdentifier`, `IHandlerCollection` |

## Files to create or change

- `src/LoomTerminal.Core/Parser/ParserConstants.cs` — create (`ParserState`, `ParserAction`, `OscState` enums,
  `static class ParserLimits { public const int PayloadLimit = 10000000; }`)
- `src/LoomTerminal.Core/Parser/ParserTypes.cs` — create (delegates, `ParsingState`, `FunctionIdentifier`)
- `src/LoomTerminal.Core/Parser/IOscHandler.cs`, `IDcsHandler.cs` — create

## Design decisions (already made — do not change)

- Delegates (all return `bool` or `void`, ADR 0002 — no `Promise`):
  `delegate bool CsiHandler(IParams p)`, `delegate void CsiFallbackHandler(int ident, IParams p)`,
  `delegate bool EscHandler()`, `delegate void EscFallbackHandler(int ident)`,
  `delegate bool ExecuteHandler()`, `delegate void ExecuteFallbackHandler(int ident)`,
  `delegate void PrintHandler(uint[] data, int start, int end)` (an array, not a span, to stay one-to-one with the
  xterm.js call sites).
  `delegate void OscFallbackHandler(int ident, OscFallbackAction action, object? payload)` with
  `enum OscFallbackAction { Start, Put, End }`;
  `delegate void DcsFallbackHandler(int ident, DcsFallbackAction action, object? payload)` with
  `enum DcsFallbackAction { Hook, Put, Unhook }` (payload is `IParams` for Hook, `string` for Put, `bool` for Unhook,
  as in xterm.js).
- `interface IOscHandler { void Start(); void Put(uint[] data, int start, int end); bool End(bool success); }`
- `interface IDcsHandler { void Hook(IParams p); void Put(uint[] data, int start, int end); bool Unhook(bool success); }`
- `IFunctionIdentifier` → `internal readonly record struct FunctionIdentifier(string? Prefix, string? Intermediates, string Final)`
  with init-friendly construction: `new FunctionIdentifier(Final: "m")`, `new(Prefix: "?", Final: "h")`.
- `IParsingState` → `internal sealed class ParsingState` with the same fields (`Position`, `Code`, `CurrentState`,
  `Collect`, `Params`, `Abort`) — mutable, the error handler may modify it.
- `IHandlerCollection<T>` is `Dictionary<int, List<T>>` at the use sites: do not create a type.
- Skip `ParserStackType`, `IParserStackState`, `ISubParserStackState`, `ResumableHandlersType` (async only, ADR 0002).

## Tests

None (types only). The build must stay warning-free.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `parser/Constants.ts`, `parser/Types.ts` (✅ P1-05, note "stack types not ported")
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Any parser logic.

## Suggested commit message

`feat(core): port the parser constants and handler types`
