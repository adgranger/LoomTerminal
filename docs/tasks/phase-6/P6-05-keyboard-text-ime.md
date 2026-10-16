# P6-05 — Avalonia keyboard, text input and IME

- **Status:** todo
- **Depends on:** P6-04
- **Size:** M

## Goal

Same as P4-07, P4-08 and P4-09 for Avalonia.

## Specification

P4-07, P4-08, P4-09.

## Files to create or change

- `src/LoomTerminal.Avalonia/Input/AvaloniaKeyMap.cs` — create (internal: `Avalonia.Input.Key` → DOM keyCode and name)
- `src/LoomTerminal.Avalonia/Input/TerminalTextInputClient.cs` — create (`TextInputMethodClient` subclass)
- `src/LoomTerminal.Avalonia/Controls/TerminalView.Keyboard.cs` — create

## Design decisions (already made — do not change)

- Avalonia `Key` values are **not** Windows virtual keys: the map is a full table (letters, digits, F1–F24, arrows,
  navigation, numpad, OEM keys mapped to their US-layout DOM keyCodes 186–222).
- DOM `key` for printable keys comes from `KeyEventArgs.KeySymbol` (Avalonia 11+); text arrives through `TextInput`
  (equivalent of `CharacterReceived`).
- IME through `TextInputMethodClient` (`SurroundingText` empty, `CursorRectangle` = cursor cell); composition shown
  with the same overlay as P4-09 (`CompositionText` at render time). Works on Windows, macOS and Linux (IBus/Fcitx)
  where Avalonia supports it.
- AltGr on Windows: Avalonia reports `KeyModifiers.Control | Alt`; detect right Alt with `Key.RightAlt` pressed state
  tracked from key down/up events.

## Tests

- [ ] New (`LoomTerminal.Avalonia` has no test project: add a small `tests/LoomTerminal.Avalonia.Tests` for the pure
      `AvaloniaKeyMap` table — every key `Keyboard.ts` handles has an entry).
- [ ] Manual: the P4-07 / P4-08 / P4-09 checks in the Avalonia gallery.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(avalonia): keyboard, text input and IME`
