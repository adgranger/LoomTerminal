# ADR 0007: Starting a process under a Unix pty

- Status: accepted
- Date: 2026-10-23

## Context

`LoomTerminal.Pty` only supports Windows (ConPTY). Phase 7 adds Linux and macOS, which need a shell started behind a
pseudo terminal: a new session whose controlling terminal is the pty slave, with the master fd kept by the parent.

- `fork()` from a running .NET process is unsafe: only the calling thread survives in the child and the runtime's locks
  may be held by threads that no longer exist. `forkpty()` therefore cannot be P/Invoked from managed code with
  anything but an immediate `exec` in the child, which managed code cannot guarantee.
- `posix_spawn` cannot make the child a session leader with the pty as controlling terminal on every platform:
  `POSIX_SPAWN_SETSID` needs Linux/glibc 2.26+ or macOS 10.15+, and `TIOCSCTTY` must run in the child.
- Known solutions: a tiny native helper (`vs-pty.net` and `node-pty` use `forkpty` + `execvp` in C, the child does
  nothing else before `exec`), or a spawn helper executable.

## Decision

- **A tiny native shim library**, `libloomterm_pty` (`.so` / `.dylib`), written in C (under 150 lines), exposing
  `int loomterm_spawn(const char* file, char* const argv[], char* const envp[], const char* cwd, int cols, int rows,
  int* masterFd, int* pid)`. It calls `forkpty`; in the child it only does `chdir`, then `execve` (or `_exit(127)` on
  failure).
- Resize, wait and kill are three more one-line exports of the shim (`loomterm_resize`, `loomterm_wait`,
  `loomterm_kill_group`) instead of P/Invokes of `ioctl`, `waitpid` and `kill` in libc. Why (decided in P7-03):
  `ioctl` is variadic, which is not callable through P/Invoke on Apple arm64, `TIOCSWINSZ` has a different value on
  Linux and macOS, and the `"libc"` library name does not resolve the same way on every Linux.
- The shim is built per RID (`linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`) by a script and by CI, and shipped in
  the Pty package under `runtimes/<rid>/native/` (same approach as LoomEditor's Oniguruma build).

## Alternatives considered

- **`forkpty` / `fork` from managed code:** unsafe, see Context.
- **`posix_spawn` only:** no portable way to set the controlling terminal.
- **A spawn helper executable:** one more process per terminal and a protocol to maintain; the shim is simpler and is
  loaded by `[LibraryImport]`, which stays Native AOT friendly.

## Consequences

- The Pty package gains native binaries for four RIDs and a build script; releases need them built before packing.
- Only the shim contains Unix-specific native code; the managed part uses `[LibraryImport]` (no reflection).
- A platform without a built shim gets `PlatformNotSupportedException`, as Linux and macOS do today.
