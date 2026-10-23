# P6-03 — Skia render surface (text, rectangles, decorations, custom glyphs)

- **Status:** done
- **Depends on:** P6-02
- **Size:** M

## Goal

Implement every `IRenderSurface` call with Skia inside an Avalonia custom draw operation.

## Specification

P4-03 (grid alignment, ASCII fast path vs per-grapheme drawing, bold/italic, layout cache), P4-05 (decorations,
cursor shapes, pixel snapping), P4-04 (custom glyph paths → `SKPath`).

## Files to create or change

- `src/LoomTerminal.Avalonia/Rendering/SkiaRenderSurface.cs` — create (internal)
- `src/LoomTerminal.Avalonia/Rendering/TerminalDrawOperation.cs` — create (`ICustomDrawOperation`: holds the frame and
  metrics, leases the `SKCanvas` with `ISkiaSharpApiLeaseFeature`, calls `FrameRenderer.Render`)
- `src/LoomTerminal.Avalonia/Rendering/SkiaTextBlobCache.cs` — create (LRU of `SKTextBlob`, same key as P4-03)

## Design decisions (already made — do not change)

- Text: `SKTextBlob` built with the resolved `SKFont`; for characters the primary typeface lacks
  (`SKFont.ContainsGlyphs` false), resolve a fallback typeface with `SKFontManager.Default.MatchCharacter(cp)` and
  draw that grapheme alone at its cell position. Emoji: color typefaces work through the same path.
- Curly underline with `SKPath.CubicTo`; dotted / dashed with `SKPathEffect.CreateDash`.

## Tests

- [ ] Manual: the gallery demo scripts of P4-16 (`colors.ps1`, `styles.ps1`, `unicode.ps1`) look like in WinUI
      (screenshots side by side in the PR).

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(avalonia): render terminal frames with Skia`
