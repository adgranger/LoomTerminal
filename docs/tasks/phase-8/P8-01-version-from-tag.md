# P8-01 — Package version computed from the git tag

- **Status:** todo
- **Depends on:** P4-21
- **Size:** S (two small PowerShell changes and one script, no product code)

## Goal

`eng/Get-ReleaseVersion.ps1` turns a tag (`v0.1.0-alpha.1`) into a version (`0.1.0-alpha.1`), and `eng/Pack.ps1` and
`eng/Test-Packages.ps1` accept `-Version`, so a release is built with the tag's version without editing any file.
Without `-Version` they behave exactly as today (`0.1.0-alpha` from `Directory.Build.props`).

## Sources

| File | What to take |
|---|---|
| `eng/Pack.ps1` | the four `dotnet pack` calls, `$version` read with `-getProperty:PackageVersion`, the version checks |
| `eng/Test-Packages.ps1` | the call to `Pack.ps1`, the version read from the WinUI `.nupkg` name |
| `Directory.Build.props` | `VersionPrefix` / `VersionSuffix` (stay as they are) |

## Files to create or change

- `eng/Get-ReleaseVersion.ps1` — create
- `eng/Pack.ps1` — add the `-Version` parameter
- `eng/Test-Packages.ps1` — add the `-Version` parameter, forwarded to `Pack.ps1`
- `Directory.Build.props` — one comment above `VersionPrefix`: these two values are the version of local builds; a
  release passes `-p:Version=<tag without v>` (see `docs/tasks/phase-8/README.md`)
- `.github/workflows/ci.yml` — job `pack`: new step "Self-test the release scripts" before "Pack and test the
  packages": `./eng/Get-ReleaseVersion.ps1 -SelfTest` (`shell: pwsh`)

## Design decisions (already made — do not change)

- `Get-ReleaseVersion.ps1`: `param([Parameter(Position = 0)][string] $TagOrVersion, [switch] $GitHubOutput, [switch] $SelfTest)`.
  Accepts the tag with or without the leading `v` (the workflow dispatch input has none; the `v*` trigger guarantees
  it for tags). Reason: one validator for both entry points.
- Accepted form: SemVer 2 without build metadata, numeric parts without leading zeros, prerelease identifiers
  `[0-9A-Za-z-]+` separated by dots, numeric identifiers without leading zeros, no empty identifier. Regex:
  `^v?(?<core>(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*))(?<pre>-(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*))*)?$`.
  Anything else: `throw "'<input>' is not a release version (expected vMAJOR.MINOR.PATCH[-prerelease], e.g. v0.1.0-alpha.1)."`.
  Reason: `+build` metadata is dropped by nuget.org and would make tag and package differ.
- Output: the version without `v` is written to the success stream (one line, nothing else). With `-GitHubOutput`,
  also append `version=<version>` and `prerelease=<true|false>` (`true` when there is a `-` part) to the file named by
  `$env:GITHUB_OUTPUT`; if that variable is empty, throw. Reason: the workflow reads both.
- `-SelfTest` ignores `$TagOrVersion`, checks these cases and prints `Self-test passed: <n> cases`, throws on the
  first mismatch with the case:
  valid → expected: `v0.1.0` → `0.1.0`, `v0.1.0-alpha.1` → `0.1.0-alpha.1`, `1.2.3-rc.1` → `1.2.3-rc.1`,
  `v10.20.30` → `10.20.30`, `v1.0.0-0` → `1.0.0-0`, `v1.0.0-x-y.7` → `1.0.0-x-y.7`;
  invalid: the empty string, `v1`, `v1.2`, `v01.2.3`, `v1.2.3-`, `v1.2.3+build`, `v1.2.3-alpha..1`, `v1.2.3-01`,
  `vfoo`, `v1.2.3.4`, `refs/tags/v1.2.3`. Also check `-GitHubOutput` against a temp file for `v1.2.3`
  (`prerelease=false`) and `v1.2.3-rc.1` (`prerelease=true`).
- `Pack.ps1`: `param([string] $Version)` placed after the comment-based help. When non-empty, every `dotnet pack` and
  the `-getProperty:PackageVersion` call get `-p:Version=$Version`; the existing checks then compare against it, and
  add one: the property read back must equal `$Version`. The global property `Version` overrides `VersionPrefix` +
  `VersionSuffix` (and gives the assembly versions) for every project, so the `LoomTerminal.*` dependency versions in
  the nuspecs follow. Reason: no edit of `Directory.Build.props` at release time, one version for the four packages.
- `Test-Packages.ps1`: `param([string] $Version)`; call `Pack.ps1` with a splatted hashtable that contains `Version`
  only when it is non-empty. The version used afterwards is still read from the WinUI `.nupkg` file name.
- A `-Version` value is validated by `Pack.ps1` only through the read-back check; the strict format check lives in
  `Get-ReleaseVersion.ps1`, which the workflow runs first (P8-06).

## Tests

- [ ] `rtk proxy pwsh -NoProfile -File eng/Get-ReleaseVersion.ps1 -SelfTest` — prints `Self-test passed: <n> cases`
- [ ] `rtk proxy pwsh -NoProfile -File eng/Get-ReleaseVersion.ps1 v0.1.0-alpha.1` prints exactly `0.1.0-alpha.1`
- [ ] `rtk proxy pwsh -NoProfile -File eng/Pack.ps1 -Version 9.9.9-test.1` — exit 0 and the eight files are named
  `LoomTerminal.<Id>.9.9.9-test.1.nupkg|snupkg`
- [ ] Negative check: `rtk proxy dotnet msbuild src/LoomTerminal.Core -getProperty:PackageVersion` (no property) still
  prints `0.1.0-alpha`
- [ ] `rtk proxy pwsh -NoProfile -File eng/Test-Packages.ps1 -Version 0.1.0-alpha.1` — exit 0, prints
  `Package test passed: 0.1.0-alpha.1`
- [ ] `rtk proxy pwsh -NoProfile -File eng/Test-Packages.ps1` (no argument) — exit 0, prints
  `Package test passed: 0.1.0-alpha` (default behavior unchanged)

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] Every command of "Tests" above passes
- [ ] `rtk git status` shows no file under `artifacts/`, no change of `VersionPrefix` / `VersionSuffix`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

- Reading the version from `git describe` (the tag name is given by the workflow; no versioning tool).
- The release workflow (P8-06), CHANGELOG (P8-04), changing the default local version.

## Suggested commit message

`build: compute the package version from the release tag`
