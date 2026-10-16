# P4-11 — WinRT clipboard

- **Status:** todo
- **Depends on:** P4-10
- **Size:** S

## Goal

Implement `IClipboard` (P3-16) with the WinRT clipboard, as VIGO's `TerminalView` does.

## Sources

| File | What to take |
|---|---|
| VIGO `src/Vigo.App/Controls/TerminalView.cs` | 319–345 (`Clipboard.GetContent`, `DataPackage`, `SetContent`, `Flush`) |

## Files to create or change

- `src/LoomTerminal.WinUI/Input/WinRtClipboard.cs` — create (internal)
- `src/LoomTerminal.WinUI/Controls/TerminalControl.cs` — set `Session.Clipboard` when a session is attached

## Design decisions (already made — do not change)

- `SetTextAsync`: `DataPackage` with `RequestedOperation = Copy`, `SetText`, `Clipboard.SetContent`, `Clipboard.Flush()`
  (text stays after the app closes). `GetTextAsync`: `Clipboard.GetContent()`, `Contains(StandardDataFormats.Text)`,
  `GetTextAsync()`; any COM exception → `null` (clipboard busy) and log.
- Called on the UI thread only.

## Tests

- [ ] Manual: Ctrl+Shift+C / Ctrl+V / Shift+Insert / right click behave as VIGO; multi-line paste into pwsh is
      bracketed (PSReadLine does not run each line).

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(winui): implement the clipboard with WinRT`
