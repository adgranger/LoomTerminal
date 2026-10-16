# P4-06 — Size to columns/rows, DPI and device loss, frame scheduling

- **Status:** todo
- **Depends on:** P4-05
- **Size:** M

## Goal

Make the control a real terminal surface: compute columns/rows from its size, resize the session, redraw only when a
frame is pending or the cursor blink changes, and survive DPI changes and Direct3D device loss.

## Sources

| File | What to take |
|---|---|
| `D:\LoomEditor\src\LoomEditor.WinUI\Controls\AnimationFrameScheduler.cs` | `CompositionTarget.Rendering` subscription only while needed |
| `D:\LoomEditor\src\LoomEditor.WinUI\Controls\LoomEditor.Font.cs` | 58–114: `RasterizationScale` watch and measurer re-creation |
| `D:\LoomEditor\src\LoomEditor.WinUI\Controls\LoomEditor.cs` | 220–240: device lost |
| VIGO `tools/editor/src/terminal.js` | 62–75: fit on resize via `requestAnimationFrame`, skip size 0 |

## Files to create or change

- `src/LoomTerminal.WinUI/Controls/TerminalControl.Layout.cs` — create (partial): `SizeChanged`, cols/rows, DPI
- `src/LoomTerminal.WinUI/Controls/FrameScheduler.cs` — create (internal)
- `src/LoomTerminal.WinUI/Controls/TerminalControl.cs` — subscribe to `Session.FrameAvailable` (marshal to UI thread,
  request a frame)

## Design decisions (already made — do not change)

- `cols = floor((ActualWidth − Padding.Left − Padding.Right − scrollbarWidth) / cell.Width)`, same for rows;
  minimum 2×1; `Session.Resize` is called at most once per frame and never with a zero size (hidden panel: VIGO
  keeps the shell, skip).
- A frame is requested when: `FrameAvailable`, size/DPI/font/theme change, focus change, cursor blink `NextChange`
  due, selection/hover change. Otherwise `CompositionTarget.Rendering` is not subscribed (idle = 0 % CPU).
- Device lost (`CanvasControl.CreateResources` with reason `DpiChanged` / `NewDevice`): clear caches, re-measure,
  redraw.

## Tests

- [ ] Manual: resize the gallery window — `mode con` (cmd) reports the new size; no redraw loop when idle (Task
      Manager CPU ≈ 0 %); moving the window between 100 % and 150 % monitors keeps text crisp.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Scrollbar (P4-12).

## Suggested commit message

`feat(winui): size the terminal, schedule frames and handle DPI and device loss`
