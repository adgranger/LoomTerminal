# P4-12 — Scrollbar and scrollback keys

- **Status:** todo
- **Depends on:** P4-06
- **Size:** S

## Goal

Add a vertical XAML `ScrollBar` showing the viewport position in the scrollback, and keyboard scrolling of the
scrollback (Shift+PageUp/PageDown, Ctrl+Shift+Home/End).

## Sources

| File | What to take |
|---|---|
| `D:\LoomEditor\src\LoomEditor.WinUI\Controls\LoomEditor.Scrolling.cs` | 92–189: template part, `IndicatorMode`, value sync without feedback loops |
| VIGO `tools/editor/src/terminal.css` | 22–41: thin scrollbar that grows on hover |

## Files to create or change

- `src/LoomTerminal.WinUI/Themes/Generic.xaml` — add `PART_VerticalScrollBar` (right, `IndicatorMode="MouseIndicator"`)
- `src/LoomTerminal.WinUI/Controls/TerminalControl.Scrolling.cs` — create (partial)

## Design decisions (already made — do not change)

- `Maximum = frame.ScrollbackLength`, `ViewportSize = rows`, `Value = frame.ViewportY`, `SmallChange = 1`,
  `LargeChange = rows - 1`; hidden (`Collapsed`) in the alternate buffer and when there is no scrollback.
- Scrollbar changes call `Session.ScrollToLine`; updates from frames set a `_syncingScrollBar` flag to avoid a loop.
- Keys handled **before** `SendKey`: Shift+PageUp/PageDown → `ScrollPages(∓1)`; Ctrl+Shift+Home/End → top/bottom.
  (Plain PageUp/PageDown go to the application.)

## Tests

- [ ] Manual: `dir /s C:\Windows` then scroll with the bar, wheel and Shift+PageUp; new output while scrolled up does
      not jump; typing jumps to the bottom; the bar disappears in `vim`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Overview ruler / search marks on the scrollbar.

## Suggested commit message

`feat(winui): add the scrollbar and scrollback keys`
