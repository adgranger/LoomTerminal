# P3-03 — Port `evaluateKeyboardEvent` (`Keyboard.ts`)

- **Status:** done
- **Depends on:** P3-02
- **Size:** M

## Goal

Port the function that turns a key event into the bytes a terminal sends (cursor keys in normal/application mode,
function keys with modifiers, Alt as ESC prefix, Ctrl combinations, keypad), with its full test suite.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/input/Keyboard.ts` | 1–373 | `KEYCODE_KEY_MAPPINGS`, `evaluateKeyboardEvent` |
| `src/common/input/Keyboard.test.ts` | 1–357 | all (`evaluateKeyEscapeSequence`, non-macOS, macOS, `macOptionIsMeta`) |

## Files to create or change

- `src/LoomTerminal.Presentation/Input/Keyboard.cs` — create (`internal static class Keyboard` with
  `KeyboardResult EvaluateKeyboardEvent(TerminalKeyEvent ev, bool applicationCursorMode, bool isMac, bool macOptionIsMeta)`)
- `tests/LoomTerminal.Presentation.Tests/Input/KeyboardTests.cs` — create

## Design decisions (already made — do not change)

- Literal port (ADR 0004): same `switch (ev.keyCode)`, same strings (`C0.ESC + '[A'` → `"\u001b[A"`). Use the C0
  constants of `LoomTerminal.Core.Data.EscapeSequences` (P1-15).
- Keep the `isMac` / `macOptionIsMeta` branches and their tests: Avalonia on macOS needs them later.
- The JS test helper `testEvaluateKeyboardEvent(partialEvent, partialOptions)` becomes a private helper with
  optional named parameters.

## Tests

- [x] Port every test of `Keyboard.test.ts`.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Deciding when the result is sent (P3-04).

## Suggested commit message

`feat(presentation): port the keyboard evaluation of xterm.js`
