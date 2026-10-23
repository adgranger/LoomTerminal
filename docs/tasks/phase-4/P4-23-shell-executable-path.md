# P4-23 — Real executable of a detected shell (app execution aliases)

- **Status:** done
- **Depends on:** P4-18
- **Size:** S

## Goal

`ShellInfo.ExecutablePath` gives the file that actually runs for each detected shell. For a Microsoft Store app
(PowerShell from the Store), `Shells.Detect()` finds a 0-byte **app execution alias** on the `PATH`
(`%LOCALAPPDATA%\Microsoft\WindowsApps\pwsh.exe`); `ExecutablePath` is the real exe behind it, so a host can read its
icon (P4-24).

## Verified facts (on the author's machine, Windows 11, PowerShell 7.6.7 from the Store)

- `Get-Command pwsh` → `C:\Users\<u>\AppData\Local\Microsoft\WindowsApps\Microsoft.PowerShell_8wekyb3d8bbwe\pwsh.exe`
  (0 bytes). `.NET` `File.ResolveLinkTarget(path, true)` returns **null** for it: .NET does not resolve these.
- `fsutil reparsepoint query` on it: tag `0x8000001b` (`IO_REPARSE_TAG_APPEXECLINK`), data =
  `uint32 version = 3`, then null-terminated UTF-16 strings: package family name
  (`Microsoft.PowerShell_8wekyb3d8bbwe`), app user model id (`Microsoft.PowerShell_8wekyb3d8bbwe!App`), **target exe**
  (`C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.7.0_x64__8wekyb3d8bbwe\pwsh.exe`), then `0`.
- The target exe is readable by the user (`File.OpenRead` works), although its folder cannot be listed.

## Files to create or change

- `src/LoomTerminal.Pty/Windows/AppExecutionAlias.cs` — create (`internal static partial class`, `[SupportedOSPlatform("windows")]`)
- `src/LoomTerminal.Pty/ShellInfo.cs` — add `ExecutablePath`
- `src/LoomTerminal.Pty/Windows/WindowsShellLocator.cs` — set `ExecutablePath` on every shell
- `src/LoomTerminal.Pty/PublicAPI.Unshipped.txt` — via `eng/Update-PublicApi.ps1`
- `tests/LoomTerminal.Pty.Tests/Windows/AppExecutionAliasTests.cs` — create
- `tests/LoomTerminal.Pty.Tests/Windows/WindowsShellLocatorTests.cs` — one test added

## Design decisions (already made — do not change)

- `ShellInfo` gets `public string? ExecutablePath { get; init; }` (not a positional parameter: no break for code that
  builds a `ShellInfo`). XML doc: "The executable file that runs, with Microsoft Store app execution aliases resolved
  to the real file; null when unknown (shells not created by `Shells.Detect`)."
- `CommandLine` is **unchanged** (it keeps the alias). Reason: a Store app started from its target path, without the
  alias, runs without its package identity; the alias is the supported way to start it.
- `AppExecutionAlias.Resolve(string path)`: returns `path` unless `File.GetAttributes(path)` has `ReparsePoint`;
  then opens it with `CreateFileW(path, 0, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, 0, OPEN_EXISTING,
  FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS, 0)` (`SafeFileHandle`), calls
  `DeviceIoControl(handle, FSCTL_GET_REPARSE_POINT = 0x000900A8, …)` into a 16 KiB buffer
  (`MAXIMUM_REPARSE_DATA_BUFFER_SIZE`), and returns `ParseTarget(buffer[..returned]) ?? path`. Any Win32 failure or
  `IOException` / `UnauthorizedAccessException` → `path`. Reason: detection never throws because of an icon nicety.
- `internal static string? ParseTarget(ReadOnlySpan<byte> data)`: `data` is the whole `REPARSE_DATA_BUFFER`:
  `uint32 tag` at 0 must be `0x8000001B`; `uint16 dataLength` at 4; data starts at 8; `uint32 version` at 8 must be 3;
  then UTF-16LE null-terminated strings from offset 12; return the **third** string when it is a fully qualified path,
  else null. Truncated or malformed buffer → null (never throws).
- `[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]` for both
  functions, declared in `AppExecutionAlias.cs` (Pty already has `AllowUnsafeBlocks`). Reason: Native AOT, rule of
  CLAUDE.md; Windows code stays in `Pty/Windows/` (architecture test).
- `WindowsShellLocator` sets `ExecutablePath = AppExecutionAlias.Resolve(<exe>)` for PowerShell 7, and the exe path as
  is for Windows PowerShell, Git Bash (`bash.exe`), Command Prompt and WSL (`wsl.exe`).

## Tests

- [x] New: `Parse_returns_the_target_of_an_app_execution_alias` (buffer built in the test with the layout above and the
  three strings of the verified example plus `"0"`)
- [x] New: `Parse_returns_null_for_another_reparse_tag` (tag `0xA000000C`, symlink)
- [x] New: `Parse_returns_null_for_a_truncated_buffer` (the valid buffer cut in the middle of the third string, and an
  empty span)
- [x] New: `Resolve_returns_a_regular_file_unchanged` (`Environment.ProcessPath`)
- [x] New: `An_installed_alias_resolves_to_a_readable_file`: first `*.exe` in `%LOCALAPPDATA%\Microsoft\WindowsApps`
  with the `ReparsePoint` attribute; `Assert.Skip` when there is none; the result differs from the alias, exists, and
  has a length > 0
- [x] New (`WindowsShellLocatorTests`): `Every_detected_shell_has_an_existing_executable` — `ExecutablePath` not null,
  `File.Exists`, `new FileInfo(path).Length > 0`

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `rtk proxy pwsh -NoProfile -File eng/Update-PublicApi.ps1 src/LoomTerminal.Pty` — run, public API file updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

- Icons (P4-24). Display names from the exe version resource (detected shells already have English names).
- Resolving symbolic links or other reparse tags.

## Suggested commit message

`feat(pty): resolve the real executable of each detected shell`
