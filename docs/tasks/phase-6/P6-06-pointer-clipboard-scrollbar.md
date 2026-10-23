# P6-06 — Avalonia pointer, clipboard and scrollbar

- **Status:** done
- **Depends on:** P6-04
- **Size:** M

## Goal

Same as P4-10, P4-11 and P4-12 for Avalonia.

## Specification

P4-10, P4-11, P4-12.

## Files to create or change

- `src/LoomTerminal.Avalonia/Controls/TerminalView.Pointer.cs`, `TerminalView.Scrolling.cs` — create
- `src/LoomTerminal.Avalonia/Input/AvaloniaClipboard.cs` — create (`TopLevel.GetTopLevel(this)!.Clipboard`)

## Design decisions (already made — do not change)

- Click count from `PointerPressedEventArgs.ClickCount`. Wheel from `PointerWheelEventArgs.Delta.Y` (1.0 per notch,
  positive = up): send `DeltaMode = Line`, `WheelDeltaY = -Delta.Y × 3`.
- Cursor shapes with `Cursor(StandardCursorType.Ibeam | Hand | Arrow)`.
- Scrollbar: Avalonia `ScrollBar` in the control template, same value rules as P4-12.

## Tests

- [ ] Manual: P4-10 / P4-11 / P4-12 checks in the Avalonia gallery (left to a human).

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(avalonia): pointer input, clipboard and scrollbar`
