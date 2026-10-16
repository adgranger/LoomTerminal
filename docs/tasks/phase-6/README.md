# Phase 6 conventions — the Avalonia control (on Windows first)

Phase 6 adds `LoomTerminal.Avalonia`: the same role as `LoomTerminal.WinUI`, drawn with Skia through Avalonia, still
on Windows (ConPTY). It validates the architecture: **if a phase 6 card needs a change in Core or Presentation, the
abstraction leaks** — stop, describe the leak, and propose a Presentation card instead of patching around it.

## Rules

- `LoomTerminal.Avalonia` targets plain `$(LoomNetTargetFramework)` (Avalonia is cross-platform), references
  Presentation only, and the Avalonia packages pinned in `Directory.Packages.props`.
- Native AOT: compiled bindings only (`x:CompileBindings="True"` / `x:DataType`), no reflection-based XAML loading, no
  `ReflectionBinding`. The sample is published with `PublishAot=true` on CI (`win-x64` in this phase).
- Every behavior is the WinUI one: the WinUI card of the same concern is the specification (P4-xx is named in each
  card). Differences are listed in `src/LoomTerminal.Avalonia/README.md`.
- Skia specifics: draw in an `ICustomDrawOperation`, get the `SKCanvas` through `ISkiaSharpApiLeaseFeature`; fonts with
  `SKTypeface` / `SKFont`, fallback with `SKFontManager.Default.MatchCharacter`.
