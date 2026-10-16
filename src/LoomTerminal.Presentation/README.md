# LoomTerminal.Presentation

The part of the control that every UI backend shares. A backend (WinUI 3, Avalonia) only draws what the render model
describes and translates its own input events into the types defined here.

Planned content (phase 3, see `docs/ROADMAP.md`):

- `Rendering/`: `RenderModel` (rows marked dirty, runs of cells with the same attributes, cursor, selection,
  link and search highlights), `IRenderSurface`, `ITextMeasurer` (cell size, baseline, glyph width, font fallback:
  implemented by each backend, because DirectWrite and Skia measure differently).
- `Input/`: a key and modifier model of our own (never `VirtualKey` or Avalonia's `Key`) and the encoders to VT
  sequences: cursor keys modes, function keys, Alt as Escape, bracketed paste, mouse protocols (X10, SGR).
- `Selection/`, `Search/`, `Viewport/`: selection by cell/word/line, search in the scrollback, scrolling.
- `IClipboard`, `IHostServices`: what the control asks from the host (clipboard, open a link, ring the bell).
