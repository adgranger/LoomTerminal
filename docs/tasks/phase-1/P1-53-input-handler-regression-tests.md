# P1-53 — `InputHandler` regression tests and chunked parsing

- **Status:** todo
- **Depends on:** P1-37 … P1-52
- **Size:** M (tests only)

## Goal

Port the remaining InputHandler tests (the `regression tests` suite and big-chunk parsing), check that every handler
of the JS constructor is registered, and close `InputHandler.ts`.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/InputHandler.test.ts` | 251–597 | `regression tests` (`insertChars`, `deleteChars`, `eraseInLine`, `eraseInLine reflow`, `ED2 with scrollOnEraseInDisplay turned on`, `eraseInDisplay`) |
| | 1441–1458 | `should parse big chunks in smaller subchunks` |
| `src/common/InputHandler.ts` | 248–386 | constructor registrations (checklist) |

## Files to create or change

- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerRegressionTests.cs` — create
- `tests/LoomTerminal.Core.Tests/InputHandling/InputHandlerRegistrationTests.cs` — create
- `docs/PORTING.md` — `InputHandler.ts` ✅; list `InputHandler - async handlers` (2353–2408) as "not ported"

## Design decisions (already made — do not change)

- `InputHandlerRegistrationTests`: one `[Theory]` over every CSI/ESC identifier and execute code registered in the
  JS constructor (copy the list from lines 248–386) that checks the parser has a handler for it (use the
  `IdentToString` of P1-09 and an internal `HasCsiHandler(int ident)` / `HasEscHandler` / `HasExecuteHandler` helper
  added to `EscapeSequenceParser` for this purpose), so nothing was forgotten across cards.

## Tests

- [ ] Port `regression tests` and `should parse big chunks in smaller subchunks`.
- [ ] New: the registration checklist above.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`test(core): port the InputHandler regression tests and check every handler is registered`
