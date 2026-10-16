# P4-13 — Focus, cursor blink timer, title / bell / exit events

- **Status:** todo
- **Depends on:** P4-06
- **Size:** S

## Goal

Drive the cursor blink with a UI timer, report focus to the session (cursor style and DECSET 1004 focus events), and
raise the control's events on the UI thread.

## Sources

| File | What to take |
|---|---|
| `D:\LoomEditor\src\LoomEditor.WinUI\Controls\LoomEditor.Caret.cs` | 43–64: `DispatcherQueueTimer` driven by the blink state |
| xterm.js `src/browser/CoreBrowserTerminal.ts` (pinned) | `_handleFocus` / `_handleBlur`: focus reports `\x1b[I` / `\x1b[O` when `sendFocus` mode is on |

## Files to create or change

- `src/LoomTerminal.WinUI/Controls/TerminalControl.Focus.cs` — create (partial)
- `src/LoomTerminal.Presentation/Session/TerminalSession.cs` — `SetFocus` sends focus reports when DECSET 1004 is on
  (`InputHandler.OnRequestSendFocus` / `DecPrivateModes.SendFocus`)

## Design decisions (already made — do not change)

- One `DispatcherQueueTimer`, re-armed at `CursorBlinkState.NextChange` (P3-11); stopped when unfocused or when
  blinking is off.
- `GotFocus` / `LostFocus` → `Session.SetFocus`. Clicking the control focuses it (`Focus(FocusState.Pointer)`).
- `TitleChanged`, `BellRang`, `Exited` are marshaled with `DispatcherQueue.TryEnqueue`. The bell has no sound or flash
  in the control: the host decides.

## Tests

- [ ] New (Presentation): `SetFocus(true)` with `\e[?1004h` sends `\e[I`; `SetFocus(false)` sends `\e[O`.
- [ ] Manual: cursor blinks only while focused; inactive window shows the outline cursor; `echo -e "\a"` raises `BellRang`.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(winui): drive focus, cursor blinking and session events`
