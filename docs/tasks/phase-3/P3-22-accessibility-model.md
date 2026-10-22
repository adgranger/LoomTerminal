# P3-22 — Accessibility text model

- **Status:** done
- **Depends on:** P3-09
- **Size:** S

## Goal

Give backends what a screen reader needs, without UI code: the visible text, the cursor position, the text that was
just printed (live region), as `xterm.js` `AccessibilityManager` computes it.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to take |
|---|---|---|
| `src/browser/AccessibilityManager.ts` | 1–435 | what is announced and when (`_handleChar`, `_handleTab`, `_clearLiveRegion`, `MAX_ROWS_TO_READ`) — logic only |
| `src/common/InputHandler.ts` | `onA11yChar`, `onA11yTab` | events ported in P1-36/P1-37 |

## Files to create or change

- `src/LoomTerminal.Presentation/Rendering/RenderFrame.cs` — add `IReadOnlyList<string> PlainLines` (viewport text,
  `translateToString(true)`), only built when the session has `AccessibilityEnabled = true`
- `src/LoomTerminal.Presentation/Session/TerminalSession.cs` — public `bool AccessibilityEnabled` (sets the
  `ScreenReaderMode` option), event `TextAnnounced(string text)` (batched characters, as xterm.js)
- `tests/LoomTerminal.Presentation.Tests/Session/AccessibilityTests.cs` — create

## Design decisions (already made — do not change)

- Announcements are batched exactly like xterm.js: read `_handleChar`, `_handleTab`, `_clearLiveRegion` and their
  constants in `AccessibilityManager.ts` and reproduce when text is flushed. Raised on the parser thread.

## Tests

- [x] New: with accessibility on, `echo hi\r\n` raises `TextAnnounced("hi")` once; `PlainLines` matches the screen;
      with accessibility off, `PlainLines` is empty and no event fires.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Public API files updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

UI Automation peers (P4-15).

## Suggested commit message

`feat(presentation): add the accessibility text model`
