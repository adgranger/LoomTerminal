# LoomTerminal.Avalonia

The terminal control for Avalonia (`LoomTerminal.Avalonia.Controls.TerminalView`), drawn with Skia, on Windows
(ConPTY) for now. The WinUI control (`LoomTerminal.WinUI`) is its specification. Targets plain `net11.0`, references
`LoomTerminal.Presentation` only, and is Native AOT compatible (compiled bindings, no XAML loading by reflection).

## Usage

```csharp
// xmlns:loom="using:LoomTerminal.Avalonia.Controls"  ->  <loom:TerminalView x:Name="Terminal" />
Terminal.Session = TerminalSession.Start(
    new PtyStartOptions(Shells.Detect()[0].CommandLine), new TerminalSessionOptions { Scrollback = 10_000 });
```

`samples/LoomTerminal.AvaloniaGallery` is the full example (tabs, shell menu, settings, demo scripts). See
`PublicAPI.Unshipped.txt` for the public surface.

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

## Platform notes

- **Windows** is the reference platform (ConPTY). **Linux and macOS** use the Unix pty of `LoomTerminal.Pty`
  (`libloomterm_pty`, ADR 0007): the library must sit next to the application, which a published app or a project
  reference with the shim built into `artifacts/native/<rid>` does.
- **Default font** when the host sets none: Windows `Cascadia Mono, Consolas`; macOS `Menlo, SF Mono`; Linux
  `DejaVu Sans Mono, Liberation Mono, monospace`.
- **macOS keyboard:** Cmd+C copies the selection (with no selection nothing happens: Ctrl+C is the interrupt), Cmd+V
  pastes, Cmd+K clears the scrollback. Ctrl+C and Ctrl+V are plain control characters there. Option is not Meta by
  default, so Option+letter types accented characters; set `TerminalSessionOptions.MacOptionIsMeta` to make it Meta.
- **Linux keyboard:** the Windows rules: Ctrl+Shift+C copies (Ctrl+C when text is selected), Ctrl+V and Ctrl+Shift+V paste.
- **Linux input methods** go through Avalonia's IBus and Fcitx support; they have not been checked by hand yet (see
  the manual tests).

