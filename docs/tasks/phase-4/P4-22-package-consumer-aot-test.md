# P4-22 — A throwaway app consumes only the packages and AOT-publishes

- **Status:** done
- **Depends on:** P4-20, P4-21
- **Size:** M (a tiny WinUI app outside the solution, a script, a CI step)

## Goal

`eng/Test-Packages.ps1` packs the libraries (P4-21), builds a minimal WinUI app that references **only** the local
`.nupkg` files (no project reference, no repository MSBuild file), publishes it with Native AOT (0 warnings) and runs
the AOT smoke test (P4-20) on it. This proves the packages carry everything an app needs: dependency graph,
`buildTransitive` PRI workaround, XAML resources (`Generic.xaml` style of `TerminalControl`).

## Sources

| File | What to take |
|---|---|
| `samples/LoomTerminal.Gallery/LoomTerminal.Gallery.csproj` | the app properties (unpackaged, self-contained Windows App SDK, `PublishAot`, `UseOptimizedPublish=false`) |
| `samples/LoomTerminal.Gallery/App.xaml(.cs)`, `app.manifest` | `XamlControlsResources`, the crash log handler, the manifest (copy) |
| `src/LoomTerminal.WinUI/README.md` | the minimal usage: XAML namespace, `TerminalSession.Start`, `Shells.Detect()` |

## Files to create or change

- `eng/package-test/Directory.Build.props` — create: `<Project />` with a comment: stops MSBuild from importing the
  repository's `Directory.Build.props` (the app must build like a stranger's app)
- `eng/package-test/Directory.Packages.props` — create: `<Project />` with the same kind of comment (no central
  package management: versions are in the csproj)
- `eng/package-test/NuGet.config` — create
- `eng/package-test/LoomTerminal.PackageTest/LoomTerminal.PackageTest.csproj` — create
- `eng/package-test/LoomTerminal.PackageTest/app.manifest` — copy of the Gallery's
- `eng/package-test/LoomTerminal.PackageTest/App.xaml(.cs)` — create (copy of the Gallery's, namespace
  `LoomTerminal.PackageTest`, crash log `%TEMP%\LoomTerminal.PackageTest.log`)
- `eng/package-test/LoomTerminal.PackageTest/MainWindow.xaml(.cs)` — create
- `eng/Test-Packages.ps1` — create
- `.github/workflows/ci.yml` — job `pack`: run `./eng/Test-Packages.ps1` (it packs; it replaces `./eng/Pack.ps1`)

## Design decisions (already made — do not change)

- The app is **not** in `LoomTerminal.slnx`: the solution build must not need packed packages. Reason: build order.
- `NuGet.config`: `<clear />`, source `local` = `../../artifacts/packages` (relative to the config file), source
  `nuget.org` = `https://api.nuget.org/v3/index.json`, and `packageSourceMapping`: `LoomTerminal.*` → `local` only,
  `*` → `nuget.org`. Reason: a `LoomTerminal.*` package can never come from nuget.org or a stale cache.
- Restore into an isolated folder: `--packages artifacts/package-test/nuget`, deleted (with the whole
  `artifacts/package-test`) at the start of each run. Reason: the version stays `0.1.0-alpha` between packs; the user's
  global NuGet cache would otherwise serve an old copy.
- csproj (literal values, nothing inherited): `OutputType` WinExe, `TargetFramework`
  `net11.0-windows10.0.26100.0`, `TargetPlatformMinVersion` 10.0.19041.0, `UseWinUI` true, `Nullable` and
  `ImplicitUsings` enable, `ApplicationManifest` app.manifest, `Platforms` x64, `RuntimeIdentifiers` win-x64,
  `WindowsPackageType` None, `WindowsAppSDKSelfContained` true, `PublishAot` true, `UseOptimizedPublish` false (with the
  same comment as the Gallery), `TreatWarningsAsErrors` true. Package references: `LoomTerminal.WinUI`
  `Version="$(LoomTerminalVersion)"` with `<LoomTerminalVersion Condition="'$(LoomTerminalVersion)' == ''">0.1.0-alpha</LoomTerminalVersion>`,
  and `Microsoft.WindowsAppSDK` `2.5.1` (every WinUI app references it directly, as the templates do). **No** `Import`
  of `LoomTerminal.WinUI.targets`: it must come from the package's `buildTransitive`.
- `MainWindow.xaml`: one `<lt:TerminalControl x:Name="Terminal" />` (`xmlns:lt="using:LoomTerminal.WinUI.Controls"`).
  `MainWindow.xaml.cs`: start `TerminalSession.Start(new PtyStartOptions(shell.CommandLine))` with
  `Shells.Detect()` first shell, else `cmd.exe`; dispose it on `Closed`. In `Terminal.Loaded`, if
  `VisualTreeHelper.GetChildrenCount(Terminal) == 0` throw
  `InvalidOperationException("The TerminalControl template was not applied: Generic.xaml is missing from the package.")`.
  Reason: a missing style does not crash by itself; this turns it into a crash log the smoke test sees.
- `eng/Test-Packages.ps1`: `$ErrorActionPreference = 'Stop'`; run `eng/Pack.ps1` (fail if it fails); read the version
  from the `.nupkg` file name of `LoomTerminal.WinUI`; delete `artifacts/package-test`;
  `dotnet publish eng/package-test/LoomTerminal.PackageTest -c Release -r win-x64 -warnaserror --packages artifacts/package-test/nuget -p:LoomTerminalVersion=<version>`
  with the same log scan as P4-20; then `eng/Test-AotApp.ps1 eng/package-test/LoomTerminal.PackageTest -NoPublish`.
  Print `Package test passed: <version>` at the end.
- **If the app builds but the smoke test fails** (for example the template check above, or "Cannot locate resource"),
  do not redesign the packaging in this card: stop and report the script output and the crash log; the fix is a
  follow-up card.

## Tests

- [x] `eng/Test-Packages.ps1` passes.
- [x] Negative check (not committed): comment out the `buildTransitive` `None` item in
  `src/LoomTerminal.WinUI/LoomTerminal.WinUI.csproj`, run the script: `eng/Pack.ps1` must fail on the missing targets
  file; revert.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [x] `rtk proxy pwsh -NoProfile -File eng/Test-Packages.ps1` — exit code 0
- [x] `rtk git status` shows no file under `artifacts/` or `eng/package-test/**/bin|obj` (all git-ignored)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

- An ARM64 run of the package test app.
- Testing the Core/Pty/Presentation packages alone (they are exercised as WinUI's dependencies).

## Suggested commit message

`build: test that an app built from the packages alone publishes with Native AOT`
