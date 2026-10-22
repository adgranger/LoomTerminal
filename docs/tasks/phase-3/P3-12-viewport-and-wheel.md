# P3-12 — Viewport scrolling and the mouse wheel

- **Status:** done
- **Depends on:** P3-10
- **Size:** M

## Goal

Scroll the viewport through the scrollback (lines, pages, top, bottom), follow the output when at the bottom, jump to
the bottom on user input, and handle the wheel: report it to applications with mouse tracking, turn it into arrow keys
in the alternate buffer, otherwise scroll.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to take |
|---|---|---|
| `src/common/CoreTerminal.ts` | 186–220 | `scroll*` methods (already in `TerminalCore`, P1-54) |
| `src/browser/CoreBrowserTerminal.ts` | 589–700 | `sendEvent` for `wheel` (mouse tracking) — logic only |
| | 805–850 | wheel without scrollback → arrow keys (`hasScrollback` branch) — logic only |
| `src/common/services/CoreMouseService.ts` | 241–288 | `consumeWheelEvent`, `_applyScrollModifier` (ported in P1-35) |

## Files to create or change

- `src/LoomTerminal.Presentation/Viewport/ViewportController.cs` — create (internal)
- `src/LoomTerminal.Presentation/Input/TerminalPointerEvent.cs` — create (public: `Column`, `Row`, `X`, `Y` (pixels,
  relative to the terminal area), `PointerButton` (`None`, `Left`, `Middle`, `Right`, `WheelUp`, `WheelDown`),
  `PointerAction` (`Down`, `Up`, `Move`, `Wheel`), `Ctrl`, `Alt`, `Shift`, `double WheelDeltaY`,
  `WheelDeltaMode DeltaMode`, `int ClickCount`)
- `src/LoomTerminal.Presentation/Session/TerminalSession.cs` — public `ScrollLines(int)`, `ScrollPages(int)`,
  `ScrollToTop()`, `ScrollToBottom()`, `ScrollToLine(int)`, `HandleWheel(TerminalPointerEvent, double cellHeight)`
- `tests/LoomTerminal.Presentation.Tests/Viewport/ViewportControllerTests.cs` — create

## Design decisions (already made — do not change)

- Wheel decision, in this order: (1) `CoreMouseService.AreMouseEventsActive` and the protocol wants wheel events and
  not Shift → report through `TriggerMouseEvent`; (2) active buffer has no scrollback (alt screen) → send `CSI A` /
  `CSI B` (or `SS3 A/B` with application cursor keys) once per line from `ConsumeWheelEvent`, as xterm.js;
  (3) otherwise scroll the viewport by `ConsumeWheelEvent` lines.
- `ScrollOnUserInput` (option, default true): `SendKey`/`SendText`/`Paste` scroll to the bottom first.
- Following output: if the viewport is at the bottom (`ydisp == ybase`), it stays there; otherwise it stays put
  (`isUserScrolling`), as `BufferService.scroll` does.

## Tests

- [x] New: each wheel branch; `ScrollPages(-1)` moves by `rows - 1`; typing scrolls to the bottom; output while
      scrolled up keeps `ydisp`.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Public API files updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Mouse buttons (P3-15).

## Suggested commit message

`feat(presentation): add viewport scrolling and wheel handling`
