# P4-21 — Package metadata, symbols and content check

- **Status:** done
- **Depends on:** P4-18
- **Size:** S (MSBuild properties and a PowerShell script)

## Goal

`eng/Pack.ps1` packs the four libraries into `artifacts/packages` with symbols (`.snupkg`), SourceLink, the right
README and the third-party notice, then checks the content of every package and fails when something is missing.

## Sources

| File | What to take |
|---|---|
| `Directory.Build.props` | package metadata already set (authors, MIT expression, URLs, `VersionPrefix` 0.1.0, `VersionSuffix` alpha, SourceLink properties) |
| `src/Directory.Build.props` | the current README packing (root `README.md` for every package) |
| `.github/workflows/ci.yml`, job `pack` | the four `dotnet pack` commands (replaced by the script) |
| Current `LoomTerminal.WinUI.0.1.0-alpha.nupkg` content (verified while writing this card) | `buildTransitive/LoomTerminal.WinUI.targets`, `lib/net11.0-windows10.0.26100/LoomTerminal.WinUI.pri`, `lib/net11.0-windows10.0.26100/LoomTerminal.WinUI/Themes/Generic.xaml`; dependencies Presentation, Win2D 1.4.0, WindowsAppSDK 2.5.1; `repository` element with `commit` |

## Files to create or change

- `src/Directory.Build.props` — "Packaging" group:
  - `<IncludeSymbols>true</IncludeSymbols>` and `<SymbolPackageFormat>snupkg</SymbolPackageFormat>`;
  - README: property `LoomPackageReadme` = `$(MSBuildProjectDirectory)\README.md` when it exists, else
    `$(MSBuildThisFileDirectory)..\README.md`; the existing `None` item uses `$(LoomPackageReadme)` with
    `PackagePath="README.md"`;
  - `<None Include="$(MSBuildThisFileDirectory)../NOTICE.md" Pack="true" PackagePath="/" Visible="false" />`
- `eng/Pack.ps1` — create
- `.github/workflows/ci.yml` — job `pack`: replace the four `dotnet pack` lines by `./eng/Pack.ps1` (`shell: pwsh`);
  the upload step also uploads `*.snupkg` (same folder, nothing to change if it uploads the folder)
- `src/LoomTerminal.WinUI/README.md` — section "Packages": the four ids, the dependency graph, where they are built
  (`artifacts/packages`, not nuget.org), how to pack (`./eng/Pack.ps1`)

## Design decisions (already made — do not change)

- Version stays `0.1.0-alpha` (`VersionPrefix` + `VersionSuffix` in `Directory.Build.props`, one place for the four
  packages). A new local drop changes `VersionSuffix` (`alpha.2`, …) by hand. Reason: one version for the four
  packages, no versioning tool before the first public release.
- Packages go to `artifacts/packages` only (git-ignored); nothing is pushed to nuget.org. Reason: publishing is the
  user's decision (P4-18).
- WinUI keeps its own `src/LoomTerminal.WinUI/README.md`; Core, Pty and Presentation use the root `README.md`.
  Reason: the WinUI README is the usage guide; the three others are rarely consumed alone.
- `NOTICE.md` goes into every package: Core contains code ported from xterm.js (MIT requires the notice).
- No package icon (no artwork exists).
- `eng/Pack.ps1`: `$ErrorActionPreference = 'Stop'`; delete `artifacts/packages/*.nupkg` and `*.snupkg`; for
  `Core, Pty, Presentation, WinUI` run `dotnet pack src/LoomTerminal.<P> -c Release -o artifacts/packages -warnaserror`
  (fail on a non-zero exit code); then check with `System.IO.Compression.ZipFile` and fail with a list of **all**
  problems found:
  - exactly four `.nupkg` and four `.snupkg`, all with the same version, equal to
    `dotnet msbuild src/LoomTerminal.Core -getProperty:PackageVersion`;
  - each `.nupkg`: `README.md`, `NOTICE.md`, `lib/<tfm>/<Id>.dll`, `lib/<tfm>/<Id>.xml`;
  - each `.snupkg`: `lib/<tfm>/<Id>.pdb`, and the pdb bytes contain the UTF-8 text
    `https://raw.githubusercontent.com/adgranger/LoomTerminal/` (the SourceLink map; the SDK's built-in GitHub
    SourceLink writes it);
  - each `.nuspec`: `<license type="expression">MIT</license>`, `<repository … commit="…">` with a non-empty commit;
  - dependencies (by id, version equal to the package version for LoomTerminal ids): Core → none, Pty → none,
    Presentation → Core and Pty, WinUI → Presentation, `Microsoft.WindowsAppSDK`, `Microsoft.Graphics.Win2D`;
  - WinUI only: `buildTransitive/LoomTerminal.WinUI.targets`, `lib/<tfm>/LoomTerminal.WinUI.pri`, and an entry
    ending in `Themes/Generic.xaml` **or** `Themes/Generic.xbf`.
  - On success print `Packed and checked: <version>` and the list of files in `artifacts/packages`.
- Whether the WinUI package works at runtime (XAML resources really found) is P4-22's job, not this card's.

## Tests

- [x] Run `eng/Pack.ps1` once after deleting `NOTICE.md` from the pack item (temporarily): it must fail and name
  `NOTICE.md` for the four packages; revert.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `rtk proxy pwsh -NoProfile -File eng/Pack.ps1` — exit code 0, four `.nupkg` and four `.snupkg` listed
- [x] The negative check above failed as expected and was reverted
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

- Consuming the packages from an app (P4-22).
- Publishing to nuget.org, signing packages, package icon, per-package READMEs for Core/Pty/Presentation.

## Suggested commit message

`build: pack symbols, SourceLink and notices, and check the package content`
