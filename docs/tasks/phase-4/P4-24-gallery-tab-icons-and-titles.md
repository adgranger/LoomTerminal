# P4-24 — Gallery tabs: shell icon, friendly name and shell title

- **Status:** done
- **Depends on:** P4-20, P4-23
- **Size:** S

## Goal

Each Gallery tab shows the icon of its shell's executable and a friendly name ("PowerShell 7"). The title the shell
sets replaces the name only when it says something; the raw executable path that ConPTY reports at startup
("C:\Program Files\WindowsApps\Microsoft.PowerShell_…\pwsh.exe") is never shown as the tab header.

## Sources

| File | What to take |
|---|---|
| `samples/LoomTerminal.Gallery/MainWindow.xaml.cs` | `AddTab`: today `TitleChanged` sets `tab.Header = title` |
| `src/LoomTerminal.Pty/ShellInfo.cs` | `Name` (friendly, English) and `ExecutablePath` (P4-23) |

## Files to create or change

- `samples/LoomTerminal.Gallery/ShellIcons.cs` — create (`internal static class`)
- `samples/LoomTerminal.Gallery/MainWindow.xaml.cs` — `AddTab`, new `TabHeader` method
- `docs/manual-tests.md` — add the checks below

## Design decisions (already made — do not change)

- Friendly name = `ShellInfo.Name` (already English and friendly: "PowerShell 7", "Windows PowerShell", "Git Bash",
  "Command Prompt", "WSL: Ubuntu"). No reading of the exe version resource. Reason: detection already names the shells.
- Icon: `ShellIcons.LoadAsync(string? executablePath)` returns `Task<ImageSource?>`:
  `StorageFile.GetFileFromPathAsync(path)` → `GetThumbnailAsync(ThumbnailMode.SingleItem, 32)` →
  `new BitmapImage()` + `await bitmap.SetSourceAsync(thumbnail)`. Null path or **any** exception → `null` (catch
  `Exception`, comment: the icon is cosmetic; a shell must open without it). Cache the `Task` per path
  (`Dictionary<string, Task<ImageSource?>>`, `StringComparer.OrdinalIgnoreCase`, UI thread only). Reason: WinRT
  projections only — no P/Invoke, no GDI handle to free, Native AOT-safe through CsWinRT; the shell extracts the exe's
  icon resource itself. The path is `ExecutablePath`, so Store aliases are already resolved (P4-23).
- In `AddTab`: `tab.Header = shell.Name`; then `_ = SetIconAsync(tab, shell.ExecutablePath)` which awaits
  `ShellIcons.LoadAsync` on the UI thread and sets `tab.IconSource = new ImageIconSource { ImageSource = icon }` when
  not null. No icon → no `IconSource` (the default tab look).
- Title rule, `private static string TabHeader(ShellInfo shell, string? title)`: return `shell.Name` when `title` is
  null/white space, or when `Path.IsPathFullyQualified(title)` and it ends with `.exe` (ordinal, ignore case);
  otherwise `title`. `TitleChanged` sets `tab.Header = TabHeader(shell, title)` and
  `ToolTipService.SetToolTip(tab, $"{shell.Name} — {title}")` (just `shell.Name` when the title is empty). Reason: the
  console title starts as the exe path (that is what the user saw); a title the shell or the prompt sets on purpose
  (oh-my-posh, `$Host.UI.RawUI.WindowTitle`) is still shown.
- `Exited` keeps `$"{shell.Name} [exited {code}]"`.
- The "+" menu is unchanged (no icons there).
- **If the Store PowerShell shows no icon** in the manual check (thumbnail refused for a file under
  `C:\Program Files\WindowsApps`), do not try another API in this card: write it in the card under a "Notes" heading
  and in the final report; it becomes a follow-up card.

## Tests

- [ ] Manual (AOT gallery, `eng/Test-AotApp.ps1` leaves no window; start
  `samples/LoomTerminal.Gallery/bin/Release/net11.0-windows10.0.26100.0/win-x64/publish/LoomTerminal.Gallery.exe`):
  every shell of the "+" menu opens a tab with its icon and its friendly name; `pwsh` shows "PowerShell 7", not the
  WindowsApps path; `$Host.UI.RawUI.WindowTitle = 'hello'` in pwsh changes the header to "hello"; the tooltip shows
  "PowerShell 7 — hello".

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `rtk proxy pwsh -NoProfile -File eng/Test-AotApp.ps1 samples/LoomTerminal.Gallery` — exit code 0
- [ ] **Manual (human):** the check above, ticked in `docs/manual-tests.md`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

- Icons in the "+" menu, tab colors, a title setting in the control (`TerminalControl` is unchanged).
- Moving the icon or title logic into a library.

## Suggested commit message

`feat(gallery): show the shell icon and a friendly name in each tab`

## Notes

The manual checks (Tests section and the "Manual (human)" criterion) are deferred to the NuGet phase by the user's decision; they are listed in `docs/manual-tests.md` and left unticked.
