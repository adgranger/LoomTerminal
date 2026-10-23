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
- Pointer: click count comes from `PointerPressedEventArgs.ClickCount`; the cursor is the standard Arrow, Hand over a link
  with Ctrl down. The scrollbar is a `ScrollBar` in the default template (a `FuncControlTemplate` set in code, no XAML
  loading) and uses the user-only `Scroll` event instead of a sync flag. `AvaloniaClipboard` hops to the UI thread.
- Accessibility: Avalonia 12 has no UI Automation text pattern, so `TerminalViewAutomationPeer` (a `Document`, polite live
  region) cannot expose the visible lines: screen readers get the name and the announcements of new output only (the
  announcement is the name of the live region). Avalonia gives no "a client listens" flag: text capture is switched on
  the first time a platform client asks for the peer.
- No `LinkActivated` or `KeyPreview` events yet: a host opens links with `TerminalSession.LinkHandler` and claims keys
  with a tunnelling `KeyDown` handler (the Avalonia gallery does both).
