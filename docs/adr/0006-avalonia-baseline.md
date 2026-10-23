# ADR 0006: Avalonia baseline

- Status: accepted
- Date: 2026-10-23

## Context

Phase 6 adds `LoomTerminal.Avalonia`, drawn with Skia, still on Windows (ConPTY). Avalonia must work on .NET 11 and
with Native AOT (compiled bindings only, no reflection-based XAML loading).

## Decision

- Avalonia **12.1.4** (latest stable on nuget.org on 2026-10-23): `Avalonia`, `Avalonia.Skia`, `Avalonia.Desktop`,
  `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter` (sample only), all pinned to the same version in
  `Directory.Packages.props`. SkiaSharp comes transitively with `Avalonia.Skia`.
- The library targets plain `$(LoomNetTargetFramework)` and references Presentation only.
- The sample sets `AvaloniaUseCompiledBindingsByDefault` and `PublishAot`; CI publishes it for `win-x64`.

## Consequences

- Bumping Avalonia means changing the five versions together and re-running the AOT job.
- Avalonia and SkiaSharp stay forbidden for Core, Pty and Presentation (architecture tests).
