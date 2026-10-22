# P3-21 — `FrameRenderer`: from a render frame to draw calls

- **Status:** done
- **Depends on:** P3-20, P3-14, P3-11
- **Size:** M

## Goal

The shared drawing algorithm: given a `RenderFrame`, `CellMetrics` and an `IRenderSurface`, issue the draw calls in the
right order (background, selection, text, decorations, cursor). Backends only implement the surface.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to take |
|---|---|---|
| `addons/addon-webgl/src/WebglRenderer.ts` | whole file | draw order, cursor drawing per style, underline variants — ideas only |
| `addons/addon-webgl/src/renderLayer/BaseRenderLayer.ts` | whole file | cursor shapes (block, bar width = `cursorWidth`, underline, outline) — ideas only |
| `src/browser/renderer/shared/RendererUtils.ts` | 93–95 | `computeNextVariantOffset` (curly/dotted/dashed continuity across runs, P3-08) |

## Files to create or change

- `src/LoomTerminal.Presentation/Rendering/FrameRenderer.cs` — create (public static class with
  `void Render(RenderFrame frame, in CellMetrics metrics, IRenderSurface surface)`)
- `tests/LoomTerminal.Presentation.Tests/Rendering/FrameRendererTests.cs` — create

## Design decisions (already made — do not change)

- **Full redraw** of the viewport every frame (as LoomEditor, ADR 0005 there): simple and correct. Backends make it
  cheap with their layout cache. Revisit only with measurements (P4-17).
- Order per frame: `Clear(theme background)`; per row: background rectangles of runs whose background differs from
  the theme background (merge adjacent runs with the same background); selection rectangles; then per row: text runs
  (skip spaces-only runs), then underline / strikethrough / overline decorations; finally the cursor
  (block: filled rect + text drawn again in `AccentColor`; bar: `CursorWidth` DIPs wide rect; underline: 2-device-pixel
  rect; outline: `DrawRectangle`).
- Selection and search highlight backgrounds are blended over the cell background (alpha from the theme colors).

## Tests

- [x] New (with `RecordingRenderSurface` and `FakeTextMeasurer`): golden text outputs for: plain text; colored
      backgrounds merged; selection over two lines; each cursor shape; curly underline over two runs continuous
      (variant offset); wide char run.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Public API files updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Box-drawing and powerline glyphs drawn as vectors (P4-04).

## Suggested commit message

`feat(presentation): add the shared frame renderer`
