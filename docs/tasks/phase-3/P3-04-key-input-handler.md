# P3-04 — `KeyInputHandler`: from key event to pty bytes, with host shortcuts

- **Status:** todo
- **Depends on:** P3-03
- **Size:** M

## Goal

Decide, for each key event from a backend, whether the host claims it (shortcut), whether the terminal sends a
sequence, or whether the key is left to the backend's text input (printable characters, IME). Reproduce VIGO's
`terminal.js` rules (AltGr, copy/paste keys).

## Sources

| File | Lines | What to take |
|---|---|---|
| xterm.js `src/browser/CoreBrowserTerminal.ts` | search `_keyDown`, `_keyPress`, `_isThirdLevelShift`, `_isComposing` | the order of checks (custom key handler → evaluate → cancel / send) — **logic only** |
| VIGO `tools/editor/src/terminal.js` | 89–120 | AltGr rule, app shortcut relay, copy/paste keys, right click (read it at `D:\VIGO\tools\editor\src\terminal.js`) |

## Files to create or change

- `src/LoomTerminal.Presentation/Input/KeyPreviewEventArgs.cs` — create (public: `TerminalKeyEvent Key`, `bool Handled`)
- `src/LoomTerminal.Presentation/Input/KeyInputHandler.cs` — create (internal)
- `src/LoomTerminal.Presentation/Input/KeyInputResult.cs` — create (internal: `enum KeyInputOutcome { HandledByHost, Sent, Copy, Paste, LeaveToTextInput, Ignored }` + `string? Data`)
- `tests/LoomTerminal.Presentation.Tests/Input/KeyInputHandlerTests.cs` — create

## Design decisions (already made — do not change)

- `KeyInputResult Handle(TerminalKeyEvent ev, KeyContext ctx)` where `KeyContext` is a record of what the decision
  needs: `ApplicationCursorKeys`, `HasSelection`, `IsMac` (false on Windows), `MacOptionIsMeta`.
- Order: (1) raise `KeyPreview` (a `Func<TerminalKeyEvent, bool>` callback set by the session; `true` = handled by
  host) — **except** when `AltGraph` is true (AltGr never reaches host shortcuts); (2) copy: Ctrl+Shift+C, or Ctrl+C
  when `HasSelection` → `Copy`; (3) paste: Ctrl+V, Ctrl+Shift+V, Shift+Insert → `Paste`; (4) `AltGraph` with a
  printable `Key` → `LeaveToTextInput`; (5) `Keyboard.EvaluateKeyboardEvent` → `Sent` with its `Key`, or
  `LeaveToTextInput` when the result has no key and the event is a printable character (single-char `Key`, no Ctrl/Alt).
- Ctrl+C without selection is **sent** (`\x03`), never copied.

## Tests

- [ ] New: each rule above, one test per rule, plus: Ctrl+Alt+Q with `AltGraph = true` → `LeaveToTextInput` (`@` on
      AZERTY); `ArrowUp` with `ApplicationCursorKeys` → `"\u001bOA"`; host preview returning `true` for Ctrl+Shift+T →
      `HandledByHost`; Ctrl+Shift+C without selection → still `Copy` (VIGO behavior: copies nothing).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Clipboard access (P3-16), text input from the backend (P3-05).

## Suggested commit message

`feat(presentation): decide what each key does (host shortcuts, copy, paste, VT)`
