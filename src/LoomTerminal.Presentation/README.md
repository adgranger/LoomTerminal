# LoomTerminal.Presentation

UI-independent presentation layer of LoomTerminal: render model, input encoding, selection.

## Install

```powershell
dotnet add package LoomTerminal.Presentation --prerelease
```

`--prerelease` is needed while the version has a suffix (for example `0.1.0-alpha.1`).

Most applications want `LoomTerminal.WinUI`, which brings the other packages.

The part of the control that every UI backend shares. A backend (WinUI 3, Avalonia) only (1) forwards its input events
as the types defined here, (2) implements the backend interfaces and (3) calls `FrameRenderer.Render` once per frame.

## Usage

```csharp
using var session = TerminalSession.Start(new PtyStartOptions("pwsh.exe") { Columns = 80, Rows = 24 });
CellMetrics metrics = measurer.Measure(font, rasterizationScale);   // ITextMeasurer, implemented by the backend

session.FrameAvailable += () => RequestRender();                     // parser thread: marshal to the UI thread
// UI thread, in the per-frame callback:
if (session.TryTakeFrame() is { } frame)
{
    FrameRenderer.Render(frame, metrics, surface);                   // IRenderSurface, implemented by the backend
}

session.SendText("echo hi\r");
```

## Contents of the package

| Folder | Content |
|---|---|
| `Session/` | `TerminalSession` (parser thread, pty, frames, selection, search, clipboard, accessibility), `TerminalSessionOptions` |
| `Input/` | key and pointer event models, VT key encoding, mouse reports, paste, `IClipboard` |
| `Theming/` | `TerminalTheme` and the color resolution (palette, OSC overrides, minimum contrast) |
| `Rendering/` | `RenderFrame` model, `ITextMeasurer`, `IRenderSurface`, `FrameRenderer`, cursor blink |
| `Selection/`, `Search/`, `Viewport/`, `Links/` | selection logic, search engine, scrolling and wheel, link detection (`ILinkHandler`) |

Threading: the parser thread owns the emulator; the UI thread only takes immutable `RenderFrame` snapshots and calls
the thread-safe methods of `TerminalSession`. Events are raised on the parser thread.
