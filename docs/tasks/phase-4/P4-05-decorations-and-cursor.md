# P4-05 — Underlines, strikethrough, overline and the cursor in Win2D

- **Status:** done
- **Depends on:** P4-03
- **Size:** S

## Goal

Implement the remaining `IRenderSurface` calls: underline styles (single, double, curly, dotted, dashed), lines and
outline rectangles, so `FrameRenderer` decorations and every cursor shape render.

## Files to create or change

- `src/LoomTerminal.WinUI/Rendering/Win2DRenderSurface.cs` — `DrawUnderline`, `DrawLine`, `DrawRectangle`

## Design decisions (already made — do not change)

- Curly: a sine-like path (`CanvasPathBuilder` with cubic curves) whose period is 2 cells' width / 4, phase from the
  variant offset (P3-21) so it continues across runs. Dotted: `CanvasStrokeStyle` with `DashStyle.Dot`; dashed:
  `DashStyle.Dash`; double: two lines separated by one thickness.
- Every line is snapped to device pixels (`Math.Floor(y × scale) / scale + thickness / 2`) to stay crisp.

## Tests

- [ ] Manual: `printf '\e[4:1mA\e[4:2mB\e[4:3mC\e[4:4mD\e[4:5mE\e[0m'` (from WSL or pwsh with `` `e ``) shows the five
      styles; `\e[9m` strikethrough, `\e[53m` overline; DECSCUSR 1–6 change the cursor shape; unfocused window shows the outline cursor.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(winui): draw text decorations and cursor shapes`
