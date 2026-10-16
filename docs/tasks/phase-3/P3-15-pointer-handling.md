# P3-15 — Pointer handling: selection gestures vs. mouse reporting

- **Status:** todo
- **Depends on:** P3-14, P3-12
- **Size:** M

## Goal

Turn backend pointer events into selection gestures (click, drag, double click = word, triple click = line, Shift+click
extends, Alt = column mode, drag beyond the edges scrolls) or into mouse reports for applications that requested them.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to take |
|---|---|---|
| `src/browser/services/SelectionService.ts` | 397–758 | `_getMouseBufferCoords`, `_getMouseEventScrollAmount`, `handleMouseDown`, `_handleIncrementalClick`, `_handleSingleClick`, `_handleDoubleClick`, `_handleTripleClick`, `_handleMouseMove`, `_dragScroll`, `_handleMouseUp`, `_fireEventIfSelectionChanged` — logic only, no DOM listeners |
| `src/browser/CoreBrowserTerminal.ts` | 589–700, 780–800 | `sendEvent` (buttons, actions, modifiers → `CoreMouseEvent`) and the `mousedown` rule (`areMouseEventsActive` and not `shouldForceSelection`) |

## Files to create or change

- `src/LoomTerminal.Presentation/Input/PointerInputHandler.cs` — create (internal)
- `src/LoomTerminal.Presentation/Session/TerminalSession.cs` — public `HandlePointer(TerminalPointerEvent e)`
- `tests/LoomTerminal.Presentation.Tests/Input/PointerInputHandlerTests.cs` — create

## Design decisions (already made — do not change)

- The backend supplies `ClickCount` (1, 2, 3) itself (each UI framework detects double clicks its own way).
- Drag scroll: the JS uses a 50 ms interval (`DRAG_SCROLL_INTERVAL`). Here the backend keeps sending `Move` events at
  frame rate while the button is held outside the area; the handler scrolls by `_getMouseEventScrollAmount` per event,
  limited to one step per 50 ms using the session's `TimeProvider`.
- `altClickMovesCursor` (move the shell cursor with arrow keys on Alt+click) is ported (`_handleSingleClick` +
  `moveToCellSequence`: find it in `src/browser/input/MoveToCell.ts` and port it in this card).
- Right button is **not** handled here: P3-16 decides copy/paste on right click.

## Tests

- [ ] New: drag selects; double click selects a word; triple click a line; Shift+click extends; Alt+drag selects a
      column block; with `\e[?1000h` a click sends a VT200 report and does not select; with Shift it selects;
      Alt+click with `AltClickMovesCursor` sends arrow keys.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (`MoveToCell.ts`)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Links (P3-17), right click (P3-16).

## Suggested commit message

`feat(presentation): handle pointer gestures and mouse reporting`
