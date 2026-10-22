# P4-14 — Live theme and font changes

- **Status:** done
- **Depends on:** P4-12, P4-13
- **Size:** S

## Goal

Changing `Theme`, `FontFamily`, `FontSize`, `LineHeight`, `Padding`, `CursorStyle`, `CursorBlink` on a running control
takes effect immediately (re-measure, resize the session if the cell size changed, redraw), as VIGO does when the app
theme or density changes.

## Files to create or change

- `src/LoomTerminal.WinUI/Controls/TerminalControl.cs` — property changed callbacks
- `src/LoomTerminal.Presentation/Session/TerminalSession.cs` — public `SetTheme(TerminalTheme)` (posts to the parser
  thread: the palette lives with the frame builder), `SetCursorStyle`, `SetCursorBlink`

## Design decisions (already made — do not change)

- Theme changes invalidate the layout cache only if it keys on colors (it does not, P4-03): just redraw.
- Font / line height / padding changes: re-measure (P4-02), recompute cols/rows (P4-06), redraw.
- The control does **not** follow `ActualThemeChanged` itself: the host sets `Theme` (VIGO maps its design tokens).
  The gallery shows how (`ActualThemeChanged` → light/dark theme).

## Tests

- [ ] Manual in the gallery: theme toggle, font size slider, line height slider all apply live without restarting the shell.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Public API files updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(winui): apply theme and font changes live`
