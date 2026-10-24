# P7-03 — `UnixPtyConnection`

- **Status:** done
- **Depends on:** P7-02
- **Size:** M

## Goal

Implement `IPtyConnection` on Linux and macOS over the native shim, with the same semantics as `ConPtyConnection`
(bounded output channel, non-blocking writes, resize, exit code, kill everything on dispose).

## Sources

| File | What to take |
|---|---|
| `src/LoomTerminal.Pty/Windows/ConPtyConnection.cs` | structure, pumps, disposal order, exit handling — same behavior |

## Files to create or change

- `src/LoomTerminal.Pty/Unix/UnixPtyNative.cs` — create (`[LibraryImport("loomterm_pty")]` for `loomterm_spawn`;
  `[LibraryImport("libc")]` for `ioctl`, `waitpid`, `kill`, `close`)
- `src/LoomTerminal.Pty/Unix/UnixPtyConnection.cs` — create (internal, `[SupportedOSPlatform("linux")]`, `[SupportedOSPlatform("macos")]`)
- `src/LoomTerminal.Pty/PseudoTerminal.cs` — dispatch on `OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()`
- `tests/LoomTerminal.Pty.Tests/Unix/UnixPtyConnectionTests.cs` — create (the ConPTY tests, with `/bin/sh -c`)
- `tests/LoomTerminal.Architecture.Tests/PlatformNeutralSourceTests.cs` — also exclude the `Unix/` folder

## Design decisions (already made — do not change)

- Command line: `PtyStartOptions.CommandLine` is split with POSIX shell rules (`sh -c` is **not** used): implement a
  small splitter (quotes, backslash) and document it in `PtyStartOptions`. Executables are looked up on `PATH`.
- Reading: a `FileStream` over the master fd (`new SafeFileHandle(fd, ownsHandle: true)`), dedicated thread, 16 KB
  reads into the bounded channel, as on Windows. `EIO` on read = the child closed the pty → end of output.
- Exit: a thread blocked in `waitpid(pid)`; `Exited` completes with `WEXITSTATUS` (or `128 + signal`).
- Dispose: `kill(-pid, SIGHUP)` (process group), then `SIGKILL` after 2 s if still alive; close the master fd.
- `TERM=xterm-256color`, `COLORTERM=truecolor` defaults as on Windows.

## Tests

- [x] Port the ConPTY tests (output + exit code, input, working directory, environment, resize via `stty size`,
      dispose kills children (`sleep 120 &`), missing program throws `IOException`). They run on Linux/macOS CI only
      (`[SupportedOSPlatform]` + a runtime skip on Windows).

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] Tests green on Linux and macOS CI (P7-05) and still green on Windows
- [x] Public API unchanged (only internal types added)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Shell detection (P7-04).

## Suggested commit message

`feat(pty): add the Unix pseudo terminal connection`
