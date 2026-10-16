# P3-08 — Cell color resolution

- **Status:** todo
- **Depends on:** P3-07
- **Size:** M

## Goal

Port the logic that turns a cell's attributes into the final foreground and background colors: default/palette/RGB,
inverse, dim, bold-as-bright, invisible, selection colors, minimum contrast ratio.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `addons/addon-webgl/src/CellColorResolver.ts` | 1–236 | `CellColorResolver.resolve` and its helpers — **without** decoration and `IColorContrastCache` DOM bits |
| `src/browser/ColorContrastCache.ts` | 1–34 | `ColorContrastCache` |
| `src/browser/ColorContrastCache.test.ts` | 1–43 | all |
| `src/browser/renderer/shared/RendererUtils.ts` | 1–95 | `treatGlyphAsBackgroundColor`, `computeNextVariantOffset`, `throwIfFalsy` (only what the resolver uses) |
| `src/browser/renderer/shared/RendererUtils.test.ts` | 1–50 | tests of the ported functions |

## Files to create or change

- `src/LoomTerminal.Presentation/Theming/CellColorResolver.cs` — create (internal)
- `src/LoomTerminal.Presentation/Theming/ColorContrastCache.cs` — create
- `src/LoomTerminal.Presentation/Rendering/RendererUtils.cs` — create
- matching test files under `tests/LoomTerminal.Presentation.Tests/`

## Design decisions (already made — do not change)

- Input: the cell's `Fg`, `Bg`, `Extended` (Core `AttributeData`), whether the cell is selected / under the cursor,
  the `ThemePalette` (P3-07) and options `DrawBoldTextInBrightColors`, `MinimumContrastRatio`. Output:
  `readonly record struct ResolvedCellColors(uint Foreground, uint Background, uint? UnderlineColor)` in RGBA.
- Search and link decorations are **not** in the resolver: P3-19 overlays them in the render model.

## Tests

- [ ] Port `ColorContrastCache.test.ts` and `RendererUtils.test.ts`.
- [ ] New: inverse swaps default colors; `p16:1` + bold → `p16:9` when `DrawBoldTextInBrightColors`; dim halves the
      alpha as xterm.js does (read how and assert it); invisible → foreground equals background; selected cell uses
      `SelectionBackground`; `MinimumContrastRatio = 4.5` raises a dark-on-dark foreground.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(presentation): port the cell color resolver`
