# Phase 4 conventions — the WinUI 3 control

Phase 4 builds `LoomTerminal.WinUI`: a WinUI 3 control drawing with Win2D, on top of `LoomTerminal.Presentation`.
The control contains **no terminal logic**: it forwards input as Presentation types, implements the backend
interfaces, and calls `FrameRenderer.Render` (P3-21) once per frame.

## Reference code (same author, MIT — copy and adapt, mark with `// Inspired by LoomEditor: <path>`)

| Concern | LoomEditor file (`D:\LoomEditor\src\…`) |
|---|---|
| Font family resolution, cell measurement, glyph centering | `LoomEditor.Rendering/Win2D/Win2DTextMeasurer.cs`, `LoomEditor.View/Layout/FontMeasurements.cs` |
| Draw calls on a `CanvasDrawingSession` | `LoomEditor.Rendering/Win2D/Win2DRenderSurface.cs` |
| DPI (`XamlRoot.RasterizationScale`) and device loss | `LoomEditor.WinUI/Controls/LoomEditor.Font.cs` (93–114), `LoomEditor.cs` (220–240) |
| Frame ticks (`CompositionTarget.Rendering`) | `LoomEditor.WinUI/Controls/AnimationFrameScheduler.cs` |
| Caret timer (`DispatcherQueueTimer`) | `LoomEditor.WinUI/Controls/LoomEditor.Caret.cs` |
| XAML scrollbars | `LoomEditor.WinUI/Controls/LoomEditor.Scrolling.cs` (92–189) |
| Keyboard state (`InputKeyboardSource`) | `LoomEditor.WinUI/Input/KeyboardInput.cs` |
| Automation peer | `LoomEditor.WinUI/Accessibility/LoomEditorAutomationPeer.cs` |
| IME design (`CoreTextEditContext`, Windows 11 only) | `D:\LoomEditor\docs\adr\0006-editing-and-text-input.md`, decision 11 |

And from VIGO (the behavior to reproduce): `D:\VIGO\src\Vigo.App\Controls\TerminalView.cs` (theme, keys, clipboard,
links) and `D:\VIGO\tools\editor\src\terminal.js` / `terminal.css` (padding, scrollbar look).

## Rules

- Native AOT rules of VIGO apply (they are WinUI-generic): `x:Bind` only in XAML (no `{Binding}`), never cast a
  resource read from `Application.Current.Resources`, `ObservableCollection<T>` for `ItemsSource`, no reflection.
- Every event from the session (raised on the parser thread) is marshaled with the control's `DispatcherQueue`
  before touching XAML.
- Win2D resources are released when the control is unloaded **for good** (`RemoveFromVisualTree`), as in the
  skeleton; a control moved between windows re-creates them (device change).
- Each card checks by hand in the gallery (`rtk dotnet build samples/LoomTerminal.Gallery -p:Platform=x64`, run the
  exe) and lists what was checked in the PR description; cards that touch startup also run the AOT publish.
