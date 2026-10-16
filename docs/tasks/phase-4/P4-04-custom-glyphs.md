# P4-04 — Box drawing, block elements and powerline glyphs as vectors

- **Status:** todo
- **Depends on:** P4-03
- **Size:** M

## Goal

Draw box-drawing (U+2500–257F), block elements (U+2580–259F), shade patterns and powerline symbols as vector shapes
filling the cell exactly, as xterm.js does with `customGlyphs: true` (default): fonts never align these perfectly,
and prompts like oh-my-posh depend on them.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `addons/addon-webgl/src/CustomGlyphs.ts` | 15–125 | `blockElementDefinitions` |
| | 126–191 | `patternCharacterDefinitions`, `Shapes`, `Style` |
| | 192–356 | `boxDrawingDefinitions`, `VectorType` |
| | 357–401 | `powerlineDefinitions` |
| | 402–693 | `tryDrawCustomChar`, `drawBlockElementChar`, `drawPatternChar`, `drawBoxDrawingChar`, `drawPowerlineChar`, `clamp`, `svgToCanvasInstructionMap`, `translateArgs` |

## Files to create or change

- `src/LoomTerminal.Presentation/Rendering/CustomGlyphs.cs` — create (internal: the **definitions** and a
  backend-neutral path description: list of commands `MoveTo/LineTo/CurveTo/Rect/Fill/Stroke` in cell-relative units)
- `src/LoomTerminal.Presentation/Rendering/IRenderSurface.cs` — add `void DrawCustomGlyph(CustomGlyphPath path, double x, double y, in CellMetrics metrics, uint rgba)`
- `src/LoomTerminal.Presentation/Rendering/FrameRenderer.cs` — runs containing custom glyph characters draw those cells
  through `DrawCustomGlyph`
- `src/LoomTerminal.WinUI/Rendering/Win2DRenderSurface.cs` — implement with `CanvasPathBuilder` / `CanvasGeometry`
- `tests/LoomTerminal.Presentation.Tests/Rendering/CustomGlyphsTests.cs` — create

## Design decisions (already made — do not change)

- Definitions and SVG-like path parsing live in **Presentation** (shared with Avalonia); only the drawing of the path
  is backend code. The JS draws on a canvas 2D context: translate each canvas call to one path command.
- Shade patterns (`░▒▓`) are drawn as filled rectangles at reduced alpha (25 %, 50 %, 75 %) instead of canvas
  patterns: document it in `docs/PORTING.md` ("Behavior differences").
- A `CustomGlyphs` option (`bool`, default `true`) on `TerminalSessionOptions` / the control disables it.

## Tests

- [ ] New (Presentation): `─` produces one horizontal line command at half height; `█` a full-cell rect; `` (U+E0B0)
      a filled triangle; characters without a definition return `null`.
- [ ] Manual: an oh-my-posh or starship prompt renders with seamless separators.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Public API files updated; `docs/PORTING.md` map updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Ligatures.

## Suggested commit message

`feat(rendering): draw box-drawing, block and powerline glyphs as vectors`
