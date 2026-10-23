# P6-02 — Skia text measurer

- **Status:** done
- **Depends on:** P6-01
- **Size:** S

## Goal

Implement `ITextMeasurer` with Skia, matching P4-02's rules.

## Specification

P4-02 (same rules: first installed family of the list, cell = advance of `W` averaged, line height multiplier,
snapping to device pixels with Avalonia's `RenderScaling`, baseline / underline / strikethrough from font metrics).

## Files to create or change

- `src/LoomTerminal.Avalonia/Rendering/SkiaTextMeasurer.cs` — create (internal)

## Design decisions (already made — do not change)

- Family check: `SKFontManager.Default.MatchFamily(name)` returns non-null; default fallbacks in this order:
  `Cascadia Mono`, `Consolas` (Windows), `Menlo` (macOS), `DejaVu Sans Mono`, `monospace`.
- Metrics from `SKFont.Metrics` (`Ascent`, `Descent`, `UnderlinePosition`, `UnderlineThickness`,
  `StrikeoutPosition`, `StrikeoutThickness`; fall back to `size/14` thickness when Skia reports none).

## Tests

- [ ] Manual: log metrics for Cascadia Mono 12 at 100 % / 150 % and compare with P4-02's log (cell width within 1 device pixel).

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(avalonia): measure terminal cells with Skia`
