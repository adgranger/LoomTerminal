# P6-04 — Avalonia control: size, scaling, frames, session events

- **Status:** todo
- **Depends on:** P6-03
- **Size:** M

## Goal

Same as P4-06 and P4-13 for Avalonia: cols/rows from the bounds, `RenderScaling` changes, frames only when needed,
cursor blink timer, focus, session events on the UI thread.

## Specification

P4-06, P4-13, P4-14.

## Files to create or change

- `src/LoomTerminal.Avalonia/Controls/TerminalView.cs` (+ partial files `Layout`, `Focus`)

## Design decisions (already made — do not change)

- Frame requests with `TopLevel.RequestAnimationFrame` (one pending request at a time) and `InvalidateVisual`.
- Blink timer: `DispatcherTimer` re-armed at `CursorBlinkState.NextChange`.
- Session events marshaled with `Dispatcher.UIThread.Post`.
- Scaling: `TopLevel.RenderScaling`, re-measure on `ScalingChanged`.

## Tests

- [ ] Manual: same checks as P4-06 / P4-13 / P4-14, in the Avalonia gallery.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Input (P6-05, P6-06).

## Suggested commit message

`feat(avalonia): size the terminal and schedule frames`
