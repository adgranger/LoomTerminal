# P4-20 — Automated smoke test of a Native AOT app

- **Status:** done
- **Depends on:** P4-18
- **Size:** S (a PowerShell script and a CI step, no product code)

## Goal

One command publishes a WinUI sample with Native AOT, starts the published exe and fails (exit code 1, with the
reason) if the app does not stay alive, shows no window, starts no shell, or writes its crash log. CI uses it, so a
broken AOT build is caught without a human. Later cards (P4-22, P4-24, P4-26) use it in their acceptance criteria.

## Sources

| File | What to take |
|---|---|
| `.github/workflows/ci.yml`, job `aot-smoke-test` | the publish command, the warning scan regex, the "Start the gallery" step (replaced by the script) |
| `samples/LoomTerminal.Gallery/App.xaml.cs` | the crash log: `%TEMP%\LoomTerminal.Gallery.log`, written on `Application.UnhandledException` |

## Files to create or change

- `eng/Test-AotApp.ps1` — create
- `.github/workflows/ci.yml` — job `aot-smoke-test`: replace the step "Start the gallery (x64 only)" by
  `./eng/Test-AotApp.ps1 samples/LoomTerminal.Gallery -NoPublish` (same `if: matrix.rid == 'win-x64'`, `shell: pwsh`)
- `CLAUDE.md` — in "Commands", add under the publish line:
  `rtk proxy pwsh -NoProfile -File eng/Test-AotApp.ps1 samples/LoomTerminal.Gallery   # AOT publish + start + crash check`

## Design decisions (already made — do not change)

- Parameters: `param([Parameter(Mandatory, Position = 0)][string] $Project, [string] $Runtime = 'win-x64',
  [int] $Seconds = 10, [switch] $NoPublish)`. `$Project` is the project **folder**; its leaf name is the assembly name
  (`LoomTerminal.Gallery`). `$ErrorActionPreference = 'Stop'`; `Set-StrictMode -Version Latest`. Reason: one script for
  the Gallery now and the package test app of P4-22.
- Publish (unless `-NoPublish`): `dotnet publish $Project -c Release -r $Runtime -warnaserror 2>&1 | Tee-Object` into
  `artifacts/aot-smoke/<name>-publish.log`; fail if `$LASTEXITCODE -ne 0` or the log matches
  `warning (IL|AOT|CS|CA|NETSDK)\d+` (same regex as CI). Reason: identical gate locally and on CI.
- Exe: the single match of `<Project>/bin/Release/*/<Runtime>/publish/<name>.exe`; fail if none.
- Crash log: `Join-Path ([IO.Path]::GetTempPath()) "<name>.log"`; delete it **before** starting. Reason: an old log
  would fail the run; a new one means this run crashed.
- Run: `Start-Process $exe -PassThru -WorkingDirectory (Split-Path $exe)`, `Start-Sleep -Seconds $Seconds`,
  `$process.Refresh()`, then check, in this order, each failure with its own message:
  1. `-not $process.HasExited` ("exited at startup with code N");
  2. `$process.MainWindowHandle -ne 0` ("no window");
  3. at least one child process: `Get-CimInstance Win32_Process -Filter "ParentProcessId=$($process.Id)"` (the shell
     started behind ConPTY — proves the session started, not only the window);
  4. the crash log does not exist (print its content when it does).
- Clean-up always runs (`try/finally`): stop the child processes found above, then the app (`Stop-Process -Force`,
  errors ignored). Reason: no shell left running on CI or on the developer's machine.
- On success print one line: `AOT smoke test passed: <name> (<Runtime>), alive after <Seconds> s, <n> child process(es).`
- Only `win-x64` is started on CI (ARM64 runners do not run x64 exes and vice versa); ARM64 is still published.
- The script does not fix anything. **If the Gallery fails the test today, stop and report** the script output: the
  fix is a separate card.

## Tests

- [x] Run `rtk proxy pwsh -NoProfile -File eng/Test-AotApp.ps1 samples/LoomTerminal.Gallery` — passes.
- [x] Negative check (not committed): temporarily add `throw new InvalidOperationException("smoke");` at the end of
  `MainWindow`'s constructor, run the script with `-Seconds 5`, see it fail on the exit or crash log check, revert.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `rtk proxy pwsh -NoProfile -File eng/Test-AotApp.ps1 samples/LoomTerminal.Gallery` — exit code 0 (needs the VS
  C++ workload, see ADR 0001 "Known issues")
- [x] The negative check above failed as expected and was reverted (`rtk git status` shows only the files of this card)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

- Fixing an AOT crash found by the script (separate card).
- Screenshots or UI automation of the running app.
- The `LoomTerminal.PtyConsole` CI step (unchanged).

## Suggested commit message

`build: add an automated smoke test of the Native AOT gallery`
