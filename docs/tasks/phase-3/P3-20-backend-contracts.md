# P3-20 — Backend contracts: `ITextMeasurer`, `CellMetrics`, `IRenderSurface`

- **Status:** todo
- **Depends on:** P3-09
- **Size:** S

## Goal

Define the interfaces a UI backend implements to measure the font and draw a frame, plus a recording implementation
for tests.

## Files to create or change

- `src/LoomTerminal.Presentation/Rendering/CellMetrics.cs` — create (public record struct)
- `src/LoomTerminal.Presentation/Rendering/ITextMeasurer.cs` — create (public)
- `src/LoomTerminal.Presentation/Rendering/IRenderSurface.cs` — create (public)
- `src/LoomTerminal.Presentation/Rendering/TerminalFont.cs` — create (public record: `IReadOnlyList<string> Families`,
  `double Size` (DIPs), `double LineHeight` (multiplier, default 1.0), `double LetterSpacing` (DIPs), `int Weight`,
  `int BoldWeight`)
- `tests/LoomTerminal.Presentation.Tests/Rendering/RecordingRenderSurface.cs` — create (test helper: records each call
  as one line of text, e.g. `FillRect 0,0 7.2x16 #1e1e1eff`)
- `tests/LoomTerminal.Presentation.Tests/Rendering/FakeTextMeasurer.cs` — create (fixed 8×16 cells)

## Design decisions (already made — do not change)

- `CellMetrics(double Width, double Height, double Baseline, double UnderlinePosition, double UnderlineThickness,
  double StrikethroughPosition, double StrikethroughThickness)`, all in DIPs, `Width`/`Height` already include
  `LineHeight` and `LetterSpacing` and are **snapped to device pixels** by the backend (it knows the DPI).
- `ITextMeasurer`: `CellMetrics Measure(TerminalFont font, double rasterizationScale)`. The backend owns font fallback
  (DirectWrite / Skia): Presentation never asks for glyph widths; cell widths come from Unicode tables (Core).
- `IRenderSurface` (all coordinates in DIPs, origin top-left of the terminal area):
  `void Clear(uint rgba)`, `void FillRectangle(double x, double y, double width, double height, uint rgba)`,
  `void DrawTextRun(int row, int column, string text, int cellCount, in TextRunStyle style, in CellMetrics metrics)`
  (the backend lays out `text` so that it spans exactly `cellCount` cells),
  `void DrawUnderline(double x, double y, double width, UnderlineStyle style, uint rgba, in CellMetrics metrics)`,
  `void DrawLine(double x1, double y1, double x2, double y2, double thickness, uint rgba)`,
  `void DrawRectangle(double x, double y, double width, double height, double thickness, uint rgba)` (outline cursor).
- No layout cache in the contract: backends cache by `RenderRow.ContentHash` themselves.

## Tests

- [ ] New: `RecordingRenderSurface` formats each call deterministically (the format is used by P3-21's tests).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Public API files updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing logic (P3-21).

## Suggested commit message

`feat(presentation): define the text measurer and render surface contracts`
