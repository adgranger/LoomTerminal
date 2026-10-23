# LoomTerminal.Avalonia

The terminal control for Avalonia (`LoomTerminal.Avalonia.Controls.TerminalView`), drawn with Skia. Still a
placeholder (phase 6 in progress); the WinUI control (`LoomTerminal.WinUI`) is its specification.

## Differences from the WinUI control

- The control is named `TerminalView` (Avalonia convention).
- `Theme` is named `TerminalTheme`: `StyledElement.Theme` (a `ControlTheme`) already exists in Avalonia.
- `TerminalView` derives from `TemplatedControl` to get `FontFamily`, `FontSize`, `FontWeight` and `Padding`; frames are
  scheduled with `TopLevel.RequestAnimationFrame`, the blink with a `DispatcherTimer`.
- `LoomTerminal.Presentation` exposes its internals to `LoomTerminal.Avalonia` (as it does for WinUI): the control reads
  `TerminalSession.CursorBlink`, `CustomGlyphs` and `RefreshFrame`.
- Keyboard: Avalonia `Key` values are not virtual keys, so `AvaloniaKeyMap` holds a full table; AltGr is Ctrl+Alt with the
  right Alt tracked from key down/up. Text arrives through `TextInput` (whole strings, no surrogate buffering). IME goes
  through `TextInputMethodClient` on every platform Avalonia supports (WinUI: Windows 11 only).
