# P7-04 — Unix shell detection

- **Status:** in progress
- **Depends on:** P7-03
- **Size:** S

## Goal

`Shells.Detect()` lists the user's shells on Linux and macOS.

## Files to create or change

- `src/LoomTerminal.Pty/Unix/UnixShellLocator.cs` — create
- `src/LoomTerminal.Pty/ShellInfo.cs` — add ids `bash`, `zsh`, `fish`, `sh`, `pwsh` (reuse `PowerShell7`) and a
  `Login` prefix rule (see decisions)
- `src/LoomTerminal.Pty/Shells.cs` — dispatch
- `tests/LoomTerminal.Pty.Tests/Unix/UnixShellLocatorTests.cs` — create

## Design decisions (already made — do not change)

- Order: the user's login shell first (`$SHELL`, else the `pw_shell` of `getpwuid(getuid())` via `[LibraryImport("libc")]`),
  then every other entry of `/etc/shells` that exists, without duplicates (resolve symlinks), names from the file
  name (`zsh` → "zsh").
- Shells start as **login shells** (`-l`), as macOS Terminal and most Linux terminals do: `CommandLine = "<path> -l"`
  (fish: `-l` too; sh: no flag).

## Tests

- [ ] New (Linux/macOS CI): the list is not empty, ids are unique, the first one matches `$SHELL` when it is set.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] Tests green on the three OSes
- [x] Public API files updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`feat(pty): detect shells on Linux and macOS`
