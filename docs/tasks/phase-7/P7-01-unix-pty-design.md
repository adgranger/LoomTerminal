# P7-01 — Design: starting a process under a Unix pty from .NET

- **Status:** done
- **Depends on:** P6-08
- **Size:** S (ADR only)

## Goal

Decide how `LoomTerminal.Pty` starts a shell under a pseudo terminal on Linux and macOS, and write the ADR.

## Context to write in the ADR

- `fork()` from a running .NET process is unsafe (only the calling thread survives; the runtime's locks may be held),
  so `forkpty()` cannot be P/Invoked directly from managed code with anything run in the child.
- `posix_spawn` cannot make the child a session leader with the pty as controlling terminal on every platform
  (`POSIX_SPAWN_SETSID` is Linux/glibc ≥ 2.26 and macOS 10.15+, and `TIOCSCTTY` must run in the child).
- Known solutions: a tiny native helper (Microsoft's `vs-pty.net` and `node-pty` use `forkpty` + `execvp` in C, the
  child does nothing else before `exec`); or a spawn helper executable.

## Decision to record (already made — write it as the ADR's decision)

- **A tiny native shim library**, `libloomterm_pty` (`.so` / `.dylib`), written in C (< 150 lines), exposing
  `int loomterm_spawn(const char* file, char* const argv[], char* const envp[], const char* cwd, int cols, int rows,
  int* masterFd, int* pid)`: it calls `forkpty`, and in the child only `chdir`, `execve` (or `_exit(127)` on failure).
  Resize and wait are done from C# with `ioctl(TIOCSWINSZ)` and `waitpid` through `[LibraryImport("libc")]`.
- Built per RID (`linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`) by a script and CI, shipped in the Pty package
  under `runtimes/<rid>/native/` (same approach as LoomEditor's Oniguruma build: `src/native/oniguruma/build.ps1` there).

## Files to create or change

- `docs/adr/0007-unix-pty.md` — create (context, decision, alternatives, consequences)

## Tests

—

## Acceptance criteria

- [x] ADR written, reviewed by the user (status `proposed` until then)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Code (P7-02, P7-03).

## Suggested commit message

`docs(adr): how to start a process under a Unix pty`
