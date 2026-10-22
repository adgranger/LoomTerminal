# P4-10 — Pointer: selection, mouse reporting, wheel, links, cursor shape

- **Status:** done
- **Depends on:** P4-06
- **Size:** M

## Goal

Forward WinUI pointer events to the session as `TerminalPointerEvent`s (P3-12) and update the mouse cursor shape.

## Files to create or change

- `src/LoomTerminal.WinUI/Controls/TerminalControl.Pointer.cs` — create (partial)

## Design decisions (already made — do not change)

- `PointerPressed` / `Moved` / `Released` / `WheelChanged` / `PointerCaptureLost`; capture the pointer on press,
  release on release. Positions are relative to the terminal area (minus `Padding`), converted to cells with the
  current `CellMetrics`.
- Click count: two presses within `UISettings.DoubleClickTime` and 4 DIPs of each other count up to 3, then restart.
- While a button is held outside the area, keep requesting frames and send synthetic `Move` events each frame (drag
  scroll, P3-15).
- Wheel uses the DOM convention, so `ConsumeWheelEvent` (P1-35) works unchanged: from
  `PointerPoint.Properties.MouseWheelDelta` (120 per notch, positive = away from the user), send
  `DeltaMode = Line` and `WheelDeltaY = -MouseWheelDelta / 120.0 × 3` (3 lines per notch, as browsers report on
  Windows). Touchpads send small deltas: `ConsumeWheelEvent` accumulates the fractions, as in JS.
- Right button press → `Session.HandleRightClick()` (P3-16), no context menu.
- Cursor shape (`ProtectedCursor`, set in code): `IBeam` normally, `Hand` over a link while Ctrl is down, `Arrow` when
  mouse reporting is active.
- `KeyDown`/`KeyUp` of Ctrl update the hovered link even without pointer movement.

## Tests

- [ ] Manual: select by drag / double / triple click; Alt+drag column selection; wheel scrolls the scrollback in pwsh
      and scrolls inside `less`/`vim` (alt screen, arrow keys); `htop` receives clicks (mouse reporting); Ctrl+hover
      underlines an https link and Ctrl+click raises `LinkActivated`.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Clipboard implementation (P4-11).

## Suggested commit message

`feat(winui): forward pointer input to the session`
