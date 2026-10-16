# P3-02 — Key event model (`TerminalKeyEvent`)

- **Status:** todo
- **Depends on:** P3-01
- **Size:** S

## Goal

Define the backend-neutral key event of ADR 0004 and the result type of key evaluation.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `src/common/Types.ts` | 49–60 | `IKeyboardEvent` |
| | 87–99 | `KeyboardResultType`, `IKeyboardResult` |

## Files to create or change

- `src/LoomTerminal.Presentation/Input/TerminalKeyEvent.cs` — create (public)
- `src/LoomTerminal.Presentation/Input/KeyboardResult.cs` — create (internal)
- `src/LoomTerminal.Presentation/Input/DomKeyCodes.cs` — create (public static class of `const int` DOM keyCodes used by
  `Keyboard.ts`: `Backspace = 8`, `Tab = 9`, `Enter = 13`, `Shift = 16`, … `ArrowLeft = 37` … `F1 = 112` … `F12 = 123`,
  plus the punctuation codes `Keyboard.ts` uses — read them from `KEYCODE_KEY_MAPPINGS` and the `switch`)
- `tests/LoomTerminal.Presentation.Tests/Input/TerminalKeyEventTests.cs` — create

## Design decisions (already made — do not change)

- `public sealed record TerminalKeyEvent(int KeyCode, string Key, bool Ctrl, bool Alt, bool Shift, bool Meta,
  TerminalKeyEventType Type)` with `enum TerminalKeyEventType { KeyDown, KeyPress }` and an extra
  `bool AltGraph { get; init; }` (AltGr on Windows arrives as Ctrl+Alt; backends set `AltGraph = true` then).
  `code` (physical key) is not ported: VIGO's rule is "always `keyCode`, never `code`".
- `KeyboardResult` → `internal sealed class` with `Type` (`KeyboardResultType`), `Cancel`, `Key` (`string?`, the bytes
  to send as a string).

## Tests

- [ ] New: record equality and `with` work; `DomKeyCodes` values spot-checked against the DOM spec.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Public API files updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Evaluation (P3-03).

## Suggested commit message

`feat(presentation): add the terminal key event model`
