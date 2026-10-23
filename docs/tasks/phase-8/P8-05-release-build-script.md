# P8-05 — One script runs every release gate

- **Status:** done
- **Depends on:** P8-02
- **Size:** S (one PowerShell script)

## Goal

`eng/Invoke-ReleaseBuild.ps1 -Version <v>` runs, in order, restore, Release build (0 warnings), all tests, and
`eng/Test-Packages.ps1 -Version <v>`, and stops at the first failure. The release workflow (P8-06) calls exactly this,
so the same command can be run on a developer machine before tagging.

## Sources

| File | What to take |
|---|---|
| `.github/workflows/ci.yml`, job `build-and-test` | the restore, build and test commands (without coverage) |
| `eng/Test-Packages.ps1`, `eng/Get-ReleaseVersion.ps1` | called by the script |

## Files to create or change

- `eng/Invoke-ReleaseBuild.ps1` — create

## Design decisions (already made — do not change)

- `param([Parameter(Mandatory)][string] $Version)`; `$ErrorActionPreference = 'Stop'`; works from the repository root
  whatever the current directory (`Push-Location (Split-Path -Parent $PSScriptRoot)` in `try/finally`).
- Step 0: `$v = & "$PSScriptRoot/Get-ReleaseVersion.ps1" $Version` (accepts a tag or a version; the result has no `v`).
  Failure here must happen **before** anything is built.
- Steps, each followed by `if ($LASTEXITCODE -ne 0) { throw "<step> failed" }` and a `Write-Host "== <step> =="` banner:
  1. `dotnet restore LoomTerminal.slnx`
  2. `dotnet build LoomTerminal.slnx -c Release --no-restore -p:Version=$v -warnaserror`
  3. `dotnet test --solution LoomTerminal.slnx -c Release --no-build`
  4. `& "$PSScriptRoot/Test-Packages.ps1" -Version $v` (it packs, checks the content, publishes the consumer app with
     Native AOT and starts it)
- Final line: `Release build passed: <v>; packages are in artifacts/packages.` Nothing is published or tagged.
- No `rtk` inside the script (the scripts are also run by the workflow); `rtk` is only used by agents when calling it
  (`rtk proxy pwsh -NoProfile -File eng/Invoke-ReleaseBuild.ps1 -Version …`).
- Coverage collection is **not** part of the release build (CI already does it on every push to `main`).

## Tests

- [x] `rtk proxy pwsh -NoProfile -File eng/Invoke-ReleaseBuild.ps1 -Version 1.2` fails at step 0 in under a few
  seconds, before any `dotnet` call, with the "not a release version" message
- [x] `rtk proxy pwsh -NoProfile -File eng/Invoke-ReleaseBuild.ps1 -Version v0.1.0-alpha.1` — exit 0, prints
  `Release build passed: 0.1.0-alpha.1 …`; the eight files in `artifacts/packages` carry that version
- [x] `rtk git status` shows no tracked file modified by the run (only git-ignored outputs)

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] Both commands of "Tests" behave as written
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

- The workflow itself (P8-06), publishing, tagging, ARM64 packages test.

## Suggested commit message

`build: add a single script that runs every release gate`
