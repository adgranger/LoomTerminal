# P4-09 — IME composition with `CoreTextEditContext`

- **Status:** done
- **Depends on:** P4-08
- **Size:** M

## Goal

Support input method editors (Japanese, Chinese, Korean…): show the composition at the cursor, place the candidate
window next to it, send the committed text.

## Sources

| File | What to take |
|---|---|
| `D:\LoomEditor\docs\adr\0006-editing-and-text-input.md` | decision 11 (CoreTextEditContext, Windows 11 only, fallback), and LoomEditor's IME cards if implemented by then |
| xterm.js `src/browser/input/CompositionHelper.ts` (pinned commit) | what a terminal does with composition (nothing is sent until commit) — logic only |

## Files to create or change

- `src/LoomTerminal.WinUI/Input/ImeTextInput.cs` — create (internal: wraps `CoreTextServicesManager`/`CoreTextEditContext`)
- `src/LoomTerminal.WinUI/Controls/TerminalControl.Keyboard.cs` — create the context on focus, dispose on unfocus
- `src/LoomTerminal.Presentation/Rendering/RenderFrame.cs` / `FrameRenderer.cs` — draw a composition overlay
  (`string? CompositionText` passed by the backend at render time, drawn underlined at the cursor)

## Design decisions (already made — do not change)

- **Windows 11 only** (same constraint as LoomEditor, microsoft-ui-xaml #4239): on Windows 10 the IME is not supported
  (characters typed without IME still work through P4-08). Check `OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)`.
- The edit context holds only the current composition (the terminal is not a text document): `TextRequested` returns
  the composition string, `SelectionRequested` its end; `TextUpdating` updates the overlay; `CompositionCompleted`
  sends the text with `SendText` and clears the overlay.
- `LayoutRequested` returns the cursor cell rectangle in screen coordinates (with DPI) so the candidate window follows
  the cursor.

## Tests

- [ ] Manual (Windows 11, Microsoft IME Japanese): type `nihongo`, convert, commit → `日本語` in the shell; the
      candidate window appears next to the cursor; Escape cancels the composition without sending.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Windows 10 IME support.

## Suggested commit message

`feat(winui): support IME composition through CoreTextEditContext`
