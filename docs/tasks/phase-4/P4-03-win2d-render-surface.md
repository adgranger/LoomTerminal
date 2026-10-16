# P4-03 — Win2D render surface: backgrounds and text runs

- **Status:** todo
- **Depends on:** P4-02
- **Size:** M

## Goal

Implement `IRenderSurface` on a `CanvasDrawingSession` for rectangles and text runs, with a layout cache, so the
control can draw real frames.

## Sources

| File | What to take |
|---|---|
| `D:\LoomEditor\src\LoomEditor.Rendering\Win2D\Win2DRenderSurface.cs` | conversions, `DrawTextLayout` usage |
| `D:\LoomEditor\src\LoomEditor.Rendering\LineLayoutCache.cs` | LRU cache shape |

## Files to create or change

- `src/LoomTerminal.WinUI/Rendering/Win2DRenderSurface.cs` — create (internal)
- `src/LoomTerminal.WinUI/Rendering/TextRunLayoutCache.cs` — create (internal LRU)
- `src/LoomTerminal.WinUI/Controls/TerminalControl.cs` — `Draw`: take the frame (`TryTakeFrame` or the last one),
  `FrameRenderer.Render(frame, metrics, surface)`

## Design decisions (already made — do not change)

- **Grid alignment first.** A run whose characters are all ASCII printable (0x20–0x7E) is drawn as one
  `CanvasTextLayout` with `CanvasTextFormat` (`WordWrapping = NoWrap`, `Options = EnableColorFont`) at
  `x = column × cellWidth`; any other run is drawn **one grapheme per cell position** (`x = column × cellWidth` for
  each cell group), so CJK, emoji and fallback glyphs never drift off the grid.
- Bold uses `FontWeight` from `TerminalFont.BoldWeight`; italic uses `FontStyle.Italic`.
- Cache key: `(text, bold, italic, font, scale)` → `CanvasTextLayout`; capacity 4 × rows × average runs, LRU; cleared
  on font/DPI/device change. Colors are not part of the key (set per draw).
- Text is vertically placed with the baseline from `CellMetrics` (same as LoomEditor's glyph top offset).

## Tests

- [ ] Manual in the gallery: `dir`, colored `git log --graph`, `echo 日本語 😀` stay on the grid; resizing the window
      keeps the text crisp at 100 % and 150 %.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Decorations and cursor (P4-05), custom glyphs (P4-04), resize logic (P4-06).

## Suggested commit message

`feat(winui): draw backgrounds and text runs with Win2D`
