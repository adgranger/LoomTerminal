# P7-05 — Cross-platform CI: tests and Avalonia AOT on Linux and macOS

- **Status:** todo
- **Depends on:** P7-03
- **Size:** S

## Goal

Run the portable test suites on Linux and macOS, and publish the Avalonia gallery with Native AOT there.

## Files to create or change

- `.github/workflows/ci.yml` — job `portable-tests` (matrix `ubuntu-latest`, `macos-latest`): build and test
  `LoomTerminal.Core.Tests`, `LoomTerminal.Presentation.Tests`, `LoomTerminal.Pty.Tests`, `LoomTerminal.Architecture.Tests`
  (download the `native-pty` artifact of the matching RID first); job `avalonia-aot` (matrix `linux-x64`, `osx-arm64`):
  publish `samples/LoomTerminal.AvaloniaGallery` with AOT, fail on IL/AOT warnings
- `LoomTerminal.slnx` — nothing; but add `tests/LoomTerminal.Portable.slnf` (solution filter without the WinUI projects)
  so non-Windows builds skip them

## Design decisions (already made — do not change)

- Windows-only tests are skipped at run time on other OSes (`Assert.SkipUnless(OperatingSystem.IsWindows(), …)` of
  xUnit v3), never excluded by name.
- The WinUI projects and the WinUI gallery are not built on Linux/macOS (the solution filter).

## Tests

- [ ] CI green on the three OSes.

## Acceptance criteria

- [ ] CI green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

—

## Suggested commit message

`ci: run the portable tests and the Avalonia AOT publish on Linux and macOS`
