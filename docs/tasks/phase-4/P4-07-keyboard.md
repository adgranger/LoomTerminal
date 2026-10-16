# P4-07 — Keyboard: `VirtualKey` → `TerminalKeyEvent`, AltGr, host shortcuts

- **Status:** todo
- **Depends on:** P4-06
- **Size:** M

## Goal

Turn WinUI key events into `TerminalKeyEvent`s (ADR 0004), let the host claim shortcuts through `KeyPreview`, and keep
Tab, arrows and Escape inside the terminal.

## Sources

| File | What to take |
|---|---|
| `D:\LoomEditor\src\LoomEditor.WinUI\Input\KeyboardInput.cs` | `InputKeyboardSource.GetKeyStateForCurrentThread` |
| VIGO `tools/editor/src/terminal.js` | 89–109: AltGr passes through; app shortcut relay |
| VIGO `src/Vigo.App/Controls/TerminalView.cs` | 353–376: which shortcuts VIGO relays |

## Files to create or change

- `src/LoomTerminal.WinUI/Input/VirtualKeyMap.cs` — create (internal: `VirtualKey` → DOM `keyCode` and DOM `key` name)
- `src/LoomTerminal.WinUI/Controls/TerminalControl.Keyboard.cs` — create (partial)

## Design decisions (already made — do not change)

- `KeyCode` = `(int)VirtualKey` for every key: browsers on Windows report the Windows virtual-key code as DOM
  `keyCode` (including the OEM keys 186–222), which is what `Keyboard.ts` expects.
- `Key` (DOM `key`): for non-printable keys, the DOM name from `VirtualKeyMap` (`"ArrowUp"`, `"Enter"`, `"F5"`…); for
  printable keys, the character the key produces in the **current layout**, obtained with `ToUnicodeEx`
  (`[LibraryImport("user32.dll")]`, flag `0x4` = do not change the keyboard state, `GetKeyboardLayout(0)`,
  `GetKeyboardState`) — empty string if it produces nothing (dead key).
- Flow: `KeyDown` (and `PreviewKeyDown` for Tab/arrows so focus navigation never steals them) builds the event and
  calls `Session.SendKey`. Outcome `Sent`, `Copy`, `Paste`, `HandledByHost` → `e.Handled = true`;
  `LeaveToTextInput` → not handled, `CharacterReceived` (P4-08) delivers the text.
- AltGr: `Ctrl` and `Alt` both down **and** `RightMenu` (right Alt) down → `AltGraph = true`.
- `KeyPreview` event of the control is raised from the session's `KeyPreview` callback (synchronously, UI thread).

## Tests

- [ ] Manual: arrows, Home/End, F1–F12 with modifiers work in `vim`/`htop` (WSL); AltGr+é/@/# types characters on an
      AZERTY layout; Ctrl+C interrupts `ping -t`; Tab completes in pwsh (focus stays); a `KeyPreview` handler in the
      gallery for Ctrl+Shift+T opens a new tab.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Character input (P4-08), IME (P4-09).

## Suggested commit message

`feat(winui): map WinUI keys to terminal key events`
