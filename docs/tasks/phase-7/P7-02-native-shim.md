# P7-02 — Native shim `libloomterm_pty` and its build

- **Status:** todo
- **Depends on:** P7-01 (ADR accepted)
- **Size:** S

## Goal

Write the C shim of ADR 0007, its build script, and the CI jobs that build it for the four Unix RIDs.

## Files to create or change

- `src/native/pty/loomterm_pty.c` — create
- `src/native/pty/build.sh` — create (`cc -O2 -fPIC -shared`; on Linux link `-lutil`; on macOS `forkpty` is in libc;
  output `artifacts/native/<rid>/libloomterm_pty.{so,dylib}`)
- `.github/workflows/ci.yml` — job `native-pty` (matrix: `ubuntu-latest` x64, `ubuntu-24.04-arm` arm64,
  `macos-13` x64, `macos-latest` arm64), uploads the libraries as artifacts
- `src/LoomTerminal.Pty/LoomTerminal.Pty.csproj` — pack `artifacts/native/<rid>/*` into `runtimes/<rid>/native/`
  (only when the files exist, so Windows-only builds still work)

## Design decisions (already made — do not change)

- The child process: `setsid` is done by `forkpty`; then `chdir(cwd)` — `_exit(126)` if it fails, so the C# side can
  report an `IOException` with a clear message —, then `execve(file, argv, envp)` and `_exit(127)` if it fails.
  No `malloc`, no stdio in the child.
- Exported with `__attribute__((visibility("default")))`; everything else `static`.

## Tests

- [ ] CI builds the four libraries; a smoke C test (`build.sh --test`) spawns `/bin/echo hi` and reads `hi`.

## Acceptance criteria

- [ ] CI `native-pty` job green on the four RIDs
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The C# side (P7-03).

## Suggested commit message

`feat(pty): add the native Unix pty spawn shim`
