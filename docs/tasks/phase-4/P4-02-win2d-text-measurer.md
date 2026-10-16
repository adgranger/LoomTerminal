# P4-02 — Win2D text measurer (cell metrics)

- **Status:** todo
- **Depends on:** P4-01
- **Size:** S

## Goal

Implement `ITextMeasurer` (P3-20) with Win2D/DirectWrite: resolve the font family list, measure the cell, snap it to
device pixels.

## Sources

| File | What to take |
|---|---|
| `D:\LoomEditor\src\LoomEditor.Rendering\Win2D\Win2DTextMeasurer.cs` | `ResolveInstalledFamily` (83–88), width reading (91–101), glyph top offset (47–48) |
| `D:\LoomEditor\src\LoomEditor.View\Layout\FontMeasurements.cs` | which characters to measure for a monospace cell |
| xterm.js `src/browser/services/CharSizeService.ts` (pinned commit) | how xterm.js measures (`W` repeated, average) |

## Files to create or change

- `src/LoomTerminal.WinUI/Rendering/Win2DTextMeasurer.cs` — create (internal)
- `tests/LoomTerminal.WinUI.Tests/` — **do not create** (no UI tests); manual check in the gallery

## Design decisions (already made — do not change)

- Family resolution: first family of `TerminalFont.Families` installed (`CanvasTextFormat.GetSystemFontFamilies()`),
  else `"Cascadia Mono"`, else `"Consolas"`. The other families of the list are **not** used for fallback here:
  DirectWrite system fallback handles missing glyphs; Nerd Font glyphs (oh-my-posh) need the user's Nerd Font first in
  the list, as in VIGO.
- Cell width = advance of `"W"` × 50 / 50 (xterm.js measures a long string to average rounding), plus `LetterSpacing`.
  Cell height = `ceil(font line height × LineHeight)`. Both snapped: `Math.Round(value × scale) / scale`.
- Baseline, underline and strikethrough positions from `CanvasTextLayout.LineMetrics` / font metrics
  (`CanvasFontFace` underline position and thickness).
- Cache per `(TerminalFont, scale)`; invalidated on DPI change (P4-06).

## Tests

- [ ] Manual: log the metrics in the gallery for Cascadia Mono 12 at 100 % and 150 %; widths are whole device pixels.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing (P4-03).

## Suggested commit message

`feat(winui): measure terminal cells with DirectWrite`
