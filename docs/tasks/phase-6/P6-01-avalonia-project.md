# P6-01 — `LoomTerminal.Avalonia` project, sample and CI

- **Status:** done
- **Depends on:** P5-05 (or P4-18 if VIGO integration is postponed)
- **Size:** S

## Goal

Create the Avalonia control project with a placeholder control, an Avalonia sample published with Native AOT on CI,
and the architecture rules for the new project.

## Files to create or change

- `Directory.Packages.props` — `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Skia` (and
  `Avalonia.Fonts.Inter` for the sample): the **latest stable** version that supports .NET 11 and Native AOT — check
  nuget.org and the Avalonia docs at the time of the card, pin it exactly, and write the version and the date in
  `docs/adr/0006-avalonia-baseline.md` (create)
- `src/LoomTerminal.Avalonia/LoomTerminal.Avalonia.csproj`, `Controls/TerminalView.cs` (placeholder: draws a text with
  a custom draw operation), `PublicAPI.*.txt` — create
- `samples/LoomTerminal.AvaloniaGallery/` — create (App, MainWindow with the control, `PublishAot=true`)
- `LoomTerminal.slnx` — add both
- `tests/LoomTerminal.Architecture.Tests/DependencyRulesTests.cs` — `["LoomTerminal.Avalonia"] = ["LoomTerminal.Presentation"]`,
  and `UiPackages` keeps "Avalonia" forbidden for Core/Pty/Presentation
- `.github/workflows/ci.yml` — publish the Avalonia sample with AOT (`win-x64`) and start it 10 s, like the WinUI gallery

## Design decisions (already made — do not change)

- The control is named `TerminalView` in `LoomTerminal.Avalonia.Controls` (Avalonia convention), not `TerminalControl`.
- Same public properties and events as the WinUI control (P4-01), with Avalonia property types (`StyledProperty`).

## Tests

- [x] Architecture tests green; the AOT sample starts locally (if the VS C++ workload is present) or on CI.

## Acceptance criteria

- [x] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [x] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] CI AOT job for the Avalonia sample green
- [x] Card status and `docs/tasks/README.md` updated

## Implementation notes

- Avalonia 12.1.4 (ADR 0006). `Theme` is named `TerminalTheme` (`StyledElement.Theme` exists); see the project README.
- The library is `IsPackable=false` until P6-08 (the package job and `eng/Pack.ps1` are untouched).
- Under Native AOT a publish that recompiles drops Avalonia's compiled XAML: the sample is built, then published with
  the no-build option (`eng/Test-AotApp.ps1 -BuildFirst`). The AOT job of CI is `aot-avalonia`.
- The "CI AOT job green" criterion is only checked once the branch is pushed; the same script passes locally.

## Out of scope

Rendering (P6-02 …).

## Suggested commit message

`feat(avalonia): add the Avalonia control project and AOT sample`
