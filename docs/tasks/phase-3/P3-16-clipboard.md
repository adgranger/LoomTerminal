# P3-16 — Clipboard: copy, paste, right click

- **Status:** todo
- **Depends on:** P3-04, P3-05, P3-15
- **Size:** S

## Goal

Wire the copy/paste decisions of P3-04 and the right-click behavior of VIGO (copy the selection, or paste when there is
none — Windows Terminal style) to a backend clipboard.

## Sources

| File | Lines | What to take |
|---|---|---|
| VIGO `tools/editor/src/terminal.js` | 89–120 | copy/paste keys and right click (`D:\VIGO\tools\editor\src\terminal.js`) |
| VIGO `src/Vigo.App/Controls/TerminalView.cs` | 319–345 | clipboard read/write through WinRT (what the backend will implement) |

## Files to create or change

- `src/LoomTerminal.Presentation/Input/IClipboard.cs` — create (public: `Task<string?> GetTextAsync()`, `Task SetTextAsync(string text)`)
- `src/LoomTerminal.Presentation/Session/TerminalSession.cs` — public `IClipboard? Clipboard { get; set; }`;
  `SendKey` executes `Copy` / `Paste` outcomes; public `HandleRightClick()`
- `tests/LoomTerminal.Presentation.Tests/Session/ClipboardTests.cs` — create (with a fake clipboard)

## Design decisions (already made — do not change)

- Copy: `GetSelectionTextAsync` then `Clipboard.SetTextAsync`; the selection is kept (VIGO behavior).
- Paste: `Clipboard.GetTextAsync` then `Paste(text)` (P3-05 encoding). Empty or null text does nothing.
- Right click: copy then clear the selection if there is one; otherwise paste. Option
  `RightClickSelectsWord` (default false on Windows) selects the word under the pointer first, as xterm.js.
- Clipboard calls run on the caller's thread (UI); only the text query and the paste go through the parser thread.

## Tests

- [ ] New: Ctrl+Shift+C copies the selection; Ctrl+V pastes with bracketed paste when the mode is on; right click with
      selection copies and clears it; without selection pastes.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Public API files updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

OSC 52 (clipboard from the application): deferred, see `docs/ROADMAP.md`.

## Suggested commit message

`feat(presentation): add copy, paste and right-click clipboard behavior`
