# P3-11 — Cursor blinking and cursor styles

- **Status:** todo
- **Depends on:** P3-09
- **Size:** S

## Goal

Port the cursor blink state machine (blink while idle, solid while typing, stop when unfocused) and compute the
cursor shape from options, focus and DECSCUSR.

## Sources (xterm.js, pinned commit `f447274f430fd22513f6adbf9862d19524471c04`)

| File | Lines | What to port |
|---|---|---|
| `addons/addon-webgl/src/CursorBlinkStateManager.ts` | 1–146 | `CursorBlinkStateManager` (`BLINK_INTERVAL`, `restartBlinkAnimation`, `pause`, `resume`, the animation frame and timeout logic) |

## Files to create or change

- `src/LoomTerminal.Presentation/Rendering/CursorBlinkState.cs` — create (internal)
- `src/LoomTerminal.Presentation/Rendering/RenderFrameBuilder.cs` — use it for `RenderCursor.Visible` and `CursorShape`
- `tests/LoomTerminal.Presentation.Tests/Rendering/CursorBlinkStateTests.cs` — create

## Design decisions (already made — do not change)

- No timer inside (as LoomEditor's `CursorBlinking`): `CursorBlinkState(TimeProvider time)` exposes
  `bool IsVisible(long timestamp)`, `void Restart()` (on input), `void Pause()` (focus lost), `void Resume()`, and
  `TimeSpan? NextChange` (when the UI should schedule its next frame). The JS `requestAnimationFrame` / `setTimeout`
  are replaced by the backend calling `IsVisible` each frame and scheduling a frame at `NextChange`.
- Shape: focused → `CursorStyle` option (`Block`, `Underline`, `Bar`); unfocused → `CursorInactiveStyle`
  (`Outline` (default), `Block`, `Bar`, `Underline`, `None` → not visible).
- `TerminalSession` gets `SetFocus(bool focused)` (public) and restarts the blink on `SendKey` / `SendText` / `Paste`.

## Tests

- [ ] New (with `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` — add it to
      `Directory.Packages.props`, test projects only): visible for 600 ms then hidden (check `BLINK_INTERVAL` in JS),
      `Restart` makes it visible immediately; `Pause` stops blinking; inactive style `None` hides the cursor.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing the cursor (P3-21, P4).

## Suggested commit message

`feat(presentation): port the cursor blink state machine`
